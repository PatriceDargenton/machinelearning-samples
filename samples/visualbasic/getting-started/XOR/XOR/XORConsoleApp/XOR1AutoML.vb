
Imports System.IO
Imports System.Threading
Imports Microsoft.ML
Imports Microsoft.ML.AutoML
Imports Microsoft.ML.Data
Imports XORApp.Common

Namespace XORApp

    Public Module XOR1AutoML

        Public Class XORData

            <LoadColumn(0)>
            Public Input1 As Single

            <LoadColumn(1)>
            Public Input2 As Single

            <LoadColumn(2)>
            Public Output As Single

            Public Sub New(input1 As Single, input2 As Single)
                Me.Input1 = input1
                Me.Input2 = input2
            End Sub

        End Class

        Public Class XORPrediction
            Public Score As Single
        End Class

        Public Class SampleXORData
            Friend Shared ReadOnly XOR1 As XORData = New XORData(1.0F, 0.0F)
            Friend Shared ReadOnly XOR2 As XORData = New XORData(0F, 0F)
            Friend Shared ReadOnly XOR3 As XORData = New XORData(0.0F, 1.0F)
            Friend Shared ReadOnly XOR4 As XORData = New XORData(1.0F, 1.0F)
        End Class

        ''' <summary>
        ''' Infer columns in the dataset with AutoML
        ''' </summary>
        Private Function InferColumns(mlContext As MLContext, TrainDataPath As String,
                LabelColumnName As String) As ColumnInferenceResults

            ConsoleHelperAutoML.ConsoleWriteHeader("=============== Inferring columns in dataset ===============")
            Dim columnInference As ColumnInferenceResults =
                mlContext.Auto().InferColumns(
                    TrainDataPath, LabelColumnName, groupColumns:=False)
            ConsoleHelperAutoML.Print(columnInference)

            Return columnInference

        End Function

        Public Sub TrainFromFile(mlContext As MLContext, ModelPath As String,
                TrainDataPath As String)

            ' STEP 1: Common data loading configuration
            Dim trainingDataView = mlContext.Data.LoadFromTextFile(Of XORData)(
                TrainDataPath, hasHeader:=True, separatorChar:=","c)
            Dim testDataView = mlContext.Data.LoadFromTextFile(Of XORData)(
                TrainDataPath, hasHeader:=True, separatorChar:=","c)

            ' STEP 2: Common data process configuration with pipeline data transformations
            Dim dataProcessPipeline = mlContext.Transforms.Concatenate("Features",
                NameOf(XORData.Input1),
                NameOf(XORData.Input2)).AppendCacheCheckpoint(mlContext)

            ' (Optional) Peek data in training DataView after applying the ProcessPipeline's transformations
            ConsoleHelper.PeekDataViewInConsole(mlContext, trainingDataView,
                dataProcessPipeline, 5)
            ConsoleHelper.PeekVectorColumnDataInConsole(mlContext, "Features", trainingDataView,
                dataProcessPipeline, 5)

            ' Infer columns in the dataset with AutoML
            Dim columnInference = XOR1AutoML.InferColumns(mlContext,
                TrainDataPath, LabelColumnName:="Output") '"#Label"); 
            ' Run an AutoML experiment on the dataset
            Dim experimentResult = RunAutoMLExperiment(mlContext,
                columnInference, trainingDataView)

            Dim directoryPath = Path.GetDirectoryName(ModelPath)
            If Not Directory.Exists(directoryPath) Then
                Dim di As New DirectoryInfo(directoryPath)
                di.Create()
            End If
            mlContext.Model.Save(experimentResult.BestRun.Model,
                trainingDataView.Schema, ModelPath)

        End Sub

        Private Function RunAutoMLExperiment(mlContext As MLContext,
                columnInference As ColumnInferenceResults,
                TrainDataView As IDataView) As ExperimentResult(Of RegressionMetrics)

            ' STEP 1: Display first few rows of the training data.
            ConsoleHelperAutoML.ShowDataViewInConsole(mlContext, TrainDataView)

            ' STEP 2: Build a pre-featurizer for use in the AutoML experiment
            ' (Internally, AutoML uses one or more train/validation data splits to 
            '  evaluate the models it produces. The pre-featurizer is fit only on the 
            '  training data split to produce a trained transform
            '  Then, the trained transform is applied to both the train and validation data splits)
            'Dim preFeaturizer As IEstimator(Of ITransformer) =
            '    mlContext.Transforms.Conversion.MapValue(
            '        "Output2", {New KeyValuePair(Of String, Boolean)("XOR", True)},
            '        "Output")

            ' STEP 3: Customize column information returned by InferColumns API
            Dim columnInformation As ColumnInformation = columnInference.ColumnInformation
            'columnInformation.CategoricalColumnNames.Remove("x")
            'columnInformation.IgnoredColumnNames.Add("y")

            ' STEP 4: Initialize a cancellation token source to stop the experiment
            Dim cts = New CancellationTokenSource()

            ' STEP 5: Initialize our user-defined progress handler that AutoML will 
            '  invoke after each model it produces and evaluates
            Dim progressHandler = New RegressionExperimentProgressHandler()

            ' STEP 6: Create experiment settings
            Dim experimentSettings = XOR1AutoML.CreateExperimentSettings(mlContext, cts)

            ' STEP 7: Run AutoML regression experiment.
            Dim experiment = mlContext.Auto().CreateRegressionExperiment(experimentSettings)
            ConsoleHelperAutoML.ConsoleWriteHeader("=============== Running AutoML experiment ===============")
            Console.WriteLine($"Running AutoML regression experiment...")
            Dim stopwatch = Diagnostics.Stopwatch.StartNew()
            ' Cancel experiment after the user presses any key
            CancelExperimentAfterAnyKeyPress(cts)
            Dim experimentResult As ExperimentResult(Of RegressionMetrics) =
                experiment.Execute(
                    TrainDataView, columnInformation, ' preFeaturizer
                    progressHandler:=progressHandler)
            Console.WriteLine($"{experimentResult.RunDetails.Count()} models were returned after {stopwatch.Elapsed.TotalSeconds:0.00} seconds{Environment.NewLine}")

            ' Print top models found by AutoML
            XOR1AutoML.PrintTopModels(experimentResult)

            Return experimentResult

        End Function

        ''' <summary>
        ''' Create AutoML regression experiment settings
        ''' </summary>
        Private Function CreateExperimentSettings(mlContext As MLContext,
                cts As CancellationTokenSource) As RegressionExperimentSettings

            Dim experimentSettings = New RegressionExperimentSettings()
            experimentSettings.MaxExperimentTimeInSeconds = 3600
            experimentSettings.CancellationToken = cts.Token

            ' Set the metric that AutoML will try to optimize over the course of the experiment
            experimentSettings.OptimizingMetric = RegressionMetric.RootMeanSquaredError

            ' Set the cache directory to Nothing
            ' This will cause all models produced by AutoML to be kept in memory 
            ' instead of written to disk after each run, as AutoML is training
            ' (Please note: for an experiment on a large dataset, opting to keep all 
            '  models trained by AutoML in memory could cause your system to run out 
            '  of memory)
            'experimentSettings.CacheDirectory = Nothing ' BC30456

            ' Remove some trainers if needed during this experiment
            ' (These trainers sometimes underperform on this dataset)
            'experimentSettings.Trainers.Remove(RegressionTrainer.LbfgsPoissonRegression)
            'experimentSettings.Trainers.Remove(RegressionTrainer.OnlineGradientDescent)

            Return experimentSettings

        End Function

        Private Sub CancelExperimentAfterAnyKeyPress(cts As CancellationTokenSource)
            Call Task.Run(Sub()
                              Console.WriteLine("Press any key to stop the experiment run...")
                              Console.ReadKey()
                              cts.Cancel()
                          End Sub)
        End Sub

        ''' <summary>
        ''' Print top models from AutoML experiment
        ''' </summary>
        Private Sub PrintTopModels(
                experimentResult As ExperimentResult(Of RegressionMetrics))

            ' Get top few runs ranked by root mean squared error
            Dim topRuns = experimentResult.RunDetails.
                Where(Function(r) r.ValidationMetrics IsNot Nothing AndAlso
                          Not Double.IsNaN(r.ValidationMetrics.RootMeanSquaredError)).
                OrderBy(Function(r) r.ValidationMetrics.RootMeanSquaredError).Take(3)

            Console.WriteLine("Top models ranked by root mean squared error --")
            ConsoleHelperAutoML.PrintRegressionMetricsHeader()
            For i = 0 To topRuns.Count() - 1
                Dim run = topRuns.ElementAt(i)
                ConsoleHelperAutoML.PrintIterationMetrics(i + 1,
                    run.TrainerName, run.ValidationMetrics, run.RuntimeInSeconds)
            Next

        End Sub

        Public Function TestSomePredictions(mlContext As MLContext, ModelPath As String) As Integer

            ' Test Classification Predictions with some hard-coded samples 
            Dim modelInputSchema As DataViewSchema = Nothing
            Dim trainedModel As ITransformer = mlContext.Model.Load(ModelPath, modelInputSchema)

            ' Create prediction engine related to the loaded trained model
            Dim predEngine = mlContext.Model.CreatePredictionEngine(
                Of XORData, XORPrediction)(trainedModel)

            Console.WriteLine("=====Predicting using model====")

            Dim resultprediction1 = predEngine.Predict(SampleXORData.XOR1)
            Dim resultprediction2 = predEngine.Predict(SampleXORData.XOR2)
            Dim resultprediction3 = predEngine.Predict(SampleXORData.XOR3)
            Dim resultprediction4 = predEngine.Predict(SampleXORData.XOR4)

            Dim expectedResult1 =
                Convert.ToBoolean(SampleXORData.XOR1.Input1) Xor
                Convert.ToBoolean(SampleXORData.XOR1.Input2)
            Dim expectedResult2 =
                Convert.ToBoolean(SampleXORData.XOR2.Input1) Xor
                Convert.ToBoolean(SampleXORData.XOR2.Input2)
            Dim expectedResult3 =
                Convert.ToBoolean(SampleXORData.XOR3.Input1) Xor
                Convert.ToBoolean(SampleXORData.XOR3.Input2)
            Dim expectedResult4 =
                Convert.ToBoolean(SampleXORData.XOR4.Input1) Xor
                Convert.ToBoolean(SampleXORData.XOR4.Input2)

            Const threshold = 0.2F

            Dim target1 As Single = 0
            If expectedResult1 Then target1 = 1
            Dim target2 As Single = 0
            If expectedResult2 Then target2 = 1
            Dim target3 As Single = 0
            If expectedResult3 Then target3 = 1
            Dim target4 As Single = 0
            If expectedResult4 Then target4 = 1
            Dim success1 As Boolean = Math.Abs(resultprediction1.Score - target1) < threshold
            Dim success2 As Boolean = Math.Abs(resultprediction2.Score - target2) < threshold
            Dim success3 As Boolean = Math.Abs(resultprediction3.Score - target3) < threshold
            Dim success4 As Boolean = Math.Abs(resultprediction4.Score - target4) < threshold
            Dim iNbSuccess = 0
            If success1 Then iNbSuccess += 1
            If success2 Then iNbSuccess += 1
            If success3 Then iNbSuccess += 1
            If success4 Then iNbSuccess += 1

            Const format = "0.00"
            Console.WriteLine(
                SampleXORData.XOR1.Input1 & " XOR " & SampleXORData.XOR1.Input2 & " : " &
                resultprediction1.Score.ToString(format) & ", target:" & target1 &
                ", success: " & success1 & " (" & threshold.ToString(format) & ")")
            Console.WriteLine(
                SampleXORData.XOR2.Input1 & " XOR " & SampleXORData.XOR2.Input2 & " : " &
                resultprediction2.Score.ToString(format) & ", target:" & target2 &
                ", success: " & success2 & " (" & threshold.ToString(format) & ")")
            Console.WriteLine(
                SampleXORData.XOR3.Input1 & " XOR " & SampleXORData.XOR3.Input2 & " : " &
                resultprediction3.Score.ToString(format) & ", target:" & target3 &
                ", success: " & success3 & " (" & threshold.ToString(format) & ")")
            Console.WriteLine(
                SampleXORData.XOR4.Input1 & " XOR " & SampleXORData.XOR4.Input2 & " : " &
                resultprediction4.Score.ToString(format) & ", target:" & target4 &
                ", success: " & success4 & " (" & threshold.ToString(format) & ")")

            Return iNbSuccess

        End Function

    End Module

End Namespace