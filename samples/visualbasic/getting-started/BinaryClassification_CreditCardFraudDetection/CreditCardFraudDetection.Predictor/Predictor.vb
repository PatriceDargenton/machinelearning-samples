
Imports BCCreditCardFraudDetection.Common.CreditCardFraudDetection.Common.DataModels
Imports Microsoft.ML
Imports Microsoft.ML.Data

Namespace BCCreditCardFraudDetection.Predictor

    Public Class Predictor

        Private ReadOnly _modelfile As String
        Private ReadOnly _dasetFile As String

        Public Sub New(modelfile As String, dasetFile As String)
            _modelfile = modelfile
            _dasetFile = dasetFile
        End Sub

        Public Sub RunMultiplePredictions(numberOfPredictions As Integer, ByRef success As Boolean)

            Dim mlContext As New MLContext

            ' Load data as input for predictions
            Dim inputDataForPredictions As IDataView = mlContext.Data.LoadFromTextFile(
                Of TransactionObservation)(_dasetFile, separatorChar:=","c, hasHeader:=True)

            Console.WriteLine($"Predictions from saved model:")

            Dim inputSchema As DataViewSchema = Nothing
            Dim model As ITransformer = mlContext.Model.Load(_modelfile, inputSchema)

            Dim predictionEngine = mlContext.Model.CreatePredictionEngine(
                Of TransactionObservation, TransactionFraudPredictionWithContribution)(model)
            Console.WriteLine(vbLf & " " & vbLf &
                $" Test {numberOfPredictions} transactions, from the test datasource, that should be predicted as fraud (true):")

            Dim nbSuccess% = 0
            mlContext.Data.CreateEnumerable(Of TransactionObservation)(
                inputDataForPredictions, reuseRowObject:=False).
                Where(Function(x) x.Label = True).Take(numberOfPredictions).
                Select(Function(testData) testData).ToList().ForEach(
                    Sub(testData)
                        Console.WriteLine($"--- Transaction ---")
                        testData.PrintToConsole()
                        Dim result = predictionEngine.Predict(testData)
                        result.PrintToConsole()
                        Console.WriteLine($"-------------------")
                        If result.Probability > 0.98 Then nbSuccess += 1
                    End Sub)

            Console.WriteLine(vbLf & " " & vbLf & $" Test {numberOfPredictions} transactions, from the test datasource, that should NOT be predicted as fraud (false):")

            mlContext.Data.CreateEnumerable(Of TransactionObservation)(
                inputDataForPredictions, reuseRowObject:=False).
                Where(Function(x) x.Label = False).Take(numberOfPredictions).ToList().
                ForEach(
                    Sub(testData)
                        Console.WriteLine($"--- Transaction ---")
                        testData.PrintToConsole()
                        Dim result = predictionEngine.Predict(testData)
                        result.PrintToConsole(
                            model.GetOutputSchema(inputDataForPredictions.Schema))
                        Console.WriteLine($"-------------------")
                        If result.Probability < 0.02 Then nbSuccess += 1
                    End Sub)

            Console.WriteLine("nb success: " & nbSuccess & "/10")
            If nbSuccess >= 10 Then success = True

        End Sub

        Private Class TransactionFraudPredictionWithContribution : Inherits TransactionFraudPrediction

            Public Property FeatureContributions As Single()

            Public Overloads Sub PrintToConsole(dataview As DataViewSchema)

                MyBase.PrintToConsole()

                Dim slots As VBuffer(Of ReadOnlyMemory(Of Char)) = Nothing
                dataview.GetColumnOrNull("Features").Value.GetSlotNames(slots)
                Dim featureNames = slots.DenseValues().ToArray()
                Console.WriteLine(
                    $"Feature Contributions: " &
                    $"[{featureNames(0)}] {FeatureContributions(0)} " &
                    $"[{featureNames(1)}] {FeatureContributions(1)} " &
                    $"[{featureNames(2)}] {FeatureContributions(2)} ... " &
                    $"[{featureNames(27)}] {FeatureContributions(27)} " &
                    $"[{featureNames(28)}] {FeatureContributions(28)}")

            End Sub

        End Class

    End Class

End Namespace