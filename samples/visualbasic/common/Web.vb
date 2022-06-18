
Imports System.IO
Imports System.Threading
Imports System.Net ' WebClient
Imports System.Net.Http ' HttpClient
Imports ICSharpCode.SharpZipLib.Zip ' FastZip
Imports ICSharpCode.SharpZipLib.GZip
Imports ICSharpCode.SharpZipLib.Tar
Imports Flurl.Http ' url.DownloadFileAsync

Namespace Common

    ' Fixing redirection for NetCore application
    ' Unlike .Net 4, Net Core does not follow redirection from https to http, for new, higher security reasons
    ' However, it is still possible to follow the redirection explicitly, this is finally reasonable behavior
    ' Message=The remote server returned an error: (301) Moved Permanently.
    ' System.Net.WebException HResult=0x80131509 Source=System.Net.Requests
    ' https://github.com/dotnet/runtime/issues/23697

    Class MyHttpClient

        ' HttpClient is intended to be instantiated once per application, rather than per-use
        ' https://docs.microsoft.com/fr-fr/dotnet/api/system.net.http.httpclient?view=net-6.0
        Shared ReadOnly m_hClient As New HttpClient()

        Public Shared Sub DownloadFile(url As String, destPath As String)
            Dim success = Task.WhenAny(DownloadFileAsync(url, destPath)).Result
        End Sub

        Private Shared Async Function DownloadFileAsync(url As String, destPath As String) As Task(Of Boolean)

            Dim request = New HttpRequestMessage(HttpMethod.[Get], url)
            Dim httpResponseMessage = Await m_hClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead)
            Dim finalUrl As String
            If httpResponseMessage.Headers.Location Is Nothing Then
                finalUrl = url ' Direct download without redirection
            Else
                finalUrl = httpResponseMessage.Headers.Location.ToString()
            End If

            ' IMPORTANT
            ' .NET Core does not allow redirection from HTTPs -> HTTP and won't give a proper exception message
            ' We have to account for that ourselves by not following redirections and instead creating a new request
            ' Note that this behaviour doesn't exist in .NET Framework 4
            Dim destFolder = IO.Path.GetDirectoryName(destPath)
            Dim destFilename = IO.Path.GetFileName(destPath)
            ' DownloadFileAsync is not a member of String: install Flurl.Http from NuGet
            Dim destFullPath = Await finalUrl.DownloadFileAsync(destFolder, destFilename)

            Return True

        End Function

    End Class

    Public Module modWeb

        Public Sub DownloadBigFile(bigFileFolder As String, bigFileUrl As String,
                bigFileDest As String,
                commonDatasetsPath As String,
                Optional destFiles As List(Of String) = Nothing,
                Optional destFolder As String = Nothing,
                Optional restart As Boolean = False)

            Dim destPath = Path.Combine(bigFileFolder, bigFileDest)
            Dim commonPath = Path.Combine(commonDatasetsPath, bigFileDest)
            ' To simplify debugging
            Dim destFullPath = Path.GetFullPath(destPath)
            Dim destFolderFullPath = ""
            If Not String.IsNullOrEmpty(destFolder) Then destFolderFullPath = Path.GetFullPath(destFolder)
            Dim commonFullPath = Path.GetFullPath(commonPath)

            If Not File.Exists(destFullPath) AndAlso File.Exists(commonFullPath) Then
                Dim parentDir = IO.Path.GetDirectoryName(destFullPath)
                If Not Directory.Exists(parentDir) Then Directory.CreateDirectory(parentDir)
                IO.File.Copy(commonFullPath, destFullPath)
            ElseIf File.Exists(destFullPath) AndAlso Not File.Exists(commonFullPath) Then
                IO.File.Copy(destFullPath, commonFullPath)
            End If

            Dim compressedFile = False
            'Dim extension = Path.GetExtension(destFullPath)
            Dim extension = GetExtensions(destFullPath)
            If extension = ".zip" OrElse extension = ".tgz" OrElse extension = ".tar.gz" Then compressedFile = True

            Dim dirExists = True
            If Not String.IsNullOrEmpty(destFolderFullPath) Then
                dirExists = Directory.Exists(destFolderFullPath)
            End If

            If destFiles Is Nothing Then
                If File.Exists(destFullPath) Then
                    If Not compressedFile AndAlso dirExists Then Exit Sub
                End If
            Else
                Dim allFilesExist = True
                For Each oneFile In destFiles
                    Dim oneFileFullPath = Path.GetFullPath(oneFile)
                    If Not File.Exists(oneFileFullPath) Then allFilesExist = False : Exit For
                Next
                If allFilesExist AndAlso dirExists Then Exit Sub
            End If

            If Not File.Exists(destFullPath) Then
                Console.WriteLine("==== Downloading... ====")
                Dim directoryPath = Path.GetDirectoryName(destFullPath)
                Dim directoryPath2 = Path.GetDirectoryName(directoryPath)
                If Not Directory.Exists(directoryPath2) Then
                    Dim di As New DirectoryInfo(directoryPath2)
                    di.Create()
                End If
                If Not Directory.Exists(directoryPath) Then
                    Dim di As New DirectoryInfo(directoryPath)
                    di.Create()
                End If

                ' The code below will download a dataset from a third-party,
                '  and may be governed by separate third-party terms
                ' By proceeding, you agree to those separate terms

                ' Works fine, but with .Net6, WebClient is now obsolete
                'Using client = New MyWebClient
                '    client.DownloadFile(bigFileUrl, destFullPath)
                'End Using

                ' Downloading a file is now very easy with .Net6... using Flurl.Http!
                ' https://github.com/tmenier/Flurl
                MyHttpClient.DownloadFile(bigFileUrl, destFullPath)

                If File.Exists(destFullPath) Then
                    Console.WriteLine("==== Downloading is completed ====")
                Else
                    Console.WriteLine("==== Downloading: Fail! ====")
                    Environment.Exit(0)
                End If
            End If

            If File.Exists(destFullPath) Then
                Console.WriteLine("==== Extracting data... ====")
                Dim unzipSuccess = False
                Try
                    Select Case extension
                        Case ".zip"
                            Dim myFastZip As New FastZip()
                            myFastZip.ExtractZip(destFullPath, bigFileFolder, fileFilter:=String.Empty)
                            unzipSuccess = True

                        Case ".tgz", ".tar.gz"
                            Using inputStream = File.OpenRead(destFullPath)
                                Using gzipStream = New GZipInputStream(inputStream)
                                    Using tarArchive As TarArchive = TarArchive.CreateInputTarArchive(
                                            gzipStream, nameEncoding:=System.Text.Encoding.Default)
                                        tarArchive.ExtractContents(bigFileFolder)
                                    End Using
                                End Using
                            End Using
                            unzipSuccess = True

                    End Select
                Catch ex As Exception
                    Console.WriteLine("Can't unzip " &
                        IO.Path.GetFileName(destFullPath) & ":")
                    Console.WriteLine(ex.Message)
                    Debug.WriteLine("Can't unzip " &
                        IO.Path.GetFileName(destFullPath) & ":")
                    Debug.WriteLine(ex.Message)
                    ' If the file is corrupted then download it again
                    File.Delete(destFullPath)
                End Try

                If unzipSuccess AndAlso Not File.Exists(commonFullPath) Then
                    IO.File.Copy(destFullPath, commonFullPath)
                End If

                Dim success = False
                If Not String.IsNullOrEmpty(destFolderFullPath) Then
                    success = Directory.Exists(destFolderFullPath)
                ElseIf destFiles Is Nothing Then
                    If File.Exists(destFullPath) Then success = True
                Else
                    success = True
                    For Each oneFile In destFiles
                        If Not File.Exists(oneFile) Then success = False
                    Next
                End If

                If unzipSuccess AndAlso success Then
                    Console.WriteLine("==== Extracting: Done. ====")
                Else
                    Console.WriteLine("==== Extracting: Fail! ====")
                    Environment.Exit(0)
                End If

                If restart AndAlso success Then
                    Console.WriteLine("==== Please restart! ====")
                    Console.ReadKey()
                    Environment.Exit(0)
                End If

            End If

        End Sub

        Public Function GetExtensions(filePath As String) As String

            Dim longStr = filePath.Length
            Dim longStrWithoutDot = filePath.Replace(".", "").Length
            Dim nbDots% = longStr - longStrWithoutDot
            If nbDots = 2 Then
                ' Example: .tar.gz
                Dim reverseFilePath = Reverse(filePath)
                Dim firstDotPos% = reverseFilePath.IndexOf(".")
                Dim secondDotPos% = reverseFilePath.IndexOf(".", firstDotPos + 1)
                Dim doubleExtRev = reverseFilePath.Substring(0, secondDotPos + 1)
                Dim doubleExt = Reverse(doubleExtRev)
                Dim longStrDbleExt = doubleExt.Length
                If longStrDbleExt <= 10 Then Return doubleExt
            End If
            Return Path.GetExtension(filePath)

        End Function

        Public Function Reverse(s As String) As String
            If s Is Nothing Then Return Nothing
            Dim charArray As Char() = s.ToCharArray()
            Array.Reverse(charArray)
            Return New String(charArray)
        End Function

    End Module

#Disable Warning BC41002 ' Class must declare a 'Sub New' because the constructor in its base class is marked obsolete
#Disable Warning SYSLIB0014 ' WebClient: Type or member is obsolete

    Public Class Web

        Public Shared Function Download(url As String, destDir As String, destFileName As String) As Boolean

            If destFileName Is Nothing Then
                destFileName = url.Split(Path.DirectorySeparatorChar).Last()
            End If

            Directory.CreateDirectory(destDir)

            Dim relativeFilePath As String = Path.Combine(destDir, destFileName)

            If File.Exists(relativeFilePath) Then
                Console.WriteLine($"{relativeFilePath} already exists.")
                Return False
            End If

            Using wc = New WebClient
                Console.WriteLine($"Downloading {relativeFilePath}")
                Dim download0 = Task.Run(Sub() wc.DownloadFile(url, relativeFilePath))
                Do While Not download0.IsCompleted
                    Thread.Sleep(1000)
                    Console.Write(".")
                Loop
            End Using
            Console.WriteLine("")
            Console.WriteLine($"Downloaded {relativeFilePath}")

            Return True

        End Function

    End Class

    Class MyWebClient : Inherits WebClient

        ' HttpClient is intended to be instantiated once per application, rather than per-use
        ' https://docs.microsoft.com/fr-fr/dotnet/api/system.net.http.httpclient?view=net-6.0
        Shared ReadOnly m_hClient As New HttpClient()

        Public Overloads Sub DownloadFile(url As String, destPath As String)
            Dim success = Task.WhenAny(DownloadFileAsyncRedirect(url, destPath)).Result
        End Sub

        Private Shared Async Function DownloadFileAsyncRedirect(url As String, destPath As String) As Task(Of Boolean)

            Dim request = New HttpRequestMessage(HttpMethod.[Get], url)
            Dim httpResponseMessage = Await m_hClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead)
            Dim finalUrl As String
            If httpResponseMessage.Headers.Location Is Nothing Then
                finalUrl = url ' Direct download without redirection
            Else
                finalUrl = httpResponseMessage.Headers.Location.ToString()
            End If

            ' IMPORTANT
            ' .NET Core does not allow redirection from HTTPs -> HTTP and won't give a proper exception message
            ' We have to account for that ourselves by not following redirections and instead creating a new request
            ' Note that this behaviour doesn't exist in .NET Framework 4
            Using wClient = New WebClient()
                wClient.DownloadFile(finalUrl, destPath)
            End Using

            Return True

        End Function

    End Class

#Enable Warning SYSLIB0014 ' WebClient: Type or member is obsolete
#Enable Warning BC41002 ' Class must declare a 'Sub New' because the constructor in its base class is marked obsolete

End Namespace