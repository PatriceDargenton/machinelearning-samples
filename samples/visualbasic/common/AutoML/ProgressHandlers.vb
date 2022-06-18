
Imports Microsoft.ML.AutoML
Imports Microsoft.ML.Data

Namespace Common

	''' <summary>
	''' Progress handler that AutoML will invoke after each model it produces and evaluates
	''' </summary>
	Public Class BinaryExperimentProgressHandler
		Implements IProgress(Of RunDetail(Of BinaryClassificationMetrics))

		Private _iterationIndex As Integer

		Public Sub Report(iterationResult As RunDetail(Of BinaryClassificationMetrics)) _
				Implements IProgress(Of RunDetail(Of BinaryClassificationMetrics)).Report

			If _iterationIndex = 0 Then
				_iterationIndex += 1
                ConsoleHelperAutoML.PrintBinaryClassificationMetricsHeader()
            Else
				_iterationIndex += 1
			End If

			If iterationResult.Exception IsNot Nothing Then
                ConsoleHelperAutoML.PrintIterationException(iterationResult.Exception)
            Else
                ConsoleHelperAutoML.PrintIterationMetrics(_iterationIndex, iterationResult.TrainerName,
                    iterationResult.ValidationMetrics, iterationResult.RuntimeInSeconds)
            End If

		End Sub

	End Class

	''' <summary>
	''' Progress handler that AutoML will invoke after each model it produces and evaluates
	''' </summary>
	Public Class MulticlassExperimentProgressHandler
		Implements IProgress(Of RunDetail(Of MulticlassClassificationMetrics))

		Private _iterationIndex As Integer

		Public Sub Report(iterationResult As RunDetail(Of MulticlassClassificationMetrics)) _
			Implements IProgress(Of RunDetail(Of MulticlassClassificationMetrics)).Report

			If _iterationIndex = 0 Then
				_iterationIndex += 1
                ConsoleHelperAutoML.PrintMulticlassClassificationMetricsHeader()
            Else
				_iterationIndex += 1
			End If

			If iterationResult.Exception IsNot Nothing Then
                ConsoleHelperAutoML.PrintIterationException(iterationResult.Exception)
            Else
                ConsoleHelperAutoML.PrintIterationMetrics(_iterationIndex, iterationResult.TrainerName,
                    iterationResult.ValidationMetrics, iterationResult.RuntimeInSeconds)
            End If

		End Sub

	End Class

	''' <summary>
	''' Progress handler that AutoML will invoke after each model it produces and evaluates
	''' </summary>
	Public Class RegressionExperimentProgressHandler
		Implements IProgress(Of RunDetail(Of RegressionMetrics))

		Private _iterationIndex As Integer

		Public Sub Report(iterationResult As RunDetail(Of RegressionMetrics)) _
			Implements IProgress(Of RunDetail(Of RegressionMetrics)).Report

			If _iterationIndex = 0 Then
				_iterationIndex += 1
                ConsoleHelperAutoML.PrintRegressionMetricsHeader()
            Else
				_iterationIndex += 1
			End If

			If iterationResult.Exception IsNot Nothing Then
                ConsoleHelperAutoML.PrintIterationException(iterationResult.Exception)
            Else
                ConsoleHelperAutoML.PrintIterationMetrics(
                    _iterationIndex, iterationResult.TrainerName,
                    iterationResult.ValidationMetrics, iterationResult.RuntimeInSeconds)
            End If

		End Sub

	End Class

End Namespace