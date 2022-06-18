
Imports System.IO
Imports DLTFImageClassification.Score.Common
Imports DLTFImageClassification.Score.DLTFImageClassification.ModelScorer

Namespace DLTFImageClassification

    Public Class Program

        Const tensorflowInceptionGraphZip As String = "inception5h.zip"
        Const tensorflowInceptionGraphUrl As String =
            "https://storage.googleapis.com/download.tensorflow.org/models/" & tensorflowInceptionGraphZip
        ' https://bit.ly/3qpM6uI alternate url

        Const imagesDatasetZip As String = "ImagesClassification.zip"
        Const imagesDatasetUrl As String = "https://bit.ly/3qmkaYo"

        Shared Sub Main(args() As String)

            Dim assetsRelativePath As String = "../../../assets"
            Dim assetsPath As String = GetAbsolutePath(assetsRelativePath)

            Dim commonDatasetsRelativePath As String = "../../../../../../../../datasets"
            Dim commonDatasetsPath As String = GetAbsolutePath(commonDatasetsRelativePath)
            Dim commonGraphsRelativePath As String = "../../../../../../../../graphs"
            Dim commonGraphsPath As String = GetAbsolutePath(commonGraphsRelativePath)

            GetResult(assetsPath, commonDatasetsPath, commonGraphsPath)

            ConsoleHelpers.ConsolePressAnyKey()

        End Sub

        Public Shared Function GetResult(myAssetsPath As String,
                myCommonDatasetsPath As String, myCommonGraphsPath As String) As Boolean

            Dim tagsTsv = Path.Combine(myAssetsPath, "inputs", "images", "tags.tsv")
            Dim imagesFolder = Path.Combine(myAssetsPath, "inputs", "images")
            Dim inceptionFolder = Path.Combine(myAssetsPath, "inputs", "inception")
            Dim inceptionPb = Path.Combine(inceptionFolder, "tensorflow_inception_graph.pb")
            Dim inceptionZipPath = Path.Combine(myAssetsPath, "inputs", "inception", tensorflowInceptionGraphZip)
            Dim commonInceptionZipPath = Path.Combine(myCommonGraphsPath, tensorflowInceptionGraphZip)
            Dim labelsTxt = Path.Combine(myAssetsPath, "inputs", "inception",
                "imagenet_comp_graph_label_strings.txt")

            ' STEP 1: Download inception graph
            'DownloadInceptionGraph(inceptionPb, inceptionZipPath, tensorflowInceptionGraphUrl,
            'commonInceptionZipPath)
            Dim destFiles As New List(Of String) From {inceptionPb}
            DownloadBigFile(inceptionFolder, tensorflowInceptionGraphUrl,
                tensorflowInceptionGraphZip, myCommonGraphsPath, destFiles)

            ' STEP 2: Download images
            'DownloadImages(imagesFolder, imagesDatasetUrl, imagesDatasetZip, commonDatasetsPath)
            Dim image1 = Path.Combine(imagesFolder, "broccoli.jpg")
            Dim destImgFiles As New List(Of String) From {image1}
            DownloadBigFile(imagesFolder, imagesDatasetUrl, imagesDatasetZip,
                myCommonDatasetsPath, destImgFiles)

            If Not checkImages(imagesFolder) Then
                Console.WriteLine("====Extracting: Fail!====")
                Environment.Exit(0)
            End If

            Try
                Dim modelScorer = New TFModelScorer(tagsTsv, imagesFolder, inceptionPb, labelsTxt)
                Dim success As Boolean
                modelScorer.Score(success)
                Console.WriteLine("Success: " & success)
                Return success

            Catch ex As Exception
                ConsoleHelpers.ConsoleWriteException(ex.ToString())
                Return False
            End Try

        End Function

        'Public Shared Sub DownloadInceptionGraph(inceptionPb As String, inceptionZipPath As String,
        '        inceptionZipUrl As String, commonAssetsPath As String)

        '    If Not File.Exists(inceptionZipPath) AndAlso File.Exists(commonAssetsPath) Then
        '        IO.File.Copy(commonAssetsPath, inceptionZipPath)
        '    ElseIf File.Exists(inceptionZipPath) AndAlso Not File.Exists(commonAssetsPath) Then
        '        IO.File.Copy(inceptionZipPath, commonAssetsPath)
        '    End If

        '    If File.Exists(inceptionPb) Then Exit Sub

        '    If Not File.Exists(inceptionZipPath) Then
        '        Console.WriteLine("====Downloading zip====")
        '        Using client = New WebClient
        '            ' The code below will download a dataset from a third-party,
        '            '  and may be governed by separate third-party terms
        '            ' By proceeding, you agree to those separate terms
        '            client.DownloadFile(inceptionZipUrl, inceptionZipPath)
        '        End Using
        '        Console.WriteLine("====Downloading is completed====")
        '        IO.File.Copy(inceptionZipPath, commonAssetsPath)
        '    End If

        '    If File.Exists(inceptionZipPath) Then
        '        Console.WriteLine("====Extracting zip====")
        '        Dim myFastZip As New FastZip()
        '        Dim actualDirectory = Path.GetDirectoryName(inceptionZipPath)
        '        myFastZip.ExtractZip(inceptionZipPath, actualDirectory, fileFilter:=String.Empty)
        '        Console.WriteLine("====Extracting is completed====")
        '        If File.Exists(inceptionPb) Then
        '            Exit Sub
        '        Else
        '            Console.WriteLine("====Extracting: Fail!====")
        '            Stop
        '        End If
        '    End If

        'End Sub

        'Public Shared Sub DownloadImages(imagesFolder As String,
        '        imagesDatasetUrl As String, imagesDatasetZip As String, commonAssetsPath As String)

        '    Dim zipPath = Path.Combine(imagesFolder, imagesDatasetZip)
        '    Dim commonPath = Path.Combine(commonAssetsPath, imagesDatasetZip)

        '    If Not File.Exists(zipPath) AndAlso File.Exists(commonPath) Then
        '        IO.File.Copy(commonPath, zipPath)
        '    ElseIf File.Exists(zipPath) AndAlso Not File.Exists(commonPath) Then
        '        IO.File.Copy(zipPath, commonPath)
        '    End If

        '    If checkImages(imagesFolder) Then Exit Sub

        '    If Not File.Exists(zipPath) Then
        '        Console.WriteLine("====Downloading zip (images)====")
        '        Using client = New MyWebClient
        '            ' The code below will download a dataset from a third-party,
        '            '  and may be governed by separate third-party terms
        '            ' By proceeding, you agree to those separate terms
        '            client.DownloadFile(imagesDatasetUrl, zipPath)
        '        End Using
        '        Console.WriteLine("====Downloading is completed (images)====")
        '        IO.File.Copy(zipPath, commonPath)
        '    End If

        '    If File.Exists(zipPath) Then
        '        Console.WriteLine("====Extracting zip (images)====")
        '        Dim myFastZip As New FastZip()
        '        myFastZip.ExtractZip(zipPath, imagesFolder, fileFilter:=String.Empty)
        '        Console.WriteLine("====Extracting is completed (images)====")
        '        If checkImages(imagesFolder) Then Exit Sub
        '        Console.WriteLine("====Extracting: Fail! (images)====")
        '        Stop
        '    End If

        'End Sub

        Public Shared Function checkImages(imagesFolder As String) As Boolean

            Dim images As New List(Of String) From {
                "broccoli.jpg", "broccoli.png",
                "canoe2.jpg", "canoe3.jpg", "canoe4.jpg",
                "coffeepot.jpg", "coffeepot2.jpg", "coffeepot3.jpg", "coffeepot4.jpg",
                "pizza.jpg", "pizza2.jpg", "pizza3.jpg",
                "teddy1.jpg", "teddy2.jpg", "teddy3.jpg", "teddy4.jpg", "teddy6.jpg",
                "toaster.jpg", "toaster2.png", "toaster3.jpg"}
            Dim imagesOk = True
            For Each image In images
                Dim imagePath = Path.Combine(imagesFolder, image)
                If Not File.Exists(imagePath) Then
                    Debug.WriteLine("Image not found: " & image)
                    imagesOk = False
                    Exit For
                End If
            Next
            Return imagesOk

        End Function

        Public Shared Function GetAbsolutePath(relativePath As String) As String
            Dim fullPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
            Return fullPath
        End Function

    End Class

End Namespace