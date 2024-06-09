
Imports System.Data
Imports System.IO
Imports System.Runtime.CompilerServices
Imports Microsoft.ML

Namespace XORApp

    Public Module Program

        Private ReadOnly BaseDatasetsRelativePath As String = "../../../../Data"
        Private ReadOnly TrainDataRelativePath As String =
            $"{BaseDatasetsRelativePath}/Xor-repeat10.txt"
        Private ReadOnly TrainDataRelativePath2 As String =
            $"{BaseDatasetsRelativePath}/Xor2.txt"
        Private ReadOnly TrainDataRelativePath3 As String =
            $"{BaseDatasetsRelativePath}/Xor3.txt"
        Private ReadOnly TrainDataRelativePath2R As String =
            $"{BaseDatasetsRelativePath}/xor2-repeat3.txt"
        Private ReadOnly TrainDataRelativePathAutoML As String =
            $"{BaseDatasetsRelativePath}/xorAutoML-repeat10.csv"
        Private ReadOnly TrainDataPath As String = GetAbsolutePath(TrainDataRelativePath)
        Private ReadOnly TrainDataPath2 As String = GetAbsolutePath(TrainDataRelativePath2)
        Private ReadOnly TrainDataPath3 As String = GetAbsolutePath(TrainDataRelativePath3)
        Private ReadOnly TrainDataPath2R As String = GetAbsolutePath(TrainDataRelativePath2R)
        Private ReadOnly TrainDataPathAutoML As String = GetAbsolutePath(TrainDataRelativePathAutoML)

        Private ReadOnly BaseModelsRelativePath As String = "../../../../MLModels"
        Private ReadOnly ModelRelativePath As String = $"{BaseModelsRelativePath}/XORModel"
        Private ReadOnly ModelPath As String = GetAbsolutePath(ModelRelativePath)

        Public Sub Main(args() As String)

            Dim ModelPathZip = ModelPath + ".zip"
            Dim ModelPath1Zip = ModelPath + "1.zip"
            Dim ModelPath2Zip = ModelPath + "2.zip"
            Dim ModelPath3Zip = ModelPath + "3.zip"
            Dim mlContext As New MLContext()

Retry:
            Console.WriteLine("")
            Console.WriteLine("")
            Console.WriteLine("XOR Test, choose an option from the following list:")
            Console.WriteLine("0: Exit")
            Console.WriteLine("1: 1 XOR from RAM")
            Console.WriteLine("2: 1 XOR from file")
            Console.WriteLine("3: 1 XOR from file (AutoML)")
            Console.WriteLine("4: 1 XOR (vector mode) from RAM")
            Console.WriteLine("5: 1 XOR (vector mode) from file")
            Console.WriteLine("6: 2 XOR (vector mode) from RAM")
            Console.WriteLine("7: 2 XOR (vector mode) from full file")
            Console.WriteLine("8: 2 XOR (vector mode) from minimal file")
            Console.WriteLine("9: 3 XOR (vector mode) from minimal file")

            Dim k = Console.ReadKey()
            Select Case k.KeyChar
                Case "0"c
                    Return

                Case "1"c
                    Dim largeSet = XOR1.LoadData()
                    XOR1.Train(MLContext, ModelPathZip, largeSet)
                    XOR1.TestSomePredictions(MLContext, ModelPathZip)

                Case "2"c
                    XOR1.TrainFromFile(mlContext, ModelPathZip, TrainDataPath)
                    XOR1.TestSomePredictions(mlContext, ModelPathZip)

                Case "3"c
                    XOR1AutoML.TrainFromFile(mlContext, ModelPathZip, TrainDataPathAutoML, isTest:=False)
                    XOR1AutoML.TestSomePredictions(mlContext, ModelPathZip)

                Case "4"c
                    Dim largeSet2 = XOR1Vector.LoadData()
                    XOR1Vector.Train(mlContext, ModelPathZip, largeSet2)
                    XOR1Vector.TestSomePredictions(mlContext, ModelPathZip)

                Case "5"c
                    XOR1Vector.TrainFromFile(mlContext, ModelPathZip, TrainDataPath)
                    XOR1Vector.TestSomePredictions(mlContext, ModelPathZip)

                Case "6"c
                    Dim largeSet3 = XOR2Vector.LoadData()
                    Dim iRowCount1 As Integer = largeSet3.Count
                    XOR2Vector.Train(mlContext, ModelPath1Zip, ModelPath2Zip, largeSet3)
                    XOR2Vector.TestSomePredictions(mlContext,
                        ModelPath1Zip, ModelPath2Zip, iRowCount1, largeSet3)

                Case "7"c
                    Dim iRowCount2 = 0
                    Dim samples2 = New List(Of XOR2Vector.XOR2Data)()
                    XOR2Vector.TrainFromFile(mlContext,
                        ModelPath1Zip, ModelPath2Zip, TrainDataPath2R,
                        iRowCount2, samples2, bRepeat:=False)
                    XOR2Vector.TestSomePredictions(mlContext,
                        ModelPath1Zip, ModelPath2Zip, iRowCount2, samples2)

                Case "8"c
                    Dim iRowCount3 = 0
                    Dim samples3 = New List(Of XOR2Vector.XOR2Data)()
                    XOR2Vector.TrainFromFile(mlContext,
                        ModelPath1Zip, ModelPath2Zip,
                        TrainDataPath2, iRowCount3, samples3, bRepeat:=True)
                    XOR2Vector.TestSomePredictions(mlContext,
                        ModelPath1Zip, ModelPath2Zip, iRowCount3, samples3)

                Case "9"c
                    Dim iRowCount = 0
                    Dim samples = New List(Of XOR3Vector.XOR3Data)()
                    XOR3Vector.TrainFromFile(mlContext,
                        ModelPath1Zip, ModelPath2Zip, ModelPath3Zip,
                        TrainDataPath3, iRowCount, samples)
                    XOR3Vector.TestSomePredictions(mlContext,
                        ModelPath1Zip, ModelPath2Zip, ModelPath3Zip, iRowCount, samples)

                Case "a"c
                    ' Does not work: VectorType for Output
                    ' ML.NET does not yet support multi-target regression (MTR)
                    ' (only via TensorFlow and Python)
                    ' https://github.com/dotnet/machinelearning/issues/2134
                    ' System.ArgumentOutOfRangeException HResult=0x80131502
                    ' Message=Schema mismatch for label column 'Output': expected Single, got Vector<Single> Arg_ParamName_Name
                    Dim largeSet4 = XOR2VectorMTR.LoadData()
                    XOR2VectorMTR.Train(mlContext, ModelPath1Zip, ModelPath2Zip, largeSet4)
                    XOR2VectorMTR.TestSomePredictions(mlContext,
                        ModelPath1Zip, ModelPath2Zip)

            End Select
            GoTo Retry

        End Sub

        Public Function GetResult1XOR(myModelPath As String) As Boolean

            Dim ModelPathZip = myModelPath + ".zip"
            Dim mlContext As New MLContext()
            Dim largeSet = XOR1.LoadData()
            XOR1.Train(mlContext, ModelPathZip, largeSet)
            Dim scoreRounded = XOR1.TestSomePredictions(mlContext, ModelPathZip)
            Dim scoreExpected = 4
            Dim success = scoreRounded >= scoreExpected
            Console.WriteLine("Success: Score = " & scoreRounded & " >= " & scoreExpected & " : " & success)

            Return success

        End Function

        Public Function GetResult1XORAutoML(myAssetsPath As String, myModelPath As String,
            Optional isTest As Boolean = False) As Boolean

            Dim ModelPathZip = myModelPath + ".zip"
            Dim mlContext As New MLContext()
            XOR1AutoML.TrainFromFile(mlContext, ModelPathZip, myAssetsPath, isTest)
            Dim scoreRounded = XOR1AutoML.TestSomePredictions(mlContext, ModelPathZip)
            Dim scoreExpected = 4
            Dim success = scoreRounded >= scoreExpected
            Console.WriteLine("Success: Score = " & scoreRounded & " >= " & scoreExpected & " : " & success)

            Return success

        End Function

        Public Function GetResult2XOR(myAssetsPath As String, myModelPath As String) As Boolean

            Dim ModelPath1Zip = myModelPath + "1.zip"
            Dim ModelPath2Zip = myModelPath + "2.zip"
            Dim mlContext As New MLContext()

            Dim iRowCount2 = 0
            Dim samples2 = New List(Of XOR2Vector.XOR2Data)()
            XOR2Vector.TrainFromFile(mlContext,
                ModelPath1Zip, ModelPath2Zip,
                myAssetsPath, iRowCount2, samples2, bRepeat:=False)
            Dim scoreRounded = XOR2Vector.TestSomePredictions(mlContext,
                ModelPath1Zip, ModelPath2Zip, iRowCount2, samples2)
            Dim scoreExpected = 96
            Dim success = scoreRounded >= scoreExpected
            Console.WriteLine("Success: Score = " & scoreRounded & " >= " & scoreExpected & " : " & success)

            Return success

        End Function

        Public Function GetResult3XOR(myAssetsPath As String, myModelPath As String) As Boolean

            Dim ModelPath1Zip = myModelPath + "1.zip"
            Dim ModelPath2Zip = myModelPath + "2.zip"
            Dim ModelPath3Zip = myModelPath + "3.zip"
            Dim mlContext As New MLContext()

            Dim iRowCount = 0
            Dim samples = New List(Of XOR3Vector.XOR3Data)()
            XOR3Vector.TrainFromFile(mlContext,
                ModelPath1Zip, ModelPath2Zip, ModelPath3Zip,
                myAssetsPath, iRowCount, samples)
            Dim scoreRounded = XOR3Vector.TestSomePredictions(mlContext,
                ModelPath1Zip, ModelPath2Zip, ModelPath3Zip, iRowCount, samples)
            Dim scoreExpected = 1152
            Dim success = scoreRounded >= scoreExpected
            Console.WriteLine("Success: Score = " & scoreRounded & " >= " & scoreExpected & " : " & success)

            Return success

        End Function

        Public Function GetAbsolutePath(ByVal relativePath As String) As String
            Dim fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
            fullPath = Path.GetFullPath(fullPath)
            Return fullPath
        End Function

    End Module

    Public Module DataViewHelper

        <Extension()>
        Public Function ToDataTable(ByVal dataView As IDataView) As DataTable

            Dim dt As DataTable = Nothing
            If dataView IsNot Nothing Then
                dt = New DataTable()
                Dim preview = dataView.Preview()
                dt.Columns.AddRange(
                    preview.Schema.Select(Function(x) New DataColumn(x.Name)).ToArray())
                For Each row In preview.RowView
                    Dim r = dt.NewRow()
                    For Each col In row.Values
                        r(col.Key) = col.Value
                    Next
                    dt.Rows.Add(r)
                Next
            End If

            Return dt

        End Function

    End Module

End Namespace