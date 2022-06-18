
Imports Microsoft.ML
Imports Microsoft.ML.Data
Imports Microsoft.ML.Trainers
Imports ProductRecommender.Common ' MyWebClient
Imports System.IO

Namespace ProductRecommender

    Public Class Program

        ' Dataset from https://snap.stanford.edu/data/amazon0302.html
        ' Replace column names with ProductID and CoPurchaseProductID. It should look like this:
        ' ProductID	CoPurchaseProductID
        ' 0         1
        ' 0         2
        Private Const datasetFile = "Amazon0302"
        Private Const datasetZip As String = datasetFile & ".zip"
        Private Const datasetUrl As String = "https://bit.ly/3qnEpVz"

        Shared Sub Main(args() As String)

            Dim assetsRelativePath As String = "../../../data"
            Dim assetsPath As String = GetAbsolutePath(assetsRelativePath)
            Dim commonDatasetsRelativePath As String = "../../../../../../../../datasets"
            Dim commonDatasetsPath As String = GetAbsolutePath(commonDatasetsRelativePath)

            GetResult(assetsPath, commonDatasetsPath)

            Console.WriteLine("=============== End of process, hit any key to finish ===============")
            Console.ReadKey()

        End Sub

        Public Shared Function GetResult(
                myAssetsPath As String, myCommonDatasetsPath As String) As Boolean

            Dim datasetFolder As String = myAssetsPath 'BaseDataSetRelativePath
            'Dim fullDataSetFilePath As String = TrainingDataRelativePath
            'GetDataSet(fullDataSetFilePath, datasetFolder, datasetUrl, datasetZip, CommonDatasetsPath)
            Dim TrainingDataRelativePath As String =
                Path.Combine(myAssetsPath, datasetFile & ".txt") ' $"{BaseDataSetRelativePath}/" & datasetFile & ".txt"
            Dim TrainingDataLocation As String = GetAbsolutePath(TrainingDataRelativePath)
            Dim destFiles As New List(Of String) From {TrainingDataRelativePath}
            DownloadBigFile(datasetFolder, datasetUrl, datasetZip, myCommonDatasetsPath, destFiles)

            ' STEP 1: Create MLContext to be shared across the model creation workflow objects 
            Dim mlContext As New MLContext

            ' STEP 2: Read the trained data using TextLoader by defining the schema for reading
            '  the product co-purchase dataset
            Dim traindata = mlContext.Data.LoadFromTextFile(
                path:=TrainingDataLocation,
                columns:=CType({
                    New TextLoader.Column("Label", DataKind.Single, 0),
                    New TextLoader.Column(
                        name:=NameOf(ProductEntry.ProductID),
                        dataKind:=DataKind.UInt32,
                        source:=New TextLoader.Range() {New TextLoader.Range(0)},
                        keyCount:=New KeyCount(262111)),
                    New TextLoader.Column(
                        name:=NameOf(ProductEntry.CoPurchaseProductID),
                        dataKind:=DataKind.UInt32,
                        source:=New TextLoader.Range() {New TextLoader.Range(1)},
                        keyCount:=New KeyCount(262111))
                    }, TextLoader.Column()),
                hasHeader:=True, separatorChar:=CChar(vbTab))

            ' STEP 3: Your data is already encoded so all you need to do is specify options for
            '  MatrixFactorizationTrainer with a few extra hyperparameters
            '  LossFunction, Alpa, Lambda and a few others like K and C as shown below and call the trainer
            Dim options As MatrixFactorizationTrainer.Options = New MatrixFactorizationTrainer.Options
            options.MatrixColumnIndexColumnName = NameOf(ProductEntry.ProductID)
            options.MatrixRowIndexColumnName = NameOf(ProductEntry.CoPurchaseProductID)
            options.LabelColumnName = "Label"
            options.LossFunction = MatrixFactorizationTrainer.LossFunctionType.SquareLossOneClass
            options.Alpha = 0.01
            options.Lambda = 0.025
            ' For better results use the following parameters
            'options.K = 100;
            'options.C = 0.00001;

            ' Step 4: Call the MatrixFactorization trainer by passing options.
            Dim est = mlContext.Recommendation().Trainers.MatrixFactorization(options)

            ' STEP 5: Train the model fitting to the DataSet
            ' Please add Amazon0302.txt dataset from https://snap.stanford.edu/data/amazon0302.html
            '  to Data folder if FileNotFoundException is thrown
            Dim model As ITransformer = est.Fit(traindata)

            ' STEP 6: Create prediction engine and predict the score for Product 63 being co-purchased
            '  with Product 3
            ' The higher the score the higher the probability for this particular productID being co-purchased 
            Dim predictionengine = mlContext.Model.CreatePredictionEngine(
                Of ProductEntry, Copurchase_prediction)(model)
            Dim prediction = predictionengine.Predict(New ProductEntry With {
                .ProductID = 3,
                .CoPurchaseProductID = 63
            })

            Dim score = Math.Round(prediction.Score, 1)
            Console.WriteLine(vbLf &
                " For ProductID = 3 and  CoPurchaseProductID = 63 the predicted score is " & score)

            Dim scoreRounded = Math.Round(score, digits:=4) * 100
            Dim scoreExpected = 37
            Dim success = scoreRounded >= scoreExpected
            Console.WriteLine("Success: Score = " & scoreRounded.ToString("0.00") & " >= " & scoreExpected & " : " & success)

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

        Public Shared Function GetAbsolutePath(relativeDatasetPath As String) As String
            Dim fullPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativeDatasetPath)
            Return fullPath
        End Function

        Public Class Copurchase_prediction
            Public Property Score As Single
        End Class

        Public Class ProductEntry

            <KeyType(262111)>
            Public Property ProductID As UInteger

            <KeyType(262111)>
            Public Property CoPurchaseProductID As UInteger

        End Class

    End Class

End Namespace