
Imports System.IO
Imports Microsoft.ML
Imports Microsoft.ML.Data

Imports MulticlassClassification_Iris.MulticlassClassification_Iris.DataStructures

Namespace MulticlassClassification_Iris

    Public Module Program

        Public Sub Main()

            Dim assetsRelativePath As String = "../../../../Data"
            Dim assetsPath As String = GetAbsolutePath(assetsRelativePath)
            Dim baseModelsRelativePath As String = "../../../../MLModels"
            Dim modelPath As String = GetAbsolutePath(baseModelsRelativePath)

            GetResult(assetsPath, modelPath)

            Console.WriteLine("=============== End of process, hit any key to finish ===============")
            Console.ReadKey()

        End Sub

        Public Function GetResult(
                myAssetsPath As String, myModelPath As String,
                Optional isTest As Boolean = False) As Boolean

            ' Create MLContext to be shared across the model creation workflow objects 
            ' Set a random seed for repeatable/deterministic results across multiple trainings
            Dim mlContext As New MLContext() 'seed:=0
            Dim modelPath As String = Path.GetFullPath(Path.Combine(myModelPath,
                "IrisClassificationModel.zip"))

            ' 1.
            Dim score# = 0
            BuildTrainEvaluateAndSaveModel(myAssetsPath, modelPath, mlContext, isTest, score)

            ' 2.
            TestSomePredictions(modelPath, mlContext)

            Dim scoreRounded = Math.Round(score, digits:=4) * 100
            Dim scoreExpected = 100
            Dim success = scoreRounded >= scoreExpected
            Console.WriteLine("Success: Score = " & scoreRounded.ToString("0.00") & " >= " & scoreExpected & " : " & success)

            Return success

        End Function

        Private Sub BuildTrainEvaluateAndSaveModel(
                myAssetsPath As String, myModelPath As String,
                mlContext As MLContext, isTest As Boolean, ByRef score#)

            Dim trainDataRelativePath As String = Path.Combine(myAssetsPath, "iris-train.txt")
            Dim testDataRelativePath As String = Path.Combine(myAssetsPath, "iris-test.txt")
            Dim trainDataPath As String = GetAbsolutePath(trainDataRelativePath)
            Dim testDataPath As String = GetAbsolutePath(testDataRelativePath)
            trainDataPath = Path.GetFullPath(trainDataPath)
            testDataPath = Path.GetFullPath(testDataPath)

            ' STEP 1: Common data loading configuration
            Dim trainingDataView = mlContext.Data.LoadFromTextFile(Of IrisData)(
                trainDataPath, hasHeader:=True)
            Dim testDataView = mlContext.Data.LoadFromTextFile(Of IrisData)(
                testDataPath, hasHeader:=True)

            ' STEP 2: Common data process configuration with pipeline data transformations
            Dim dataProcessPipeline = mlContext.Transforms.Conversion.MapValueToKey(
                outputColumnName:="KeyColumn",
                inputColumnName:=NameOf(IrisData.Label)).
                Append(mlContext.Transforms.Concatenate(
                    "Features",
                    NameOf(IrisData.SepalLength),
                    NameOf(IrisData.SepalWidth),
                    NameOf(IrisData.PetalLength),
                    NameOf(IrisData.PetalWidth)).
                AppendCacheCheckpoint(mlContext))
            ' Use in-memory cache for small/medium datasets to lower training time
            ' Do NOT use it (remove .AppendCacheCheckpoint()) when handling very large datasets

            ' STEP 3: Set the training algorithm, then append the trainer to the pipeline  
            Dim trainer = mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                labelColumnName:="KeyColumn", featureColumnName:="Features").
                Append(mlContext.Transforms.Conversion.MapKeyToValue(
                    outputColumnName:=NameOf(IrisData.Label), inputColumnName:="KeyColumn"))

            Dim trainingPipeline = dataProcessPipeline.Append(trainer)

            ' STEP 4: Train the model fitting to the DataSet
            Console.WriteLine("=============== Training the model ===============")
            Dim trainedModel As ITransformer = trainingPipeline.Fit(trainingDataView)

            ' STEP 5: Evaluate the model and show accuracy stats
            Console.WriteLine("===== Evaluating Model's accuracy with Test data =====")
            Dim predictions = trainedModel.Transform(testDataView)
            Dim metrics = mlContext.MulticlassClassification.Evaluate(predictions, "Label", "Score")
            score = metrics.MacroAccuracy

            Common.ConsoleHelper.PrintMultiClassClassificationMetrics(trainer.ToString(), metrics)

            ' STEP 6: Save/persist the trained model to a .ZIP file
            Dim directoryPath = IO.Path.GetDirectoryName(myModelPath)
            If Not IO.Directory.Exists(directoryPath) Then
                Dim di As New IO.DirectoryInfo(directoryPath)
                di.Create()
            End If
            mlContext.Model.Save(trainedModel, trainingDataView.Schema, myModelPath)
            Console.WriteLine("The model is saved to {0}", myModelPath)

        End Sub

        Private Sub TestSomePredictions(myModelPath As String, mlContext As MLContext)

            ' Test Classification Predictions with some hard-coded samples 
            Dim modelInputSchema As DataViewSchema = Nothing
            Dim trainedModel As ITransformer = mlContext.Model.Load(myModelPath, modelInputSchema)

            ' Create prediction engine related to the loaded trained model
            Dim predEngine = mlContext.Model.CreatePredictionEngine(Of IrisData, IrisPrediction)(trainedModel)

            ' During prediction we will get Score column with 3 float values
            ' We need to find way to map each score to original label
            ' In order to do that we need to get TrainingLabelValues from Score column
            ' TrainingLabelValues on top of Score column represent original labels for i-th value in Score array
            ' Let's look how we can convert key value for PredictedLabel to original labels
            ' We need to read KeyValues for "PredictedLabel" column
            Dim keys As VBuffer(Of Single) = Nothing
            predEngine.OutputSchema("PredictedLabel").GetKeyValues(keys)
            Dim labelsArray = keys.DenseValues().ToArray()

            ' Since we apply MapValueToKey estimator with default parameters, key values
            ' depends on order of occurence in data file. Which is "Iris-setosa", "Iris-versicolor", "Iris-virginica"
            ' So if we have Score column equal to [0.2, 0.3, 0.5] that's mean what score for:
            '  Iris-setosa is 0.2
            '  Iris-versicolor is 0.3
            '  Iris-virginica is 0.5
            ' Add a dictionary to map the above float values to strings
            Dim IrisFlowers As New Dictionary(Of Single, String)
            IrisFlowers.Add(0, "Setosa")
            IrisFlowers.Add(1, "versicolor")
            IrisFlowers.Add(2, "virginica")

            Console.WriteLine("=====Predicting using model====")
            ' Score sample 1
            Dim resultprediction1 = predEngine.Predict(SampleIrisData.Iris1)

            Console.WriteLine($"Actual: setosa.     Predicted label and score:  {IrisFlowers(labelsArray(0))}: {resultprediction1.Score(0):0.####}")
            Console.WriteLine($"                                                {IrisFlowers(labelsArray(1))}: {resultprediction1.Score(1):0.####}")
            Console.WriteLine($"                                                {IrisFlowers(labelsArray(2))}: {resultprediction1.Score(2):0.####}")
            Console.WriteLine()

            ' Score sample 2
            Dim resultprediction2 = predEngine.Predict(SampleIrisData.Iris2)

            Console.WriteLine($"Actual: Virginica.   Predicted label and score:  {IrisFlowers(labelsArray(0))}: {resultprediction2.Score(0):0.####}")
            Console.WriteLine($"                                                 {IrisFlowers(labelsArray(1))}: {resultprediction2.Score(1):0.####}")
            Console.WriteLine($"                                                 {IrisFlowers(labelsArray(2))}: {resultprediction2.Score(2):0.####}")
            Console.WriteLine()

            ' Score sample 3
            Dim resultprediction3 = predEngine.Predict(SampleIrisData.Iris3)

            Console.WriteLine($"Actual: Versicolor.   Predicted label and score: {IrisFlowers(labelsArray(0))}: {resultprediction3.Score(0):0.####}")
            Console.WriteLine($"                                                 {IrisFlowers(labelsArray(1))}: {resultprediction3.Score(1):0.####}")
            Console.WriteLine($"                                                 {IrisFlowers(labelsArray(2))}: {resultprediction3.Score(2):0.####}")
            Console.WriteLine()

        End Sub

        Public Function GetAbsolutePath(relativePath As String) As String
            Dim fullPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
            Return fullPath
        End Function

    End Module

End Namespace