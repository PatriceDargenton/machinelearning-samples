
Namespace TFFEImageClassification.DataModels

    Public Class ImagePrediction

        Public Score() As Single

        Public PredictedLabelValue As String

    End Class

    Public Class ImageWithLabelPrediction : Inherits ImagePrediction

        Public Sub New(pred As ImagePrediction, label As String)
            Me.Label = label
            Score = pred.Score
            PredictedLabelValue = pred.PredictedLabelValue
        End Sub

        Public Label As String

    End Class

End Namespace