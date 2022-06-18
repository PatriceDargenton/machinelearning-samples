﻿
Imports Microsoft.ML.Data

Namespace MNIST.DataStructures

	Friend Class InputData

		<ColumnName("PixelValues")>
		<VectorType(64)>
		Public PixelValues() As Single

		<LoadColumn(64)>
		Public Number As Single

	End Class

End Namespace