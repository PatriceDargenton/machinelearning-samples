﻿
Imports System.IO
Imports Microsoft.ML
Imports Microsoft.ML.Data
Imports Microsoft.ML.Transforms
Imports CustomerSegmentation.Train.CustomerSegmentation.DataStructures
Imports CustomerSegmentation.Train.Common

Namespace CustomerSegmentation.Train

    Public Class Program

        Shared Sub Main(args() As String)

            Dim assetsRelativePath As String = "../../../assets"
            Dim assetsPath As String = GetAbsolutePath(assetsRelativePath)

            Try
                GetResult(assetsPath)
            Catch ex As Exception
                Common.ConsoleHelper.ConsoleWriteException(ex.ToString())
            End Try

            Common.ConsoleHelper.ConsolePressAnyKey()

        End Sub

        Public Shared Function GetResult(myAssetsPath As String) As Boolean

            Dim transactionsCsv As String = Path.Combine(myAssetsPath, "inputs", "transactions.csv")
            Dim offersCsv As String = Path.Combine(myAssetsPath, "inputs", "offers.csv")
            Dim pivotCsv As String = Path.Combine(myAssetsPath, "inputs", "pivot.csv")
            Dim modelPath As String = Path.Combine(myAssetsPath, "outputs", "retailClustering.zip")
            'Dim modelPathPredict As String =
            '    Path.Combine(myAssetsPathPredict, "inputs", "retailClustering.zip")

            ' STEP 0: Special data pre-process in this sample creating the PivotTable csv file
            DataHelpers.PreProcessAndSave(offersCsv, transactionsCsv, pivotCsv)

            ' Create the MLContext to share across components for deterministic results
            ' Seed set to any number so you have a deterministic environment
            Dim mlContext As New MLContext() ' seed:=1

            ' STEP 1: Common data loading configuration
            Dim pivotDataView = mlContext.Data.LoadFromTextFile(path:=pivotCsv,
                columns:={
                    New TextLoader.Column("Features", DataKind.Single,
                        New TextLoader.Range() {New TextLoader.Range(0, 31)}),
                    New TextLoader.Column(NameOf(PivotData.LastName), DataKind.String, 32)
                }, hasHeader:=True, separatorChar:=","c)

            ' STEP 2: Configure data transformations in pipeline
            Dim dataProcessPipeline = mlContext.Transforms.ProjectToPrincipalComponents(
                outputColumnName:="PCAFeatures", inputColumnName:="Features", rank:=2).
                Append(mlContext.Transforms.Categorical.OneHotEncoding(
                    outputColumnName:="LastNameKey",
                    inputColumnName:=NameOf(PivotData.LastName),
                    OneHotEncodingEstimator.OutputKind.Indicator))

            ' (Optional) Peek data in training DataView after applying the ProcessPipeline's transformations
            Common.ConsoleHelper.PeekDataViewInConsole(
                mlContext, pivotDataView, dataProcessPipeline, 10)
            Common.ConsoleHelper.PeekVectorColumnDataInConsole(
                mlContext, "Features", pivotDataView, dataProcessPipeline, 10)

            ' STEP 3: Create the training pipeline
            Dim trainer = mlContext.Clustering.Trainers.KMeans(
                featureColumnName:="Features", numberOfClusters:=3)
            Dim trainingPipeline = dataProcessPipeline.Append(trainer)

            ' STEP 4: Train the model fitting to the pivotDataView
            Console.WriteLine("=============== Training the model ===============")
            Dim trainedModel As ITransformer = trainingPipeline.Fit(pivotDataView)

            ' STEP 5: Evaluate the model and show accuracy stats
            Console.WriteLine("===== Evaluating Model's accuracy with Test data =====")
            Dim predictions = trainedModel.Transform(pivotDataView)
            Dim metrics = mlContext.Clustering.Evaluate(
                predictions, scoreColumnName:="Score", featureColumnName:="Features")

            ConsoleHelper.PrintClusteringMetrics(trainer.ToString(), metrics)

            ' STEP 6: Save/persist the trained model to a .ZIP file
            mlContext.Model.Save(trainedModel, pivotDataView.Schema, modelPath)
            Console.WriteLine("The model is saved to {0}", modelPath)

            ' Copy the trained model also into the Predict project directory
            'mlContext.Model.Save(trainedModel, pivotDataView.Schema, modelPathPredict)

            Dim dbiIndex = metrics.DaviesBouldinIndex
            Dim indRounded = Math.Round(dbiIndex, digits:=4)
            Dim indExpected = 3
            Dim success2 = indRounded <= indExpected
            Console.WriteLine("Success: Index = " & indRounded.ToString("0.00") & " <= " & indExpected & " : " & success2)

            Return success2

        End Function

        Public Shared Function GetAbsolutePath(relativePath As String) As String
            Dim fullPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
            Return fullPath
        End Function

    End Class

End Namespace