﻿
Imports Microsoft.ML.Data

Namespace PowerAnomalyDetection.DataStructures

    Friend Class SpikePrediction

        <VectorType(3)>
        Public Property Prediction As Double()

    End Class

End Namespace