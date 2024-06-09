
Imports System.IO
Imports System.Threading
Imports AdvancedTaxiFarePrediction.AdvancedTaxiFarePrediction.DataStructures
Imports AdvancedTaxiFarePrediction.Common
Imports Microsoft.ML
Imports Microsoft.ML.AutoML
Imports Microsoft.ML.Data
Imports PLplot

Namespace AdvancedTaxiFarePrediction

    Public Module Program

        Private Const datasetFile = "taxi-fare"
        Private Const datasetZip As String = datasetFile & ".zip"
        Private Const datasetUrl As String = "https://bit.ly/3qISgov"

        Private ReadOnly commonDatasetsRelativePath As String = "../../../../../../../../datasets"
        Private ReadOnly commonDatasetsPath As String = GetAbsolutePath(commonDatasetsRelativePath)

        Private ReadOnly BaseDatasetsRelativePath As String = "../../../Data"

        Private ReadOnly TrainDataFileName As String = "taxi-fare-train.csv"
        Private TrainDataView As IDataView = Nothing

        Private ReadOnly TestDataFileName As String = "taxi-fare-test.csv"
        Private TestDataView As IDataView = Nothing

        Private ReadOnly BaseModelsRelativePath As String = "../../../MLModels"
        Private ReadOnly ModelFileName As String = "TaxiFareModel.zip"

        Private ReadOnly LabelColumnName As String = "fare_amount"

        ' If args[0] == "svg" a vector-based chart will be created instead a .png chart
        Sub Main(args() As String)

            GetResult(BaseDatasetsRelativePath, commonDatasetsPath, BaseModelsRelativePath, args)

            Console.WriteLine("Press any key to exit..")
            Console.ReadLine()

        End Sub

        Public Function GetResult(
                myAssetsPath As String, myCommonDatasetsPath As String, myModelPath As String,
                Optional args() As String = Nothing,
                Optional isTest As Boolean = False) As Boolean

            Dim datasetFolder As String = myAssetsPath
            'Dim fullDataSetFilePath As String = TrainDataRelativePath
            'GetDataSet(fullDataSetFilePath, datasetFolder, datasetUrl, datasetZip, commonDatasetsPath)
            Dim TrainDataPath = Path.Combine(myAssetsPath, TrainDataFileName)
            Dim TestDataPath = Path.Combine(myAssetsPath, TestDataFileName)
            Dim ModelPath = Path.Combine(myModelPath, ModelFileName)
            Dim destFiles As New List(Of String) From {TrainDataPath, TestDataPath}
            DownloadBigFile(datasetFolder, datasetUrl, datasetZip, myCommonDatasetsPath, destFiles)

            Dim mlContext As New MLContext

            ' Infer columns in the dataset with AutoML
            Dim columnInference = InferColumns(mlContext, TrainDataPath)

            ' Load data from files using inferred columns
            LoadData(mlContext, columnInference, TrainDataPath, TestDataPath)

            ' Run an AutoML experiment on the dataset
            Dim score# = 0
            Dim experimentResult = RunAutoMLExperiment(mlContext, columnInference, score, isTest)

            ' Evaluate the model and print metrics
            EvaluateModel(mlContext, experimentResult.BestRun.Model, experimentResult.BestRun.TrainerName)

            ' Save / persist the best model to a.ZIP file
            SaveModel(mlContext, experimentResult.BestRun.Model, ModelPath)

            ' Make a single test prediction loading the model from .ZIP file
            TestSinglePrediction(mlContext, ModelPath)

            ' Paint regression distribution chart for a number of elements read from a Test DataSet file
            If Not isTest Then PlotRegressionChart(mlContext, TestDataPath, ModelPath, 100, args)

            ' Re-fit best pipeline on train and test data, to produce 
            '  a model that is trained on as much data as is available
            ' This is the final model that can be deployed to production
            Dim refitModel = RefitBestPipeline(mlContext, experimentResult, columnInference,
                TrainDataPath, TestDataPath)

            ' Save the re-fit model to a.ZIP file
            SaveModel(mlContext, refitModel, ModelPath)

            Dim scoreRounded = Math.Round(score, digits:=4) * 100
            Dim scoreExpected = 89 '94
            Dim success = scoreRounded >= scoreExpected
            Console.WriteLine("Success: Score = " & scoreRounded & " >= " & scoreExpected & " : " & success)

            Return success

        End Function

        'Public Sub GetDataSet(destinationFile As String,
        '        datasetFolder As String, dataSetUrl As String, dataSetZip As String,
        '        commonDatasetsPath As String)

        '    Dim zipPath = Path.Combine(datasetFolder, dataSetZip)
        '    Dim commonPath = Path.Combine(commonDatasetsPath, dataSetZip)

        '    If Not File.Exists(zipPath) AndAlso File.Exists(commonPath) Then
        '        IO.File.Copy(commonPath, zipPath)
        '    ElseIf File.Exists(zipPath) AndAlso Not File.Exists(commonPath) Then
        '        IO.File.Copy(zipPath, commonPath)
        '    End If

        '    If File.Exists(destinationFile) Then Exit Sub

        '    If Not File.Exists(zipPath) Then
        '        Console.WriteLine("====Downloading zip====")
        '        Using client = New MyWebClient
        '            ' The code below will download a dataset from a third-party,
        '            '  and may be governed by separate third-party terms
        '            ' By proceeding, you agree to those separate terms
        '            client.DownloadFile(dataSetUrl, zipPath)
        '        End Using
        '        Console.WriteLine("====Downloading is completed====")
        '        IO.File.Copy(zipPath, commonPath)
        '    End If

        '    If File.Exists(zipPath) Then
        '        Console.WriteLine("====Extracting zip====")
        '        Dim myFastZip As New FastZip()
        '        myFastZip.ExtractZip(zipPath, datasetFolder, fileFilter:=String.Empty)
        '        Console.WriteLine("====Extracting is completed====")
        '        If File.Exists(destinationFile) Then
        '            Console.WriteLine("====Extracting is OK====")
        '        Else
        '            Console.WriteLine("====Extracting: Fail!====")
        '            Stop
        '        End If
        '    End If

        'End Sub

        ''' <summary>
        ''' Infer columns in the dataset with AutoML
        ''' </summary>
        Private Function InferColumns(mlContext As MLContext, TrainDataPath As String) As ColumnInferenceResults
            ConsoleHelperAutoML.ConsoleWriteHeader("=============== Inferring columns in dataset ===============")
            Dim columnInference As ColumnInferenceResults =
                mlContext.Auto().InferColumns(TrainDataPath, LabelColumnName, groupColumns:=False)
            ConsoleHelperAutoML.Print(columnInference)
            Return columnInference
        End Function

        ''' <summary>
        ''' Load data from files using inferred columns
        ''' </summary>
        Private Sub LoadData(mlContext As MLContext, columnInference As ColumnInferenceResults,
                TrainDataPath As String, TestDataPath As String)
            Dim textLoader As TextLoader = mlContext.Data.CreateTextLoader(columnInference.TextLoaderOptions)
            TrainDataView = textLoader.Load(TrainDataPath)
            TestDataView = textLoader.Load(TestDataPath)
        End Sub

        Private Function RunAutoMLExperiment(mlContext As MLContext,
                columnInference As ColumnInferenceResults, ByRef score#, isTest As Boolean) As ExperimentResult(Of RegressionMetrics)

            ' STEP 1: Display first few rows of the training data
            ConsoleHelperAutoML.ShowDataViewInConsole(mlContext, TrainDataView)

            ' STEP 2: Build a pre-featurizer for use in the AutoML experiment
            ' (Internally, AutoML uses one or more train/validation data splits to 
            '  evaluate the models it produces. The pre-featurizer is fit only on the 
            '  training data split to produce a trained transform. Then, the trained transform 
            '  is applied to both the train and validation data splits.)
            Dim preFeaturizer As IEstimator(Of ITransformer) =
                mlContext.Transforms.Conversion.MapValue("is_cash",
                    {New KeyValuePair(Of String, Boolean)("CSH", True)}, "payment_type")

            ' STEP 3: Customize column information returned by InferColumns API
            Dim columnInformation As ColumnInformation = columnInference.ColumnInformation
            columnInformation.CategoricalColumnNames.Remove("payment_type")
            columnInformation.IgnoredColumnNames.Add("payment_type")

            ' STEP 4: Initialize a cancellation token source to stop the experiment.
            Dim cts = New CancellationTokenSource

            ' STEP 5: Initialize our user-defined progress handler that AutoML will 
            '  invoke after each model it produces and evaluates
            Dim progressHandler = New RegressionExperimentProgressHandler

            ' STEP 6: Create experiment settings
            Dim experimentSettings = CreateExperimentSettings(mlContext, cts)

            ' STEP 7: Run AutoML regression experiment
            Dim experiment = mlContext.Auto().CreateRegressionExperiment(experimentSettings)
            ConsoleHelperAutoML.ConsoleWriteHeader("=============== Running AutoML experiment ===============")
            Console.WriteLine($"Running AutoML regression experiment...")
            Dim stopwatch = System.Diagnostics.Stopwatch.StartNew()

            If isTest Then
                ' Cancel experiment after 30 sec.
                CancelExperiment(cts)
            Else
                ' Cancel experiment after the user presses any key
                CancelExperimentAfterAnyKeyPress(cts)
            End If

            Dim experimentResult As ExperimentResult(Of RegressionMetrics) =
                experiment.Execute(TrainDataView, columnInformation, preFeaturizer, progressHandler)
            Console.WriteLine($"{experimentResult.RunDetails.Count()} models were returned after {stopwatch.Elapsed.TotalSeconds:0.00} seconds{Environment.NewLine}")

            ' Print top models found by AutoML
            PrintTopModels(experimentResult, score)

            Return experimentResult

        End Function

        ''' <summary>
        ''' Create AutoML regression experiment settings
        ''' </summary>
        Private Function CreateExperimentSettings(mlContext As MLContext,
                cts As CancellationTokenSource) As RegressionExperimentSettings

            Dim experimentSettings = New RegressionExperimentSettings
            experimentSettings.MaxExperimentTimeInSeconds = 30 '3600
            experimentSettings.CancellationToken = cts.Token

            ' Set the metric that AutoML will try to optimize over the course of the experiment
            experimentSettings.OptimizingMetric = RegressionMetric.RootMeanSquaredError

            ' Set the cache directory to null.
            ' This will cause all models produced by AutoML to be kept in memory 
            '  instead of written to disk after each run, as AutoML is training
            ' (Please note: for an experiment on a large dataset, opting to keep all 
            '  models trained by AutoML in memory could cause your system to run out 
            '  of memory.)
            'experimentSettings.CacheDirectory = Nothing
            experimentSettings.CacheDirectoryName = Nothing

            ' Don't use LbfgsPoissonRegression and OnlineGradientDescent trainers during this experiment
            ' (These trainers sometimes underperform on this dataset.)
            experimentSettings.Trainers.Remove(RegressionTrainer.LbfgsPoissonRegression)
            'experimentSettings.Trainers.Remove(RegressionTrainer.OnlineGradientDescent)

            Return experimentSettings

        End Function

        ''' <summary>
        ''' Print top models from AutoML experiment
        ''' </summary>
        Private Sub PrintTopModels(experimentResult As ExperimentResult(Of RegressionMetrics), ByRef score#)

            ' Get top few runs ranked by root mean squared error
            Dim topRuns = experimentResult.RunDetails.Where(
                Function(r) r.ValidationMetrics IsNot Nothing AndAlso Not Double.IsNaN(
                    r.ValidationMetrics.RootMeanSquaredError)).
                OrderBy(Function(r) r.ValidationMetrics.RootMeanSquaredError).Take(3)

            Console.WriteLine("Top models ranked by root mean squared error --")
            ConsoleHelperAutoML.PrintRegressionMetricsHeader()
            For i = 0 To topRuns.Count() - 1
                Dim run = topRuns.ElementAt(i)
                ConsoleHelperAutoML.PrintIterationMetrics(i + 1, run.TrainerName,
                    run.ValidationMetrics, run.RuntimeInSeconds)
                If i = 0 Then score = run.ValidationMetrics.RSquared
            Next i

        End Sub

        ''' <summary>
        ''' Re-fit best pipeline on all available data
        ''' </summary>
        Private Function RefitBestPipeline(mlContext As MLContext,
                experimentResult As ExperimentResult(Of RegressionMetrics),
                columnInference As ColumnInferenceResults,
                TrainDataPath As String, TestDataPath As String) As ITransformer

            ConsoleHelperAutoML.ConsoleWriteHeader("=============== Re-fitting best pipeline ===============")
            Dim textLoader = mlContext.Data.CreateTextLoader(columnInference.TextLoaderOptions)
            Dim combinedDataView = textLoader.Load(New MultiFileSource(TrainDataPath, TestDataPath))
            Dim bestRun As RunDetail(Of RegressionMetrics) = experimentResult.BestRun
            Return bestRun.Estimator.Fit(combinedDataView)

        End Function

        ''' <summary>
        ''' Evaluate the model and print metrics
        ''' </summary>
        Private Sub EvaluateModel(mlContext As MLContext, model As ITransformer, trainerName As String)

            ConsoleHelperAutoML.ConsoleWriteHeader("===== Evaluating model's accuracy with test data =====")
            Dim predictions As IDataView = model.Transform(TestDataView)
            Dim metrics = mlContext.Regression.Evaluate(
                predictions, labelColumnName:=LabelColumnName, scoreColumnName:="Score")
            ConsoleHelperAutoML.PrintRegressionMetrics(trainerName, metrics)

        End Sub

        ''' <summary>
        ''' Save/persist the best model to a .ZIP file
        ''' </summary>
        Private Sub SaveModel(mlContext As MLContext, model As ITransformer, ModelPath As String)

            ConsoleHelperAutoML.ConsoleWriteHeader("=============== Saving the model ===============")
            Dim directoryPath = IO.Path.GetDirectoryName(ModelPath)
            If Not IO.Directory.Exists(directoryPath) Then
                Dim di As New IO.DirectoryInfo(directoryPath)
                di.Create()
            End If
            mlContext.Model.Save(model, TrainDataView.Schema, ModelPath)
            Console.WriteLine("The model is saved to {0}", ModelPath)

        End Sub

        Private Sub CancelExperimentAfterAnyKeyPress(cts As CancellationTokenSource)
            Task.Run(Sub()
                         Console.WriteLine($"Press any key to stop the experiment run...")
                         Console.ReadKey()
                         cts.Cancel()
                     End Sub)
        End Sub

        Private Sub CancelExperiment(cts As CancellationTokenSource)
            Task.Run(Sub()
                         Thread.Sleep(30 * 1000) ' Wait 30 sec.
                         cts.Cancel()
                     End Sub)
        End Sub

        Private Sub TestSinglePrediction(mlContext As MLContext, ModelPath As String)

            ConsoleHelperAutoML.ConsoleWriteHeader("=============== Testing prediction engine ===============")

            ' Sample: 
            ' vendor_id,rate_code,passenger_count,trip_time_in_secs,trip_distance,payment_type,fare_amount
            '  VTS,1,1,1140,3.75,CRD,15.5

            Dim taxiTripSample = New TaxiTrip With {
                .VendorId = "VTS",
                .RateCode = 1,
                .PassengerCount = 1,
                .TripTime = 1140,
                .TripDistance = 3.75F,
                .PaymentType = "CRD",
                .FareAmount = 0
            }

            Dim modelInputSchema As DataViewSchema = Nothing
            Dim trainedModel As ITransformer = mlContext.Model.Load(ModelPath, modelInputSchema)

            ' Create prediction engine related to the loaded trained model
            Dim predEngine = mlContext.Model.CreatePredictionEngine(
                Of TaxiTrip, TaxiTripFarePrediction)(trainedModel)

            ' Score
            Dim predictedResult = predEngine.Predict(taxiTripSample)

            Console.WriteLine($"**********************************************************************")
            Console.WriteLine($"Predicted fare: {predictedResult.FareAmount:0.####}, actual fare: 15.5")
            Console.WriteLine($"**********************************************************************")

        End Sub

        Private Sub PlotRegressionChart(mlContext As MLContext,
                testDataSetPath As String, ModelPath As String,
                numberOfRecordsToRead As Integer, args() As String)

            Dim trainedModel As ITransformer
            Using stream = New FileStream(ModelPath, FileMode.Open, FileAccess.Read, FileShare.Read)
                Dim modelInputSchema As DataViewSchema = Nothing
                trainedModel = mlContext.Model.Load(stream, modelInputSchema)
            End Using

            ' Create prediction engine related to the loaded trained model
            Dim predFunction = mlContext.Model.CreatePredictionEngine(
                Of TaxiTrip, TaxiTripFarePrediction)(trainedModel)

            Dim chartFileName As String = ""

            ' https://github.com/surban/PLplotNet/issues/2
            ' System.TypeInitializationException HResult=0x80131534
            ' Message=The type initializer for 'PLplot.Native' threw an exception.
            ' InvalidOperationException: Cannot find support PLplot support files in System.String[].
            ' -> Fix: .NET 5.0 -> .NET Core 2.1 or:
            ' -> Fix: <Target Name="CopyFiles" AfterTargets="Build"> in .vbproj
            ' https://github.com/surban/PLplotNet/issues/2#issuecomment-1006874961
            Using pl = New PLStream
                ' Use SVG backend and write to SineWaves.svg in current directory
                If args.Length = 1 AndAlso args(0) = "svg" Then
                    pl.sdev("svg")
                    chartFileName = "TaxiRegressionDistribution.svg"
                    pl.sfnam(chartFileName)
                Else
                    pl.sdev("pngcairo")
                    chartFileName = "TaxiRegressionDistribution.png"
                    pl.sfnam(chartFileName)
                End If

                ' Use white background with black foreground
                pl.spal0("cmap0_alternate.pal")

                ' Initialize plplot
                pl.init()

                ' Set axis limits
                Const xMinLimit As Integer = 0
                Const xMaxLimit As Integer = 35 ' Rides larger than $35 are not shown in the chart
                Const yMinLimit As Integer = 0
                Const yMaxLimit As Integer = 35 ' Rides larger than $35 are not shown in the chart
                pl.env(xMinLimit, xMaxLimit, yMinLimit, yMaxLimit,
                    AxesScale.Independent, AxisBox.BoxTicksLabelsAxes)

                ' Set scaling for mail title text 125% size of default
                pl.schr(0, 1.25)

                ' The main title
                pl.lab("Measured", "Predicted", "Distribution of Taxi Fare Prediction")

                ' Plot using different colors
                '  see http://plplot.sourceforge.net/examples.php?demo=02 for palette indices
                pl.col0(1)

                Dim totalNumber As Integer = numberOfRecordsToRead
                Dim testData = TaxiTripCsvReader.GetDataFromCsv(testDataSetPath, totalNumber).ToList()

                ' This code is the symbol to paint
                Dim code As Char = ChrW(9)

                ' Plot using other color
                'pl.col0(9); //Light Green
                'pl.col0(4); //Red
                pl.col0(2) 'Blue

                Dim yTotal As Double = 0
                Dim xTotal As Double = 0
                Dim xyMultiTotal As Double = 0
                Dim xSquareTotal As Double = 0

                For i As Integer = 0 To testData.Count - 1
                    Dim x = New Double(0) {}
                    Dim y = New Double(0) {}

                    ' Make Prediction
                    Dim FarePrediction = predFunction.Predict(testData(i))

                    x(0) = testData(i).FareAmount
                    y(0) = FarePrediction.FareAmount

                    ' Paint a dot
                    pl.poin(x, y, code)

                    xTotal += x(0)
                    yTotal += y(0)

                    Dim multi As Double = x(0) * y(0)
                    xyMultiTotal += multi

                    Dim xSquare As Double = x(0) * x(0)
                    xSquareTotal += xSquare

                    Dim ySquare As Double = y(0) * y(0)

                    Console.WriteLine($"-------------------------------------------------")
                    Console.WriteLine($"Predicted : {FarePrediction.FareAmount}")
                    Console.WriteLine($"Actual:    {testData(i).FareAmount}")
                    Console.WriteLine($"-------------------------------------------------")
                Next i

                ' Regression Line calculation explanation:
                ' https://www.khanacademy.org/math/statistics-probability/describing-relationships-quantitative-data/more-on-regression/v/regression-line-example

                Dim minY As Double = yTotal / totalNumber
                Dim minX As Double = xTotal / totalNumber
                Dim minXY As Double = xyMultiTotal / totalNumber
                Dim minXsquare As Double = xSquareTotal / totalNumber

                Dim m As Double = ((minX * minY) - minXY) / ((minX * minX) - minXsquare)

                Dim b As Double = minY - (m * minX)

                ' Generic function for Y for the regression line
                ' y = (m * x) + b;

                Dim x1 As Double = 1

                ' Function for Y1 in the line
                Dim y1 As Double = (m * x1) + b

                Dim x2 As Double = 39

                ' Function for Y2 in the line
                Dim y2 As Double = (m * x2) + b

                Dim xArray = New Double(1) {}
                Dim yArray = New Double(1) {}
                xArray(0) = x1
                yArray(0) = y1
                xArray(1) = x2
                yArray(1) = y2

                pl.col0(4)
                pl.line(xArray, yArray)

                ' End page (writes output to disk)
                pl.eop()

                ' Output version of PLplot
                Dim verText As String = ""
                pl.gver(verText)
                Console.WriteLine("PLplot version " & verText)

            End Using ' The pl object is disposed here

            ' Open chart file in Microsoft Photos App (or default app for .svg or .png, like browser)

            Console.WriteLine("Showing chart...")
            Dim p = New Process
            Dim chartFileNamePath As String = ".\" & chartFileName
            p.StartInfo = New ProcessStartInfo(chartFileNamePath) With {.UseShellExecute = True}
            p.Start()

        End Sub

        Public Function GetAbsolutePath(relativePath As String) As String
            Dim fullPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
            Return fullPath
        End Function

    End Module

    Public Class TaxiTripCsvReader

        Public Shared Function GetDataFromCsv(dataLocation As String,
                numMaxRecords As Integer) As IEnumerable(Of TaxiTrip)

            Dim records As IEnumerable(Of TaxiTrip) =
                File.ReadAllLines(dataLocation).Skip(1).
                    Select(Function(x) x.Split(","c)).Select(Function(x) New TaxiTrip With {
                .VendorId = x(0),
                .RateCode = Single.Parse(x(1)),
                .PassengerCount = Single.Parse(x(2)),
                .TripTime = Single.Parse(x(3)),
                .TripDistance = Single.Parse(x(4)),
                .PaymentType = x(5),
                .FareAmount = Single.Parse(x(6))
            }).Take(numMaxRecords)

            Return records

        End Function

    End Class

End Namespace