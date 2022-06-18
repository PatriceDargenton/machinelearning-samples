
Imports System.IO
Imports ImageClassification.Train
Imports ImageClassification.Train.ImageClassification.DataModels
Imports Microsoft.ML
Imports Microsoft.ML.Data

Namespace ImageClassificationPredict

    Public Class Program

        Shared Sub Main(args() As String)

            Dim assetsRelativePath As String = "../../../assets"
            Dim modelRelativePath As String = "../../../../ImageClassification.Train/assets"
            Dim assetsPath As String = GetAbsolutePath(assetsRelativePath)
            Dim modelPath As String = GetAbsolutePath(modelRelativePath)

            GetResult(assetsPath, modelPath)

            Console.WriteLine("Press any key to end the app.")
            Console.ReadKey()

        End Sub

        Public Shared Function GetResult(
                myAssetsPath As String, myModelPath As String) As Boolean

            Dim imagesForPredictions As String = Path.Combine(myAssetsPath,
                "inputs", "images-for-predictions")

            ' Use directly the last saved:
            'Dim imageClassifierModelZipFilePath = Path.Combine(myAssetsPath,
            '   "inputs", "MLNETModel", "imageClassifier.zip")
            Dim imageClassifierModelZipFilePath = Path.Combine(myModelPath,
                "outputs", "imageClassifier.zip")

            Try
                Dim mlContext As New MLContext() ' seed:=1

                If Not File.Exists(imageClassifierModelZipFilePath) Then
                    Console.WriteLine("Please train first to save the model!")
                    Console.ReadKey()
                    Return False
                End If
                Console.WriteLine($"Loading model from: {IO.Path.GetFullPath(imageClassifierModelZipFilePath)}")

                ' Load the model
                Dim modelInputSchema As DataViewSchema = Nothing
                Dim loadedModel As ITransformer = mlContext.Model.Load(
                        imageClassifierModelZipFilePath, modelInputSchema)

                ' Create prediction engine to try a single prediction (input = ImageData, output = ImagePrediction)
                Dim predictionEngine = mlContext.Model.CreatePredictionEngine(
                        Of InMemoryImageData, ImageClassification.DataModels.ImagePrediction)(loadedModel)

                Dim imagesToPredict As IEnumerable(Of InMemoryImageData) =
                        ImageClassificationTrain.Program.LoadImagesFromDirectoryInMemory(
                            imagesForPredictions, useFolderNameAsLabel:=True, SearchOption.TopDirectoryOnly)

                ' Predict the first image in the folder
                Dim imageToPredict = imagesToPredict.First()

                ' System.EntryPointNotFoundException HResult=0x80131523
                ' Unable to find an entry point named 'TF_StringEncodedSize' in DLL 'tensorflow':
                ' Max compatible version for SciSharp.TensorFlow.Redist: 2.3.1 < 2.7.0
                Dim prediction = predictionEngine.Predict(imageToPredict)

                Console.WriteLine(
                    $"ImageFile : [{Path.GetFileName(imageToPredict.ImageFileName)}], " &
                    $"Probability : [{String.Join(",", prediction.Score.Max)}], " &
                    $"Predicted Label : {prediction.PredictedLabel}")

                Dim score! = prediction.Score.Max
                Dim successScore = Math.Round(score, digits:=4) * 100 >= 97
                Dim successLabel = prediction.PredictedLabel = "roses"
                Console.WriteLine("Success: " & (successScore AndAlso successLabel))

                ' Predict all images in the folder
                Console.WriteLine("")
                Console.WriteLine("Predicting several images...")

                Dim numImage% = 0
                Dim nbSuccess% = 0
                For Each currentImageToPredict As InMemoryImageData In imagesToPredict
                    numImage += 1

                    Dim currentPrediction = predictionEngine.Predict(currentImageToPredict)
                    Console.WriteLine(
                        $"ImageFile : [{Path.GetFileName(currentImageToPredict.ImageFileName)}], " &
                        $"Probability : [{currentPrediction.Score.Max}], " &
                        $"Predicted Label : {currentPrediction.PredictedLabel}")

                    Dim scoreI! = currentPrediction.Score.Max
                    Dim successScoreI = Math.Round(scoreI, digits:=4) * 100 >= 97
                    Dim successLabelI =
                        (currentPrediction.PredictedLabel = "roses" AndAlso numImage = 1) OrElse
                        (currentPrediction.PredictedLabel = "daisy" AndAlso numImage = 2) OrElse
                        (currentPrediction.PredictedLabel = "tulips" AndAlso numImage = 3) OrElse
                        (currentPrediction.PredictedLabel = "roses" AndAlso numImage = 4)
                    Console.WriteLine("Success: " & (successScoreI AndAlso successLabelI))
                    If successLabelI Then nbSuccess += 1

                Next currentImageToPredict
                Dim success = nbSuccess >= 4
                Return success

            Catch ex As Exception
                Console.WriteLine(ex.ToString())
                Return False
            End Try

        End Function

        Public Shared Function GetAbsolutePath(relativePath As String) As String
            Dim fullPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
            Return fullPath
        End Function

    End Class

End Namespace