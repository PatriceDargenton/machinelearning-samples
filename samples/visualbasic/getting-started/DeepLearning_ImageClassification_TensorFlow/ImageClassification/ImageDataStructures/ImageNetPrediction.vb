﻿
Imports DLTFImageClassification.Score.DLTFImageClassification.ModelScorer
Imports Microsoft.ML.Data

Namespace DLTFImageClassification.ImageDataStructures

    Public Class ImageNetPrediction

        <ColumnName(TFModelScorer.InceptionSettings.outputTensorName)>
        Public PredictedLabels() As Single

    End Class

End Namespace