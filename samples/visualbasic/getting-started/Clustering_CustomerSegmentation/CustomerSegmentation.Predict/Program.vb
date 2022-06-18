
Imports System.IO
Imports Microsoft.ML
Imports CustomerSegmentation.Predict.CustomerSegmentation.Model

Namespace CustomerSegmentation.Predict

    Public Class Program

        Shared Sub Main(args() As String)

            Dim assetsRelativePath = "../../../assets"
            Dim assetsPath As String = GetAbsolutePath(assetsRelativePath)
            Dim assetsRelativeTrainPath = "../../../../CustomerSegmentation.Train/assets/outputs"
            Dim assetsTrainPath = GetAbsolutePath(assetsRelativeTrainPath)

            GetResult(assetsPath, assetsTrainPath)

            Common.ConsoleHelper.ConsolePressAnyKey()

        End Sub

        Public Shared Function GetResult(myAssetsPath As String, assetsTrainPath As String,
                Optional isTest As Boolean = False) As Boolean

            Dim pivotCsv = Path.Combine(myAssetsPath, "inputs", "pivot.csv")
            ' Use directly the last saved:
            'Dim modelPath = Path.Combine(myAssetsPath, "inputs", "retailClustering.zip")
            Dim modelPath = Path.Combine(assetsTrainPath, "retailClustering.zip")
            modelPath = Path.GetFullPath(modelPath)
            If Not File.Exists(modelPath) Then
                Console.WriteLine("Please run the model training first.")
                Environment.Exit(0)
            End If
            Dim plotSvg = Path.Combine(myAssetsPath, "outputs", "customerSegmentation.svg")
            Dim plotCsv = Path.Combine(myAssetsPath, "outputs", "customerSegmentation.csv")

            Try
                ' Seed set to any number so you have a deterministic results
                Dim mlContext As New MLContext

                ' Create the clusters: Create data files and plot a chart
                Dim clusteringModelScorer = New ClusteringModelScorer(
                    mlContext, pivotCsv, plotSvg, plotCsv)
                clusteringModelScorer.LoadModel(modelPath)
                Dim success = False
                clusteringModelScorer.CreateCustomerClusters(success, isTest)
                Return success

            Catch ex As Exception
                Common.ConsoleHelper.ConsoleWriteException(ex.ToString())
                Return False
            End Try

        End Function

        Public Shared Function GetAbsolutePath(relativePath As String) As String
            Dim fullPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
            Return fullPath
        End Function

    End Class

End Namespace