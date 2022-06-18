
Imports SpamDetectionConsoleApp.Common
Imports SpamDetectionConsoleApp.SpamDetectionConsoleApp.MLDataStructures
Imports System.IO
Imports Microsoft.ML

Namespace SpamDetectionConsoleApp

    Public Class Program

        Const datasetFile = "SMSSpamCollection" '"spam"
        Const datasetZip As String = datasetFile & ".zip"
        Const datasetUrl As String =
                "https://archive.ics.uci.edu/ml/machine-learning-databases/00228/smsspamcollection.zip"

        Shared Sub Main(args() As String)

            'Dim assetsRelativePath As String = "../../.."
            Dim assetsRelativePath As String = "../../../Data"
            Dim assetsPath As String = GetAbsolutePath(assetsRelativePath)
            'Dim DataDirectoryPath As String = Path.Combine(assetsPath, "Data", "spamfolder")

            Dim commonDatasetsRelativePath As String = "../../../../../../../../datasets"
            Dim commonDatasetsPath As String = GetAbsolutePath(commonDatasetsRelativePath)

            GetResult(assetsPath, commonDatasetsPath)

            Console.WriteLine("=============== End of process, hit any key to finish =============== ")
            Console.ReadLine()

        End Sub

        Public Shared Function GetResult(
                myAssetsPath As String, myCommonDatasetsPath As String) As Boolean

            ' Download the dataset if it doesn't exist
            'Dim datasetFolder As String = Path.Combine(myAssetsPath, "Data")
            'Dim TrainDataPath As String = Path.Combine(myAssetsPath, "Data", "spamfolder", "SMSSpamCollection")
            'Dim datasetFolder As String = myAssetsPath
            Dim datasetFolder As String = Path.Combine(myAssetsPath, "spamfolder")
            ' Note: This is a file, not a directory
            'Dim TrainDataPath As String = Path.Combine(myAssetsPath, "spamfolder", datasetFile) '"SMSSpamCollection")
            Dim TrainDataPath As String = Path.Combine(datasetFolder, datasetFile) '"SMSSpamCollection")
            'Dim TrainDataPath As String = Path.Combine(myAssetsPath, datasetFile) ' "SMSSpamCollection"
            Dim destFiles As New List(Of String) From {TrainDataPath}
            DownloadBigFile(datasetFolder, datasetUrl, datasetZip, myCommonDatasetsPath, destFiles)
            'destFolder:=TrainDataPath)
            'If Not File.Exists(TrainDataPath) Then
            '    Using client = New WebClient
            '        ' The code below will download a dataset from a third-party, UCI (link),
            '        '  and may be governed by separate third-party terms
            '        ' By proceeding, you agree to those separate terms
            '        client.DownloadFile(datasetUrl, datasetZip)
            '    End Using
            '    ZipFile.ExtractToDirectory(datasetZip, DataDirectoryPath)
            'End If

            ' Set up the MLContext, which is a catalog of components in ML.NET
            Dim mlContext As New MLContext

            ' Specify the schema for spam data and read it into DataView
            Dim data = mlContext.Data.LoadFromTextFile(Of SpamInput)(
                path:=TrainDataPath, hasHeader:=True, separatorChar:=CType(vbTab, Char))

            ' Create the estimator which converts the text label to boolean, featurizes the text,
            '  and adds a linear trainer
            ' Data process configuration with pipeline data transformations 
            Dim dataProcessPipeline =
                mlContext.Transforms.Conversion.MapValueToKey(
                    "Label", "Label").
                    Append(mlContext.Transforms.Text.FeaturizeText("FeaturesText",
                        New Microsoft.ML.Transforms.Text.TextFeaturizingEstimator.Options With {
                        .WordFeatureExtractor = New Microsoft.ML.Transforms.Text.WordBagEstimator.Options With {
                            .NgramLength = 2,
                            .UseAllLengths = True
                        },
                        .CharFeatureExtractor = New Microsoft.ML.Transforms.Text.WordBagEstimator.Options With {
                            .NgramLength = 3,
                            .UseAllLengths = False
                        }
                    }, "Message")).
                    Append(mlContext.Transforms.CopyColumns("Features", "FeaturesText")).
                    Append(mlContext.Transforms.NormalizeLpNorm("Features", "Features")).
                    AppendCacheCheckpoint(mlContext)

            ' Set the training algorithm 
            Dim trainer = mlContext.MulticlassClassification.Trainers.OneVersusAll(
                mlContext.BinaryClassification.Trainers.AveragedPerceptron(
                labelColumnName:="Label", numberOfIterations:=10, featureColumnName:="Features"),
                labelColumnName:="Label").
                Append(mlContext.Transforms.Conversion.MapKeyToValue(
                    "PredictedLabel", "PredictedLabel"))
            Dim trainingPipeLine = dataProcessPipeline.Append(trainer)

            ' Evaluate the model using cross-validation
            ' Cross-validation splits our dataset into 'folds', trains a model on some folds and 
            '  evaluates it on the remaining fold. We are using 5 folds so we get back 5 sets of scores
            ' Let's compute the average AUC, which should be between 0.5 and 1 (higher is better)
            Console.WriteLine("=============== Cross-validating to get model's accuracy metrics ===============")
            Dim crossValidationResults = mlContext.MulticlassClassification.CrossValidate(
                data:=data, estimator:=trainingPipeLine, numberOfFolds:=5)
            Dim score# = 0
            ConsoleHelper.PrintMulticlassClassificationFoldsAverageMetrics(
                trainer.ToString(), crossValidationResults, score)

            ' Now let's train a model on the full dataset to help us get better results
            Dim model = trainingPipeLine.Fit(data)

            ' Create a PredictionFunction from our model 
            Dim predictor = mlContext.Model.CreatePredictionEngine(
                Of SpamInput, SpamPrediction)(model)

            Console.WriteLine("=============== Predictions for below data===============")
            ' Test a few examples
            ClassifyMessage(predictor, "That's a great idea. It should work.")
            ClassifyMessage(predictor, "free medicine winner! congratulations")
            ClassifyMessage(predictor, "Yes we should meet over the weekend!")
            ClassifyMessage(predictor, "you win pills and free entry vouchers")

            Dim scoreRounded = Math.Round(score, digits:=4) * 100
            Dim scoreExpected = 98
            Dim success = scoreRounded >= scoreExpected
            Console.WriteLine("Success: Score = " & scoreRounded.ToString("0.00") & " >= " & scoreExpected & " : " & success)

            Return success

        End Function

        Private Shared Sub ClassifyMessage(
                predictor As PredictionEngine(Of SpamInput, SpamPrediction), message As String)
            Dim input = New SpamInput With {.Message = message}
            Dim prediction = predictor.Predict(input)
            Console.WriteLine("The message '{0}' is {1}", input.Message,
                If(prediction.isSpam = "spam", "spam", "not spam"))
        End Sub

        Public Shared Function GetAbsolutePath(relativePath As String) As String
            Dim fullPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
            Return fullPath
        End Function

    End Class

End Namespace