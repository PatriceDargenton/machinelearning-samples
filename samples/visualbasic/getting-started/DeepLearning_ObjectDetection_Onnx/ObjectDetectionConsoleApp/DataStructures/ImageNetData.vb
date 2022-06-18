﻿
Imports System.IO
Imports Microsoft.ML.Data

Namespace ObjectDetection.DataStructures

    Public Class ImageNetData

        <LoadColumn(0)>
        Public ImagePath As String

        <LoadColumn(1)>
        Public Label As String

        Public Shared Function ReadFromFile(imageFolder As String) As IEnumerable(Of ImageNetData)
            Return Directory.GetFiles(imageFolder).
                Where(Function(filePath) Path.GetExtension(filePath) = ".jpg").
                Select(Function(filePath) New ImageNetData With {
                    .ImagePath = filePath,
                    .Label = Path.GetFileName(filePath)
            })
        End Function

    End Class

End Namespace