
Imports LargeDatasets.Common
Imports LargeDatasets.LargeDatasets.DataStructures

Imports ICSharpCode.SharpZipLib.GZip
Imports ICSharpCode.SharpZipLib.Tar

Imports Microsoft.ML
Imports Microsoft.ML.DataOperationsCatalog

Imports System.IO

Namespace LargeDatasets

    Public Class Program

        Const dir_svmlight As String = "url_svmlight"
        Const zip_svmlight As String = dir_svmlight & ".tar.gz"
        Const url_svmlight As String =
            "https://archive.ics.uci.edu/ml/machine-learning-databases/url/url_svmlight.tar.gz"
        'Const zip_svmlight As String = dir_svmlight & ".zip"
        'Const url_svmlight As String = "https://bit.ly/3rImsR4"

        Shared Sub Main(args() As String)

            Dim assetsRelativePath As String = "../../../Data/OriginalUrlData"
            Dim assetsPath As String = GetAbsolutePath(assetsRelativePath)

            Dim commonDatasetsRelativePath As String = "../../../../../../../../datasets"
            Dim commonDatasetsPath As String = GetAbsolutePath(commonDatasetsRelativePath)

            GetResult(assetsPath, commonDatasetsPath)

            Console.WriteLine("====End of Process. Press any key to exit====")
            Console.ReadLine()

        End Sub

        Public Shared Function GetResult(
                myAssetsPath As String, myCommonDatasetsPath As String) As Boolean

            ' STEP 1: Download dataset
            'DownloadDataset(originalDataDirectoryPath, dir_svmlight, url_svmlight, tarGz_svmlight,
            'myCommonDatasetsPath)
            Dim datasetFolder = myAssetsPath 'originalDataDirectoryRelativePath
            Dim originalDataRelativePath As String =
                Path.Combine(myAssetsPath, "url_svmlight") '"../../../Data/OriginalUrlData/url_svmlight"
            Dim preparedDataRelativePath As String =
                Path.Combine(myAssetsPath, "../PreparedUrlData", "url_svmlight") ' "../../../Data/PreparedUrlData/url_svmlight"
            Dim originalDataPath As String = GetAbsolutePath(originalDataRelativePath)
            Dim preparedDataPath As String = GetAbsolutePath(preparedDataRelativePath)
            Dim testFilePath As String = Path.Combine(originalDataPath, "Day0.svm")
            Dim destFiles As New List(Of String) From {testFilePath}
            DownloadBigFile(datasetFolder, url_svmlight, zip_svmlight, myCommonDatasetsPath,
                destFiles, originalDataRelativePath)

            ' Step 2: Prepare data by adding second column with value total number of features
            PrepareDataset(originalDataPath, preparedDataPath)

            Dim mlContext As New MLContext

            ' STEP 3: Common data loading configuration  
            Dim fullDataView = mlContext.Data.LoadFromTextFile(Of UrlData)(
                path:=Path.Combine(preparedDataPath, "*"), hasHeader:=False, allowSparse:=True)

            ' Step 4: Divide the whole dataset into 80% training and 20% testing data
            Dim trainTestData As TrainTestData = mlContext.Data.TrainTestSplit(
                fullDataView, testFraction:=0.2, seed:=1)
            Dim trainDataView As IDataView = trainTestData.TrainSet
            Dim testDataView As IDataView = trainTestData.TestSet

            ' Step 5: Map label value from string to bool
            Dim UrlLabelMap = New Dictionary(Of String, Boolean)
            UrlLabelMap("+1") = True ' Malicious url
            UrlLabelMap("-1") = False ' Benign
            Dim dataProcessingPipeLine = mlContext.Transforms.Conversion.MapValue(
                "LabelKey", UrlLabelMap, "LabelColumn")
            ConsoleHelper.PeekDataViewInConsole(mlContext, trainDataView, dataProcessingPipeLine, 2)

            ' Step 6: Append trainer to pipeline
            Dim trainingPipeLine = dataProcessingPipeLine.Append(
                mlContext.BinaryClassification.Trainers.FieldAwareFactorizationMachine(
                    labelColumnName:="LabelKey", featureColumnName:="FeatureVector"))

            ' Step 7: Train the model
            Console.WriteLine("====Training the model=====")
            Dim mlModel = trainingPipeLine.Fit(trainDataView)
            Console.WriteLine("====Completed Training the model=====")
            Console.WriteLine("")

            ' Step 8: Evaluate the model
            Console.WriteLine("====Evaluating the model=====")
            Dim predictions = mlModel.Transform(testDataView)
            Dim metrics = mlContext.BinaryClassification.Evaluate(
                data:=predictions, labelColumnName:="LabelKey", scoreColumnName:="Score")
            ConsoleHelper.PrintBinaryClassificationMetrics(mlModel.ToString(), metrics)

            ' Try a single prediction
            Console.WriteLine("====Predicting sample data=====")
            Dim predEngine = mlContext.Model.CreatePredictionEngine(
                Of UrlData, UrlPrediction)(mlModel)
            ' Create sample data to do a single prediction with it 
            Dim sampleDatas = CreateSingleDataSample(mlContext, trainDataView)
            For Each sampleData In sampleDatas
                Dim predictionResult As UrlPrediction = predEngine.Predict(sampleData)
                Console.WriteLine(
                    $"Single Prediction --> Actual value: {sampleData.LabelColumn} | Predicted value: {predictionResult.Prediction}")
            Next sampleData

            Dim score = metrics.Accuracy
            Dim scoreRounded = Math.Round(score, digits:=4) * 100
            Dim scoreExpected = 98
            Dim success = scoreRounded >= scoreExpected
            Console.WriteLine("Success: Score = " & scoreRounded.ToString("0.00") & " >= " & scoreExpected & " : " & success)

            Return success

        End Function

        'Public Shared Sub DownloadDataset(originalDataDirectoryPath As String,
        '        dir_svmlight As String, datasetUrl As String, datasetZip As String,
        '        commonDatasetsPath As String)

        '    Dim zipPath = Path.Combine(originalDataDirectoryPath, datasetZip)
        '    Dim commonPath = Path.Combine(commonDatasetsPath, datasetZip)

        '    If Not File.Exists(zipPath) AndAlso File.Exists(commonPath) Then
        '        Dim parentDir = IO.Path.GetDirectoryName(zipPath)
        '        If Not Directory.Exists(parentDir) Then Directory.CreateDirectory(parentDir)
        '        IO.File.Copy(commonPath, zipPath)
        '    ElseIf File.Exists(zipPath) AndAlso Not File.Exists(commonPath) Then
        '        IO.File.Copy(zipPath, commonPath)
        '    End If

        '    If Not IO.File.Exists(zipPath) Then 'originalDataDirectoryPath) Then
        '        Console.WriteLine("====Downloading data====")
        '        Dim directoryPath = Path.GetDirectoryName(zipPath)
        '        Dim directoryPath2 = Path.GetDirectoryName(directoryPath)
        '        If Not Directory.Exists(directoryPath2) Then
        '            Dim di As New DirectoryInfo(directoryPath2)
        '            di.Create()
        '        End If
        '        If Not Directory.Exists(directoryPath) Then
        '            Dim di As New DirectoryInfo(directoryPath)
        '            di.Create()
        '        End If
        '        Using client = New MyWebClient
        '            ' The code below will download a dataset from a third-party, UCI (link),
        '            '  and may be governed by separate third-party terms
        '            ' By proceeding, you agree to those separate terms
        '            client.DownloadFile(datasetUrl, zipPath) 'url_svmlight zip_svmlight)
        '            Console.WriteLine("====Downloading is completed====")
        '            IO.File.Copy(zipPath, commonPath)
        '        End Using
        '    End If

        '    Dim destPath = Path.Combine(originalDataDirectoryPath, dir_svmlight)
        '    If Not Directory.Exists(destPath) Then
        '        Console.WriteLine("====Extracting zip====")
        '        Using inputStream = File.OpenRead(zipPath) 'zip_svmlight)
        '            Using gzipStream = New GZipInputStream(inputStream)
        '                Using tarArchive As TarArchive = TarArchive.CreateInputTarArchive(
        '                        gzipStream, nameEncoding:=System.Text.Encoding.Default)
        '                    tarArchive.ExtractContents(destPath)
        '                End Using
        '            End Using
        '        End Using
        '        Console.WriteLine("====Extracting is completed====")
        '    End If

        'End Sub

        Private Shared Sub PrepareDataset(originalDataPath As String, preparedDataPath As String)

            ' Create folder for prepared Data path if it does not exist
            If Not Directory.Exists(preparedDataPath) Then Directory.CreateDirectory(preparedDataPath)
            Console.WriteLine("====Preparing Data====")
            Console.WriteLine("")
            ' ML.Net API checks for number of features column before the sparse matrix format
            ' So add total number of features i.e 3231961 as second column by taking all the files from 
            '  originalDataPath and save those files in preparedDataPath
            If Directory.GetFiles(preparedDataPath).Length = 0 Then
                Dim ext = New List(Of String) From {".svm"}
                Dim filesInDirectory = Directory.GetFiles(originalDataPath, "*.*",
                    SearchOption.AllDirectories).Where(Function(s) ext.Contains(Path.GetExtension(s)))
                For Each file In filesInDirectory
                    AddFeaturesColumn(Path.GetFullPath(file), preparedDataPath)
                Next file
            End If
            Console.WriteLine("====Data Preparation is done====")
            Console.WriteLine("")
            Console.WriteLine("original data path= {0}", originalDataPath)
            Console.WriteLine("")
            Console.WriteLine("prepared data path= {0}", preparedDataPath)
            Console.WriteLine("")

        End Sub

        Private Shared Sub AddFeaturesColumn(sourceFilePath As String, preparedDataPath As String)

            Dim sourceFileName As String = Path.GetFileName(sourceFilePath)
            Dim preparedFilePath As String = Path.Combine(preparedDataPath, sourceFileName)

            ' If the file does not exist in preparedFilePath then copy from sourceFilePath and then add new column
            If Not File.Exists(preparedFilePath) Then File.Copy(sourceFilePath, preparedFilePath, True)
            Dim newColumnData As String = "3231961"
            Dim CSVDump() As String = File.ReadAllLines(preparedFilePath)
            Dim CSV As List(Of List(Of String)) =
                CSVDump.Select(Function(x) x.Split(" "c).ToList()).ToList()
            For i As Integer = 0 To CSV.Count - 1
                CSV(i).Insert(1, newColumnData)
            Next i

            File.WriteAllLines(preparedFilePath, CSV.Select(Function(x) String.Join(vbTab, x)))

        End Sub

        Public Shared Function GetAbsolutePath(relativePath As String) As String
            Dim fullPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
            Return fullPath
        End Function

        Private Shared Function CreateSingleDataSample(
                mlContext As MLContext, dataView As IDataView) As List(Of UrlData)

            ' Here (ModelInput object) you could provide new test data, hardcoded or from the
            '  end-user application, instead of the row from the file
            Dim sampleForPredictions As List(Of UrlData) =
                mlContext.Data.CreateEnumerable(Of UrlData)(dataView, False).Take(4).ToList()

            Return sampleForPredictions

        End Function

    End Class

End Namespace