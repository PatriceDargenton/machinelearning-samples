
Imports System.IO
Imports Microsoft.ML
Imports Microsoft.ML.AutoML
Imports Microsoft.ML.Data
Imports SentimentAnalysisAutoML.Common
Imports SentimentAnalysisAutoML.SentimentAnalysisAutoML.DataStructures

Namespace SentimentAnalysisAutoML

    Public Module Program

        Private ExperimentTime As UInteger = 60

        Public Sub Main(args() As String)

            Dim assetsRelativePath As String = "../../../Data"
            Dim assetsPath As String = GetAbsolutePath(assetsRelativePath)

            Dim baseModelsRelativePath As String = "../../../MLModels"
            Dim modelPath As String = GetAbsolutePath(baseModelsRelativePath)

            GetResult(assetsPath, modelPath)

            ConsoleHelperAutoML.ConsoleWriteHeader("=============== End of process, hit any key to finish ===============")
            Console.ReadKey()

        End Sub

        Public Function GetResult(
                myAssetsPath As String, myModelPath As String,
                Optional isTest As Boolean = False) As Boolean

            Dim mlContext As New MLContext() ' seed:=1
            Dim modelPath As String = Path.GetFullPath(Path.Combine(myModelPath,
                "SentimentModel.zip"))

            ' Create, train, evaluate and save a model
            Dim score# = 0
            BuildTrainEvaluateAndSaveModel(myAssetsPath, modelPath, mlContext, score)

            ' Make a single test prediction loading the model from .ZIP file
            TestSinglePrediction(modelPath, mlContext)

            Dim scoreRounded = Math.Round(score, digits:=4) * 100
            Dim scoreExpected = 70
            Dim success = scoreRounded >= scoreExpected
            Console.WriteLine("Success: Score = " & scoreRounded & " >= " & scoreExpected & " : " & success)

            Return success

        End Function

        Private Function BuildTrainEvaluateAndSaveModel(
                myAssetsPath As String, myModelPath As String,
                MLContext As MLContext, ByRef score#) As ITransformer

            Dim trainDataRelativePath As String = Path.Combine(myAssetsPath,
                "wikipedia-detox-250-line-data.tsv")
            Dim testDataRelativePath As String = Path.Combine(myAssetsPath,
                "wikipedia-detox-250-line-test.tsv")
            Dim trainDataPath As String = GetAbsolutePath(trainDataRelativePath)
            Dim testDataPath As String = GetAbsolutePath(testDataRelativePath)
            trainDataPath = Path.GetFullPath(trainDataPath)
            testDataPath = Path.GetFullPath(testDataPath)

            ' STEP 1: Load data
            Dim trainingDataView As IDataView =
                MLContext.Data.LoadFromTextFile(Of SentimentIssue)(trainDataPath, hasHeader:=True)
            Dim testDataView As IDataView =
                MLContext.Data.LoadFromTextFile(Of SentimentIssue)(testDataPath, hasHeader:=True)

            ' STEP 2: Display first few rows of training data
            ConsoleHelperAutoML.ShowDataViewInConsole(MLContext, trainingDataView)

            ' STEP 3: Initialize our user-defined progress handler that AutoML will 
            '  invoke after each model it produces and evaluates
            Dim progressHandler = New BinaryExperimentProgressHandler

            ' STEP 4: Run AutoML binary classification experiment
            ConsoleHelperAutoML.ConsoleWriteHeader("=============== Running AutoML experiment ===============")
            Console.WriteLine($"Running AutoML binary classification experiment for {ExperimentTime} seconds...")
            Dim experimentResult As ExperimentResult(Of BinaryClassificationMetrics) =
                MLContext.Auto().CreateBinaryClassificationExperiment(ExperimentTime).
                Execute(trainingDataView, progressHandler:=progressHandler)

            ' Print top models found by AutoML
            Console.WriteLine()
            PrintTopModels(experimentResult)

            ' STEP 5: Evaluate the model and print metrics
            ConsoleHelperAutoML.ConsoleWriteHeader("=============== Evaluating model's accuracy with test data ===============")
            Dim bestRun As RunDetail(Of BinaryClassificationMetrics) = experimentResult.BestRun
            Dim trainedModel As ITransformer = bestRun.Model
            Dim predictions = trainedModel.Transform(testDataView)
            Dim metrics = MLContext.BinaryClassification.EvaluateNonCalibrated(
                data:=predictions, scoreColumnName:="Score")
            ConsoleHelperAutoML.PrintBinaryClassificationMetrics(bestRun.TrainerName, metrics)
            score = metrics.Accuracy

            ' STEP 6: Save/persist the trained model to a .ZIP file
            Dim directoryPath = Path.GetDirectoryName(myModelPath)
            If Not Directory.Exists(directoryPath) Then
                Dim di As New DirectoryInfo(directoryPath)
                di.Create()
            End If
            MLContext.Model.Save(trainedModel, trainingDataView.Schema, myModelPath)
            Console.WriteLine("The model is saved to {0}", myModelPath)

            Return trainedModel

        End Function

        ' (OPTIONAL) Try/test a single prediction by loading the model from the file, first
        Private Sub TestSinglePrediction(myModelPath As String, mlContext As MLContext)

            ConsoleHelperAutoML.ConsoleWriteHeader("=============== Testing prediction engine ===============")
            Dim sampleStatement As SentimentIssue =
                New SentimentIssue With {.Text = "This is a very rude movie"}

            Dim modelInputSchema As DataViewSchema = Nothing
            Dim trainedModel As ITransformer = mlContext.Model.Load(myModelPath, modelInputSchema)
            Console.WriteLine($"=============== Loaded Model OK  ===============")

            ' Create prediction engine related to the loaded trained model
            Dim predEngine = mlContext.Model.CreatePredictionEngine(Of SentimentIssue, SentimentPrediction)(trainedModel)
            Console.WriteLine($"=============== Created Prediction Engine OK  ===============")
            ' Score
            Dim predictedResult = predEngine.Predict(sampleStatement)

            Console.WriteLine($"=============== Single Prediction  ===============")
            Console.WriteLine($"Text: {sampleStatement.Text} | Prediction: {(If(Convert.ToBoolean(predictedResult.Prediction), "Toxic", "Non Toxic"))} sentiment")
            Console.WriteLine($"==================================================")

        End Sub

        Public Function GetAbsolutePath(relativePath As String) As String
            Dim fullPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
            Return fullPath
        End Function

        ''' <summary>
        ''' Prints top models from AutoML experiment
        ''' </summary>
        Private Sub PrintTopModels(experimentResult As ExperimentResult(Of BinaryClassificationMetrics))

            ' Get top few runs ranked by accuracy
            Dim topRuns = experimentResult.RunDetails.Where(
                Function(r) r.ValidationMetrics IsNot Nothing AndAlso Not Double.IsNaN(r.ValidationMetrics.Accuracy)).
                OrderByDescending(Function(r) r.ValidationMetrics.Accuracy).Take(3)

            Console.WriteLine("Top models ranked by accuracy --")
            ConsoleHelperAutoML.PrintBinaryClassificationMetricsHeader()
            For i = 0 To topRuns.Count() - 1
                Dim run = topRuns.ElementAt(i)
                ConsoleHelperAutoML.PrintIterationMetrics(i + 1, run.TrainerName, run.ValidationMetrics, run.RuntimeInSeconds)
            Next i

        End Sub

    End Module

End Namespace