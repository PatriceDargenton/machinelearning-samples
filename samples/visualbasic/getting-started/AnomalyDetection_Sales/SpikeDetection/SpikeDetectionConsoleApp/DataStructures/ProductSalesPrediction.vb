﻿
Imports Microsoft.ML.Data

Namespace SpikeDetection.DataStructures

	Public Class ProductSalesPrediction

		' Vector to hold alert, score, p-value values
		<VectorType(3)>
		Public Property Prediction As Double()

	End Class

End Namespace