﻿
Imports Microsoft.ML.Data

Namespace SentimentAnalysisAutoML.DataStructures

	Public Class SentimentIssue

		<LoadColumn(0)>
		Public Property Label As Boolean

		<LoadColumn(1)>
		Public Property Text As String

	End Class

End Namespace