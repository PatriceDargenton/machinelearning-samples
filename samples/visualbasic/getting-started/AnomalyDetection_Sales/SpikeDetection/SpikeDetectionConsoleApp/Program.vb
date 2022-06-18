
Imports Microsoft.ML
Imports SpikeDetection.SpikeDetection.DataStructures
Imports System.IO

Namespace SpikeDetection

    Public Module Program

        Private mlContext As MLContext

        Public Sub Main()

            Dim assetsRelativePath As String = "../../../../Data"
            Dim assetsPath As String = GetAbsolutePath(assetsRelativePath)

            GetResult(assetsPath)

            Console.WriteLine("=============== End of process, hit any key to finish ===============")

            Console.ReadLine()

        End Sub

        Public Function GetResult(myAssetsPath As String) As Boolean

            Dim trainDataRelativePath As String = Path.Combine(myAssetsPath,
                "product-sales.csv")
            Dim trainDataPath As String = GetAbsolutePath(trainDataRelativePath)
            trainDataPath = Path.GetFullPath(trainDataPath)

            ' Create MLContext to be shared across the model creation workflow objects 
            mlContext = New MLContext

            ' Assign the Number of records in dataset file to cosntant variable
            Const size As Integer = 36

            ' Load the data into IDataView
            ' This dataset is used while prediction/detecting spikes or changes
            Dim dataView As IDataView = mlContext.Data.LoadFromTextFile(Of ProductSalesData)(
                path:=trainDataPath, hasHeader:=True, separatorChar:=","c)

            ' To detech temporay changes in the pattern
            DetectSpike(size, dataView)

            ' To detect persistent change in the pattern
            Dim pValue# = 0, mValue# = 0
            DetectChangepoint(size, dataView, pValue, mValue)

            Dim pValueRounded = Math.Round(pValue, digits:=2) * 100
            Dim mValueRounded = Math.Round(mValue, digits:=2)
            Dim pValueExpected = 48
            Dim mValueExpected = 44
            Dim success =
                pValueRounded >= pValueExpected AndAlso
                mValueRounded >= mValueExpected
            Console.WriteLine(
                "Success: P-Value = " & pValueRounded & " >= " & pValueExpected &
                ", M. value = " & mValueRounded & " >= " & mValueExpected & " : " &
                success)

            Return success

        End Function

        Private Sub DetectSpike(size As Integer, dataView As IDataView)

            Console.WriteLine("===============Detect temporary changes in pattern===============")

            ' STEP 1: Create Estimator
            ' Note: The Warning is also present for the C # version
            Dim estimator = mlContext.Transforms.DetectIidSpike(
                outputColumnName:=NameOf(ProductSalesPrediction.Prediction),
                inputColumnName:=NameOf(ProductSalesData.numSales),
                confidence:=95, pvalueHistoryLength:=size \ 4)

            ' STEP 2: The Transformed Model
            ' In IID Spike detection, we don't need to do training, we just need to do transformation
            ' As you are not training the model, there is no need to load IDataView with real data,
            '  you just need schema of data
            ' So create empty data view and pass to Fit() method
            Dim tansformedModel As ITransformer = estimator.Fit(CreateEmptyDataView())

            ' STEP 3: Use/test model
            ' Apply data transformation to create predictions.
            Dim transformedData As IDataView = tansformedModel.Transform(dataView)
            Dim predictions = mlContext.Data.CreateEnumerable(Of ProductSalesPrediction)(
                transformedData, reuseRowObject:=False)

            Console.WriteLine("Alert" & vbTab & "Score" & vbTab & "P-Value")
            For Each p In predictions
                If p.Prediction(0) = 1 Then
                    Console.BackgroundColor = ConsoleColor.DarkYellow
                    Console.ForegroundColor = ConsoleColor.Black
                End If
                Console.WriteLine("{0}" & vbTab & "{1:0.00}" & vbTab & "{2:0.00}", p.Prediction(0), p.Prediction(1), p.Prediction(2))
                Console.ResetColor()
            Next p
            Console.WriteLine("")

        End Sub

        Private Sub DetectChangepoint(size As Integer, dataView As IDataView,
                ByRef pValue#, ByRef mValue#)

            Console.WriteLine("===============Detect Persistent changes in pattern===============")

            ' STEP 1: Setup transformations using DetectIidChangePoint
            ' Note: The Warning is also present for the C # version
            Dim estimator = mlContext.Transforms.DetectIidChangePoint(
                outputColumnName:=NameOf(ProductSalesPrediction.Prediction),
                inputColumnName:=NameOf(ProductSalesData.numSales),
                confidence:=95, changeHistoryLength:=size \ 4)

            ' STEP 2: The Transformed Model
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
                    Console.WriteLine("{0}" & vbTab & "{1:0.00}" & vbTab & "{2:0.00}" & vbTab & "{3:0.00}  <-- alert is on, predicted changepoint", p.Prediction(0), p.Prediction(1), p.Prediction(2), p.Prediction(3))
                    pValue = p.Prediction(2)
                    mValue = p.Prediction(3)
                Else
                    Console.WriteLine("{0}" & vbTab & "{1:0.00}" & vbTab & "{2:0.00}" & vbTab & "{3:0.00}", p.Prediction(0), p.Prediction(1), p.Prediction(2), p.Prediction(3))
                End If
            Next p
            Console.WriteLine("")

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