
Imports System.IO
Imports Microsoft.ML
Imports Microsoft.ML.Data
Imports XORApp.Common

Namespace XORApp

    Public Module XOR1Vector

        Public Class XORData

            <LoadColumn(0, 1)>
            <VectorType(2)>
            Public Input As Single()

            <LoadColumn(2)>
            Public Output As Single

            Public Sub New(input1 As Single, input2 As Single)
                Me.Input = New Single(1) {}
                Me.Input(0) = input1
                Me.Input(1) = input2
            End Sub

            Public Sub New(input1 As Single, input2 As Single, output As Single)
                Me.Input = New Single(1) {}
                Me.Input(0) = input1
                Me.Input(1) = input2
                Me.Output = output
            End Sub

            Public Function Equals1(obj As Object) As Boolean
                ' Check for null and compare run-time types
                If obj Is Nothing OrElse Not [GetType]().Equals(obj.GetType()) Then
                    Return False
                Else
                    Dim p = CType(obj, XORData)
                    Return Me.Input(0) = p.Input(0) AndAlso
                           Me.Input(1) = p.Input(1) AndAlso Output = p.Output
                End If
            End Function

        End Class

        Public Class XORPrediction
            Public Score As Single
        End Class

        Public Class SampleXORData
            Friend Shared ReadOnly XOR1 As XORData = New XORData(1.0F, 0.0F)
            Friend Shared ReadOnly XOR2 As XORData = New XORData(0.0F, 0.0F)
            Friend Shared ReadOnly XOR3 As XORData = New XORData(0.0F, 1.0F)
            Friend Shared ReadOnly XOR4 As XORData = New XORData(1.0F, 1.0F)
        End Class

        Public Function LoadData() As List(Of XORData)

            ' STEP 1: Common data loading configuration

            ' Récup depuis : https://stackoverflow.com/questions/53472759/why-does-this-ml-net-code-fail-to-predict-the-correct-output
            Dim data = New List(Of XORData) From {
                New XORData(1.0F, 0.0F, 1.0F),
                New XORData(0.0F, 0.0F, 0.0F),
                New XORData(0.0F, 1.0F, 1.0F),
                New XORData(1.0F, 1.0F, 0.0F)
            }

            ' minimal set: Repeat 10
            Dim largeSet = Enumerable.Repeat(data, 10).SelectMany(Function(a) a).ToList()
            Return largeSet

        End Function

        Public Sub Train(mlContext As MLContext, ModelPath As String, largeSet As List(Of XORData))

            Dim trainingDataView = mlContext.Data.LoadFromEnumerable(Of XORData)(largeSet)
            Dim testDataView = mlContext.Data.LoadFromEnumerable(Of XORData)(largeSet)

            ' Check if the Repeat function works fine
            Dim iNumSample = 0
            Dim iNumSampleMod4 = 0
            Dim iNbSuccess = 0
            Dim data = LoadData()
            For Each d In largeSet
                Dim d2 = data(iNumSampleMod4)
                Dim b = d.Equals1(d2)
                If b Then iNbSuccess += 1
                iNumSample += 1
                iNumSampleMod4 += 1
                If iNumSampleMod4 >= 4 Then iNumSampleMod4 = 0
            Next

            ' STEP 2: Common data process configuration with pipeline data transformations
            Dim dataProcessPipeline = mlContext.Transforms.Concatenate("Features",
                NameOf(XORData.Input),
                NameOf(XORData.Input)).AppendCacheCheckpoint(mlContext)

            ' (Optional) Peek data in training DataView after applying the ProcessPipeline's transformations  
            ConsoleHelper.PeekDataViewInConsole(mlContext, trainingDataView,
                dataProcessPipeline, 5)
            ConsoleHelper.PeekVectorColumnDataInConsole(mlContext, "Features", trainingDataView,
                dataProcessPipeline, 5)

            ' STEP 3: Set the training algorithm, then append the trainer to the pipeline  
            Dim trainer = mlContext.Regression.Trainers.FastTree(
                labelColumnName:="Output", featureColumnName:="Features", learningRate:=0.1)

            Dim trainingPipeline = dataProcessPipeline.Append(trainer)

            ' STEP 4: Train the model fitting to the DataSet
            Console.WriteLine("=============== Training the model ===============")
            Dim trainedModel As ITransformer = trainingPipeline.Fit(trainingDataView)

            ' STEP 5: Evaluate the model and show accuracy stats
            Console.WriteLine("===== Evaluating Model's accuracy with Test data =====")
            Dim predictions = trainedModel.Transform(testDataView)
            Dim metrics = mlContext.Regression.Evaluate(predictions,
                labelColumnName:="Output", scoreColumnName:="Score")
            ConsoleHelper.PrintRegressionMetrics(trainer.ToString(), metrics)

            ' STEP 6: Save/persist the trained model to a .ZIP file
            Dim directoryPath = Path.GetDirectoryName(ModelPath)
            If Not Directory.Exists(directoryPath) Then
                Dim di As New DirectoryInfo(directoryPath)
                di.Create()
            End If
            mlContext.Model.Save(trainedModel, trainingDataView.Schema, ModelPath)
            Console.WriteLine("The model is saved to {0}", Path.GetFullPath(ModelPath))

        End Sub

        Public Sub TrainFromFile(mlContext As MLContext, ModelPath As String,
                TrainDataPath As String)

            ' STEP 1: Common data loading configuration
            Dim trainingDataView = mlContext.Data.LoadFromTextFile(Of XORData)(
                TrainDataPath, hasHeader:=True)
            Dim testDataView = mlContext.Data.LoadFromTextFile(Of XORData)(
                TrainDataPath, hasHeader:=True)

            ' STEP 2: Common data process configuration with pipeline data transformations
            Dim dataProcessPipeline = mlContext.Transforms.Concatenate("Features",
                NameOf(XORData.Input),
                NameOf(XORData.Input)).AppendCacheCheckpoint(mlContext)

            ' (Optional) Peek data in training DataView after applying the ProcessPipeline's transformations  
            ConsoleHelper.PeekDataViewInConsole(mlContext, trainingDataView,
                dataProcessPipeline, 5)
            ConsoleHelper.PeekVectorColumnDataInConsole(mlContext, "Features", trainingDataView,
                dataProcessPipeline, 5)

            ' STEP 3: Set the training algorithm, then append the trainer to the pipeline  
            Dim trainer = mlContext.Regression.Trainers.FastTree(
                labelColumnName:="Output", featureColumnName:="Features", learningRate:=0.1)

            Dim trainingPipeline = dataProcessPipeline.Append(trainer)

            ' STEP 4: Train the model fitting to the DataSet
            Console.WriteLine("=============== Training the model ===============")
            Dim trainedModel As ITransformer = trainingPipeline.Fit(trainingDataView)

            ' STEP 5: Evaluate the model and show accuracy stats
            Console.WriteLine("===== Evaluating Model's accuracy with Test data =====")
            Dim predictions = trainedModel.Transform(testDataView)
            Dim metrics = mlContext.Regression.Evaluate(predictions,
                labelColumnName:="Output", scoreColumnName:="Score")
            ConsoleHelper.PrintRegressionMetrics(trainer.ToString(), metrics)

            ' STEP 6: Save/persist the trained model to a .ZIP file
            Dim directoryPath = Path.GetDirectoryName(ModelPath)
            If Not Directory.Exists(directoryPath) Then
                Dim di As New DirectoryInfo(directoryPath)
                di.Create()
            End If
            mlContext.Model.Save(trainedModel, trainingDataView.Schema, ModelPath)
            Console.WriteLine("The model is saved to {0}", Path.GetFullPath(ModelPath))

        End Sub

        Public Sub TestSomePredictions(mlContext As MLContext, ModelPath As String)

            ' Test Classification Predictions with some hard-coded samples 
            Dim modelInputSchema As DataViewSchema = Nothing
            Dim trainedModel As ITransformer = mlContext.Model.Load(
                ModelPath, modelInputSchema)

            ' Create prediction engine related to the loaded trained model
            Dim predEngine = mlContext.Model.CreatePredictionEngine(
                Of XORData, XORPrediction)(trainedModel)

            Console.WriteLine("=====Predicting using model====")

            Dim resultprediction1 = predEngine.Predict(SampleXORData.XOR1)
            Dim resultprediction2 = predEngine.Predict(SampleXORData.XOR2)
            Dim resultprediction3 = predEngine.Predict(SampleXORData.XOR3)
            Dim resultprediction4 = predEngine.Predict(SampleXORData.XOR4)

            Dim expectedResult1 =
                Convert.ToBoolean(SampleXORData.XOR1.Input(0)) Xor
                Convert.ToBoolean(SampleXORData.XOR1.Input(1))
            Dim expectedResult2 =
                Convert.ToBoolean(SampleXORData.XOR2.Input(0)) Xor
                Convert.ToBoolean(SampleXORData.XOR2.Input(1))
            Dim expectedResult3 =
                Convert.ToBoolean(SampleXORData.XOR3.Input(0)) Xor
                Convert.ToBoolean(SampleXORData.XOR3.Input(1))
            Dim expectedResult4 =
                Convert.ToBoolean(SampleXORData.XOR4.Input(0)) Xor
                Convert.ToBoolean(SampleXORData.XOR4.Input(1))

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

            Const format = "0.00"
            Console.WriteLine(
                SampleXORData.XOR1.Input(0) & " XOR " & SampleXORData.XOR1.Input(1) & " : " &
                resultprediction1.Score.ToString(format) & ", target:" & target1 &
                ", success: " & success1 & " (" & threshold.ToString(format) & ")")
            Console.WriteLine(
                SampleXORData.XOR2.Input(0) & " XOR " & SampleXORData.XOR2.Input(1) & " : " &
                resultprediction2.Score.ToString(format) & ", target:" & target2 &
                ", success: " & success2 & " (" & threshold.ToString(format) & ")")
            Console.WriteLine(
                SampleXORData.XOR3.Input(0) & " XOR " & SampleXORData.XOR3.Input(1) & " : " &
                resultprediction3.Score.ToString(format) & ", target:" & target3 &
                ", success: " & success3 & " (" & threshold.ToString(format) & ")")
            Console.WriteLine(
                SampleXORData.XOR4.Input(0) & " XOR " & SampleXORData.XOR4.Input(1) & " : " &
                resultprediction4.Score.ToString(format) & ", target:" & target4 &
                ", success: " & success4 & " (" & threshold.ToString(format) & ")")
        End Sub

    End Module

End Namespace