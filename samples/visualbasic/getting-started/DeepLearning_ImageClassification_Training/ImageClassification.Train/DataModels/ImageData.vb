
Imports Microsoft.ML.Data
'Imports System
'Imports System.Collections.Generic
'Imports System.IO
'Imports System.Linq

Namespace ImageClassification.DataModels

	Public Class ImageData

		<LoadColumn(0)>
		Public ImagePath As String

		<LoadColumn(1)>
		Public Label As String

	End Class

	Public Class InMemoryImageData

		Public ReadOnly Image As Byte()
		Public ReadOnly Label As String
		Public ReadOnly ImageFileName As String

		Public Sub New(Image0 As Byte(), ImageFileName0$, Label0$)
			Image = Image0
			ImageFileName = ImageFileName0
			Label = Label0
		End Sub

		Public Sub New(Img As ImageData)
			ImageFileName = IO.Path.GetFileName(Img.ImagePath)
			Label = Img.Label
		End Sub

	End Class

End Namespace