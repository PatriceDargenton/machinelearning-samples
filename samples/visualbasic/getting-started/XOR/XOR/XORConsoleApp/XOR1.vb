
Imports System.IO
Imports Microsoft.ML
Imports Microsoft.ML.Data
Imports XORApp.Common

Namespace XORApp

    Public Module XOR1

        Public Class XORData

            <LoadColumn(0)>
            Public Input1 As Single

            <LoadColumn(1)>
            Public Input2 As Single

            <LoadColumn(2)>
            Public Output As Single ' Label

            Public Sub New(input1 As Single, input2 As Single)
                Me.Input1 = input1
                Me.Input2 = input2
            End Sub

            Public Sub New(input1 As Single, input2 As Single, output As Single)
                Me.Input1 = input1
                Me.Input2 = input2
                Me.Output = output
            End Sub

            Public Function Equals1(obj As Object) As Boolean

                ' Check for null and compare run-time types
                If obj Is Nothing OrElse Not [GetType]().Equals(obj.GetType()) Then
                    Return False
                Else
                    Dim p = CType(obj, XORData)
                    Return Me.Input1 = p.Input1 AndAlso
                           Me.Input2 = p.Input2 AndAlso
                           Me.Output = p.Output
                End If

            End Function

            Public Shared Function Equals2(p As XORData, dr As System.Data.DataRow) As Boolean

                Dim I1 = CStr(dr(0))
                Dim I2 = CStr(dr(1))
                Dim O = CStr(dr(2))
                Dim Input1 As Single = 0, Input2 As Single = 0, Output As Single = 0
                Single.TryParse(I1, Input1)
                Single.TryParse(I2, Input2)
                Single.TryParse(O, Output)

                Return Input1 = p.Input1 AndAlso
                       Input2 = p.Input2 AndAlso
                       Output = p.Output

            End Function

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

        Public Function LoadData() As List(Of XORData)

            ' STEP 1: Common data loading configuration

            Dim data = New List(Of XORData) From {
                New XORData(1.0F, 0.0F, 1.0F),
                New XORData(0.0F, 0.0F, 0.0F),
                New XORData(0.0F, 1.0F, 1.0F),
                New XORData(1.0F, 1.0F, 0.0F)}

            ' This is not optimal, it causes an overconsumption of RAM,
            '  it would have been better to add more iterations instead
            ' From: https://stackoverflow.com/questions/53472759/why-does-this-ml-net-code-fail-to-predict-the-correct-output
            ' minimal set: Repeat 10
            Dim largeSet = Enumerable.Repeat(data, 10).SelectMany(Function(a) a).ToList()
            Return largeSet

        End Function

        Public Sub Train(mlContext As MLContext, ModelPath As String, largeSet As List(Of XORData))

            ' https://docs.microsoft.com/fr-fr/dotnet/machine-learning/how-to-guides/load-data-ml-net
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
                NameOf(XORData.Input1),
                NameOf(XORData.Input2)).AppendCacheCheckpoint(mlContext)

            ' (Optional) Peek data in training DataView after applying the ProcessPipeline's transformations  
            ConsoleHelper.PeekDataViewInConsole(mlContext,
                trainingDataView, dataProcessPipeline, 5)
            ConsoleHelper.PeekVectorColumnDataInConsole(mlContext, "Features",
                trainingDataView, dataProcessPipeline, 5)

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

        Public Sub TrainFromFile(mlContext As MLContext,
                ModelPath As String, TrainDataPath As String)

            ' STEP 1: Common data loading configuration
            If Not File.Exists(TrainDataPath) Then
                Console.WriteLine("Can't find this file: " & TrainDataPath)
                Exit Sub
            End If
            Dim trainingDataView = mlContext.Data.LoadFromTextFile(Of XORData)(
                TrainDataPath, hasHeader:=True)
            Dim testDataView = mlContext.Data.LoadFromTextFile(Of XORData)(
                TrainDataPath, hasHeader:=True)

            ' Check if the XOR file matches with the RAM data
            Dim table = trainingDataView.ToDataTable()
            Dim iNumSample = 0
            Dim iNbSuccess = 0
            Dim data = LoadData()
            For Each d In data
                Dim d2 = table.Rows(iNumSample)
                Dim b = XORData.Equals2(d, d2)
                If b Then iNbSuccess += 1
                iNumSample += 1
            Next

            ' STEP 2: Common data process configuration with pipeline data transformations
            Dim dataProcessPipeline = mlContext.Transforms.Concatenate("Features",
                NameOf(XORData.Input1),
                NameOf(XORData.Input2)).AppendCacheCheckpoint(mlContext)

            ' (Optional) Peek data in training DataView after applying the ProcessPipeline's transformations  
            ConsoleHelper.PeekDataViewInConsole(mlContext,
                trainingDataView, dataProcessPipeline, 5)
            ConsoleHelper.PeekVectorColumnDataInConsole(mlContext, "Features",
                trainingDataView, dataProcessPipeline, 5)

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

        Public Function TestSomePredictions(mlContext As MLContext, ModelPath As String) As Integer

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