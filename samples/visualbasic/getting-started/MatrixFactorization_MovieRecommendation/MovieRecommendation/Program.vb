
Imports Microsoft.ML
Imports MovieRecommendation.MovieRecommendation.DataStructures
Imports MovieRecommendation.MovieRecommendationConsoleApp.DataStructures
Imports System.IO
Imports Microsoft.ML.Trainers
Imports MovieRecommendation.Common ' MyWebClient

Namespace MovieRecommendation

    Public Class Program

        ' Using the ml-latest-small.zip as dataset from https://grouplens.org/datasets/movielens 
        ' https://files.grouplens.org/datasets/movielens/ml-latest-small.zip
        Private Const datasetFile = "MovieRecommendation" ' "ml-latest-small"
        Private Const datasetZip As String = datasetFile & ".zip"
        Private Const datasetUrl As String = "https://bit.ly/3nvLf9U"

        'Private Shared ModelsRelativePath As String = "../../../../MLModels"
        Public Shared DatasetsRelativePath As String = "../../../../Data"

        Private Const predictionuserId As Single = 6
        Private Const predictionmovieId As Integer = 10

        Shared Sub Main(args() As String)

            Dim assetsRelativePath As String = "../../../../data"
            Dim assetsPath As String = GetAbsolutePath(assetsRelativePath)
            Dim commonDatasetsRelativePath As String = "../../../../../../../../datasets"
            Dim commonDatasetsPath As String = GetAbsolutePath(commonDatasetsRelativePath)

            GetResult(assetsPath, commonDatasetsPath)

            Console.WriteLine("=============== End of process, hit any key to finish ===============")
            Console.ReadLine()

        End Sub

        Public Shared Function GetResult(
                myAssetsPath As String, myCommonDatasetsPath As String) As Boolean

            Program.DatasetsRelativePath = myAssetsPath
            Dim datasetFolder As String = myAssetsPath
            'Dim fullDataSetFilePath As String = Path.Combine(myAssetsPath, "recommendation-ratings-train.csv")
            'GetDataSet(fullDataSetFilePath, datasetFolder, datasetUrl, datasetZip, myCommonDatasetsPath)
            Dim TrainingDataRelativePath As String =
                Path.Combine(myAssetsPath, "recommendation-ratings-train.csv")
            Dim TestDataRelativePath As String =
                Path.Combine(myAssetsPath, "recommendation-ratings-test.csv")
            Dim TrainingDataLocation As String = GetAbsolutePath(TrainingDataRelativePath)
            Dim TestDataLocation As String = GetAbsolutePath(TestDataRelativePath)
            Dim destFiles As New List(Of String) From {TrainingDataRelativePath, TestDataRelativePath}
            DownloadBigFile(datasetFolder, datasetUrl, datasetZip, myCommonDatasetsPath, destFiles)

            ' STEP 1: Create MLContext to be shared across the model creation workflow objects 
            Dim mlContext As New MLContext

            ' STEP 2: Read the training data which will be used to train the movie recommendation model    
            ' The schema for training data is defined by type 'TInput' in LoadFromTextFile<TInput>() method
            Dim trainingDataView As IDataView = mlcontext.Data.LoadFromTextFile(Of MovieRating)(
                TrainingDataLocation, hasHeader:=True, separatorChar:=","c)

            ' STEP 3: Transform your data by encoding the two features userId and movieID
            ' These encoded features will be provided as input to our MatrixFactorizationTrainer
            Dim dataProcessingPipeline =
                mlcontext.Transforms.Conversion.MapValueToKey(
                    outputColumnName:="userIdEncoded",
                    inputColumnName:=NameOf(MovieRating.userId)).
                Append(mlcontext.Transforms.Conversion.MapValueToKey(
                    outputColumnName:="movieIdEncoded",
                    inputColumnName:=NameOf(MovieRating.movieId)))

            ' Specify the options for MatrixFactorization trainer            
            Dim options As MatrixFactorizationTrainer.Options = New MatrixFactorizationTrainer.Options
            options.MatrixColumnIndexColumnName = "userIdEncoded"
            options.MatrixRowIndexColumnName = "movieIdEncoded"
            options.LabelColumnName = "Label"
            options.NumberOfIterations = 20
            options.ApproximationRank = 100

            ' STEP 4: Create the training pipeline 
            Dim trainingPipeLine = dataProcessingPipeline.Append(
                mlcontext.Recommendation().Trainers.MatrixFactorization(options))

            ' STEP 5: Train the model fitting to the DataSet
            Console.WriteLine("=============== Training the model ===============")
            Dim model As ITransformer = trainingPipeLine.Fit(trainingDataView)

            ' STEP 6: Evaluate the model performance 
            Console.WriteLine("=============== Evaluating the model ===============")
            Dim testDataView As IDataView = mlcontext.Data.LoadFromTextFile(Of MovieRating)(
                TestDataLocation, hasHeader:=True, separatorChar:=","c)
            Dim prediction = model.Transform(testDataView)
            Dim metrics = mlcontext.Regression.Evaluate(
                prediction, labelColumnName:="Label", scoreColumnName:="Score")
            Console.WriteLine("The model evaluation metrics RootMeanSquaredError:" &
                metrics.RootMeanSquaredError)

            ' STEP 7:  Try/test a single prediction by predicting a single movie rating for a specific user
            Dim predictionengine = mlcontext.Model.CreatePredictionEngine(
                Of MovieRating, MovieRatingPrediction)(model)
            ' Make a single movie rating prediction, the scores are for a particular user and will range from 1 - 5 
            ' The higher the score the higher the likelyhood of a user liking a particular movie
            ' You can recommend a movie to a user if say rating > 3.5
            Dim movieratingprediction = predictionengine.Predict(New MovieRating With {
                .userId = predictionuserId,
                .movieId = predictionmovieId
            })

            Dim movieService As Movie = New Movie
            Console.WriteLine(
                "For userId:" & predictionuserId &
                " movie rating prediction (1 - 5 stars) for movie:" &
                movieService.Get(predictionmovieId).movieTitle &
                " is:" & Math.Round(movieratingprediction.Score, 1))

            Dim errorRMS = metrics.RootMeanSquaredError
            Dim errorRMSRounded = Math.Round(errorRMS, digits:=2)
            Dim errorExpected = 1.1
            Dim success = errorRMSRounded <= errorExpected
            Console.WriteLine("Success: Error = " & errorRMSRounded.ToString("0.00") & " <= " & errorExpected & " : " & success)

            Return success

        End Function

        'Public Shared Sub GetDataSet(destinationFile As String,
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
        '        Dim directoryPath = Path.GetDirectoryName(zipPath)
        '        If Not Directory.Exists(directoryPath) Then
        '            Dim di As New DirectoryInfo(directoryPath)
        '            di.Create()
        '        End If
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

        Public Shared Function GetAbsolutePath(relativePath As String) As String
            Dim fullPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
            Return fullPath
        End Function

    End Class

End Namespace