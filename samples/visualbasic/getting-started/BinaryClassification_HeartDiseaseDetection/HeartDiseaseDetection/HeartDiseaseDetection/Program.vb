
Imports System.IO
Imports HeartDiseaseDetection.HeartDiseasePredictionConsoleApp.DataStructures
Imports Microsoft.ML

Namespace HeartDiseasePredictionConsoleApp

    Public Class Program

        Shared Sub Main(args() As String)

            Dim baseDassetsRelativePath As String = "../../../../Data"
            Dim baseDassetsPath As String = GetAbsolutePath(baseDassetsRelativePath)
            Dim baseModelsRelativePath As String = "../../../../MLModels"
            Dim baseModelsPath As String = GetAbsolutePath(baseModelsRelativePath)
            GetResult(baseDassetsPath, baseModelsPath)

            Console.WriteLine("=============== End of process, hit any key to finish ===============")
            Console.ReadKey()

        End Sub

        Public Shared Function GetResult(
                myBaseDassetsPath As String, myBaseModelsPath As String) As Boolean

            Dim trainDataPath As String = Path.Combine(myBaseDassetsPath, "HeartTraining.csv")
            Dim testDataPath As String = Path.Combine(myBaseDassetsPath, "HeartTest.csv")
            Dim modelPath As String = Path.Combine(myBaseModelsPath, "HeartClassification.zip")

            Dim mlContext As New MLContext
            Dim score# = 0
            BuildTrainEvaluateAndSaveModel(mlContext,
                trainDataPath, testDataPath, modelPath, score)

            TestPrediction(mlContext, modelPath)

            Dim scoreRounded = Math.Round(score, digits:=4) * 100
            Dim scoreExpected = 94
            Dim success = scoreRounded >= scoreExpected
            Console.WriteLine("Success: Score = " & scoreRounded.ToString("0.00") & " >= " & scoreExpected & " : " & success)

            Return success

        End Function

        Private Shared Sub BuildTrainEvaluateAndSaveModel(
                mlContext As MLContext,
                trainDataPath As String, testDataPath As String, modelPath As String,
                ByRef score#)

            ' STEP 1: Common data loading configuration
            Dim trainingDataView = mlContext.Data.LoadFromTextFile(Of HeartData)(
                trainDataPath, hasHeader:=True, separatorChar:=";"c)
            Dim testDataView = mlContext.Data.LoadFromTextFile(Of HeartData)(
                testDataPath, hasHeader:=True, separatorChar:=";"c)

            ' STEP 2: Concatenate the features and set the training algorithm
            Dim pipeline = mlContext.Transforms.Concatenate(
                "Features", "Age", "Sex", "Cp", "TrestBps", "Chol", "Fbs", "RestEcg", "Thalac",
                "Exang", "OldPeak", "Slope", "Ca", "Thal").
                Append(mlContext.BinaryClassification.Trainers.FastTree(
                    labelColumnName:="Label", featureColumnName:="Features"))

            Console.WriteLine("=============== Training the model ===============")
            Dim trainedModel As ITransformer = pipeline.Fit(trainingDataView)
            Console.WriteLine("")
            Console.WriteLine("")
            Console.WriteLine("=============== Finish the train model. Push Enter ===============")
            Console.WriteLine("")
            Console.WriteLine("")

            Console.WriteLine("===== Evaluating Model's accuracy with Test data =====")
            Dim predictions = trainedModel.Transform(testDataView)

            Dim metrics = mlContext.BinaryClassification.Evaluate(
                data:=predictions, labelColumnName:="Label", scoreColumnName:="Score")

            Console.WriteLine("")
            Console.WriteLine("")
            Console.WriteLine($"************************************************************")
            Console.WriteLine($"*       Metrics for {DirectCast(trainedModel, Object).ToString()} binary classification model      ")
            Console.WriteLine($"*-----------------------------------------------------------")
            Console.WriteLine($"*       Accuracy: {metrics.Accuracy:P2}")
            Console.WriteLine($"*       Area Under Roc Curve:      {metrics.AreaUnderRocCurve:P2}")
            Console.WriteLine($"*       Area Under PrecisionRecall Curve:  {metrics.AreaUnderPrecisionRecallCurve:P2}")
            Console.WriteLine($"*       F1Score:  {metrics.F1Score:P2}")
            Console.WriteLine($"*       LogLoss:  {metrics.LogLoss:#.##}")
            Console.WriteLine($"*       LogLossReduction:  {metrics.LogLossReduction:#.##}")
            Console.WriteLine($"*       PositivePrecision:  {metrics.PositivePrecision:#.##}")
            Console.WriteLine($"*       PositiveRecall:  {metrics.PositiveRecall:#.##}")
            Console.WriteLine($"*       NegativePrecision:  {metrics.NegativePrecision:#.##}")
            Console.WriteLine($"*       NegativeRecall:  {metrics.NegativeRecall:P2}")
            Console.WriteLine($"************************************************************")
            Console.WriteLine("")
            Console.WriteLine("")
            score = metrics.Accuracy

            Console.WriteLine("=============== Saving the model to a file ===============")
            Dim directoryPath = IO.Path.GetDirectoryName(modelPath)
            If Not IO.Directory.Exists(directoryPath) Then
                Dim di As New IO.DirectoryInfo(directoryPath)
                di.Create()
            End If
            mlContext.Model.Save(trainedModel, trainingDataView.Schema, modelPath)
            Console.WriteLine("")
            Console.WriteLine("")
            Console.WriteLine("=============== Model Saved ============= ")

        End Sub

        Private Shared Sub TestPrediction(mlContext As MLContext, modelPath As String)

            Dim modelInputSchema As DataViewSchema = Nothing
            Dim trainedModel As ITransformer = mlContext.Model.Load(modelPath, modelInputSchema)

            ' Create prediction engine related to the loaded trained model
            Dim predictionEngine = mlContext.Model.CreatePredictionEngine(
                Of HeartData, HeartPrediction)(trainedModel)

            For Each heartData In HeartSampleData.heartDataList
                Dim prediction = predictionEngine.Predict(heartData)

                Console.WriteLine($"=============== Single Prediction  ===============")
                Console.WriteLine($"Age: {heartData.Age} ")
                Console.WriteLine($"Sex: {heartData.Sex} ")
                Console.WriteLine($"Cp: {heartData.Cp} ")
                Console.WriteLine($"TrestBps: {heartData.TrestBps} ")
                Console.WriteLine($"Chol: {heartData.Chol} ")
                Console.WriteLine($"Fbs: {heartData.Fbs} ")
                Console.WriteLine($"RestEcg: {heartData.RestEcg} ")
                Console.WriteLine($"Thalac: {heartData.Thalac} ")
                Console.WriteLine($"Exang: {heartData.Exang} ")
                Console.WriteLine($"OldPeak: {heartData.OldPeak} ")
                Console.WriteLine($"Slope: {heartData.Slope} ")
                Console.WriteLine($"Ca: {heartData.Ca} ")
                Console.WriteLine($"Thal: {heartData.Thal} ")
                Console.WriteLine($"Prediction Value: {prediction.Prediction} ")
                Console.WriteLine($"Prediction: {(If(prediction.Prediction, "A disease could be present", "Not present disease"))} ")
                Console.WriteLine($"Probability: {prediction.Probability} ")
                Console.WriteLine($"==================================================")
                Console.WriteLine("")
                Console.WriteLine("")
            Next heartData

        End Sub

        Public Shared Function GetAbsolutePath(relativePath As String) As String
            Dim fullPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
            Return fullPath
        End Function

    End Class

End Namespace