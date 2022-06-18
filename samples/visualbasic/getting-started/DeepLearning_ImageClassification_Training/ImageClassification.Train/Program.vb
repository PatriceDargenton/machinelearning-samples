
Imports System.IO
Imports Microsoft.ML
Imports Microsoft.ML.DataOperationsCatalog
Imports Microsoft.ML.Data
Imports ImageClassification.Train.ImageClassification.DataModels
Imports ICSharpCode.SharpZipLib.Zip
Imports Microsoft.ML.Transforms.ValueToKeyMappingEstimator
Imports ICSharpCode.SharpZipLib.GZip
Imports ICSharpCode.SharpZipLib.Tar
Imports ImageClassification.Train.Common ' MyWebClient

Namespace ImageClassificationTrain

    Public Class Program

        ' SINGLE SMALL FLOWERS IMAGESET (200 files)
        Const imagesDataset As String = "flower_photos_small_set"
        Const imagesDatasetZip As String = imagesDataset & ".zip"
        Const imagesDatasetUrl As String = "https://bit.ly/3fkRKYy"

        ' https://bit.ly/3HZmnz1 à tester
        ' SINGLE FULL FLOWERS IMAGESET (3,600 files)
        'Const imagesDataset As String = "flower_photos"
        'Const imagesDatasetZip As String = imagesDataset & ".tgz"
        'Const imagesDatasetUrl As String = "http://download.tensorflow.org/example_images/" & imagesDatasetZip

        Shared Sub Main() ' args() As String

            Dim assetsRelativePath As String = "../../../assets"
            Dim assetsPath As String = GetAbsolutePath(assetsRelativePath)

            Dim commonDatasetsRelativePath As String = "../../../../../../../../datasets"
            Dim commonDatasetsPath As String = GetAbsolutePath(commonDatasetsRelativePath)

            GetResult(assetsPath, commonDatasetsPath)

            Console.WriteLine("Press any key to finish")
            Console.ReadKey()

        End Sub

        Public Shared Function GetResult(
                myAssetsPath As String, myCommonDatasetsPath As String) As Boolean

            Dim outputMlNetModelFilePath = Path.Combine(myAssetsPath, "outputs", "imageClassifier.zip")
            Dim imagesForPredictions As String = Path.Combine(
                myAssetsPath, "inputs", "images-For-predictions", "FlowersForPredictions")

            Dim imagesFolder = Path.Combine(myAssetsPath, "inputs", "images")

            ' 1. Download the image set and unzip
            'DownloadImageSet(imagesFolder, imagesDatasetUrl, imagesDatasetZip, myCommonDatasetsPath)
            Dim fullImagesetFolderPath As String = Path.Combine(
                imagesFolder, imagesDataset)
            Dim image1 = Path.Combine(fullImagesetFolderPath, "daisy\286875003_f7c0e1882d.jpg")
            Dim destFiles As New List(Of String) From {image1}
            DownloadBigFile(imagesFolder, imagesDatasetUrl, imagesDatasetZip, myCommonDatasetsPath,
                destFiles, fullImagesetFolderPath)

            Dim mlContext As New MLContext() ' seed:=1

            ' Specify MLContext Filter to only show feedback log/traces about ImageClassification
            ' This Is Not needed for feedback output if using the explicit MetricsCallback parameter
            AddHandler mlContext.Log, AddressOf FilterMLContextLog ' C#: mlContext.Log += FilterMLContextLog()

            ' 2. Load the initial full image-set into an IDataView and shuffle so it'll be better balanced
            Dim images As IEnumerable(Of ImageData) = LoadImagesFromDirectory(
                fullImagesetFolderPath, useFolderNameasLabel:=True)
            Dim fullImagesDataset = mlContext.Data.LoadFromEnumerable(images)
            Dim shuffledFullImageFilePathsDataset = mlContext.Data.ShuffleRows(fullImagesDataset)

            ' 3. Load Images with in-memory type within the IDataView And Transform Labels to Keys (Categorical)
            Dim shuffledFullImagesDataset = mlContext.Transforms.Conversion.
                MapValueToKey(
                    outputColumnName:="LabelAsKey",
                    inputColumnName:="Label",
                    keyOrdinality:=KeyOrdinality.ByValue).
                Append(mlContext.Transforms.LoadRawImageBytes(
                    outputColumnName:="Image",
                    imageFolder:=fullImagesetFolderPath,
                    inputColumnName:="ImagePath")).
                Fit(shuffledFullImageFilePathsDataset).
                Transform(shuffledFullImageFilePathsDataset)

            ' 4. Split the data 80:20 into train and test sets, train and evaluate
            Dim trainTestData = mlContext.Data.TrainTestSplit(shuffledFullImagesDataset, testFraction:=0.2)
            Dim trainDataView = trainTestData.TrainSet
            Dim testDataView = trainTestData.TestSet

            ' 5. Define the model's training pipeline using DNN default values
            Dim pipeline = mlContext.MulticlassClassification.Trainers.
                ImageClassification(
                    featureColumnName:="Image",
                    labelColumnName:="LabelAsKey",
                    validationSet:=testDataView).
                Append(mlContext.Transforms.Conversion.MapKeyToValue(
                    outputColumnName:="PredictedLabel",
                    inputColumnName:="PredictedLabel"))

            ' 5.1 (OPTIONAL) Define the model's training pipeline by using explicit hyper-parameters
            ' Arch: Just by changing/selecting InceptionV3/MobilenetV2/ResnetV250
            '  you can try a different DNN architecture (TensorFlow pre-trained model)
            'Dim options As New ImageClassificationTrainer.Options() With {
            '    .FeatureColumnName = "Image",
            '    .LabelColumnName = "LabelAsKey",
            '    .Arch = ImageClassificationTrainer.Architecture.MobilenetV2,
            '    .Epoch = 50, ' 100
            '    .BatchSize = 10,
            '    .LearningRate = 0.01F,
            '    .MetricsCallback = Sub(metrics) Console.WriteLine(metrics),
            '    .ValidationSet = testDataView
            '}

            ' 6. Train/create the ML model
            Console.WriteLine("*** Training the image classification model with DNN Transfer Learning on top of the selected pre-trained model/architecture ***")

            ' Measuring training time
            Dim watch = Stopwatch.StartNew()

            ' Train
            ' System.EntryPointNotFoundException HResult=0x80131523
            ' Unable to find an entry point named 'TF_StringEncodedSize' in DLL 'tensorflow':
            ' Max compatible version for SciSharp.TensorFlow.Redist: 2.3.1 < 2.7.0
            Dim trainedModel = pipeline.Fit(trainDataView)

            watch.Stop()
            Dim elapsedMs = watch.ElapsedMilliseconds

            Console.WriteLine($"Training with transfer learning took: {elapsedMs / 1000} seconds")

            ' 7. Get the quality metrics (accuracy, etc.)
            Dim score# = 0
            EvaluateModel(mlContext, testDataView, trainedModel, score)

            ' 8. Save the model to assets/outputs (You get ML.NET .zip model file and TensorFlow .pb model file)
            Dim directoryPath = Path.GetDirectoryName(outputMlNetModelFilePath)
            If Not Directory.Exists(directoryPath) Then
                Dim di As New DirectoryInfo(directoryPath)
                di.Create()
            End If
            mlContext.Model.Save(trainedModel, trainDataView.Schema, outputMlNetModelFilePath)
            Console.WriteLine($"Model saved to: {outputMlNetModelFilePath}")

            ' 9. Try a single prediction simulating an end-user app
            TrySinglePrediction(imagesForPredictions, mlContext, trainedModel)

            Dim scoreRounded = Math.Round(score, digits:=4) * 100
            Dim scoreExpected = 70
            Dim success = scoreRounded >= scoreExpected
            Console.WriteLine("Success: Score = " & scoreRounded.ToString("0.00") & " >= " & scoreExpected & " : " & success)

            Return success

        End Function

        Private Shared Sub EvaluateModel(mlContext As MLContext, testDataset As IDataView,
                trainedModel As ITransformer, ByRef score#)

            Console.WriteLine("Making predictions in bulk for evaluating model's quality...")

            Dim predictionsDataView = trainedModel.Transform(testDataset)

            Dim metrics = mlContext.MulticlassClassification.Evaluate(
                predictionsDataView, labelColumnName:="LabelAsKey",
                predictedLabelColumnName:="PredictedLabel")
            Common.ConsoleHelper.PrintMultiClassClassificationMetrics(
                "TensorFlow DNN Transfer Learning", metrics)
            score = metrics.MacroAccuracy

        End Sub

        Private Shared Sub TrySinglePrediction(imagesForPredictions As String, mlContext As MLContext,
                trainedModel As ITransformer)

            ' Create prediction function to try one prediction
            Dim predictionEngine = mlContext.Model.CreatePredictionEngine(
                Of InMemoryImageData, ImagePrediction)(trainedModel)
            Dim testImages As IEnumerable(Of InMemoryImageData) =
                LoadImagesFromDirectoryInMemory(imagesForPredictions, useFolderNameAsLabel:=True)
            Dim imageToPredict = testImages.First()
            Dim prediction = predictionEngine.Predict(imageToPredict)

            Console.WriteLine(
                $"Image Filename : [{imageToPredict.ImageFileName}], " +
                $"Scores : [{String.Join(",", prediction.Score)}], " +
                $"Predicted Label : {prediction.PredictedLabel}")

            'Dim score! = prediction.Score(2)
            'Dim successScore = Math.Round(score, digits:=4) * 100 >= 99.99
            'Dim successLabel = prediction.PredictedLabel = "roses"
            'Console.WriteLine("Single prediction: Success: " & (successScore AndAlso successLabel))

        End Sub

        Public Shared Iterator Function LoadImagesFromDirectory(folder As String,
                Optional useFolderNameasLabel As Boolean = True,
                Optional searchOption As SearchOption = SearchOption.AllDirectories) As IEnumerable(Of ImageData)

            Dim files = Directory.GetFiles(folder, "*", searchOption)

            For Each file In files

                If Path.GetExtension(file) <> ".jpg" AndAlso Path.GetExtension(file) <> ".png" Then
                    Continue For
                End If

                Dim label = Path.GetFileName(file)
                If useFolderNameasLabel Then
                    label = Directory.GetParent(file).Name
                Else
                    Dim index As Integer = 0
                    Do While index < label.Length
                        If Not Char.IsLetter(label.Chars(index)) Then
                            label = label.Substring(0, index)
                            Exit Do
                        End If
                        index += 1
                    Loop
                End If

                Yield New ImageData With {
                    .ImagePath = file,
                    .Label = label
                }

            Next file

        End Function

        Public Shared Function LoadImagesFromDirectoryInMemory(folder As String,
                Optional useFolderNameAsLabel As Boolean = True,
                Optional searchOption As SearchOption = SearchOption.AllDirectories) As IEnumerable(Of InMemoryImageData)

            Return LoadImagesFromDirectory(folder, useFolderNameAsLabel, searchOption).Select(
                Function(x) New InMemoryImageData(
                    Image0:=File.ReadAllBytes(x.ImagePath),
                    ImageFileName0:=Path.GetFileName(x.ImagePath),
                    Label0:=x.Label))

        End Function

        'Public Shared Sub DownloadImageSet(imagesFolder As String,
        '        imagesDatasetUrl As String, imagesDatasetZip As String, commonAssetsPath As String)

        '    ' Get a set of images to teach the network about the new classes

        '    Dim zipPath = Path.Combine(imagesFolder, imagesDatasetZip)
        '    Dim commonPath = Path.Combine(commonAssetsPath, imagesDatasetZip)

        '    If Not File.Exists(zipPath) AndAlso File.Exists(commonPath) Then
        '        IO.File.Copy(commonPath, zipPath)
        '    ElseIf File.Exists(zipPath) AndAlso Not File.Exists(commonPath) Then
        '        IO.File.Copy(zipPath, commonPath)
        '    End If

        '    Dim imagesDirectory = Path.Combine(
        '        imagesFolder, Path.GetFileNameWithoutExtension(imagesDatasetZip))
        '    If Directory.Exists(imagesDirectory) Then Exit Sub

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
        '        Console.WriteLine("====Extracting zip====")
        '        If zipPath.EndsWith(".tgz") Then
        '            Using inputStream = File.OpenRead(zipPath)
        '                Using gzipStream = New GZipInputStream(inputStream)
        '                    Using tarArchive As TarArchive = TarArchive.CreateInputTarArchive(
        '                            gzipStream, nameEncoding:=System.Text.Encoding.Default)
        '                        tarArchive.ExtractContents(imagesFolder)
        '                    End Using
        '                End Using
        '            End Using
        '        Else
        '            Dim myFastZip As New FastZip()
        '            myFastZip.ExtractZip(zipPath, imagesFolder, fileFilter:=String.Empty)
        '        End If
        '        Console.WriteLine("====Extracting is completed (images)====")
        '        If Directory.Exists(imagesDirectory) Then Exit Sub
        '        Console.WriteLine("====Extracting: Fail! (images)====")
        '        Stop
        '    End If

        '    ' SINGLE SMALL FLOWERS IMAGESET (200 files)
        '    'Dim fileName As String = "flower_photos_small_set.zip"
        '    'Dim url As String = $"https://mlnetfilestorage.file.core.windows.net/imagesets/flower_images/flower_photos_small_set.zip?st=2019-08-07T21%3A27%3A44Z&se=2030-08-08T21%3A27%3A00Z&sp=rl&sv=2018-03-28&sr=f&sig=SZ0UBX47pXD0F1rmrOM%2BfcwbPVob8hlgFtIlN89micM%3D"
        '    'Common.Web.Download(url, imagesDownloadFolder, fileName)
        '    'Common.Compress.UnZip(Path.Join(imagesDownloadFolder, fileName), imagesDownloadFolder)

        '    ' SINGLE FULL FLOWERS IMAGESET (3,600 files)
        '    'Dim fileName As String = "flower_photos.tgz"
        '    'Dim url As String = $"http://download.tensorflow.org/example_images/{fileName}"
        '    'Common.Web.Download(url, imagesDownloadFolder, fileName)
        '    'Common.Compress.ExtractTGZ(Path.Join(imagesDownloadFolder, fileName), imagesDownloadFolder)

        '    'Return Path.GetFileNameWithoutExtension(fileName)

        'End Sub

        Public Shared Function GetAbsolutePath(relativePath As String) As String
            Dim fullPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
            Return fullPath
        End Function

        Private Shared Sub FilterMLContextLog(ByVal sender As Object, ByVal e As LoggingEventArgs)
            If e.Message.StartsWith("[Source=ImageClassificationTrainer;") Then
                Console.WriteLine(e.Message)
            End If
        End Sub

    End Class

End Namespace