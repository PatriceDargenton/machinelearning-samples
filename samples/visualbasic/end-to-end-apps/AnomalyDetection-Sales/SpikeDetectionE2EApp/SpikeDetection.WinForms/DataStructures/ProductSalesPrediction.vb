﻿
Imports Microsoft.ML.Data

Namespace SpikeDetection.WinForms

	Friend Class ProductSalesPrediction

		' Vector to hold Alert, Score, and P-Value values
		<VectorType(3)>
		Public Property Prediction As Double()

	End Class

End Namespace