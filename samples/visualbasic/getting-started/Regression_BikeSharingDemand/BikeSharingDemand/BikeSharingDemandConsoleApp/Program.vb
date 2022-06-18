
Imports BikeSharingDemandConsoleApp.BikeSharingDemand.DataStructures
Imports BikeSharingDemandConsoleApp.Common
Imports Microsoft.ML
Imports System.IO

Namespace BikeSharingDemand

    Public Module Program

        Sub Main(args() As String)

            Dim ModelsLocation As String = "../../../../MLModels"
            Dim DatasetsLocation As String = "../../../../Data"

            Dim commonDatasetsRelativePath As String = "../../../../../../../../../datasets"
            Dim commonDatasetsPath As String = GetAbsolutePath(commonDatasetsRelativePath)

            GetResult(DatasetsLocation, ModelsLocation, commonDatasetsPath)

            Common.ConsoleHelper.ConsolePressAnyKey()

        End Sub

        Public Function GetResult(myAssetsPath As String, myModelsLocation As String,
                myCommonDatasetsPath As String) As Boolean

            Dim TrainingDataRelativePath As String = $"{myAssetsPath}/hour_train.csv"
            Dim TestDataRelativePath As String = $"{myAssetsPath}/hour_test.csv"
            Dim CommonTrainingDataPath As String = $"{myCommonDatasetsPath}/hour_train.csv"
            Dim CommonTestDataPath As String = $"{myCommonDatasetsPath}/hour_test.csv"

            Dim TrainingDataLocation As String = GetAbsolutePath(TrainingDataRelativePath)
            Dim TestDataLocation As String = GetAbsolutePath(TestDataRelativePath)

            If Not File.Exists(TrainingDataRelativePath) Then
                If File.Exists(CommonTrainingDataPath) Then
                    IO.File.Copy(CommonTrainingDataPath, TrainingDataRelativePath)
                End If
            End If
            If Not File.Exists(TestDataRelativePath) Then
                If File.Exists(CommonTestDataPath) Then
                    IO.File.Copy(CommonTestDataPath, TestDataRelativePath)
                End If
            End If

            ' Create MLContext to be shared across the model creation workflow objects 
            ' Set a random seed for repeatable/deterministic results across multiple trainings
            Dim mlContext As New MLContext() 'seed:=0

            ' 1. Common data loading configuration
            Dim trainingDataView = mlContext.Data.LoadFromTextFile(Of DemandObservation)(
                path:=TrainingDataLocation, hasHeader:=True, separatorChar:=","c)
            Dim testDataView = mlContext.Data.LoadFromTextFile(Of DemandObservation)(
                path:=TestDataLocation, hasHeader:=True, separatorChar:=","c)

            ' 2. Common data pre-process with pipeline data transformations

            ' Concatenate all the numeric columns into a single features column
            Dim dataProcessPipeline = mlContext.Transforms.Concatenate("Features",
                NameOf(DemandObservation.Season),
                NameOf(DemandObservation.Year),
                NameOf(DemandObservation.Month),
                NameOf(DemandObservation.Hour),
                NameOf(DemandObservation.Holiday),
                NameOf(DemandObservation.Weekday),
                NameOf(DemandObservation.WorkingDay),
                NameOf(DemandObservation.Weather),
                NameOf(DemandObservation.Temperature),
                NameOf(DemandObservation.NormalizedTemperature),
                NameOf(DemandObservation.Humidity),
                NameOf(DemandObservation.Windspeed)).AppendCacheCheckpoint(mlContext)

            ' Use in-memory cache for small/medium datasets to lower training time
            ' Do NOT use it (remove .AppendCacheCheckpoint()) when handling very large datasets

            ' (Optional) Peek data in training DataView after applying the ProcessPipeline's transformations  
            Common.ConsoleHelper.PeekDataViewInConsole(mlContext, trainingDataView, dataProcessPipeline, 10)
            Common.ConsoleHelper.PeekVectorColumnDataInConsole(mlContext, "Features",
                trainingDataView, dataProcessPipeline, 10)

            ' Definition of regression trainers/algorithms to use
            Dim regressionLearners As (name As String, value As IEstimator(Of ITransformer))() = {
                ("FastTree", mlContext.Regression.Trainers.FastTree()),
                ("Poisson", mlContext.Regression.Trainers.LbfgsPoissonRegression()),
                ("SDCA", mlContext.Regression.Trainers.Sdca()),
                ("FastTreeTweedie", mlContext.Regression.Trainers.FastTreeTweedie())
            }

            ' 3. Phase for Training, Evaluation and model file persistence
            ' Per each regression trainer: Train, Evaluate, and Save a different model
            Dim score# = 0
            For Each trainer In regressionLearners
                Console.WriteLine("=============== Training the current model ===============")
                Dim trainingPipeline = dataProcessPipeline.Append(trainer.value)
                Dim trainedModel = trainingPipeline.Fit(trainingDataView)

                Console.WriteLine("===== Evaluating Model's accuracy with Test data =====")
                Dim predictions As IDataView = trainedModel.Transform(testDataView)
                Dim metrics = mlContext.Regression.Evaluate(data:=predictions,
                    labelColumnName:="Label", scoreColumnName:="Score")
                ConsoleHelper.PrintRegressionMetrics(trainer.value.ToString(), metrics)
                If metrics.RSquared > score Then score = metrics.RSquared

                ' Save the model file that can be used by any application
                Dim modelRelativeLocation As String = $"{myModelsLocation}/{trainer.name}Model.zip"
                Dim modelPath As String = GetAbsolutePath(modelRelativeLocation)
                Dim directoryPath = IO.Path.GetDirectoryName(modelPath)
                If Not IO.Directory.Exists(directoryPath) Then
                    Dim di As New IO.DirectoryInfo(directoryPath)
                    di.Create()
                End If
                mlContext.Model.Save(trainedModel, trainingDataView.Schema, modelPath)
                Console.WriteLine("The model is saved to {0}", modelPath)
            Next trainer

            ' 4. Try/test Predictions with the created models
            ' The following test predictions could be implemented/deployed in a different application (production apps)
            '  that's why it is seggregated from the previous loop
            ' For each trained model, test 10 predictions           
            For Each learner In regressionLearners
                ' Load current model from .ZIP file
                Dim modelRelativeLocation As String = $"{myModelsLocation}/{learner.name}Model.zip"
                Dim modelPath As String = GetAbsolutePath(modelRelativeLocation)
                Dim modelInputSchema As DataViewSchema = Nothing
                Dim trainedModel As ITransformer = mlContext.Model.Load(modelPath, modelInputSchema)

                ' Create prediction engine related to the loaded trained model
                Dim predEngine = mlContext.Model.CreatePredictionEngine(
                    Of DemandObservation, DemandPrediction)(trainedModel)

                Console.WriteLine($"================== Visualize/test 10 predictions for model {learner.name}Model.zip ==================")
                ' Visualize 10 tests comparing prediction with actual/observed values from the test dataset
                ModelScoringTester.VisualizeSomePredictions(
                    mlContext, learner.name, TestDataLocation, predEngine, 10)

            Next learner

            Dim scoreRounded = Math.Round(score, digits:=4) * 100
            Dim scoreExpected = 91
            Dim success = scoreRounded >= scoreExpected
            Console.WriteLine("Success: Score = " & scoreRounded.ToString("0.00") & " >= " & scoreExpected & " : " & success)

            Return success

        End Function

        Public Function GetAbsolutePath(relativePath As String) As String
            Dim fullPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
            Return fullPath
        End Function

    End Module

End Namespace