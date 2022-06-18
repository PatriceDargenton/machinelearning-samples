
Imports System.IO
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports Microsoft.ML
Imports ObjectDetection.ObjectDetection.DataStructures
Imports ObjectDetection.ObjectDetection.YoloParser
Imports ObjectDetection.Common ' MyWebClient

Namespace ObjectDetection

    Public Class Program

        Const imagesDataset As String = "ObjectDetectionPhotosSet"
        Const imagesDatasetZip As String = imagesDataset & ".zip"
        Const imagesDatasetUrl As String = "https://bit.ly/34M7MbT"

        Const yoloGraphOnnx As String = "TinyYolo2_model.onnx"
        Const yoloGraphUrl As String = "https://bit.ly/3rdrfKe"

        Shared Sub Main()

            Dim assetsRelativePath = "../../../assets"
            Dim assetsPath As String = GetAbsolutePath(assetsRelativePath)

            Dim commonDatasetsRelativePath As String = "../../../../../../../../datasets"
            Dim commonDatasetsPath As String = GetAbsolutePath(commonDatasetsRelativePath)

            Dim commonGraphsRelativePath As String = "../../../../../../../../graphs"
            Dim commonGraphsPath As String = GetAbsolutePath(commonGraphsRelativePath)

            Try

                GetResult(assetsPath, commonDatasetsPath, commonGraphsPath)

            Catch ex As Exception
                Console.WriteLine(ex.ToString())
            End Try

            Console.WriteLine("========= End of Process. Hit any Key ========")
            Console.ReadLine()

        End Sub

        Public Shared Function GetResult(
                myAssetsPath As String, myCommonDatasetsPath As String,
                myCommonGraphsPath As String) As Boolean

            Dim modelFilePath = Path.Combine(myAssetsPath, "Model", yoloGraphOnnx)
            Dim imagesFolder = Path.Combine(myAssetsPath, "images")
            Dim outputFolder = Path.Combine(myAssetsPath, "images", "output")
            Dim imagePath1 = Path.Combine(imagesFolder, "image1.jpg")
            Dim imagePath2 = Path.Combine(imagesFolder, "image2.jpg")
            Dim imagePath3 = Path.Combine(imagesFolder, "image3.jpg")
            Dim imagePath4 = Path.Combine(imagesFolder, "image4.jpg")

            Dim destFiles As New List(Of String) From
                {imagePath1, imagePath2, imagePath3, imagePath4}
            DownloadBigFile(imagesFolder, imagesDatasetUrl, imagesDatasetZip,
                myCommonDatasetsPath, destFiles)

            ' Initialize MLContext
            Dim mlContext As New MLContext

            ' Load Data
            Dim images As IEnumerable(Of ImageNetData) = ImageNetData.ReadFromFile(imagesFolder)
            Dim imageDataView As IDataView = mlContext.Data.LoadFromEnumerable(images)

            'DownloadGraph(modelFilePath, yoloGraphUrl, commonYoloGraphPath)
            Dim fullYoloFolderPath As String = Path.Combine(myAssetsPath, "Model")
            DownloadBigFile(fullYoloFolderPath, yoloGraphUrl, yoloGraphOnnx, myCommonGraphsPath)

            ' Create instance of model scorer
            Dim modelScorer = New OnnxModelScorer(imagesFolder, modelFilePath, mlContext)

            ' Use model to score data
            Dim probabilities As IEnumerable(Of Single()) = modelScorer.Score(imageDataView)

            ' Post-process model output
            Dim parser As YoloOutputParser = New YoloOutputParser

            Dim boundingBoxes = probabilities.
                Select(Function(probability) parser.ParseOutputs(probability)).
                Select(Function(boxes) parser.FilterBoundingBoxes(boxes, 5, 0.5F))

            ' Draw bounding boxes for detected objects in each of the images
            Dim nbImages% = images.Count()
            Dim nbImagesBox% = boundingBoxes.Count()
            Dim nbSuccess% = 0
            For i = 0 To nbImages - 1
                Dim imageFileName As String = images.ElementAt(i).Label
                Dim detectedObjects As IList(Of YoloBoundingBox) = boundingBoxes.ElementAt(i)

                DrawBoundingBox(imagesFolder, outputFolder, imageFileName, detectedObjects)

                LogDetectedObjects(imageFileName, detectedObjects, nbSuccess)
            Next i
            Console.WriteLine("Nb success:" & nbSuccess & "/5")
            Dim success = nbSuccess >= 4

            Return success

        End Function

        'Public Shared Sub DownloadGraph(graphPath As String, graphUrl As String,
        '        commonGraphsPath As String)

        '    If Not File.Exists(graphPath) AndAlso File.Exists(commonGraphsPath) Then
        '        IO.File.Copy(commonGraphsPath, graphPath)
        '    ElseIf File.Exists(graphPath) AndAlso Not File.Exists(commonGraphsPath) Then
        '        IO.File.Copy(graphPath, commonGraphsPath)
        '    End If

        '    If File.Exists(graphPath) Then Exit Sub

        '    Console.WriteLine("====Downloading graph====")
        '    Using client = New MyWebClient
        '        ' The code below will download a dataset from a third-party,
        '        '  and may be governed by separate third-party terms
        '        ' By proceeding, you agree to those separate terms
        '        client.DownloadFile(graphUrl, graphPath)
        '    End Using
        '    Console.WriteLine("====Downloading is completed====")
        '    IO.File.Copy(graphPath, commonGraphsPath)

        'End Sub

        Public Shared Function GetAbsolutePath(relativePath As String) As String
            Dim fullPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
            Return fullPath
        End Function

        Private Shared Sub DrawBoundingBox(inputImageLocation As String, outputImageLocation As String,
                imageName As String, filteredBoundingBoxes As IList(Of YoloBoundingBox))

            Dim image As Image = Image.FromFile(Path.Combine(inputImageLocation, imageName))

            Dim originalImageHeight = image.Height
            Dim originalImageWidth = image.Width

            For Each box In filteredBoundingBoxes
                ' Get Bounding Box Dimensions
                Dim x = CUInt(Math.Truncate(Math.Max(box.Dimensions.X, 0)))
                Dim y = CUInt(Math.Truncate(Math.Max(box.Dimensions.Y, 0)))
                Dim width = CUInt(Math.Min(originalImageWidth - x, box.Dimensions.Width))
                Dim height = CUInt(Math.Min(originalImageHeight - y, box.Dimensions.Height))

                ' Resize To Image
                x = CUInt(CUInt(originalImageWidth) * x \ OnnxModelScorer.ImageNetSettings.imageWidth)
                y = CUInt(CUInt(originalImageHeight) * y \ OnnxModelScorer.ImageNetSettings.imageHeight)
                width = CUInt(CUInt(originalImageWidth) * width \ OnnxModelScorer.ImageNetSettings.imageWidth)
                height = CUInt(CUInt(originalImageHeight) * height \ OnnxModelScorer.ImageNetSettings.imageHeight)

                ' Bounding Box Text
                Dim text As String = $"{box.Label} ({(box.Confidence * 100).ToString("0")}%)"

                Using thumbnailGraphic As Graphics = Graphics.FromImage(image)
                    thumbnailGraphic.CompositingQuality = CompositingQuality.HighQuality
                    thumbnailGraphic.SmoothingMode = SmoothingMode.HighQuality
                    thumbnailGraphic.InterpolationMode = InterpolationMode.HighQualityBicubic

                    ' Define Text Options
                    Dim drawFont As New Font("Arial", 12, FontStyle.Bold)
                    Dim size As SizeF = thumbnailGraphic.MeasureString(text, drawFont)
                    Dim fontBrush As New SolidBrush(Color.Black)
                    Dim atPoint As New Point(CInt(x), CInt(y) - CInt(size.Height) - 1)

                    ' Define BoundingBox options
                    Dim pen As New Pen(box.BoxColor, 3.2F)
                    Dim colorBrush As New SolidBrush(box.BoxColor)

                    ' Draw text on image 
                    thumbnailGraphic.FillRectangle(colorBrush,
                        CInt(x), CInt(y - size.Height - 1), CInt(size.Width), CInt(size.Height))
                    thumbnailGraphic.DrawString(text, drawFont, fontBrush, atPoint)

                    ' Draw bounding box on image
                    thumbnailGraphic.DrawRectangle(pen, x, y, width, height)
                End Using
            Next box

            If Not Directory.Exists(outputImageLocation) Then
                Directory.CreateDirectory(outputImageLocation)
            End If

            image.Save(Path.Combine(outputImageLocation, imageName))

        End Sub

        Private Shared Sub LogDetectedObjects(imageName As String,
                boundingBoxes As IList(Of YoloBoundingBox), ByRef nbSuccess%)

            Console.WriteLine($".....The objects in the image {imageName} are detected as below....")

            For Each box In boundingBoxes
                Console.WriteLine($"{box.Label} and its Confidence score: {box.Confidence}")

                Select Case imageName
                    Case "image1.jpg" : If box.Label = "car" AndAlso box.Confidence > 0.95 Then nbSuccess += 1
                    ' Note: 2 cats detected for image2 whereas the first is a dog!
                    Case "image2.jpg" : If box.Label = "cat" AndAlso box.Rect.X > 240 AndAlso
                        box.Confidence > 0.6 Then nbSuccess += 1
                    Case "image2.jpg" : If box.Label = "dog" AndAlso box.Rect.X > 40 AndAlso
                        box.Confidence > 0.6 Then nbSuccess += 1
                    Case "image3.jpg" : If box.Label = "chair" AndAlso box.Confidence > 0.8 Then nbSuccess += 1
                    Case "image4.jpg" : If box.Label = "dog" AndAlso box.Confidence > 0.75 Then nbSuccess += 1
                End Select

            Next box

            Console.WriteLine("")

        End Sub

    End Class

End Namespace