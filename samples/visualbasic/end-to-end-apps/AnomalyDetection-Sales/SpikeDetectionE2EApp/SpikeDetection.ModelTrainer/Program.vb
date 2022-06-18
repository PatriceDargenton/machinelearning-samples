
Imports System.IO
Imports Microsoft.ML

Namespace SpikeDetection.WinFormsTrainer

    Friend Module Program

        Private BaseDatasetsRelativePath As String = "../../../../Data"
        Private DatasetRelativePath As String = $"{BaseDatasetsRelativePath}/Product-sales.csv"

        Private DatasetPath As String = GetAbsolutePath(DatasetRelativePath)

        Private BaseModelsRelativePath As String = "../../../../MLModels"
        Private ModelRelativePath1 As String = $"{BaseModelsRelativePath}/ProductSalesSpikeModel.zip"
        Private ModelRelativePath2 As String = $"{BaseModelsRelativePath}/ProductSalesChangePointModel.zip"

        Private SpikeModelPath As String = GetAbsolutePath(ModelRelativePath1)
        Private ChangePointModelPath As String = GetAbsolutePath(ModelRelativePath2)

        Private mlContext As MLContext

        Sub Main()

            ' Create MLContext to be shared across the model creation workflow objects 
            mlContext = New MLContext

            ' Assign the Number of records in dataset file to constant variable
            Const size As Integer = 36

            ' Load the data into IDataView
            ' This dataset is used for detecting spikes or changes not for training
            Dim dataView As IDataView = mlContext.Data.LoadFromTextFile(Of ProductSalesData)(
                path:=DatasetPath, hasHeader:=True, separatorChar:=","c)

            ' Detect temporary changes (spikes) in the pattern
            Dim trainedSpikeModel As ITransformer = DetectSpike(size, dataView)

            ' Detect persistent change in the pattern
            Dim pValue# = 0, mValue# = 0
            Dim trainedChangePointModel As ITransformer =
                DetectChangepoint(size, dataView, pValue, mValue)

            SaveModel(mlContext, trainedSpikeModel, SpikeModelPath, dataView)
            SaveModel(mlContext, trainedChangePointModel, ChangePointModelPath, dataView)

            Dim pValueRounded = Math.Round(pValue, digits:=2) * 100
            Dim mValueRounded = Math.Round(mValue, digits:=2)
            Dim pValueExpected = 48
            Dim mValueExpected = 44
            Dim success = pValueRounded >= pValueExpected AndAlso mValueRounded >= mValueExpected
            Console.WriteLine("Success: P-Value = " & pValueRounded & " >= " & pValueExpected &
                                     ", M. value = " & mValueRounded & " >= " & mValueExpected & " : " & success)

            Console.WriteLine("=============== End of process, hit any key to finish ===============")
            Console.ReadLine()

        End Sub

        Private Function DetectSpike(size As Integer, dataView As IDataView) As ITransformer
            Console.WriteLine("===============Detect temporary changes in pattern===============")

            ' STEP 1: Create Esimator   
            Dim estimator = mlContext.Transforms.DetectIidSpike(
                outputColumnName:=NameOf(ProductSalesPrediction.Prediction),
                inputColumnName:=NameOf(ProductSalesData.numSales),
                confidence:=95, pvalueHistoryLength:=size \ 4)

            ' STEP 2:The Transformed Model
            ' In IID Spike detection, we don't need to do training, we just need to do transformation
            ' As you are not training the model, there is no need to load IDataView with real data,
            '  you just need schema of data
            ' So create empty data view and pass to Fit() method
            Dim tansformedModel As ITransformer = estimator.Fit(CreateEmptyDataView())

            ' STEP 3: Use/test model
            ' Apply data transformation to create predictions
            Dim transformedData As IDataView = tansformedModel.Transform(dataView)
            Dim predictions = mlContext.Data.CreateEnumerable(Of ProductSalesPrediction)(
                transformedData, reuseRowObject:=False)

            Console.WriteLine("Alert" & vbTab & "Score" & vbTab & "P-Value")
            For Each p In predictions
                If p.Prediction(0) = 1 Then
                    Console.BackgroundColor = ConsoleColor.DarkYellow
                    Console.ForegroundColor = ConsoleColor.Black
                End If
                Console.WriteLine("{0}" & vbTab & "{1:0.00}" & vbTab & "{2:0.00}",
                                  p.Prediction(0), p.Prediction(1), p.Prediction(2))
                Console.ResetColor()
            Next p
            Console.WriteLine("")
            Return tansformedModel

        End Function

        Private Function DetectChangepoint(size As Integer, dataView As IDataView,
                ByRef pValue#, ByRef mValue#) As ITransformer

            Console.WriteLine("===============Detect Persistent changes in pattern===============")

            ' STEP 1: Setup transformations using DetectIidChangePoint
            Dim estimator = mlContext.Transforms.DetectIidChangePoint(
                outputColumnName:=NameOf(ProductSalesPrediction.Prediction),
                inputColumnName:=NameOf(ProductSalesData.numSales),
                confidence:=95, changeHistoryLength:=size \ 4)

            ' STEP 2:The Transformed Model
            ' In IID Change point detection, we don't need need to do training, we just need to do transformation
            ' As you are not training the model, there is no need to load IDataView with real data,
            '  you just need schema of data
            ' So create empty data view and pass to Fit() method
            Dim tansformedModel As ITransformer = estimator.Fit(CreateEmptyDataView())

            ' STEP 3: Use/test model
            ' Apply data transformation to create predictions
            Dim transformedData As IDataView = tansformedModel.Transform(dataView)
            Dim predictions = mlContext.Data.CreateEnumerable(Of ProductSalesPrediction)(
                transformedData, reuseRowObject:=False)

            Console.WriteLine($"{NameOf(ProductSalesPrediction.Prediction)} column obtained post-transformation.")
            Console.WriteLine("Alert" & vbTab & "Score" & vbTab & "P-Value" & vbTab & "Martingale value")

            For Each p In predictions
                If p.Prediction(0) = 1 Then
                    Console.WriteLine("{0}" & vbTab & "{1:0.00}" & vbTab & "{2:0.00}" & vbTab &
                        "{3:0.00}  <-- alert is on, predicted changepoint",
                        p.Prediction(0), p.Prediction(1), p.Prediction(2), p.Prediction(3))
                    pValue = p.Prediction(2)
                    mValue = p.Prediction(3)
                Else
                    Console.WriteLine("{0}" & vbTab & "{1:0.00}" & vbTab & "{2:0.00}" & vbTab & "{3:0.00}",
                        p.Prediction(0), p.Prediction(1), p.Prediction(2), p.Prediction(3))
                End If
            Next p
            Console.WriteLine("")
            Return tansformedModel

        End Function

        Private Sub SaveModel(mlcontext As MLContext, trainedModel As ITransformer,
                              modelPath As String, dataView As IDataView)

            Console.WriteLine("=============== Saving model ===============")
            Dim directoryPath = IO.Path.GetDirectoryName(modelPath)
            If Not IO.Directory.Exists(directoryPath) Then
                Dim di As New IO.DirectoryInfo(directoryPath)
                di.Create()
            End If
            mlcontext.Model.Save(trainedModel, dataView.Schema, modelPath)
            Console.WriteLine("The model is saved to {0}", modelPath)

        End Sub

        Public Function GetAbsolutePath(relativePath As String) As String
            Dim fullPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
            Return fullPath
        End Function

        Private Function CreateEmptyDataView() As IDataView
            ' Create empty DataView. We just need the schema to call fit()
            Dim enumerableData As IEnumerable(Of ProductSalesData) = New List(Of ProductSalesData)
            Dim dv = mlContext.Data.LoadFromEnumerable(enumerableData)
            Return dv
        End Function

    End Module

End Namespace