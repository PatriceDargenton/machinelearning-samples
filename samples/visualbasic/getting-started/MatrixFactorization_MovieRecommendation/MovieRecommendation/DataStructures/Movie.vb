﻿
Imports System.IO

Namespace MovieRecommendation.DataStructures

    Friend Class Movie

        Public movieId As Integer

        Public movieTitle As String

        Private Shared moviesdatasetRelativepath As String =
            $"{Program.DatasetsRelativePath}/recommendation-movies.csv"
        Private Shared moviesdatasetpath As String = Program.GetAbsolutePath(moviesdatasetRelativepath)

        Public _movies As New Lazy(Of List(Of Movie))(Function() LoadMovieData(moviesdatasetpath))

        Public Sub New()
        End Sub

        Public Function [Get](id As Integer) As Movie
            Return _movies.Value.Single(Function(m) m.movieId = id)
        End Function

        Private Shared Function LoadMovieData(moviesdatasetpath As String) As List(Of Movie)

            Dim result = New List(Of Movie)

            Try
                Using fileReader As Stream = File.OpenRead(moviesdatasetpath)
                    Using reader As New StreamReader(fileReader)
                        Dim header As Boolean = True
                        Dim index As Integer = 0
                        Dim line = ""
                        Do While Not reader.EndOfStream
                            If header Then
                                line = reader.ReadLine()
                                header = False
                            End If
                            line = reader.ReadLine()
                            Dim fields() As String = line.Split(","c)
                            Dim movieId As Integer = Int32.Parse(fields(0).ToString().TrimStart(New Char() {"0"c}))
                            Dim movieTitle As String = fields(1).ToString()
                            result.Add(New Movie With {
                                .movieId = movieId,
                                .movieTitle = movieTitle
                            })
                            index += 1
                        Loop
                    End Using
                End Using
            Finally
            End Try

            Return result

        End Function

    End Class

End Namespace