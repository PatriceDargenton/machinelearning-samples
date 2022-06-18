
Imports System.IO
Imports Microsoft.ML
Imports Microsoft.ML.DataOperationsCatalog
Imports SentimentAnalysisConsoleApp.Common
Imports SentimentAnalysisConsoleApp.SentimentAnalysisConsoleApp.DataStructures

Namespace SentimentAnalysisConsoleApp

    Public Module Program

        Private Const datasetFile = "wikiDetoxAnnotated40kRows"
        Private Const datasetZip As String = datasetFile & ".zip"
        Private Const datasetUrl As String = "https://bit.ly/3tiuGls"

        Private ReadOnly commonDatasetsRelativePath As String =
            "../../../../../../../../../datasets"
        Private ReadOnly commonDatasetsPath As String = GetAbsolutePath(commonDatasetsRelativePath)

        Private ReadOnly BaseDatasetsRelativePath As String = "../../../../Data"

        Private ReadOnly BaseModelsRelativePath As String = "../../../../MLModels"

        Sub Main(args() As String)

            GetResult(BaseDatasetsRelativePath, commonDatasetsPath)

            Console.WriteLine($"================End of Process. Hit any key to exit==================================")
            Console.ReadLine()

        End Sub

        Public Function GetResult(
                myAssetsPath As String, myCommonDatasetsPath As String) As Boolean

            Dim datasetFolder As String = myAssetsPath
            'Dim fullDataSetFilePath As String = DataRelativePath
            'GetDataSet(fullDataSetFilePath, datasetFolder, datasetUrl, datasetZip, commonDatasetsPath)
            Dim DataRelativePath As String = Path.Combine(myAssetsPath, datasetFile & ".tsv")
            Dim DataPath As String = GetAbsolutePath(DataRelativePath)
            Dim ModelRelativePath As String = Path.Combine(myAssetsPath, "SentimentModel.zip")
            Dim ModelPath As String = GetAbsolutePath(ModelRelativePath)
            Dim destFiles As New List(Of String) From {DataRelativePath}
            DownloadBigFile(datasetFolder, datasetUrl, datasetZip, myCommonDatasetsPath, destFiles)

            ' Create MLContext to be shared across the model creation workflow objects 
            ' Set a random seed for repeatable/deterministic results across multiple trainings
            Dim mlContext As New MLContext() 'seed:=1

            ' STEP 1: Common data loading configuration
            Dim dataView As IDataView = mlContext.Data.LoadFromTextFile(
                Of SentimentIssue)(DataPath, hasHeader:=True)

            Dim trainTestSplit As TrainTestData = mlContext.Data.TrainTestSplit(
                dataView, testFraction:=0.2)
            Dim trainingData As IDataView = trainTestSplit.TrainSet
            Dim testData As IDataView = trainTestSplit.TestSet

            ' STEP 2: Common data process configuration with pipeline data transformations          
            Dim dataProcessPipeline = mlContext.Transforms.Text.FeaturizeText(
                outputColumnName:="Features", inputColumnName:=NameOf(SentimentIssue.Text))

            ' STEP 3: Set the training algorithm, then create and config the modelBuilder                            
            Dim trainer = mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(
                labelColumnName:="Label", featureColumnName:="Features")
            Dim trainingPipeline = dataProcessPipeline.Append(trainer)

            ' STEP 4: Train the model fitting to the DataSet
            Dim trainedModel As ITransformer = trainingPipeline.Fit(trainingData)

            ' STEP 5: Evaluate the model and show accuracy stats
            Dim predictions = trainedModel.Transform(testData)
            Dim metrics = mlContext.BinaryClassification.Evaluate(
                data:=predictions, labelColumnName:="Label", scoreColumnName:="Score")

            ConsoleHelper.PrintBinaryClassificationMetrics(trainer.ToString(), metrics)

            Dim directoryPath = IO.Path.GetDirectoryName(ModelPath)
            If Not IO.Directory.Exists(directoryPath) Then
                Dim di As New IO.DirectoryInfo(directoryPath)
                di.Create()
            End If
            ' STEP 6: Save/persist the trained model to a .ZIP file
            mlContext.Model.Save(trainedModel, trainingData.Schema, ModelPath)

            Console.WriteLine("The model is saved to {0}", ModelPath)

            ' TRY IT: Make a single test prediction, loading the model from .ZIP file
            Dim sampleStatement As SentimentIssue = New SentimentIssue With {
                .Text = "I love this movie!"}

            ' Create prediction engine related to the loaded trained model
            Dim predEngine = mlContext.Model.CreatePredictionEngine(
                Of SentimentIssue, SentimentPrediction)(trainedModel)

            ' Score
            Dim resultprediction = predEngine.Predict(sampleStatement)

            Console.WriteLine($"=============== Single Prediction  ===============")
            Console.WriteLine($"Text: {sampleStatement.Text} | Prediction: {(If(Convert.ToBoolean(resultprediction.Prediction), "Toxic", "Non Toxic"))} sentiment | Probability of being toxic: {resultprediction.Probability} ")

            Dim scoreRounded = Math.Round(metrics.Accuracy, digits:=4) * 100
            Dim scoreExpected = 94
            Dim success = scoreRounded >= scoreExpected
            Console.WriteLine("Success: Score = " & scoreRounded.ToString("0.00") & " >= " & scoreExpected & " : " & success)

            Return success

        End Function

        'Public Sub GetDataSet(destinationFile As String,
        '        datasetFolder As String, dataSetUrl As String, dataSetZip As String,
        '        commonDatasetsPath As String)

        '    Dim zipPath = Path.Combine(datasetFolder, dataSetZip)
        '    Dim commonPath = Path.Combine(commonDatasetsPath, dataSetZip)

        '    If Not File.Exists(zipPath) AndAlso File.Exists(commonPath) Then
        '        IO.File.Copy(commonPath, zipPath)
        '    ElseIf File.Exists(zipPath) AndAlso Not File.Exists(commonPath) Then
        '        IO.File.Copy(zipPath, commonPath)
        '    End If

        '    If File.Exists(destinationFile) Then Exit Sub

        '    If Not File.Exists(zipPath) Then
        '        Console.WriteLine("====Downloading zip====")
        '        Using client = New MyWebClient
        '            ' The code below will download a dataset from a third-party,
        '            '  and may be governed by separate third-party terms
        '            ' By proceeding, you agree to those separate terms
        '            client.DownloadFile(dataSetUrl, zipPath)
        '        End Using
        '        Console.WriteLine("====Downloading is completed====")
        '        IO.File.Copy(zipPath, commonPath)
        '    End If

        '    If File.Exists(zipPath) Then
        '        Console.WriteLine("====Extracting zip====")
        '        Dim myFastZip As New FastZip()
        '        myFastZip.ExtractZip(zipPath, datasetFolder, fileFilter:=String.Empty)
        '        Console.WriteLine("====Extracting is completed====")
        '        If File.Exists(destinationFile) Then
        '            Console.WriteLine("====Extracting is OK====")
        '        Else
        '            Console.WriteLine("====Extracting: Fail!====")
        '            Stop
        '        End If
        '    End If

        'End Sub

        Public Function GetAbsolutePath(relativePath As String) As String
            Dim fullPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
            Return fullPath
        End Function

    End Module

End Namespace