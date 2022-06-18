
Imports System.IO
Imports TFFEImageClassification.Predict.Common
Imports TFFEImageClassification.Predict.TFFEImageClassification.Model

Namespace TFFEImageClassification.Predict

    Public Class Program

        Shared Sub Main(args() As String)

            Dim assetsRelativePath As String = "../../../assets"
            Dim assetsPath As String = GetAbsolutePath(assetsRelativePath)

            'Dim imageClassifierZip = Path.Combine(assetsPath, "inputs", "MLNETModel", "imageClassifier.zip")
            ' Use directly the last saved:
            Dim modelRelativePath As String =
                "../../../../ImageClassification.Train/assets"
            Dim modelPath As String = GetAbsolutePath(modelRelativePath)

            GetResult(assetsPath, modelPath)

            ConsolePressAnyKey()

        End Sub

        Public Shared Function GetResult(
                myAssetsPath As String, myModelPath As String,
                Optional isTest As Boolean = False) As Boolean

            Try
                myAssetsPath = Path.GetFullPath(myAssetsPath)
                myModelPath = Path.GetFullPath(myModelPath)
                Dim imagesFolder = Path.Combine(myAssetsPath, "inputs",
                    "images-for-predictions")
                Dim imageClassifierZip = Path.Combine(myModelPath, "outputs",
                    "imageClassifier.zip")
                If Not File.Exists(imageClassifierZip) Then
                    Console.WriteLine("Please run the model training first.")
                    Environment.Exit(0)
                End If
                imagesFolder = Path.GetFullPath(imagesFolder)
                Dim modelScorer = New ModelScorer(imagesFolder, imageClassifierZip)
                Dim success = False
                modelScorer.ClassifyImages(success)
                Console.WriteLine("Success: " & success)
                Return success
            Catch ex As Exception
                ConsoleWriteException(ex.ToString())
                Return False
            End Try

        End Function

        Public Shared Function GetAbsolutePath(relativePath As String) As String
            Dim fullPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
            Return fullPath
        End Function

    End Class

End Namespace