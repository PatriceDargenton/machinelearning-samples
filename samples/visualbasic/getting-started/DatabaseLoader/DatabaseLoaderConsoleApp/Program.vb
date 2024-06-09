
Imports DatabaseLoaderConsoleApp.Common
Imports DatabaseLoaderConsoleApp.DatabaseLoaderConsoleApp.DataModels
Imports Microsoft.ML
Imports Microsoft.ML.Data
Imports Microsoft.ML.Transforms
Imports System.Data
Imports System.Data.SqlClient
Imports System.Drawing
Imports System.IO

Namespace DatabaseLoaderConsoleApp

    Public Class Program

        Const databaseFile As String = "Criteo-100k-rows.mdf"
        Const databaseFile2 As String = "Criteo-100k-rows_log.ldf"
        Const databaseZip As String = "DatabaseLoaderSqlLocalDb.zip"
        Const databaseUrl As String = "https://bit.ly/3Gromvt"

        Shared Sub Main()

            Dim assetsRelativePath As String = "../../../SqlLocalDb"
            Dim databaseFolder As String = GetAbsolutePath(assetsRelativePath)

            Dim commonDatasetsRelativePath As String = "../../../../../../../../datasets"
            Dim commonDatasetsPath As String = GetAbsolutePath(commonDatasetsRelativePath)

            GetResult(databaseFolder, commonDatasetsPath)

            Console.WriteLine("=============== Press any key ===============")
            Console.ReadKey()

        End Sub

        Public Shared Function GetResult(
                myAssetsPath As String, myCommonDatasetsPath As String) As Boolean

            Dim databasePath = Path.Combine(myAssetsPath, databaseFile)
            Dim databasePath2 = Path.Combine(myAssetsPath, databaseFile2)
            'DownloadDatabase(databasePath, myAssetsPath, databaseUrl, databaseZip, commonDatasetsPath)
            Dim destFiles As New List(Of String) From {databasePath, databasePath2}
            DownloadBigFile(myAssetsPath, databaseUrl, databaseZip, myCommonDatasetsPath, destFiles,
                restart:=False)

            Dim databasePathBin = GetAbsolutePath("SqlLocalDb")
            Dim databasePathDest = Path.Combine(databasePathBin, Path.GetFileName(databasePath))
            Dim databasePathDest2 = Path.Combine(databasePathBin, Path.GetFileName(databasePath2))
            If Not Directory.Exists(databasePathBin) OrElse
                Not File.Exists(databasePathDest) OrElse
                Not File.Exists(databasePathDest2) Then
                Directory.CreateDirectory(databasePathBin)
                IO.File.Copy(databasePath, databasePathDest)
                IO.File.Copy(databasePath2, databasePathDest2)
            End If

            Dim mlContext As New MLContext

            ' localdb SQL database connection string using a filepath to attach the database file into localdb
            Dim dbFilePath As String = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "SqlLocalDb", databaseFile)
            If Not File.Exists(dbFilePath) Then Environment.Exit(0)
            Dim connectionString As String =
                $"Data Source = (LocalDB)\MSSQLLocalDB;AttachDbFilename={dbFilePath};" &
                "Database=Criteo-100k-rows;Integrated Security = True"

            ' ConnString Example: localdb SQL database connection string for 'localdb default location' (usually files located at /Users/YourUser/)
            'string connectionString = @"Data Source=(localdb)\MSSQLLocalDb;Initial Catalog=YOUR_DATABASE;Integrated Security=True;Pooling=False";
            '
            ' ConnString Example: on-premises SQL Server Database (Integrated security)
            'string connectionString = @"Data Source=YOUR_SERVER;Initial Catalog=YOUR_DATABASE;Integrated Security=True;Pooling=False";
            '
            ' ConnString Example:  Azure SQL Database connection string
            'string connectionString = @"Server=tcp:yourserver.database.windows.net,1433; Initial Catalog = YOUR_DATABASE; Persist Security Info = False; User ID = YOUR_USER; Password = YOUR_PASSWORD; MultipleActiveResultSets = False; Encrypt = True; TrustServerCertificate = False; Connection Timeout = 60; ConnectRetryCount = 5; ConnectRetryInterval = 10;";

            Dim commandText As String = "SELECT * from URLClicks"

            Dim loader As DatabaseLoader = mlContext.Data.CreateDatabaseLoader(Of UrlClick)()

            Dim dbSource As New DatabaseSource(SqlClientFactory.Instance, connectionString, commandText)

            Dim dataView As IDataView = loader.Load(dbSource)

            Dim trainTestData = mlContext.Data.TrainTestSplit(dataView)

            ' Do the transformation in IDataView
            ' Transform categorical features into binary
            Dim CatogoriesTranformer = mlContext.Transforms.Conversion.ConvertType(
                NameOf(UrlClick.Label), outputKind:=Microsoft.ML.Data.DataKind.Boolean).
                Append(mlContext.Transforms.Categorical.OneHotEncoding({
                    New InputOutputColumnPair("Cat14Encoded", "Cat14"),
                    New InputOutputColumnPair("Cat15Encoded", "Cat15"),
                    New InputOutputColumnPair("Cat16Encoded", "Cat16"),
                    New InputOutputColumnPair("Cat17Encoded", "Cat17"),
                    New InputOutputColumnPair("Cat18Encoded", "Cat18"),
                    New InputOutputColumnPair("Cat19Encoded", "Cat19"),
                    New InputOutputColumnPair("Cat20Encoded", "Cat20"),
                    New InputOutputColumnPair("Cat21Encoded", "Cat21"),
                    New InputOutputColumnPair("Cat22Encoded", "Cat22"),
                    New InputOutputColumnPair("Cat23Encoded", "Cat23"),
                    New InputOutputColumnPair("Cat24Encoded", "Cat24"),
                    New InputOutputColumnPair("Cat25Encoded", "Cat25"),
                    New InputOutputColumnPair("Cat26Encoded", "Cat26"),
                    New InputOutputColumnPair("Cat27Encoded", "Cat27"),
                    New InputOutputColumnPair("Cat28Encoded", "Cat28"),
                    New InputOutputColumnPair("Cat29Encoded", "Cat29"),
                    New InputOutputColumnPair("Cat30Encoded", "Cat30"),
                    New InputOutputColumnPair("Cat31Encoded", "Cat31"),
                    New InputOutputColumnPair("Cat32Encoded", "Cat32"),
                    New InputOutputColumnPair("Cat33Encoded", "Cat33"),
                    New InputOutputColumnPair("Cat34Encoded", "Cat34"),
                    New InputOutputColumnPair("Cat35Encoded", "Cat35"),
                    New InputOutputColumnPair("Cat36Encoded", "Cat36"),
                    New InputOutputColumnPair("Cat37Encoded", "Cat37"),
                    New InputOutputColumnPair("Cat38Encoded", "Cat38"),
                    New InputOutputColumnPair("Cat39Encoded", "Cat39")
                }, OneHotEncodingEstimator.OutputKind.Binary))

            Dim featuresTransformer = CatogoriesTranformer.Append(
                mlContext.Transforms.Text.FeaturizeText(
                    outputColumnName:="Feat01Featurized",
                    inputColumnName:=NameOf(UrlClick.Feat01))).
                Append(mlContext.Transforms.Text.FeaturizeText(
                    outputColumnName:="Feat02Featurized",
                    inputColumnName:=NameOf(UrlClick.Feat02))).
                Append(mlContext.Transforms.Text.FeaturizeText(
                    outputColumnName:="Feat03Featurized",
                    inputColumnName:=NameOf(UrlClick.Feat03))).
                Append(mlContext.Transforms.Text.FeaturizeText(
                    outputColumnName:="Feat04Featurized",
                    inputColumnName:=NameOf(UrlClick.Feat04))).
                Append(mlContext.Transforms.Text.FeaturizeText(
                    outputColumnName:="Feat05Featurized",
                    inputColumnName:=NameOf(UrlClick.Feat05))).
                Append(mlContext.Transforms.Text.FeaturizeText(
                    outputColumnName:="Feat06Featurized",
                    inputColumnName:=NameOf(UrlClick.Feat06))).
                Append(mlContext.Transforms.Text.FeaturizeText(
                    outputColumnName:="Feat07Featurized",
                    inputColumnName:=NameOf(UrlClick.Feat07))).
                Append(mlContext.Transforms.Text.FeaturizeText(
                    outputColumnName:="Feat08Featurized",
                    inputColumnName:=NameOf(UrlClick.Feat08))).
                Append(mlContext.Transforms.Text.FeaturizeText(
                    outputColumnName:="Feat09Featurized",
                    inputColumnName:=NameOf(UrlClick.Feat09))).
                Append(mlContext.Transforms.Text.FeaturizeText(
                    outputColumnName:="Feat10Featurized",
                    inputColumnName:=NameOf(UrlClick.Feat10))).
                Append(mlContext.Transforms.Text.FeaturizeText(
                    outputColumnName:="Feat11Featurized",
                    inputColumnName:=NameOf(UrlClick.Feat11))).
                Append(mlContext.Transforms.Text.FeaturizeText(
                    outputColumnName:="Feat12Featurized",
                    inputColumnName:=NameOf(UrlClick.Feat12))).
                Append(mlContext.Transforms.Text.FeaturizeText(
                    outputColumnName:="Feat13Featurized",
                    inputColumnName:=NameOf(UrlClick.Feat13)))

            Dim finalTransformerPipeLine = featuresTransformer.Append(
                mlContext.Transforms.Concatenate("Features", "Feat01Featurized", "Feat02Featurized",
                    "Feat03Featurized", "Feat04Featurized", "Feat05Featurized", "Feat06Featurized",
                    "Feat07Featurized", "Feat08Featurized", "Feat09Featurized", "Feat10Featurized",
                    "Feat11Featurized", "Feat12Featurized", "Feat12Featurized",
                    "Cat14Encoded", "Cat15Encoded", "Cat16Encoded", "Cat17Encoded", "Cat18Encoded",
                    "Cat19Encoded", "Cat20Encoded", "Cat21Encoded", "Cat22Encoded", "Cat23Encoded",
                    "Cat24Encoded", "Cat25Encoded", "Cat26Encoded", "Cat27Encoded", "Cat28Encoded",
                    "Cat29Encoded", "Cat30Encoded", "Cat31Encoded", "Cat32Encoded", "Cat33Encoded",
                    "Cat34Encoded", "Cat35Encoded", "Cat36Encoded", "Cat37Encoded", "Cat38Encoded",
                    "Cat39Encoded"))

            ' Apply the ML algorithm
            Dim trainingPipeLine = finalTransformerPipeLine.Append(
                mlContext.BinaryClassification.Trainers.FieldAwareFactorizationMachine(
                    labelColumnName:="Label", featureColumnName:="Features"))

            Console.WriteLine("Training the ML model while streaming data from a SQL database...")
            Dim watch As Stopwatch = New Stopwatch
            watch.Start()

            ' 09/06/2024 Bug: SqlException: A network-related or instance-specific error occurred while 
            '  establishing a connection to SQL Server. The server was not found or was not accessible.
            ' Verify that the instance name is correct and that SQL Server is configured to allow remote connections.
            ' (provider: SQL Network Interfaces, error: 50 - Local Database Runtime error occurred.)
            Dim model = trainingPipeLine.Fit(trainTestData.TrainSet)

            watch.Stop()
            Console.WriteLine("Elapsed time for training the model = {0} seconds",
                watch.ElapsedMilliseconds \ 1000)

            Console.WriteLine("Evaluating the model...")
            Dim watch2 As Stopwatch = New Stopwatch
            watch2.Start()

            Dim predictions = model.Transform(trainTestData.TestSet)
            ' Now that we have the test predictions, calculate the metrics of those predictions
            '  and output the results
            Dim metrics = mlContext.BinaryClassification.Evaluate(predictions)

            watch2.Stop()
            Console.WriteLine("Elapsed time for evaluating the model = {0} seconds",
                watch2.ElapsedMilliseconds \ 1000)

            ConsoleHelper.PrintBinaryClassificationMetrics(
                "==== Evaluation Metrics training from a Database ====", metrics)

            Console.WriteLine("Trying a single prediction:")

            Dim predictionEngine = mlContext.Model.CreatePredictionEngine(
                Of UrlClick, ClickPrediction)(model)

            Dim sampleData As UrlClick = New UrlClick With {
                .Label = String.Empty,
                .Feat01 = "32",
                .Feat02 = "3",
                .Feat03 = "5",
                .Feat04 = "NULL",
                .Feat05 = "1",
                .Feat06 = "0",
                .Feat07 = "0",
                .Feat08 = "61",
                .Feat09 = "5",
                .Feat10 = "0",
                .Feat11 = "1",
                .Feat12 = "3157",
                .Feat13 = "5",
                .Cat14 = "e5f3fd8d",
                .Cat15 = "a0aaffa6",
                .Cat16 = "aa15d56f",
                .Cat17 = "da8a3421",
                .Cat18 = "cd69f233",
                .Cat19 = "6fcd6dcb",
                .Cat20 = "ab16ed81",
                .Cat21 = "43426c29",
                .Cat22 = "1df5e154",
                .Cat23 = "00c5ffb7",
                .Cat24 = "be4ee537",
                .Cat25 = "f3bbfe99",
                .Cat26 = "7de9c0a9",
                .Cat27 = "6652dc64",
                .Cat28 = "99eb4e27",
                .Cat29 = "4cdc3efa",
                .Cat30 = "d20856aa",
                .Cat31 = "a1eb1511",
                .Cat32 = "9512c20b",
                .Cat33 = "febfd863",
                .Cat34 = "a3323ca1",
                .Cat35 = "c8e1ee56",
                .Cat36 = "1752e9e8",
                .Cat37 = "75350c8a",
                .Cat38 = "991321ea",
                .Cat39 = "b757e957"
            }

            Dim clickPrediction = predictionEngine.Predict(sampleData)

            Console.WriteLine($"Predicted Label: {clickPrediction.PredictedLabel} - Score:{Sigmoid(clickPrediction.Score)}", Color.YellowGreen)
            Console.WriteLine()

            '*** Detach database from localdb only if you used a conn-string with a filepath
            '  to attach the database file into localdb ***
            Console.WriteLine("... Detaching database from SQL localdb ...")
            DetachDatabase(connectionString)

            Dim score = metrics.Accuracy
            Dim scoreRounded = Math.Round(score, digits:=4) * 100
            Dim scoreExpected = 97
            Dim success = scoreRounded >= scoreExpected
            Console.WriteLine("Success: Score = " & scoreRounded.ToString("0.00") & " >= " & scoreExpected & " : " & success)

            Return success

        End Function

        Public Shared Function Sigmoid(x As Single) As Single
            Return CSng(100 / (1 + Math.Exp(-x)))
        End Function

        Public Shared Sub DetachDatabase(userConnectionString As String) 'DELETE PARAM *************

            Dim dbName As String = String.Empty
            Using userSqlDatabaseConnection As New SqlConnection(userConnectionString)
                userSqlDatabaseConnection.Open()
                dbName = userSqlDatabaseConnection.Database
            End Using

            Dim masterConnString As String = $"Data Source = (LocalDB)\MSSQLLocalDB;Integrated Security = True"
            Using sqlDatabaseConnection As New SqlConnection(masterConnString)
                sqlDatabaseConnection.Open()

                Dim prepareDbcommandString As String = $"ALTER DATABASE [{dbName}] SET OFFLINE WITH ROLLBACK IMMEDIATE ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE"
                ' (ALTERNATIVE) string prepareDbcommandString = $"ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
                Dim sqlPrepareCommand As New SqlCommand(prepareDbcommandString, sqlDatabaseConnection)
                sqlPrepareCommand.ExecuteNonQuery()

                Dim detachCommandString As String = "sp_detach_db"
                Dim sqlDetachCommand As New SqlCommand(detachCommandString, sqlDatabaseConnection)
                sqlDetachCommand.CommandType = CommandType.StoredProcedure
                sqlDetachCommand.Parameters.AddWithValue("@dbname", dbName)
                sqlDetachCommand.ExecuteNonQuery()
            End Using

        End Sub

        'Public Shared Sub DownloadDatabase(databasePath As String, databaseFolder As String,
        '        databaseUrl As String, databaseZip As String, commonDatasetsPath As String)

        '    Dim zipPath = Path.Combine(databaseFolder, databaseZip)
        '    Dim commonPath = Path.Combine(commonDatasetsPath, databaseZip)

        '    If Not File.Exists(zipPath) AndAlso File.Exists(commonPath) Then
        '        IO.File.Copy(commonPath, zipPath)
        '    ElseIf File.Exists(zipPath) AndAlso Not File.Exists(commonPath) Then
        '        IO.File.Copy(zipPath, commonPath)
        '    End If

        '    If File.Exists(databasePath) Then Exit Sub

        '    If Not File.Exists(zipPath) Then
        '        Console.WriteLine("====Downloading zip====")
        '        Using client = New MyWebClient
        '            ' The code below will download a dataset from a third-party,
        '            '  and may be governed by separate third-party terms
        '            ' By proceeding, you agree to those separate terms
        '            client.DownloadFile(databaseUrl, zipPath)
        '        End Using
        '        Console.WriteLine("====Downloading is completed====")
        '        IO.File.Copy(zipPath, commonPath)
        '    End If

        '    If File.Exists(zipPath) Then
        '        Console.WriteLine("====Extracting zip====")
        '        Dim myFastZip As New FastZip()
        '        myFastZip.ExtractZip(zipPath, databaseFolder, fileFilter:=String.Empty)
        '        Console.WriteLine("====Extracting is complete====")
        '        If File.Exists(databasePath) Then
        '            Console.WriteLine("====Extracting is OK====")
        '            Console.WriteLine("====Please restart!====")
        '            Console.ReadKey()
        '            End
        '        Else
        '            Console.WriteLine("====Extracting: Fail!====")
        '            Stop
        '        End If
        '    End If

        'End Sub

        Public Shared Function GetAbsolutePath(relativePath As String) As String
            Dim fullPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
            Return fullPath
        End Function

    End Class

End Namespace