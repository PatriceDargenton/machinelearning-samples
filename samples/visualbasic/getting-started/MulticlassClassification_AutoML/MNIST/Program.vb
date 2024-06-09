
Imports System.IO
Imports Microsoft.ML
Imports Microsoft.ML.AutoML
Imports Microsoft.ML.Data
Imports MNIST.Common
Imports MNIST.MNIST.DataStructures

Namespace MNIST

    Public Class Program

        Private Shared ExperimentTime As UInteger = 60

        Shared Sub Main(args() As String)

            Dim assetsRelativePath As String = "../../../Data"
            Dim assetsPath As String = GetAbsolutePath(assetsRelativePath)

            Dim baseModelsRelativePath As String = "../../../MLModels"
            Dim modelPath As String = GetAbsolutePath(baseModelsRelativePath)

            GetResult(assetsPath, modelPath)

            Console.WriteLine("Hit any key to finish the app")
            Console.ReadKey()

        End Sub

        Public Shared Function GetResult(
                myAssetsPath As String, myModelPath As String,
                Optional isTest As Boolean = False) As Boolean

            Dim mlContext As New MLContext
            Dim modelPath As String = Path.GetFullPath(Path.Combine(myModelPath, "Model.zip"))

            Dim score# = 0
            Train(myAssetsPath, modelPath, mlContext, isTest, score)
            TestSomePredictions(modelPath, mlContext)

            Dim scoreRounded = Math.Round(score, digits:=4) * 100
            Dim scoreExpected = 94 ' 96
            Dim success = scoreRounded >= scoreExpected
            Console.WriteLine("Success: Score = " & scoreRounded.ToString("0.00") & " >= " & scoreExpected & " : " & success)

            Return success

        End Function

        Public Shared Sub Train(myAssetsPath As String, modelPath As String,
                mlContext As MLContext, isTest As Boolean, ByRef score#)

            Try

                Dim trainDataRelativePath As String = Path.Combine(myAssetsPath, "optdigits-train.csv")
                Dim testDataRelativePath As String = Path.Combine(myAssetsPath, "optdigits-test.csv")
                Dim trainDataPath As String = GetAbsolutePath(trainDataRelativePath)
                Dim testDataPath As String = GetAbsolutePath(testDataRelativePath)
                trainDataPath = Path.GetFullPath(trainDataPath)
                testDataPath = Path.GetFullPath(testDataPath)

                ' STEP 1: Load the data
                Dim trainData = mlContext.Data.LoadFromTextFile(path:=trainDataPath,
                    columns:={
                        New TextLoader.Column(NameOf(InputData.PixelValues), DataKind.Single, 0, 63),
                        New TextLoader.Column("Number", DataKind.Single, 64)
                    }, hasHeader:=False, separatorChar:=","c)

                Dim testData = mlContext.Data.LoadFromTextFile(path:=testDataPath,
                    columns:={
                        New TextLoader.Column(NameOf(InputData.PixelValues), DataKind.Single, 0, 63),
                        New TextLoader.Column("Number", DataKind.Single, 64)
                    }, hasHeader:=False, separatorChar:=","c)

                ' STEP 2: Initialize our user-defined progress handler that AutoML will 
                '  invoke after each model it produces and evaluates
                Dim progressHandler = New Common.MulticlassExperimentProgressHandler

                ' STEP 3: Run an AutoML multiclass classification experiment
                ConsoleHelperAutoML.ConsoleWriteHeader("=============== Running AutoML experiment ===============")
                Console.WriteLine(
                    $"Running AutoML multiclass classification experiment for {ExperimentTime} seconds...")
                ' Note: FastTree fails with the exception: FastTree was canceled,
                '  and the AutoML stops, but the demo continues
                ' (this is also the case in the C# and F# samples)
                Dim experimentResult As ExperimentResult(Of MulticlassClassificationMetrics) =
                    mlContext.Auto().CreateMulticlassClassificationExperiment(ExperimentTime).
                    Execute(trainData, "Number", progressHandler:=progressHandler)

                ' Print top models found by AutoML
                Console.WriteLine()
                PrintTopModels(experimentResult)

                ' STEP 4: Evaluate the model and print metrics
                ConsoleHelperAutoML.ConsoleWriteHeader("===== Evaluating model's accuracy with test data =====")
                Dim bestRun As RunDetail(Of MulticlassClassificationMetrics) = experimentResult.BestRun
                Dim trainedModel As ITransformer = bestRun.Model
                Dim predictions = trainedModel.Transform(testData)
                Dim metrics = mlContext.MulticlassClassification.Evaluate(
                    data:=predictions, labelColumnName:="Number", scoreColumnName:="Score")
                ConsoleHelperAutoML.PrintMulticlassClassificationMetrics(bestRun.TrainerName, metrics)
                score = metrics.MacroAccuracy

                ' STEP 5: Save/persist the trained model to a .ZIP file
                Dim directoryPath = IO.Path.GetDirectoryName(modelPath)
                If Not IO.Directory.Exists(directoryPath) Then
                    Dim di As New IO.DirectoryInfo(directoryPath)
                    di.Create()
                End If
                mlContext.Model.Save(trainedModel, trainData.Schema, modelPath)
                Console.WriteLine("The model is saved to {0}", modelPath)

            Catch ex As Exception
                Console.WriteLine(ex)
            End Try

        End Sub

        Public Shared Function GetAbsolutePath(relativePath As String) As String
            Dim fullPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
            Return fullPath
        End Function

        ''' <summary>
        ''' Print top models from AutoML experiment
        ''' </summary>
        Private Shared Sub PrintTopModels(
                experimentResult As ExperimentResult(Of MulticlassClassificationMetrics))

            ' Get top few runs ranked by accuracy
            Dim topRuns = experimentResult.RunDetails.Where(
                Function(r) r.ValidationMetrics IsNot Nothing AndAlso
                    Not Double.IsNaN(r.ValidationMetrics.MicroAccuracy)).
                    OrderByDescending(Function(r) r.ValidationMetrics.MicroAccuracy).Take(3)

            Console.WriteLine("Top models ranked by accuracy --")
            ConsoleHelperAutoML.PrintMulticlassClassificationMetricsHeader()
            For i = 0 To topRuns.Count() - 1
                Dim run = topRuns.ElementAt(i)
                ConsoleHelperAutoML.PrintIterationMetrics(i + 1, run.TrainerName, run.ValidationMetrics,
                                                    run.RuntimeInSeconds)
            Next i

        End Sub

        Private Shared Sub TestSomePredictions(myModelPath As String, mlContext As MLContext)

            ConsoleHelperAutoML.ConsoleWriteHeader("=============== Testing prediction engine ===============")

            Dim modelInputSchema As DataViewSchema = Nothing
            Dim trainedModel As ITransformer = mlContext.Model.Load(myModelPath, modelInputSchema)

            ' Create prediction engine related to the loaded trained model
            Dim predEngine = mlContext.Model.CreatePredictionEngine(Of InputData, OutputData)(trainedModel)

            'InputData data1 = SampleMNISTData.MNIST1;
            Dim predictedResult1 = predEngine.Predict(SampleMNISTData.MNIST1)

            Console.WriteLine($"Actual: 7     Predicted probability:       zero:  {predictedResult1.Score(0):0.####}")
            Console.WriteLine($"                                           One :  {predictedResult1.Score(1):0.####}")
            Console.WriteLine($"                                           two:   {predictedResult1.Score(2):0.####}")
            Console.WriteLine($"                                           three: {predictedResult1.Score(3):0.####}")
            Console.WriteLine($"                                           four:  {predictedResult1.Score(4):0.####}")
            Console.WriteLine($"                                           five:  {predictedResult1.Score(5):0.####}")
            Console.WriteLine($"                                           six:   {predictedResult1.Score(6):0.####}")
            Console.WriteLine($"                                           seven: {predictedResult1.Score(7):0.####}")
            Console.WriteLine($"                                           eight: {predictedResult1.Score(8):0.####}")
            Console.WriteLine($"                                           nine:  {predictedResult1.Score(9):0.####}")
            Console.WriteLine()

            Dim predictedResult2 = predEngine.Predict(SampleMNISTData.MNIST2)

            Console.WriteLine($"Actual: 1     Predicted probability:       zero:  {predictedResult2.Score(0):0.####}")
            Console.WriteLine($"                                           One :  {predictedResult2.Score(1):0.####}")
            Console.WriteLine($"                                           two:   {predictedResult2.Score(2):0.####}")
            Console.WriteLine($"                                           three: {predictedResult2.Score(3):0.####}")
            Console.WriteLine($"                                           four:  {predictedResult2.Score(4):0.####}")
            Console.WriteLine($"                                           five:  {predictedResult2.Score(5):0.####}")
            Console.WriteLine($"                                           six:   {predictedResult2.Score(6):0.####}")
            Console.WriteLine($"                                           seven: {predictedResult2.Score(7):0.####}")
            Console.WriteLine($"                                           eight: {predictedResult2.Score(8):0.####}")
            Console.WriteLine($"                                           nine:  {predictedResult2.Score(9):0.####}")
            Console.WriteLine()

        End Sub

    End Class

End Namespace