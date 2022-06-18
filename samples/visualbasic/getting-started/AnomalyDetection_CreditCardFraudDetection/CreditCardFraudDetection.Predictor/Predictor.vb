
Imports Microsoft.ML

Imports CreditCardFraudDetection.Common.CreditCardFraudDetection.Common.DataModels

Namespace CreditCardFraudDetection.Predictor

    Public Class Predictor

        Private ReadOnly _modelfile As String
        Private ReadOnly _dasetFile As String

        Public Sub New(modelfile As String, dasetFile As String)
            If modelfile Is Nothing Then
                Throw New ArgumentNullException(NameOf(modelfile))
            End If
            If dasetFile Is Nothing Then
                Throw New ArgumentNullException(NameOf(dasetFile))
            End If

            _modelfile = modelfile
            _dasetFile = dasetFile
        End Sub

        Public Sub RunMultiplePredictions(numberOfPredictions As Integer,
                ByRef success As Boolean)

            Dim mlContext As New MLContext

            ' Load data as input for predictions
            Dim inputDataForPredictions As IDataView =
                mlContext.Data.LoadFromTextFile(Of TransactionObservation)(
                    _dasetFile, separatorChar:=","c, hasHeader:=True)

            Console.WriteLine($"Predictions from saved model:")

            Dim inputSchema As DataViewSchema = Nothing
            Dim model As ITransformer = mlContext.Model.Load(_modelfile, inputSchema)

            Dim predictionEngine = mlContext.Model.CreatePredictionEngine(
                Of TransactionObservation, TransactionFraudPrediction)(model)

            Console.WriteLine(vbLf & " " & vbLf &
                $" Test {numberOfPredictions} transactions, from the test datasource, that should be predicted as fraud (true):")

            Dim nbSuccess% = 0
            mlContext.Data.CreateEnumerable(Of TransactionObservation)(
                inputDataForPredictions, reuseRowObject:=False).
                    Where(Function(x) x.Label > 0).Take(numberOfPredictions).
                    Select(Function(testData) testData).
                    ToList().ForEach(
                        Sub(testData)
                            Console.WriteLine($"--- Transaction ---")
                            testData.PrintToConsole()
                            Dim result = predictionEngine.Predict(testData)
                            result.PrintToConsole()
                            Console.WriteLine($"-------------------")
                            If result.Score > 0.4 Then nbSuccess += 1
                        End Sub)

            Console.WriteLine(vbLf & " " & vbLf &
                $" Test {numberOfPredictions} transactions, from the test datasource, that should NOT be predicted as fraud (false):")

            mlContext.Data.CreateEnumerable(Of TransactionObservation)(
                inputDataForPredictions, reuseRowObject:=False).
                    Where(Function(x) x.Label < 1).Take(numberOfPredictions).
                    ToList().ForEach(
                        Sub(testData)
                            Console.WriteLine($"--- Transaction ---")
                            testData.PrintToConsole()
                            Dim result = predictionEngine.Predict(testData)
                            result.PrintToConsole()
                            Console.WriteLine($"-------------------")
                            If result.Score < 0.02 Then nbSuccess += 1
                        End Sub)

            Console.WriteLine("nb success: " & nbSuccess & "/10")
            If nbSuccess >= 10 Then success = True

        End Sub

    End Class

End Namespace