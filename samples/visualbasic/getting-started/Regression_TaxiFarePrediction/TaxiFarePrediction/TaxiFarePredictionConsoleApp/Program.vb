﻿
Imports System.Globalization
Imports System.IO
Imports Microsoft.ML
Imports Microsoft.ML.Data
Imports PLplot
Imports TaxiFarePrediction.Common
Imports TaxiFarePrediction.Regression_TaxiFarePrediction.DataStructures

Namespace Regression_TaxiFarePrediction

    Public Module Program

        Private Const datasetFile = "taxi-fare"
        Private Const datasetZip As String = datasetFile & ".zip"
        Private Const datasetUrl As String = "https://bit.ly/3qISgov"

        Private ReadOnly commonDatasetsRelativePath As String = "../../../../../../../../../datasets"
        Private ReadOnly commonDatasetsPath As String = GetAbsolutePath(commonDatasetsRelativePath)

        Private BaseDatasetsRelativePath As String = "../../../../Data"
        Private BaseModelsRelativePath As String = "../../../../MLModels"

        ' If args[0] == "svg" a vector-based chart will be created instead a .png chart
        Sub Main(args() As String)

            GetResult(BaseDatasetsRelativePath, commonDatasetsPath,
                BaseModelsRelativePath, args)

            Console.WriteLine("Press any key to exit..")
            Console.ReadLine()

        End Sub

        Public Function GetResult(
                myAssetsPath As String, myCommonDatasetsPath As String,
                myModelPath As String,
                Optional args() As String = Nothing,
                Optional isTest As Boolean = False) As Boolean

            Dim datasetFolder As String = myAssetsPath

            Dim TrainDataRelativePath As String = Path.Combine(myAssetsPath, "taxi-fare-train.csv")
            Dim TestDataRelativePath As String = Path.Combine(myAssetsPath, "taxi-fare-test.csv")

            Dim TrainDataPath As String = GetAbsolutePath(TrainDataRelativePath)
            Dim TestDataPath As String = GetAbsolutePath(TestDataRelativePath)

            'Dim fullDataSetFilePath As String = TrainDataRelativePath
            'GetDataSet(fullDataSetFilePath, datasetFolder, datasetUrl, datasetZip, commonDatasetsPath)
            Dim destFiles As New List(Of String) From {TrainDataRelativePath, TestDataRelativePath}
            DownloadBigFile(datasetFolder, datasetUrl, datasetZip, myCommonDatasetsPath, destFiles)

            ' Create ML Context with seed for repeteable/deterministic results
            Dim mlContext As New MLContext() 'seed:=0
            Dim modelPath = Path.GetFullPath(Path.Combine(myModelPath, "TaxiFareModel.zip"))

            ' Create, Train, Evaluate and Save a model
            Dim score# = 0
            BuildTrainEvaluateAndSaveModel(mlContext, TrainDataPath, TestDataPath, modelPath, score)

            ' Make a single test prediction loding the model from .ZIP file
            TestSinglePrediction(mlContext, modelPath)

            ' Paint regression distribution chart for a number of elements read from a Test DataSet file
            If Not isTest Then PlotRegressionChart(mlContext, TestDataPath, modelPath, 100, args)

            Dim scoreRounded = Math.Round(score, digits:=4) * 100
            Dim scoreExpected = 65
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
                TrainDataPath As String, TestDataPath As String,
                ModelPath As String, ByRef score#) As ITransformer

            ' STEP 1: Common data loading configuration
            Dim baseTrainingDataView As IDataView =
                mlContext.Data.LoadFromTextFile(Of TaxiTrip)(
                    TrainDataPath, hasHeader:=True, separatorChar:=","c)
            Dim testDataView As IDataView = mlContext.Data.LoadFromTextFile(Of TaxiTrip)(
                TestDataPath, hasHeader:=True, separatorChar:=","c)

            ' Sample code of removing extreme data like "outliers" for FareAmounts higher than $150
            '  and lower than $1 which can be error-data 
            Dim cnt = baseTrainingDataView.GetColumn(Of Single)(NameOf(TaxiTrip.FareAmount)).Count()
            Dim trainingDataView As IDataView = mlContext.Data.FilterRowsByColumn(
                baseTrainingDataView, NameOf(TaxiTrip.FareAmount), lowerBound:=1, upperBound:=150)
            Dim cnt2 = trainingDataView.GetColumn(Of Single)(NameOf(TaxiTrip.FareAmount)).Count()

            ' STEP 2: Common data process configuration with pipeline data transformations
            Dim dataProcessPipeline = mlContext.Transforms.CopyColumns(
                outputColumnName:="Label", inputColumnName:=NameOf(TaxiTrip.FareAmount)).
                Append(mlContext.Transforms.Categorical.OneHotEncoding(
                    outputColumnName:="VendorIdEncoded",
                    inputColumnName:=NameOf(TaxiTrip.VendorId))).
                Append(mlContext.Transforms.Categorical.OneHotEncoding(
                    outputColumnName:="RateCodeEncoded",
                    inputColumnName:=NameOf(TaxiTrip.RateCode))).
                Append(mlContext.Transforms.Categorical.OneHotEncoding(
                    outputColumnName:="PaymentTypeEncoded",
                    inputColumnName:=NameOf(TaxiTrip.PaymentType))).
                Append(mlContext.Transforms.NormalizeMeanVariance(
                    outputColumnName:=NameOf(TaxiTrip.PassengerCount))).
                Append(mlContext.Transforms.NormalizeMeanVariance(
                    outputColumnName:=NameOf(TaxiTrip.TripTime))).
                Append(mlContext.Transforms.NormalizeMeanVariance(
                    outputColumnName:=NameOf(TaxiTrip.TripDistance))).
                Append(mlContext.Transforms.Concatenate(
                    "Features", "VendorIdEncoded", "RateCodeEncoded", "PaymentTypeEncoded",
                    NameOf(TaxiTrip.PassengerCount),
                    NameOf(TaxiTrip.TripTime),
                    NameOf(TaxiTrip.TripDistance)))

            ' (OPTIONAL) Peek data (such as 5 records) in training DataView after applying
            '  the ProcessPipeline's transformations into "Features" 
            ConsoleHelper.PeekDataViewInConsole(mlContext, trainingDataView, dataProcessPipeline, 5)
            ConsoleHelper.PeekVectorColumnDataInConsole(
                mlContext, "Features", trainingDataView, dataProcessPipeline, 5)

            ' STEP 3: Set the training algorithm, then create and config the
            '  modelBuilder - Selected Trainer (SDCA Regression algorithm)                            
            Dim trainer = mlContext.Regression.Trainers.Sdca(
                labelColumnName:="Label", featureColumnName:="Features")
            Dim trainingPipeline = dataProcessPipeline.Append(trainer)

            ' STEP 4: Train the model fitting to the DataSet
            ' The pipeline is trained on the dataset that has been loaded and transformed
            Console.WriteLine("=============== Training the model ===============")
            Dim trainedModel = trainingPipeline.Fit(trainingDataView)

            ' STEP 5: Evaluate the model and show accuracy stats
            Console.WriteLine("===== Evaluating Model's accuracy with Test data =====")

            Dim predictions As IDataView = trainedModel.Transform(testDataView)
            Dim metrics = mlContext.Regression.Evaluate(
                predictions, labelColumnName:="Label", scoreColumnName:="Score")
            score = metrics.RSquared

            Common.ConsoleHelper.PrintRegressionMetrics(trainer.ToString(), metrics)

            ' STEP 6: Save/persist the trained model to a .ZIP file
            Dim directoryPath = IO.Path.GetDirectoryName(ModelPath)
            If Not IO.Directory.Exists(directoryPath) Then
                Dim di As New IO.DirectoryInfo(directoryPath)
                di.Create()
            End If
            mlContext.Model.Save(trainedModel, trainingDataView.Schema, ModelPath)

            Console.WriteLine("The model is saved to {0}", ModelPath)

            Return trainedModel

        End Function

        Private Sub TestSinglePrediction(mlContext As MLContext, ModelPath As String)

            ' Sample: 
            ' vendor_id,rate_code,passenger_count,trip_time_in_secs,trip_distance,payment_type,fare_amount
            '  VTS,1,1,1140,3.75,CRD,15.5

            Dim taxiTripSample = New TaxiTrip With {
                .VendorId = "VTS",
                .RateCode = "1",
                .PassengerCount = 1,
                .TripTime = 1140,
                .TripDistance = 3.75F,
                .PaymentType = "CRD",
                .FareAmount = 0
            }

            '''

            Dim modelInputSchema As DataViewSchema = Nothing
            Dim trainedModel As ITransformer = mlContext.Model.Load(ModelPath, modelInputSchema)

            ' Create prediction engine related to the loaded trained model
            Dim predEngine = mlContext.Model.CreatePredictionEngine(
                Of TaxiTrip, TaxiTripFarePrediction)(trainedModel)

            ' Score
            Dim resultprediction = predEngine.Predict(taxiTripSample)

            '''

            Console.WriteLine($"**********************************************************************")
            Console.WriteLine($"Predicted fare: {resultprediction.FareAmount:0.####}, actual fare: 15.5")
            Console.WriteLine($"**********************************************************************")

        End Sub

        Private Sub PlotRegressionChart(
                mlContext As MLContext, testDataSetPath As String, ModelPath As String,
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
                Dim testData = (New TaxiTripCsvReader).GetDataFromCsv(
                    testDataSetPath, totalNumber).ToList()

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

            ' Open Chart File In Microsoft Photos App (Or default app, like browser for .svg)

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

        Public Function GetDataFromCsv(
                dataLocation As String, numMaxRecords As Integer) As IEnumerable(Of TaxiTrip)

            Dim records As IEnumerable(Of TaxiTrip) =
                File.ReadAllLines(dataLocation).Skip(1).
                Select(Function(x) x.Split(","c)).
                Select(Function(x) New TaxiTrip With {
                .VendorId = x(0),
                .RateCode = x(1),
                .PassengerCount = Single.Parse(x(2), CultureInfo.InvariantCulture),
                .TripTime = Single.Parse(x(3), CultureInfo.InvariantCulture),
                .TripDistance = Single.Parse(x(4), CultureInfo.InvariantCulture),
                .PaymentType = x(5),
                .FareAmount = Single.Parse(x(6), CultureInfo.InvariantCulture)
            }).Take(numMaxRecords)

            Return records

        End Function

    End Class

End Namespace