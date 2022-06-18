
Imports Microsoft.ML
Imports System.IO
Imports WebRanking.Common ' MyWebClient
Imports WebRanking.WebRanking.DataStructures

Namespace WebRanking

    Public Class Program

        Const AssetsPath As String = "../../../Assets"
        'Const TrainDatasetUrl As String = "https://aka.ms/mlnet-resources/benchmarks/MSLRWeb10KTrain720kRows.tsv"
        'Const ValidationDatasetUrl As String = "https://aka.ms/mlnet-resources/benchmarks/MSLRWeb10KValidate240kRows.tsv"
        'Const TestDatasetUrl As String = "https://aka.ms/mlnet-resources/benchmarks/MSLRWeb10KTest240kRows.tsv"

        Const webRankingZip As String = "WebRanking.zip" 'MSLR-WEB10K.zip
        Const datasetUrl As String = "https://bit.ly/3tp3dyv"
        ' From https://github.com/dotnet/machinelearning-samples/pull/533#discussion_r297458274
        ' broken link: "https://express-tlcresources.azureedge.net/datasets/MSLR-WEB10K/MSLR-WEB10K.zip"

        Shared Sub Main(args() As String)

            Dim assetsRelativePath As String = "../../../Assets/Input"
            Dim databaseFolder As String = GetAbsolutePath(assetsRelativePath)

            Dim commonDatasetsRelativePath As String =
                "../../../../../../../../datasets"
            Dim commonDatasetsPath As String = GetAbsolutePath(commonDatasetsRelativePath)

            'Dim baseModelsRelativePath As String = "../../../MLModels"
            Dim baseModelsRelativePath As String = "../../../Assets/Output"
            Dim modelPath As String = GetAbsolutePath(baseModelsRelativePath)
            modelPath = Path.GetFullPath(modelPath)

            GetResult(databaseFolder, commonDatasetsPath, modelPath)

            Console.Write("Done!")
            Console.ReadLine()

        End Sub

        Public Shared Function GetResult(myAssetsPath As String,
                myCommonDatasetsPath As String, myModelPath As String,
                Optional isTest As Boolean = False) As Boolean

            ' Create a common ML.NET context
            ' Seed set to any number so you have a deterministic environment for repeateable results
            Dim mlContext As New MLContext() 'seed:=0
            Dim modelPath As String = Path.GetFullPath(
                Path.Combine(myModelPath, "RankingModel.zip"))

            Try
                'PrepareData(InputPath, OutputPath, TrainDatasetPath, ValidationDatasetPath, TestDatasetPath,
                '    databaseFolder, datasetUrl, webRankingZip, commonDatasetsPath)

                Dim TrainDatasetRelativePath As String =
                    Path.Combine(myAssetsPath, "MSLRWeb10KTrain720kRows.tsv")
                Dim TestDatasetRelativePath As String =
                    Path.Combine(myAssetsPath, "MSLRWeb10KTest240kRows.tsv")
                Dim ValidationDatasetRelativePath As String =
                    Path.Combine(myAssetsPath, "MSLRWeb10KValidate240kRows.tsv")

                Dim TrainDatasetPath As String = GetAbsolutePath(TrainDatasetRelativePath)
                Dim TestDatasetPath As String = GetAbsolutePath(TestDatasetRelativePath)
                Dim ValidationDatasetPath As String = GetAbsolutePath(ValidationDatasetRelativePath)

                Dim destFiles As New List(Of String) From {TrainDatasetPath, ValidationDatasetPath, TestDatasetPath}
                DownloadBigFile(myAssetsPath, datasetUrl, webRankingZip,
                    myCommonDatasetsPath, destFiles:=destFiles)

                ' Create the pipeline using the training data's schema
                '  the validation and testing data have the same schema
                Dim trainData As IDataView = mlContext.Data.LoadFromTextFile(
                    Of SearchResultData)(TrainDatasetPath, separatorChar:=CChar(vbTab), hasHeader:=True)
                Dim pipeline As IEstimator(Of ITransformer) = CreatePipeline(mlContext, trainData)

                ' Train the model on the training dataset. To perform training you need to call the Fit() method
                Console.WriteLine("===== Train the model on the training dataset =====" & vbLf)
                Dim model As ITransformer = pipeline.Fit(trainData)

                ' Evaluate the model using the metrics from the validation dataset
                '  you would then retrain and reevaluate the model until
                '  the desired metrics are achieved
                Console.WriteLine("===== Evaluate the model's result quality with the validation data =====" & vbLf)
                Dim validationData As IDataView = mlContext.Data.LoadFromTextFile(
                    Of SearchResultData)(ValidationDatasetPath, separatorChar:=CChar(vbTab), hasHeader:=False)
                EvaluateModel(mlContext, model, validationData)

                ' Combine the training and validation datasets.
                Dim validationDataEnum = mlContext.Data.CreateEnumerable(Of SearchResultData)(validationData, False)
                Dim trainDataEnum = mlContext.Data.CreateEnumerable(Of SearchResultData)(trainData, False)
                Dim trainValidationDataEnum = validationDataEnum.Concat(trainDataEnum)
                Dim trainValidationData As IDataView = mlContext.Data.LoadFromEnumerable(Of SearchResultData)(trainValidationDataEnum)

                ' Train the model on the train + validation dataset
                Console.WriteLine("===== Train the model on the training + validation dataset =====" & vbLf)
                model = pipeline.Fit(trainValidationData)

                ' Evaluate the model using the metrics from the testing dataset
                '  you do this only once and these are your final metrics
                Console.WriteLine("===== Evaluate the model's result quality with the testing data =====" & vbLf)
                Dim testData As IDataView = mlContext.Data.LoadFromTextFile(
                    Of SearchResultData)(TestDatasetPath, separatorChar:=CChar(vbTab), hasHeader:=False)
                EvaluateModel(mlContext, model, testData)

                ' Combine the training, validation, and testing datasets
                Dim testDataEnum = mlContext.Data.CreateEnumerable(Of SearchResultData)(testData, False)
                Dim allDataEnum = trainValidationDataEnum.Concat(testDataEnum)
                Dim allData As IDataView = mlContext.Data.LoadFromEnumerable(Of SearchResultData)(allDataEnum)

                ' Retrain the model on all of the data, train + validate + test
                Console.WriteLine("===== Train the model on the training + validation + test dataset =====" & vbLf)
                model = pipeline.Fit(allData)

                ' Save and consume the model to perform predictions
                ' Normally, you would use new incoming data
                '  however, for the purposes of this sample, we'll reuse the test data
                '  to show how to do predictions
                ConsumeModel(mlContext, model, modelPath, testData)

                Dim success = True ' No exception: Success!
                Console.WriteLine("Success: " & success)
                Return success

            Catch e As Exception
                Console.WriteLine(e.Message)
                Return False
            End Try

        End Function

        'Private Shared Sub PrepareData(inputPath As String, outputPath As String,
        '        trainDatasetPath As String, validationDatasetPath As String, testDatasetPath As String,
        '        databaseFolder As String, dataSetUrl As String, dataSetZip As String,
        '        commonDatasetsPath As String)

        '    Console.WriteLine("===== Prepare data =====" & vbLf)

        '    If Not Directory.Exists(outputPath) Then
        '        Directory.CreateDirectory(outputPath)
        '    End If

        '    If Not Directory.Exists(inputPath) Then
        '        Directory.CreateDirectory(inputPath)
        '    End If

        '    ' Ok, but not compressed!
        '    'If Not File.Exists(trainDatasetPath) Then
        '    '    Console.WriteLine("===== Download the train dataset - this may take several minutes =====" & vbLf)
        '    '    Using client = New WebClient
        '    '        client.DownloadFile(trainDatasetUrl, Program.TrainDatasetPath)
        '    '    End Using
        '    'End If

        '    'If Not File.Exists(validationDatasetPath) Then
        '    '    Console.WriteLine("===== Download the validation dataset - this may take several minutes =====" & vbLf)
        '    '    Using client = New WebClient
        '    '        client.DownloadFile(validationDatasetUrl, validationDatasetPath)
        '    '    End Using
        '    'End If

        '    'If Not File.Exists(testDatasetPath) Then
        '    '    Console.WriteLine("===== Download the test dataset - this may take several minutes =====" & vbLf)
        '    '    Using client = New WebClient
        '    '        client.DownloadFile(testDatasetUrl, testDatasetPath)
        '    '    End Using
        '    'End If

        '    Dim zipPath = Path.Combine(databaseFolder, dataSetZip)
        '    Dim commonPath = Path.Combine(commonDatasetsPath, dataSetZip)

        '    If Not File.Exists(zipPath) AndAlso File.Exists(commonPath) Then
        '        IO.File.Copy(commonPath, zipPath)
        '    ElseIf File.Exists(zipPath) AndAlso Not File.Exists(commonPath) Then
        '        IO.File.Copy(zipPath, commonPath)
        '    End If

        '    If File.Exists(trainDatasetPath) AndAlso
        '       File.Exists(validationDatasetPath) AndAlso
        '       File.Exists(testDatasetPath) Then Exit Sub

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
        '        myFastZip.ExtractZip(zipPath, databaseFolder, fileFilter:=String.Empty)
        '        Console.WriteLine("====Extracting is completed====")
        '        If File.Exists(trainDatasetPath) AndAlso
        '           File.Exists(validationDatasetPath) AndAlso
        '           File.Exists(testDatasetPath) Then
        '            Console.WriteLine("====Extracting is OK====")
        '        Else
        '            Console.WriteLine("====Extracting: Fail!====")
        '            Stop
        '        End If
        '    End If

        '    ' Common Download And UnZip
        '    'Common.Web.Download(dataSetUrl, Program.TrainDatasetPath, dataSetZip)
        '    'Dim zipPath = Path.Join(Program.TrainDatasetPath, dataSetZip)
        '    'Common.Compress.UnZip(zipPath, Program.TrainDatasetPath)

        'End Sub

        Private Shared Function CreatePipeline(
                mlContext As MLContext, dataView As IDataView) As IEstimator(Of ITransformer)

            Const FeaturesVectorName As String = "Features"

            Console.WriteLine("===== Set up the trainer =====" & vbLf)

            ' Specify the columns to include in the feature input data
            Dim featureCols = dataView.Schema.AsQueryable().Select(
                Function(s) s.Name).Where(
                    Function(c) c <> NameOf(SearchResultData.Label) AndAlso
                                c <> NameOf(SearchResultData.GroupId)).ToArray()

            ' Create an Estimator and transform the data:
            ' 1. Concatenate the feature columns into a single Features vector
            ' 2. Create a key type for the label input data by using the value to key transform
            ' 3. Create a key type for the group input data by using a hash transform
            Dim dataPipeline As IEstimator(Of ITransformer) =
                mlContext.Transforms.Concatenate(FeaturesVectorName, featureCols).
                Append(mlContext.Transforms.Conversion.MapValueToKey(
                    NameOf(SearchResultData.Label))).
                Append(mlContext.Transforms.Conversion.Hash(
                    NameOf(SearchResultData.GroupId),
                    NameOf(SearchResultData.GroupId), numberOfBits:=20))

            ' Set the LightGBM LambdaRank trainer
            Dim trainer As IEstimator(Of ITransformer) =
                mlContext.Ranking.Trainers.LightGbm(
                    labelColumnName:=NameOf(SearchResultData.Label),
                    featureColumnName:=FeaturesVectorName,
                    rowGroupColumnName:=NameOf(SearchResultData.GroupId))
            Dim trainerPipeline As IEstimator(Of ITransformer) = dataPipeline.Append(trainer)

            Return trainerPipeline

        End Function

        Private Shared Sub EvaluateModel(mlContext As MLContext, model As ITransformer,
                                         data As IDataView)

            ' Use the model to perform predictions on the test data
            Dim predictions As IDataView = model.Transform(data)

            Console.WriteLine("===== Use metrics for the data using NDCG@3 =====" & vbLf)

            ' Evaluate the metrics for the data using NDCG; by default, metrics for the up to
            '  3 search results in the query are reported (e.g. NDCG@3)
            WebRanking.CommonWR.ConsoleHelper.EvaluateMetrics(mlContext, predictions)
            'Common.ConsoleHelper.EvaluateMetrics(mlContext, predictions)

            Console.WriteLine("===== Use metrics for the data using NDCG@10 =====" & vbLf)

            ' TO CHECK:
            ' Evaluate metrics for up to 10 search results (e.g. NDCG@10)
            'ConsoleHelper.EvaluateMetrics(mlContext, predictions, 10)

        End Sub

        Private Shared Sub ConsumeModel(mlContext As MLContext, model As ITransformer,
                                        modelPath As String, data As IDataView)

            Console.WriteLine("===== Save the model =====" & vbLf)

            ' Save the model
            Dim parentDir = IO.Path.GetDirectoryName(modelPath)
            If Not Directory.Exists(parentDir) Then Directory.CreateDirectory(parentDir)
            mlContext.Model.Save(model, Nothing, modelPath)

            Console.WriteLine("===== Consume the model =====" & vbLf)

            ' Load the model to perform predictions with it
            Dim predictionPipelineSchema As DataViewSchema = Nothing
            Dim predictionPipeline As ITransformer = mlContext.Model.Load(
                modelPath, predictionPipelineSchema)

            ' Predict rankings.
            Dim predictions As IDataView = predictionPipeline.Transform(data)

            ' In the predictions, get the scores of the search results included in the first query (e.g. group)
            Dim searchQueries As IEnumerable(Of SearchResultPrediction) =
                mlContext.Data.CreateEnumerable(Of SearchResultPrediction)(predictions, reuseRowObject:=False)
            Dim firstGroupId = searchQueries.First().GroupId
            Dim firstGroupPredictions As IEnumerable(Of SearchResultPrediction) =
                searchQueries.
                Take(100).
                Where(Function(p) p.GroupId = firstGroupId).
                OrderByDescending(Function(p) p.Score).ToList()

            ' The individual scores themselves are NOT a useful measure of result quality;
            '  instead, they are only useful as a relative measure to other scores in the group
            ' The scores are used to determine the ranking where a higher score indicates a higher ranking
            '  versus another candidate result
            WebRanking.CommonWR.ConsoleHelper.PrintScores(firstGroupPredictions)
            'Common.ConsoleHelper.PrintScores(firstGroupPredictions)

        End Sub

        Public Shared Function GetAbsolutePath(relativePath As String) As String
            Dim fullPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
            Return fullPath
        End Function

    End Class

End Namespace