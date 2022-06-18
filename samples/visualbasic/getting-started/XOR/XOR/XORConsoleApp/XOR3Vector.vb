
Imports System.IO
Imports Microsoft.ML
Imports Microsoft.ML.Data
Imports System.Runtime.InteropServices
Imports XORApp.Common

Namespace XORApp

    Public Module XOR3Vector

        Public Class XOR3Data

            <LoadColumn(0, 5)>
            <VectorType(6)>
            Public Input As Single()

            <LoadColumn(6)>
            Public Output1 As Single

            <LoadColumn(7)>
            Public Output2 As Single

            <LoadColumn(8)>
            Public Output3 As Single

            ' ML.NET does not yet support multi-target regression
            ' (only via TensorFlow and Python)
            ' https://github.com/dotnet/machinelearning/issues/2134
            '<LoadColumn(6,8)>
            '<VectorType(3)>
            'Public Output As Single()

            Public Sub New(input1 As Single, input2 As Single, input3 As Single, input4 As Single, input5 As Single, input6 As Single, output1 As Single, output2 As Single, output3 As Single)
                Input = New Single(5) {}
                Input(0) = input1
                Input(1) = input2
                Input(2) = input3
                Input(3) = input4
                Input(4) = input5
                Input(5) = input6
                Me.Output1 = output1
                Me.Output2 = output2
                Me.Output3 = output3
            End Sub

        End Class

        Public Class XOR3CsvReader

            Public Shared Function GetDataFromCsv(
                    dataLocation As String, numMaxRecords As Integer) As IEnumerable(Of XOR3Data)

                Dim records As IEnumerable(Of XOR3Data) =
                    File.ReadAllLines(dataLocation).
                        Skip(1).
                        Select(Function(x) x.Split(vbTab)).
                        Select(Function(x) New XOR3Data(
                            Single.Parse(x(0)), Single.Parse(x(1)), Single.Parse(x(2)),
                            Single.Parse(x(3)), Single.Parse(x(4)), Single.Parse(x(5)),
                            Single.Parse(x(6)), Single.Parse(x(7)), Single.Parse(x(8)))).
                        Take(numMaxRecords)

                Return records

            End Function

        End Class

        Public Class XOR3Prediction
            Public Score1 As Single
            Public Score2 As Single
            Public Score3 As Single
        End Class

        Public Sub TrainFromFile(mlContext As MLContext,
                ModelPath1Zip As String, ModelPath2Zip As String, ModelPath3Zip As String,
                TrainDataPath As String,
                ByRef iRowCount As Integer, ByRef samples As List(Of XOR3Data))

            ' STEP 1: Common data loading configuration
            Dim iRowCountMin = 64
            Dim samplesMin = XOR3CsvReader.GetDataFromCsv(TrainDataPath, iRowCountMin).ToList()
            ' Repeat 1: no repeat, same size, Repeat 2: one repeat, double size
            ' minimal set: Repeat 6
            samples = Enumerable.Repeat(samplesMin, 6).SelectMany(Function(a) a).ToList()
            iRowCount = samples.Count ' 384 = 6 * 64 lines
            Dim trainingDataView = mlContext.Data.LoadFromEnumerable(Of XOR3Data)(samples)
            Dim testDataView = mlContext.Data.LoadFromEnumerable(Of XOR3Data)(samples)

            ' STEP 2: Common data process configuration with pipeline data transformations
            Dim dataProcessPipeline = mlContext.Transforms.Concatenate("Features",
                NameOf(XOR3Data.Input),
                NameOf(XOR3Data.Input),
                NameOf(XOR3Data.Input),
                NameOf(XOR3Data.Input),
                NameOf(XOR3Data.Input),
                NameOf(XOR3Data.Input)).AppendCacheCheckpoint(mlContext)

            ' (Optional) Peek data in training DataView after applying the ProcessPipeline's transformations  
            Const iNbSamples% = 17
            ConsoleHelper.PeekDataViewInConsole(mlContext, trainingDataView,
                dataProcessPipeline, iNbSamples)
            ConsoleHelper.PeekVectorColumnDataInConsole(mlContext, "Features", trainingDataView,
                dataProcessPipeline, iNbSamples)

            Const scoreColumnName = "Score"

            Const labelColumnName1 = "Output1"
            Const outputColumnName1 = "Score1"
            Regression(mlContext, dataProcessPipeline, trainingDataView, testDataView,
                labelColumnName1, outputColumnName1, scoreColumnName, ModelPath1Zip)

            Const labelColumnName2 = "Output2"
            Const outputColumnName2 = "Score2"
            Regression(mlContext, dataProcessPipeline, trainingDataView, testDataView,
                labelColumnName2, outputColumnName2, scoreColumnName, ModelPath2Zip)

            Const labelColumnName3 = "Output3"
            Const outputColumnName3 = "Score3"
            Regression(mlContext, dataProcessPipeline, trainingDataView, testDataView,
                labelColumnName3, outputColumnName3, scoreColumnName, ModelPath3Zip)

        End Sub

        Private Sub Regression(mlContext As MLContext,
                dataProcessPipeline As EstimatorChain(Of ColumnConcatenatingTransformer),
                trainingDataView As IDataView, testDataView As IDataView,
                labelColumnName As String, outputColumnName As String,
                scoreColumnName As String, ModelPathZip As String)

            ' STEP 3: Set the training algorithm, then append the trainer to the pipeline  
            Dim trainer = mlContext.Regression.Trainers.FastTree(
                labelColumnName:=labelColumnName, featureColumnName:="Features",
                learningRate:=0.2) ' min: 0.03, max: 0.9

            Dim trainingPipeline = dataProcessPipeline.Append(trainer).Append(
                mlContext.Transforms.CopyColumns(
                    outputColumnName:=outputColumnName, inputColumnName:=scoreColumnName))

            ' STEP 4: Train the model fitting to the DataSet
            Console.WriteLine("=============== Training the model ===============")
            Dim trainedModel As ITransformer = trainingPipeline.Fit(trainingDataView)

            ' STEP 5: Evaluate the model and show accuracy stats
            Console.WriteLine("===== Evaluating Model's accuracy with Test data =====")
            Dim predictions = trainedModel.Transform(testDataView)
            Dim metrics = mlContext.Regression.Evaluate(predictions,
                labelColumnName:=labelColumnName, scoreColumnName:=scoreColumnName)
            ConsoleHelper.PrintRegressionMetrics(trainer.ToString(), metrics)

            ' STEP 6: Save/persist the trained model to a .ZIP file
            Dim directoryPath = Path.GetDirectoryName(ModelPathZip)
            If Not Directory.Exists(directoryPath) Then
                Dim di As New DirectoryInfo(directoryPath)
                di.Create()
            End If
            mlContext.Model.Save(trainedModel, trainingDataView.Schema, ModelPathZip)
            Console.WriteLine("The model is saved to {0}", Path.GetFullPath(ModelPathZip))

        End Sub

        Public Function TestSomePredictions(mlContext As MLContext,
                ModelPath1Zip As String, ModelPath2Zip As String, ModelPath3Zip As String,
                iRowCount As Integer, samples As List(Of XOR3Data)) As Integer

            ' Test Classification Predictions with some hard-coded samples 
            Dim modelInputSchema1 As DataViewSchema = Nothing
            Dim trainedModel1 As ITransformer = mlContext.Model.Load(ModelPath1Zip, modelInputSchema1)
            Dim modelInputSchema2 As DataViewSchema = Nothing
            Dim trainedModel2 As ITransformer = mlContext.Model.Load(ModelPath2Zip, modelInputSchema2)
            Dim modelInputSchema3 As DataViewSchema = Nothing
            Dim trainedModel3 As ITransformer = mlContext.Model.Load(ModelPath3Zip, modelInputSchema3)

            ' Create prediction engine related to the loaded trained model
            Dim predEngine1 = mlContext.Model.CreatePredictionEngine(Of XOR3Data, XOR3Prediction)(trainedModel1)
            Dim predEngine2 = mlContext.Model.CreatePredictionEngine(Of XOR3Data, XOR3Prediction)(trainedModel2)
            Dim predEngine3 = mlContext.Model.CreatePredictionEngine(Of XOR3Data, XOR3Prediction)(trainedModel3)

            Console.WriteLine("=====Predicting using model====")

            Dim resultPreda = New List(Of XOR3Prediction)()
            Dim resultPredb = New List(Of XOR3Prediction)()
            Dim resultPredc = New List(Of XOR3Prediction)()
            Dim expecteda = New List(Of Boolean)()
            Dim expectedb = New List(Of Boolean)()
            Dim expectedc = New List(Of Boolean)()
            Dim iNbSamples = iRowCount
            Dim targeta = New Single(iNbSamples - 1) {}
            Dim targetb = New Single(iNbSamples - 1) {}
            Dim targetc = New Single(iNbSamples - 1) {}
            Dim successa = New Boolean(iNbSamples - 1) {}
            Dim successb = New Boolean(iNbSamples - 1) {}
            Dim successc = New Boolean(iNbSamples - 1) {}
            Dim iNumSample = 0
            Const threshold = 0.05F
            Const format = "0.00"
            Dim iNbSuccess = 0
            Dim iNbSuccessExpected = 0
            Console.WriteLine("Treshold:" & threshold.ToString(format))
            For Each d In samples
                resultPreda.Add(predEngine1.Predict(d))
                resultPredb.Add(predEngine2.Predict(d))
                resultPredc.Add(predEngine3.Predict(d))
                ' XOR : ^
                Dim d1 = Convert.ToBoolean(d.Input(0))
                Dim d2 = Convert.ToBoolean(d.Input(1))
                Dim d3 = Convert.ToBoolean(d.Input(2))
                Dim d4 = Convert.ToBoolean(d.Input(3))
                Dim d5 = Convert.ToBoolean(d.Input(4))
                Dim d6 = Convert.ToBoolean(d.Input(5))
                Dim bXOR1 = d1 Xor d2
                Dim bXOR2 = d3 Xor d4
                Dim bXOR3 = d5 Xor d6
                expecteda.Add(bXOR1)
                expectedb.Add(bXOR2)
                expectedc.Add(bXOR3)
                targeta(iNumSample) = 0
                If expecteda(iNumSample) Then targeta(iNumSample) = 1
                targetb(iNumSample) = 0
                If expectedb(iNumSample) Then targetb(iNumSample) = 1
                targetc(iNumSample) = 0
                If expectedc(iNumSample) Then targetc(iNumSample) = 1
                Dim dev1 = Math.Abs(resultPreda(iNumSample).Score1 - targeta(iNumSample))
                Dim dev2 = Math.Abs(resultPredb(iNumSample).Score2 - targetb(iNumSample))
                Dim dev3 = Math.Abs(resultPredc(iNumSample).Score3 - targetc(iNumSample))
                Dim s1 = dev1 < threshold
                Dim s2 = dev2 < threshold
                Dim s3 = dev3 < threshold
                successa(iNumSample) = s1
                successb(iNumSample) = s2
                successc(iNumSample) = s3
                iNbSuccess += If(s1, 1, 0)
                iNbSuccess += If(s2, 1, 0)
                iNbSuccess += If(s3, 1, 0)
                iNbSuccessExpected += 3

                Console.WriteLine("Sample n°" & (iNumSample + 1).ToString("00") & " : " & d.Input(0) & " XOR " & d.Input(1) & " : " & resultPreda(iNumSample).Score1.ToString(format) & ", target:" & targeta(iNumSample) & ", success: " & successa(iNumSample) & ", " & d.Input(2) & " XOR " & d.Input(3) & " : " & resultPredb(iNumSample).Score2.ToString(format) & ", target:" & targetb(iNumSample) & ", success: " & successb(iNumSample) & ", " & d.Input(4) & " XOR " & d.Input(5) & " : " & resultPredc(iNumSample).Score3.ToString(format) & ", target:" & targetc(iNumSample) & ", success: " & successc(iNumSample) & ", " & iNbSuccess.ToString("00") & "/" & iNbSuccessExpected.ToString("00"))

                iNumSample += 1
            Next

            Return iNbSuccess

        End Function

    End Module

End Namespace