
Imports Microsoft.ML
Imports System.IO
Imports Microsoft.ML.DataOperationsCatalog
Imports ICSharpCode.SharpZipLib.Zip
Imports BCCreditCardFraudDetection.Trainer.Common
Imports BCCreditCardFraudDetection.Common.CreditCardFraudDetection.Common.DataModels

Namespace BCCreditCardFraudDetection.Trainer

    Public Class Program

        Private Const creditcardFraudDatasetZip As String = "creditcardfraud-dataset.zip"
        Private Const creditcardFraudDatasetUrl As String = "https://bit.ly/3GtwH1S"

        Shared Sub Main(args() As String)

            ' File paths
            Dim assetsRelativePath As String = "../../../assets"
            Dim assetsPath As String = GetAbsolutePath(assetsRelativePath)

            Dim commonDatasetsRelativePath As String = "../../../../../../../../datasets"
            Dim commonDatasetsPath As String = GetAbsolutePath(commonDatasetsRelativePath)

            GetResult(assetsPath, commonDatasetsPath)

            Console.WriteLine("=============== Press any key ===============")
            Console.ReadKey()

        End Sub

        Public Shared Function GetResult(
                myAssetsPath As String, myCommonDatasetsPath As String) As Boolean

            Dim datasetFolder As String = Path.Combine(myAssetsPath, "input")
            Dim fullDataSetFilePath As String = Path.Combine(myAssetsPath, "input", "creditcard.csv")
            Dim trainDataSetFilePath As String = Path.Combine(myAssetsPath, "output", "trainData.csv")
            Dim testDataSetFilePath As String = Path.Combine(myAssetsPath, "output", "testData.csv")
            Dim modelFilePath As String = Path.Combine(myAssetsPath, "output", "fastTree.zip")

            'GetDataSet(fullDataSetFilePath, datasetFolder,
            '    creditcardFraudDatasetUrl, creditcardFraudDatasetZip, commonDatasetsPath)
            Dim destFiles As New List(Of String) From {fullDataSetFilePath}
            DownloadBigFile(datasetFolder, creditcardFraudDatasetUrl, creditcardFraudDatasetZip,
                myCommonDatasetsPath, destFiles)

            ' Create a common ML.NET context
            ' Seed set to any number so you have a deterministic environment for repeateable results
            Dim mlContext As New MLContext() 'seed:=1

            ' Prepare data and create Train/Test split datasets
            PrepDatasets(mlContext, fullDataSetFilePath, trainDataSetFilePath, testDataSetFilePath)

            ' Load Datasets
            Dim trainingDataView As IDataView = mlContext.Data.LoadFromTextFile(
                Of TransactionObservation)(trainDataSetFilePath, separatorChar:=","c, hasHeader:=True)
            Dim testDataView As IDataView = mlContext.Data.LoadFromTextFile(
                Of TransactionObservation)(testDataSetFilePath, separatorChar:=","c, hasHeader:=True)

            ' Train Model
            ' C#: (ITransformer model, string trainerName) = TrainModel(mlContext, trainingDataView);
            Dim score# = 0
            With TrainModel(mlContext, trainingDataView)
                ' Evaluate quality of Model
                EvaluateModel(mlContext, .model, testDataView, .trainerName, score)

                ' Save model
                SaveModel(mlContext, .model, modelFilePath, trainingDataView.Schema)
            End With

            Dim scoreRounded = Math.Round(score, digits:=4) * 100
            Dim scoreExpected = 99
            Dim success = scoreRounded >= scoreExpected
            Console.WriteLine("Success: Score = " & scoreRounded & " >= " & scoreExpected & " : " & success)

            Return success

        End Function

        Public Shared Sub PrepDatasets(mlContext As MLContext, fullDataSetFilePath As String,
                                       trainDataSetFilePath As String, testDataSetFilePath As String)

            ' Only prep-datasets if train and test datasets don't exist yet

            If Not File.Exists(trainDataSetFilePath) AndAlso Not File.Exists(testDataSetFilePath) Then

                Console.WriteLine("===== Preparing train/test datasets =====")

                ' Load the original single dataset
                Dim originalFullData As IDataView = mlContext.Data.LoadFromTextFile(
                    Of TransactionObservation)(fullDataSetFilePath, separatorChar:=","c, hasHeader:=True)

                ' Split the data 80:20 into train and test sets, train and evaluate
                Dim trainTestData As TrainTestData = mlContext.Data.TrainTestSplit(
                    originalFullData, testFraction:=0.2, seed:=1)
                Dim trainData As IDataView = trainTestData.TrainSet
                Dim testData As IDataView = trainTestData.TestSet

                ' Inspect TestDataView to make sure there are true and false observations
                '  in test dataset, after spliting 
                InspectData(mlContext, testData, 4)

                ' Save train split
                Using fileStream = File.Create(trainDataSetFilePath)
                    mlContext.Data.SaveAsText(trainData, fileStream, separatorChar:=","c,
                                              headerRow:=True, schema:=True)
                End Using

                ' Save test split 
                Using fileStream = File.Create(testDataSetFilePath)
                    mlContext.Data.SaveAsText(testData, fileStream, separatorChar:=","c,
                                              headerRow:=True, schema:=True)
                End Using

            End If

        End Sub

        Public Shared Function TrainModel(mlContext As MLContext,
                trainDataView As IDataView) As (model As ITransformer, trainerName As String)

            ' Get all the feature column names (All except the Label and the IdPreservationColumn)
            Dim featureColumnNames() As String =
                trainDataView.Schema.AsQueryable().Select(
                    Function(column) column.Name).
                    Where(Function(name) name <> NameOf(TransactionObservation.Label)).
                    Where(Function(name) name <> "IdPreservationColumn").
                    Where(Function(name) name <> "Time").ToArray()

            ' Create the data process pipeline
            Dim dataProcessPipeline As IEstimator(Of ITransformer) =
                mlContext.Transforms.Concatenate("Features", featureColumnNames).
                Append(mlContext.Transforms.DropColumns(New String() {"Time"})).
                Append(mlContext.Transforms.NormalizeMeanVariance(
                    inputColumnName:="Features", outputColumnName:="FeaturesNormalizedByMeanVar"))

            ' (OPTIONAL) Peek data (such as 2 records) in training DataView after applying the ProcessPipeline's transformations into "Features" 
            ConsoleHelper.PeekDataViewInConsole(mlContext, trainDataView, dataProcessPipeline, 2)
            ConsoleHelper.PeekVectorColumnDataInConsole(mlContext, "Features",
                                                        trainDataView, dataProcessPipeline, 1)

            ' Set the training algorithm
            Dim trainer = mlContext.BinaryClassification.Trainers.FastTree(
                labelColumnName:=NameOf(TransactionObservation.Label),
                featureColumnName:="FeaturesNormalizedByMeanVar",
                numberOfLeaves:=20, numberOfTrees:=100,
                minimumExampleCountPerLeaf:=10, learningRate:=0.2)

            Dim trainingPipeline = dataProcessPipeline.Append(trainer)

            ConsoleHelper.ConsoleWriteHeader("=============== Training model ===============")

            Dim model = trainingPipeline.Fit(trainDataView)

            ConsoleHelper.ConsoleWriteHeader("=============== End of training process ===============")

            ' Append feature contribution calculator in the pipeline
            ' This will be used at prediction time for explainability
            Dim fccPipeline = model.Append(mlContext.Transforms.CalculateFeatureContribution(
                model.LastTransformer).Fit(dataProcessPipeline.Fit(trainDataView).Transform(trainDataView)))

            Return (fccPipeline, fccPipeline.ToString())

        End Function

        Private Shared Sub EvaluateModel(mlContext As MLContext, model As ITransformer,
                testDataView As IDataView, trainerName As String, ByRef score#)

            ' Evaluate the model and show accuracy stats
            Console.WriteLine("===== Evaluating Model's accuracy with Test data =====")
            Dim predictions = model.Transform(testDataView)

            Dim metrics = mlContext.BinaryClassification.Evaluate(
                data:=predictions, labelColumnName:=NameOf(TransactionObservation.Label),
                scoreColumnName:="Score")

            ConsoleHelper.PrintBinaryClassificationMetrics(trainerName, metrics)
            score = metrics.Accuracy

        End Sub

        Public Shared Function GetAbsolutePath(relativePath As String) As String
            Dim fullPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
            Return fullPath
        End Function

        Public Shared Sub InspectData(mlContext As MLContext, data As IDataView, records As Integer)

            ' We want to make sure we have True and False observations

            Console.WriteLine("Show 4 fraud transactions (true)")
            ShowObservationsFilteredByLabel(mlContext, data, label:=True, count:=records)

            Console.WriteLine("Show 4 NOT-fraud transactions (false)")
            ShowObservationsFilteredByLabel(mlContext, data, label:=False, count:=records)

        End Sub

        Public Shared Sub ShowObservationsFilteredByLabel(mlContext As MLContext, dataView As IDataView,
                Optional label As Boolean = True, Optional count As Integer = 2)

            ' Convert to an enumerable of user-defined type. 
            Dim data = mlContext.Data.CreateEnumerable(Of TransactionObservation)(
                dataView, reuseRowObject:=False).Where(Function(x) x.Label = label).Take(count).ToList()

            ' Print to console
            data.ForEach(Sub(row)
                             row.PrintToConsole()
                         End Sub)

        End Sub

        'Public Shared Sub GetDataSet(destinationFile As String,
        '        datasetFolder As String, dataSetUrl As String, dataSetZip As String,
        '        commonDatasetsPath As String)

        '    ' If a file already exists, fail!
        '    'If Not File.Exists(destinationFile) Then
        '    '    Dim destinationDirectory = Path.GetDirectoryName(destinationFile)
        '    '    ZipFile.ExtractToDirectory(zipDataSet, $"{destinationDirectory}")
        '    'End If

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

        Private Shared Sub SaveModel(mlContext As MLContext, model As ITransformer,
                                     modelFilePath As String, trainingDataSchema As DataViewSchema)
            mlContext.Model.Save(model, trainingDataSchema, modelFilePath)
            Console.WriteLine("Saved model to " & modelFilePath)
        End Sub

    End Class

End Namespace