﻿
Imports Microsoft.ML
Imports Microsoft.ML.DataOperationsCatalog
Imports TFFEImageClassification.Train.TFFEImageClassification.DataModels
Imports TFFEImageClassification.Train.Common
Imports System.IO

Namespace TFFEImageClassification.Model

    Public Class ModelBuilder

        Private ReadOnly dataLocation As String
        Private ReadOnly imagesFolder As String
        Private ReadOnly inputTensorFlowModelFilePath As String
        Private ReadOnly outputMlNetModelFilePath As String
        Private ReadOnly mlContext As MLContext
        Private Shared LabelAsKey As String = NameOf(LabelAsKey)
        Private Shared ImageReal As String = NameOf(ImageReal)
        Private Shared PredictedLabelValue As String = NameOf(PredictedLabelValue)

        Public Sub New(inputModelLocation As String, outputModelLocation As String)
            Me.inputTensorFlowModelFilePath = inputModelLocation
            Me.outputMlNetModelFilePath = outputModelLocation
            mlContext = New MLContext() 'seed:=1
        End Sub

        Private Structure ImageSettingsForTFModel
            Public Const imageHeight As Integer = 299 '224 for Inception v1 --- 299 for Inception v3
            Public Const imageWidth As Integer = 299 '224 for Inception v1 --- 299 for Inception v3
            Public Const mean As Single = 117 ' (offsetImage: ImageSettingsForTFModel.mean)
            Public Const scale As Single = 1 / 255.0F '1/255f for InceptionV3. Not used for InceptionV1
            ' true for Inception v1 (interleavePixelColors: ImageSettingsForTFModel.channelsLast)
            Public Const channelsLast As Boolean = True

            'Public Const int imageHeight = 299;        //224 For Inception v1 --- 299 For Inception v3
            'Public Const int imageWidth = 299;         //224 For Inception v1 --- 299 For Inception v3
            'Public Const float mean = 117;             // (offsetImage: ImageSettingsForTFModel.mean)
            'Public Const float scale = 1/255F;         //1/255F For InceptionV3. Not used For InceptionV1
            'Public Const bool channelsLast = True;     //True For Inception v1 (interleavePixelColors: ImageSettingsForTFModel.channelsLast)

        End Structure

        Public Sub BuildAndTrain(imageSet As IEnumerable(Of ImageData), ByRef score#)

            ConsoleWriteHeader("Read model")
            Console.WriteLine($"Model location: {inputTensorFlowModelFilePath}")
            Console.WriteLine($"Training file: {dataLocation}")

            ' 1. Load images information (filenames and labels) in IDataView

            ' Load the initial single full Image-Set
            Dim fullImagesDataset As IDataView = mlContext.Data.LoadFromEnumerable(imageSet)
            Dim shuffledFullImagesDataset As IDataView = mlContext.Data.ShuffleRows(fullImagesDataset)

            ' Split the data 90:10 into train and test sets, train and evaluate
            Dim trainTestData As TrainTestData = mlContext.Data.TrainTestSplit(
                shuffledFullImagesDataset, testFraction:=0.1)
            Dim trainDataView As IDataView = trainTestData.TrainSet
            Dim testDataView As IDataView = trainTestData.TestSet

            ' 2. Load images in-memory while applying image transformations 
            ' Input and output column names have to coincide with the input and output tensor names
            '  of the TensorFlow model
            ' You can check out those tensor names by opening the Tensorflow .pb model with a visual tool
            '  like Netron: https://github.com/lutzroeder/netron
            ' TF .pb model --> input node --> INPUTS --> input --> id: "input" 
            ' TF .pb model --> Softmax node --> INPUTS --> logits --> 
            '  id: "softmax2_pre_activation" (Inceptionv1) or 
            '      "InceptionV3/Predictions/Reshape" (Inception v3)

            Dim dataProcessPipeline = mlContext.Transforms.Conversion.MapValueToKey(
                outputColumnName:=LabelAsKey, inputColumnName:="Label").
                Append(mlContext.Transforms.LoadImages(
                    outputColumnName:="image_object", imageFolder:=imagesFolder,
                    inputColumnName:=NameOf(DataModels.ImageData.ImagePath))).
                Append(mlContext.Transforms.ResizeImages(
                    outputColumnName:="image_object_resized",
                    imageWidth:=ImageSettingsForTFModel.imageWidth,
                    imageHeight:=ImageSettingsForTFModel.imageHeight,
                    inputColumnName:="image_object")).
                Append(mlContext.Transforms.ExtractPixels(
                    outputColumnName:="input", inputColumnName:="image_object_resized",
                    interleavePixelColors:=ImageSettingsForTFModel.channelsLast,
                    offsetImage:=ImageSettingsForTFModel.mean,
                    scaleImage:=ImageSettingsForTFModel.scale)).
                Append(mlContext.Model.LoadTensorFlowModel(inputTensorFlowModelFilePath).
                ScoreTensorFlowModel(
                    outputColumnNames:={"InceptionV3/Predictions/Reshape"},
                    inputColumnNames:={"input"},
                    addBatchDimensionInput:=False))
            ' (For Inception v1 --> addBatchDimensionInput: true)
            ' (For Inception v3 --> addBatchDimensionInput: false)

            ' 3. Set the training algorithm and convert back the key to the categorical values                            
            Dim trainer = mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy(
                labelColumnName:=LabelAsKey, featureColumnName:="InceptionV3/Predictions/Reshape")
            '"softmax2_pre_activation" for Inception v1

            Dim trainingPipeline = dataProcessPipeline.Append(trainer).
                Append(mlContext.Transforms.Conversion.MapKeyToValue(PredictedLabelValue, "PredictedLabel"))

            ' 4. Train the model
            ' Measuring training time
            Dim watch = System.Diagnostics.Stopwatch.StartNew()

            ConsoleWriteHeader("Training the ML.NET classification model")
            Dim model As ITransformer = trainingPipeline.Fit(trainDataView)

            watch.Stop()
            Dim elapsedMs As Long = watch.ElapsedMilliseconds
            Console.WriteLine("Training with transfer learning took: " &
                              (elapsedMs \ 1000).ToString() & " seconds")

            ' 5. Make bulk predictions and calculate quality metrics
            ConsoleWriteHeader("Create Predictions and Evaluate the model quality")
            Dim predictionsDataView As IDataView = model.Transform(testDataView)

            ' This is an optional step, but it's useful for debugging issues
            Dim loadedModelOutputColumnNames =
                predictionsDataView.Schema.
                Where(Function(col) Not col.IsHidden).Select(Function(col) col.Name)

            ' 5.1 Show the predictions
            ConsoleWriteHeader("*** Showing all the predictions ***")
            Dim predictions As List(Of ImagePredictionEx) =
                mlContext.Data.CreateEnumerable(Of ImagePredictionEx)(
                    predictionsDataView,
                    reuseRowObject:=False, ignoreMissingColumns:=True).ToList()
            predictions.ForEach(
                Sub(pred) ConsoleWriteImagePrediction(
                    pred.ImagePath, pred.Label, pred.PredictedLabelValue, pred.Score.Max()))

            ' 5.2 Show the performance metrics for the multi-class classification            
            Dim classificationContext = mlContext.MulticlassClassification
            ConsoleWriteHeader("Classification metrics")
            Dim metrics = classificationContext.Evaluate(
                predictionsDataView, labelColumnName:=LabelAsKey, predictedLabelColumnName:="PredictedLabel")
            ConsoleHelper.PrintMultiClassClassificationMetrics(trainer.ToString(), metrics)
            score = metrics.MacroAccuracy

            ' 6. Save the model to assets/outputs
            ConsoleWriteHeader("Save model to local file")
            ModelHelpers.DeleteAssets(outputMlNetModelFilePath)

            Dim parentDir = IO.Path.GetDirectoryName(outputMlNetModelFilePath)
            If Not Directory.Exists(parentDir) Then Directory.CreateDirectory(parentDir)
            mlContext.Model.Save(model, predictionsDataView.Schema, outputMlNetModelFilePath)
            Console.WriteLine($"Model saved: {outputMlNetModelFilePath}")

        End Sub

    End Class

End Namespace