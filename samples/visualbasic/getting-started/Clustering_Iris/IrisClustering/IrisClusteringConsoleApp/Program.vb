
Imports System.IO
Imports Microsoft.ML
Imports Clustering_Iris.Common
Imports Clustering_Iris.Clustering_Iris.DataStructures
Imports Microsoft.ML.Data

Namespace Clustering_Iris

    Public Module Program

        Private BaseDatasetsRelativePath As String = "../../../../Data"
        Private BaseModelsRelativePath As String = "../../../../MLModels"

        Private trainingDataView As IDataView
        Private testingDataView As IDataView

        Public Sub Main(args() As String)

            GetResult(BaseDatasetsRelativePath, BaseModelsRelativePath)

            Console.WriteLine("=============== End of process, hit any key to finish ===============")
            Console.ReadKey()

        End Sub

        Public Function GetResult(myDataPath As String, myModelPath As String) As Boolean

            Dim datasetPath = Path.GetFullPath(Path.Combine(myDataPath, "iris-full.txt"))
            Dim modelPath = Path.GetFullPath(Path.Combine(myModelPath, "IrisModel.zip"))

            ' Create the MLContext to share across components for deterministic results
            ' Seed set to any number so you have a deterministic environment
            Dim mlContext As New MLContext() 'seed:=1

            ' STEP 1: Common data loading configuration            
            Dim fullData As IDataView = mlContext.Data.LoadFromTextFile(
                path:=datasetPath,
                columns:=CType({
                    New TextLoader.Column("Label", DataKind.Single, 0),
                    New TextLoader.Column(NameOf(IrisData.SepalLength), DataKind.Single, 1),
                    New TextLoader.Column(NameOf(IrisData.SepalWidth), DataKind.Single, 2),
                    New TextLoader.Column(NameOf(IrisData.PetalLength), DataKind.Single, 3),
                    New TextLoader.Column(NameOf(IrisData.PetalWidth), DataKind.Single, 4)
                }, TextLoader.Column()),
                hasHeader:=True,
                separatorChar:=CChar(vbTab))

            ' Split dataset in two parts: TrainingDataset (80%) and TestDataset (20%)
            Dim trainTestData As DataOperationsCatalog.TrainTestData =
                mlContext.Data.TrainTestSplit(fullData, testFraction:=0.2)
            trainingDataView = trainTestData.TrainSet
            testingDataView = trainTestData.TestSet

            ' STEP 2: Process data transformations in pipeline
            Dim dataProcessPipeline = mlContext.Transforms.Concatenate(
                "Features",
                NameOf(IrisData.SepalLength),
                NameOf(IrisData.SepalWidth),
                NameOf(IrisData.PetalLength),
                NameOf(IrisData.PetalWidth))

            ' (Optional) Peek data in training DataView after applying the ProcessPipeline's transformations  
            ConsoleHelper.PeekDataViewInConsole(
                mlContext, trainingDataView, dataProcessPipeline, 10)
            ConsoleHelper.PeekVectorColumnDataInConsole(
                mlContext, "Features", trainingDataView, dataProcessPipeline, 10)

            ' STEP 3: Create and train the model     
            Dim trainer = mlContext.Clustering.Trainers.KMeans(
                featureColumnName:="Features", numberOfClusters:=3)
            Dim trainingPipeline = dataProcessPipeline.Append(trainer)
            Dim trainedModel = trainingPipeline.Fit(trainingDataView)

            ' STEP4: Evaluate accuracy of the model
            Dim predictions As IDataView = trainedModel.Transform(testingDataView)
            Dim metrics = mlContext.Clustering.Evaluate(
                predictions, scoreColumnName:="Score", featureColumnName:="Features")

            ConsoleHelper.PrintClusteringMetrics(trainer.ToString(), metrics)

            ' STEP5: Save/persist the model as a .ZIP file
            Dim directoryPath = IO.Path.GetDirectoryName(modelPath)
            If Not IO.Directory.Exists(directoryPath) Then
                Dim di As New IO.DirectoryInfo(directoryPath)
                di.Create()
            End If
            mlContext.Model.Save(trainedModel, trainingDataView.Schema, modelPath)

            Console.WriteLine("=============== End of training process ===============")

            Console.WriteLine("=============== Predict a cluster for a single case (Single Iris data sample) ===============")

            ' Test with one sample text 
            Dim sampleIrisData = New IrisData With {
                .SepalLength = 3.3F,
                .SepalWidth = 1.6F,
                .PetalLength = 0.2F,
                .PetalWidth = 5.1F
            }

            Dim modelInputSchema As DataViewSchema = Nothing
            Dim model As ITransformer = mlContext.Model.Load(ModelPath, modelInputSchema)
            ' Create prediction engine related to the loaded trained model
            Dim predEngine = mlContext.Model.CreatePredictionEngine(Of IrisData, IrisPrediction)(model)

            ' Score
            Dim resultprediction = predEngine.Predict(sampleIrisData)

            Console.WriteLine($"Cluster assigned for setosa flowers:" &
                resultprediction.SelectedClusterId)

            Dim dbiIndex = metrics.DaviesBouldinIndex
            Dim indRounded = Math.Round(dbiIndex, digits:=4)
            Dim indExpected = 1
            Dim success = indRounded <= indExpected
            Console.WriteLine("Success: Index = " & indRounded.ToString("0.00") & " <= " & indExpected & " : " & success)

            Return success

        End Function

        'Public Function GetAbsolutePath(relativePath As String) As String
        '    Dim fullPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
        '    Return fullPath
        'End Function

    End Module

End Namespace