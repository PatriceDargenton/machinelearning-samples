
Imports System.IO
Imports Microsoft.ML
Imports Microsoft.ML.Data
Imports PowerAnomalyDetection.PowerAnomalyDetection.DataStructures

Namespace PowerAnomalyDetection

    Public Class Program

        Shared Sub Main()

            Dim assetsRelativePath As String = "../../../Data"
            Dim assetsPath As String = GetAbsolutePath(assetsRelativePath)
            Dim baseModelsRelativePath As String = "../../../MLModels"
            'Dim modelRelativePath As String = Path.Combine(baseModelsRelativePath,
            '    "PowerAnomalyDetectionModel.zip")
            Dim modelPath As String = GetAbsolutePath(baseModelsRelativePath)

            GetResult(assetsPath, modelPath)

            Console.WriteLine(vbLf & "Press any key to exit")
            Console.Read()

        End Sub

        Public Shared Function GetResult(
                myAssetsPath As String, myModelPath As String,
                Optional isTest As Boolean = False) As Boolean

            Dim trainDataRelativePath As String = Path.Combine(myAssetsPath, "power-export_min.csv")
            Dim trainDataPath As String = GetAbsolutePath(trainDataRelativePath)
            trainDataPath = Path.GetFullPath(trainDataPath)

            Dim mlContext As New MLContext() 'seed:=0
            Dim modelPath As String = Path.Combine(myModelPath, "PowerAnomalyDetectionModel.zip")

            ' Load data
            Dim dataView = mlContext.Data.LoadFromTextFile(Of MeterData)(
                trainDataPath, separatorChar:=","c, hasHeader:=True)

            ' Transform options
            BuildTrainModel(modelPath, mlContext, dataView) ' using SsaSpikeEstimator

            Dim score# = 0
            DetectAnomalies(modelPath, mlContext, dataView, score)

            Dim scoreRounded = Math.Round(score, digits:=0)
            Dim scoreExpected = 2800
            Dim success = scoreRounded >= scoreExpected
            Console.WriteLine("Success: Score = " & scoreRounded & " >= " & scoreExpected & " : " & success)

            Return success

        End Function

        Public Shared Sub BuildTrainModel(myModelPath As String,
                mlContext As MLContext, dataView As IDataView)

            ' Configure the Estimator
            Const PValueSize As Integer = 30
            Const SeasonalitySize As Integer = 30
            Const TrainingSize As Integer = 90
            Const ConfidenceInterval As Integer = 98

            Dim outputColumnName As String = NameOf(SpikePrediction.Prediction)
            Dim inputColumnName As String = NameOf(MeterData.ConsumptionDiffNormalized)

            ' Note: The Warning is also present for the C # version
            Dim trainigPipeLine = mlContext.Transforms.DetectSpikeBySsa(
                outputColumnName, inputColumnName,
                confidence:=ConfidenceInterval,
                pvalueHistoryLength:=PValueSize,
                trainingWindowSize:=TrainingSize,
                seasonalityWindowSize:=SeasonalitySize)

            Dim trainedModel As ITransformer = trainigPipeLine.Fit(dataView)

            ' STEP 6: Save/persist the trained model to a .ZIP file
            Dim directoryPath = IO.Path.GetDirectoryName(myModelPath)
            If Not IO.Directory.Exists(directoryPath) Then
                Dim di As New IO.DirectoryInfo(directoryPath)
                di.Create()
            End If
            mlContext.Model.Save(trainedModel, dataView.Schema, myModelPath)
            Console.WriteLine("The model is saved to {0}", myModelPath)
            Console.WriteLine("")

        End Sub

        Public Shared Sub DetectAnomalies(myModelPath As String,
                mlContext As MLContext, dataView As IDataView, ByRef score#)

            Dim modelInputSchema As DataViewSchema = Nothing
            Dim trainedModel As ITransformer = mlContext.Model.Load(myModelPath, modelInputSchema)

            Dim transformedData = trainedModel.Transform(dataView)

            ' Getting the data of the newly created column as an IEnumerable
            Dim predictions As IEnumerable(Of SpikePrediction) =
                mlContext.Data.CreateEnumerable(Of SpikePrediction)(transformedData, False)

            Dim colCDN = dataView.GetColumn(Of Single)("ConsumptionDiffNormalized").ToArray()
            Dim colTime = dataView.GetColumn(Of DateTime)("time").ToArray()

            ' Output the input data and predictions
            Console.WriteLine("======Displaying anomalies in the Power meter data=========")
            Console.WriteLine("Date              " & vbTab &
                "ReadingDiff" & vbTab & "Alert" & vbTab & "Score" & vbTab & "P-Value")

            Dim i As Integer = 0
            For Each p In predictions
                If p.Prediction(0) = 1 Then
                    Console.BackgroundColor = ConsoleColor.DarkYellow
                    Console.ForegroundColor = ConsoleColor.Black
                    score = p.Prediction(1)
                End If
                Console.WriteLine(
                    "{0}" & vbTab & "{1:0.0000}" & vbTab & "{2:0.00}" & vbTab &
                    "{3:0.00}" & vbTab & "{4:0.00}",
                    colTime(i), colCDN(i), p.Prediction(0), p.Prediction(1), p.Prediction(2))
                Console.ResetColor()
                i += 1
            Next p

        End Sub

        Public Shared Function GetAbsolutePath(relativePath As String) As String
            Dim fullPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
            Return fullPath
        End Function

    End Class

End Namespace