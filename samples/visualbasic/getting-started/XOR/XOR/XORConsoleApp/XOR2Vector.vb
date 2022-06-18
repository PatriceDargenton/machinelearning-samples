
Imports System.IO
Imports Microsoft.ML
Imports Microsoft.ML.Data
Imports System.Runtime.InteropServices
Imports XORApp.Common

Namespace XORApp

    Public Module XOR2Vector

        Public Class XOR2Data

            <LoadColumn(0, 3)>
            <VectorType(4)>
            Public Input As Single()

            <LoadColumn(4)>
            Public Output1 As Single

            <LoadColumn(5)>
            Public Output2 As Single

            ' ML.NET does not yet support multi-target regression
            ' (only via TensorFlow and Python)
            ' https://github.com/dotnet/machinelearning/issues/2134
            '<LoadColumn(4,5)>
            '<VectorType(2)>
            'Public Output As Single()

            Public Sub New(
                    input1 As Single, input2 As Single, input3 As Single, input4 As Single,
                    output1 As Single, output2 As Single)

                Me.Input = New Single(3) {}
                Me.Input(0) = input1
                Me.Input(1) = input2
                Me.Input(2) = input3
                Me.Input(3) = input4
                Me.Output1 = output1
                Me.Output2 = output2

            End Sub

        End Class

        Public Class XOR2Prediction
            Public Score1 As Single
            Public Score2 As Single
        End Class

        Public Class SampleXOR2Data

            Public Const iNbSamples As Integer = 16

            Friend Shared ReadOnly XOR01 As New XOR2Data(1.0F, 0.0F, 1.0F, 0.0F, 1.0F, 1.0F)
            Friend Shared ReadOnly XOR02 As New XOR2Data(1.0F, 0.0F, 0.0F, 0.0F, 1.0F, 0.0F)
            Friend Shared ReadOnly XOR03 As New XOR2Data(1.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F)
            Friend Shared ReadOnly XOR04 As New XOR2Data(1.0F, 0.0F, 1.0F, 1.0F, 1.0F, 0.0F)

            Friend Shared ReadOnly XOR05 As New XOR2Data(0.0F, 0.0F, 1.0F, 0.0F, 0.0F, 1.0F)
            Friend Shared ReadOnly XOR06 As New XOR2Data(0.0F, 0.0F, 0.0F, 0.0F, 0.0F, 0.0F)
            Friend Shared ReadOnly XOR07 As New XOR2Data(0.0F, 0.0F, 0.0F, 1.0F, 0.0F, 1.0F)
            Friend Shared ReadOnly XOR08 As New XOR2Data(0.0F, 0.0F, 1.0F, 1.0F, 0.0F, 0.0F)

            Friend Shared ReadOnly XOR09 As New XOR2Data(0.0F, 1.0F, 1.0F, 0.0F, 1.0F, 1.0F)
            Friend Shared ReadOnly XOR10 As New XOR2Data(0.0F, 1.0F, 0.0F, 0.0F, 1.0F, 0.0F)
            Friend Shared ReadOnly XOR11 As New XOR2Data(0.0F, 1.0F, 0.0F, 1.0F, 1.0F, 1.0F)
            Friend Shared ReadOnly XOR12 As New XOR2Data(0.0F, 1.0F, 1.0F, 1.0F, 1.0F, 0.0F)

            Friend Shared ReadOnly XOR13 As New XOR2Data(1.0F, 1.0F, 1.0F, 0.0F, 0.0F, 1.0F)
            Friend Shared ReadOnly XOR14 As New XOR2Data(1.0F, 1.0F, 0.0F, 0.0F, 0.0F, 0.0F)
            Friend Shared ReadOnly XOR15 As New XOR2Data(1.0F, 1.0F, 0.0F, 1.0F, 0.0F, 1.0F)
            Friend Shared ReadOnly XOR16 As New XOR2Data(1.0F, 1.0F, 1.0F, 1.0F, 0.0F, 0.0F)

        End Class

        Public Function LoadData() As List(Of XOR2Data)

            ' STEP 1: Common data loading configuration
            ' Récup depuis : https://stackoverflow.com/questions/53472759/why-does-this-ml-net-code-fail-to-predict-the-correct-output
            Dim data = New List(Of XOR2Data) From {
                SampleXOR2Data.XOR01,
                SampleXOR2Data.XOR02,
                SampleXOR2Data.XOR03,
                SampleXOR2Data.XOR04,
                SampleXOR2Data.XOR05,
                SampleXOR2Data.XOR06,
                SampleXOR2Data.XOR07,
                SampleXOR2Data.XOR08,
                SampleXOR2Data.XOR09,
                SampleXOR2Data.XOR10,
                SampleXOR2Data.XOR11,
                SampleXOR2Data.XOR12,
                SampleXOR2Data.XOR13,
                SampleXOR2Data.XOR14,
                SampleXOR2Data.XOR15,
                SampleXOR2Data.XOR16
            }

            ' minimal set: Repeat 3
            Dim largeSet = Enumerable.Repeat(data, 3).SelectMany(Function(a) a).ToList()
            Return largeSet

        End Function

        Public Class XOR2CsvReader

            Public Shared Function GetDataFromCsv(
                    dataLocation As String, numMaxRecords As Integer) As IEnumerable(Of XOR2Data)

                Dim records As IEnumerable(Of XOR2Data) =
                    File.ReadAllLines(dataLocation).
                        Skip(1).
                        Select(Function(x) x.Split(vbTab)).
                        Select(Function(x) New XOR2Data(
                            Single.Parse(x(0)), Single.Parse(x(1)), Single.Parse(x(2)),
                            Single.Parse(x(3)), Single.Parse(x(4)), Single.Parse(x(5)))).
                        Take(numMaxRecords)

                Return records

            End Function

        End Class

        Public Sub Train(mlContext As MLContext,
                ModelPath1Zip As String, ModelPath2Zip As String, largeSet As List(Of XOR2Data))

            Dim trainingDataView = mlContext.Data.LoadFromEnumerable(Of XOR2Data)(largeSet)
            Dim testDataView = mlContext.Data.LoadFromEnumerable(Of XOR2Data)(largeSet)

            ' Check if the Repeat function works fine
            Dim iNumSample = 0
            Dim iNumSampleMod16 = 0
            Dim iNbSuccess = 0
            Dim data = LoadData()
            For Each d In largeSet
                Dim b = d Is data(iNumSampleMod16)
                If b Then iNbSuccess += 1
                iNumSample += 1
                iNumSampleMod16 += 1
                If iNumSampleMod16 >= 16 Then iNumSampleMod16 = 0
            Next

            ' STEP 2: Common data process configuration with pipeline data transformations
            Dim dataProcessPipeline = mlContext.Transforms.Concatenate("Features",
                NameOf(XOR2Data.Input),
                NameOf(XOR2Data.Input),
                NameOf(XOR2Data.Input),
                NameOf(XOR2Data.Input)).AppendCacheCheckpoint(mlContext)

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

            Dim trainingPipeline = dataProcessPipeline.
                Append(trainer).Append(
                    mlContext.Transforms.CopyColumns(
                        outputColumnName:=outputColumnName,
                        inputColumnName:=scoreColumnName))

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

        Public Sub TrainFromFile(mlContext As MLContext,
                ModelPath1Zip As String, ModelPath2Zip As String, TrainDataPath As String,
                ByRef iRowCount As Integer, ByRef samples As List(Of XOR2Data),
                bRepeat As Boolean)

            ' STEP 1: Common data loading configuration
            Dim iRowCountMin = 16
            Dim trainingDataView, testDataView As IDataView
            If bRepeat Then
                ' Repeat the minimal file
                Dim samplesMin = XOR2CsvReader.GetDataFromCsv(TrainDataPath, iRowCountMin).ToList()
                ' Repeat 1: no repeat, same size, Repeat 2: one repeat, double size
                ' minimal set: Repeat 3
                samples = Enumerable.Repeat(samplesMin, 3).SelectMany(Function(a) a).ToList()
                iRowCount = samples.Count ' 48 = 16 * 3 lines
                trainingDataView = mlContext.Data.LoadFromEnumerable(Of XOR2Data)(samples)
                testDataView = mlContext.Data.LoadFromEnumerable(Of XOR2Data)(samples)
            Else
                ' File is yet repeated
                trainingDataView = mlContext.Data.LoadFromTextFile(Of XOR2Data)(
                    TrainDataPath, hasHeader:=True)
                testDataView = mlContext.Data.LoadFromTextFile(Of XOR2Data)(
                    TrainDataPath, hasHeader:=True)
                samples = XOR2CsvReader.GetDataFromCsv(TrainDataPath, 48).ToList()
                iRowCount = samples.Count
            End If

            ' STEP 2: Common data process configuration with pipeline data transformations
            Dim dataProcessPipeline = mlContext.Transforms.Concatenate("Features",
                NameOf(XOR2Data.Input),
                NameOf(XOR2Data.Input),
                NameOf(XOR2Data.Input),
                NameOf(XOR2Data.Input)).AppendCacheCheckpoint(mlContext)

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

        End Sub

        Public Function TestSomePredictions(mlContext As MLContext,
                ModelPath1Zip As String, ModelPath2Zip As String,
                iRowCount As Integer, samples As List(Of XOR2Data)) As Integer

            ' Test Classification Predictions with some hard-coded samples 
            Dim modelInputSchema1 As DataViewSchema = Nothing
            Dim trainedModel1 As ITransformer = mlContext.Model.Load(ModelPath1Zip, modelInputSchema1)
            Dim modelInputSchema2 As DataViewSchema = Nothing
            Dim trainedModel2 As ITransformer = mlContext.Model.Load(ModelPath2Zip, modelInputSchema2)

            ' Create prediction engine related to the loaded trained model
            Dim predEngine1 = mlContext.Model.CreatePredictionEngine(
                Of XOR2Data, XOR2Prediction)(trainedModel1)
            Dim predEngine2 = mlContext.Model.CreatePredictionEngine(
                Of XOR2Data, XOR2Prediction)(trainedModel2)

            Console.WriteLine("=====Predicting using model====")

            Dim listData = samples

            Dim resultPreda = New List(Of XOR2Prediction)()
            Dim resultPredb = New List(Of XOR2Prediction)()
            Dim expecteda = New List(Of Boolean)()
            Dim expectedb = New List(Of Boolean)()
            Dim iNbSamples = iRowCount
            Dim targeta = New Single(iNbSamples - 1) {}
            Dim targetb = New Single(iNbSamples - 1) {}
            Dim successa = New Boolean(iNbSamples - 1) {}
            Dim successb = New Boolean(iNbSamples - 1) {}
            Dim iNumSample = 0
            Const threshold = 0.05F
            Const format = "0.00"
            Dim iNbSuccess = 0
            Dim iNbSuccessExpected = 0
            Console.WriteLine("Treshold:" & threshold.ToString(format))
            For Each d In listData
                resultPreda.Add(predEngine1.Predict(d))
                resultPredb.Add(predEngine2.Predict(d))
                Dim d1 = Convert.ToBoolean(d.Input(0))
                Dim d2 = Convert.ToBoolean(d.Input(1))
                Dim d3 = Convert.ToBoolean(d.Input(2))
                Dim d4 = Convert.ToBoolean(d.Input(3))
                Dim bXOR1 = d1 Xor d2
                Dim bXOR2 = d3 Xor d4
                expecteda.Add(bXOR1)
                expectedb.Add(bXOR2)
                targeta(iNumSample) = 0
                If expecteda(iNumSample) Then targeta(iNumSample) = 1
                targetb(iNumSample) = 0
                If expectedb(iNumSample) Then targetb(iNumSample) = 1
                Dim dev1 = Math.Abs(resultPreda(iNumSample).Score1 - targeta(iNumSample))
                Dim dev2 = Math.Abs(resultPredb(iNumSample).Score2 - targetb(iNumSample))
                Dim s1 = dev1 < threshold
                Dim s2 = dev2 < threshold
                successa(iNumSample) = s1
                successb(iNumSample) = s2
                iNbSuccess += If(s1, 1, 0)
                iNbSuccess += If(s2, 1, 0)
                iNbSuccessExpected += 2

                Console.WriteLine("Sample n°" & (iNumSample + 1).ToString("00") & " : " &
                    d.Input(0) & " XOR " & d.Input(1) & " : " &
                    resultPreda(iNumSample).Score1.ToString(format) &
                    ", target:" & targeta(iNumSample) &
                    ", success: " & successa(iNumSample) & ", " &
                    d.Input(2) & " XOR " & d.Input(3) & " : " &
                    resultPredb(iNumSample).Score2.ToString(format) &
                    ", target:" & targetb(iNumSample) &
                    ", success: " & successb(iNumSample) & ", " &
                    iNbSuccess.ToString("00") & "/" & iNbSuccessExpected.ToString("00"))

                iNumSample += 1
            Next

            Return iNbSuccess

        End Function

    End Module

End Namespace