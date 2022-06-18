
Imports System.IO
Imports Microsoft.ML
Imports Microsoft.ML.AutoML
Imports Microsoft.ML.Data
Imports PLplot
Imports TaxiFarePrediction2.Common ' MyWebClient
Imports TaxiFarePrediction2.TaxiFarePrediction2.DataStructures

Namespace TaxiFarePrediction2

    Public Module Program

        Private Const datasetFile = "taxi-fare"
        Private Const datasetZip As String = datasetFile & ".zip"
        Private Const datasetUrl As String = "https://bit.ly/3qISgov"

        Private ReadOnly commonDatasetsRelativePath As String = "../../../../../../../../datasets"
        Private ReadOnly commonDatasetsPath As String = GetAbsolutePath(commonDatasetsRelativePath)

        Private ReadOnly BaseDatasetsRelativePath As String = "../../../Data" ' Data
        Private ReadOnly BaseModelsRelativePath As String = "../../../MLModels"

        Private ReadOnly LabelColumnName As String = "FareAmount"
        Private ReadOnly ExperimentTime As UInteger = 60

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
            Dim TrainDataPath = GetAbsolutePath(Path.Combine(datasetFolder, "taxi-fare-train.csv"))
            Dim TestDataPath = GetAbsolutePath(Path.Combine(datasetFolder, "taxi-fare-test.csv"))
            Dim destFiles As New List(Of String) From {TrainDataPath, TestDataPath}
            DownloadBigFile(datasetFolder, datasetUrl, datasetZip, myCommonDatasetsPath, destFiles)

            Dim mlContext As New MLContext

            ' Create, train, evaluate and save a model
            Dim score# = 0
            Dim ModelPath = GetAbsolutePath(Path.Combine(myModelPath, "TaxiFareModel.zip"))
            BuildTrainEvaluateAndSaveModel(mlContext,
                TrainDataPath, TestDataPath, ModelPath, score)

            ' Make a single test prediction loading the model from .ZIP file
            TestSinglePrediction(mlContext, ModelPath)

            ' Paint regression distribution chart for a number of elements read from a Test DataSet file
            If Not isTest Then PlotRegressionChart(mlContext, TestDataPath, ModelPath, 100, args)

            Dim scoreRounded = Math.Round(score, digits:=4) * 100
            Dim scoreExpected = 94
            Dim success = scoreRounded >= scoreExpected
            Console.WriteLine("Success: Score = " & scoreRounded.ToString("0.00") & " >= " & scoreExpected & " : " & success)

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

        Private Function BuildTrainEvaluateAndSaveModel(mlContext As MLContext,
                TrainDataPath As String, TestDataPath As String, ModelPath As String,
                ByRef score#) As ITransformer

            ' STEP 1: Common data loading configuration
            Dim trainingDataView As IDataView = mlContext.Data.LoadFromTextFile(Of TaxiTrip)(
                TrainDataPath, hasHeader:=True, separatorChar:=","c)
            Dim testDataView As IDataView = mlContext.Data.LoadFromTextFile(Of TaxiTrip)(
                TestDataPath, hasHeader:=True, separatorChar:=","c)

            ' STEP 2: Display first few rows of the training data
            ConsoleHelperAutoML.ShowDataViewInConsole(mlContext, trainingDataView)

            ' STEP 3: Initialize our user-defined progress handler that AutoML will 
            '  invoke after each model it produces and evaluates
            Dim progressHandler = New RegressionExperimentProgressHandler

            ' STEP 4: Run AutoML regression experiment
            ConsoleHelperAutoML.ConsoleWriteHeader("=============== Training the model ===============")
            Console.WriteLine($"Running AutoML regression experiment for {ExperimentTime} seconds...")
            Dim experimentResult As ExperimentResult(Of RegressionMetrics) =
                mlContext.Auto().CreateRegressionExperiment(ExperimentTime).Execute(
                    trainingDataView, LabelColumnName, progressHandler:=progressHandler)

            ' Print top models found by AutoML
            Console.WriteLine()
            PrintTopModels(experimentResult, score)

            ' STEP 5: Evaluate the model and print metrics
            ConsoleHelperAutoML.ConsoleWriteHeader("===== Evaluating model's accuracy with test data =====")
            Dim best As RunDetail(Of RegressionMetrics) = experimentResult.BestRun
            Dim trainedModel As ITransformer = best.Model
            Dim predictions As IDataView = trainedModel.Transform(testDataView)
            Dim metrics = mlContext.Regression.Evaluate(predictions,
                labelColumnName:=LabelColumnName, scoreColumnName:="Score")

            ' Print metrics from top model
            ConsoleHelperAutoML.PrintRegressionMetrics(best.TrainerName, metrics)

            ' STEP 6: Save/persist the trained model to a .ZIP file
            Dim directoryPath = Path.GetDirectoryName(ModelPath)
            If Not Directory.Exists(directoryPath) Then
                Dim di As New DirectoryInfo(directoryPath)
                di.Create()
            End If
            mlContext.Model.Save(trainedModel, trainingDataView.Schema, ModelPath)

            Console.WriteLine("The model is saved to {0}", ModelPath)

            Return trainedModel

        End Function

        Private Sub TestSinglePrediction(mlContext As MLContext, ModelPath As String)

            ConsoleHelperAutoML.ConsoleWriteHeader("=============== Testing prediction engine ===============")

            ' Sample: 
            ' vendor_id,rate_code,passenger_count,trip_time_in_secs,trip_distance,payment_type,fare_amount
            ' VTS,1,1,1140,3.75,CRD,15.5

            Dim taxiTripSample = New TaxiTrip With {
                .VendorId = "VTS",
                .RateCode = "1",
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
                ' see http://plplot.sourceforge.net/examples.php?demo=02 for palette indices
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

        ''' <summary>
        ''' Print top models from AutoML experiment
        ''' </summary>
        Private Sub PrintTopModels(
                experimentResult As ExperimentResult(Of RegressionMetrics), ByRef score#)

            ' Get top few runs ranked by R-Squared
            ' R-Squared is a metric to maximize, so OrderByDescending() is correct
            ' For RMSE and other regression metrics, OrderByAscending() is correct
            Dim topRuns = experimentResult.RunDetails.Where(
                Function(r) r.ValidationMetrics IsNot Nothing AndAlso
                    Not Double.IsNaN(r.ValidationMetrics.RSquared)).
                    OrderByDescending(Function(r) r.ValidationMetrics.RSquared).Take(3)

            Console.WriteLine("Top models ranked by R-Squared --")
            ConsoleHelperAutoML.PrintRegressionMetricsHeader()
            For i = 0 To topRuns.Count() - 1
                Dim run = topRuns.ElementAt(i)
                ConsoleHelperAutoML.PrintIterationMetrics(i + 1, run.TrainerName, run.ValidationMetrics,
                                                    run.RuntimeInSeconds)
                If i = 0 Then score = run.ValidationMetrics.RSquared
            Next i

        End Sub

    End Module

    Public Class TaxiTripCsvReader

        Public Shared Function GetDataFromCsv(dataLocation As String,
                numMaxRecords As Integer) As IEnumerable(Of TaxiTrip)

            Dim records As IEnumerable(Of TaxiTrip) = File.ReadAllLines(dataLocation).Skip(1).
                Select(Function(x) x.Split(","c)).Select(Function(x) New TaxiTrip With {
                .VendorId = x(0),
                .RateCode = x(1),
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