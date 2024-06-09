
Imports System.IO
Imports TFFEImageClassification.Train.Common
Imports TFFEImageClassification.Train.TFFEImageClassification.DataModels
Imports TFFEImageClassification.Train.TFFEImageClassification.Model

Namespace TFFEImageClassification.Train

    Public Class Program

        ' SINGLE SMALL FLOWERS IMAGESET (200 files)
        Const imagesDatasetZip As String = "flower_photos_small_set.zip"
        Const imagesDatasetUrl As String = "https://bit.ly/3fkRKYy"

        ' https://bit.ly/3HZmnz1 à tester
        ' SINGLE FULL FLOWERS IMAGESET (3,600 files)
        'Const imagesDatasetZip As String = "flower_photos.tgz"
        'Const imagesDatasetUrl As String =
        '   "http://download.tensorflow.org/example_images/" & imagesDatasetZip

        ' https://storage.googleapis.com/download.tensorflow.org/models/inception5h.zip = inception-v1.zip
        ' https://bit.ly/3nppmc2 v1 inception-v1.zip
        ' https://bit.ly/3KjEiCH v3 inception-v3.zip
        ' Does not work (nor in the C# version, despite the instructions in the README.md,
        '  but works fine in the F# version):
        'Const inceptionFile As String = "inception-v1"
        'Const inceptionGraph As String = "tensorflow_inception_graph.pb"
        ' Works fine:
        Const inceptionFile As String = "inception-v3"
        Const inceptionGraph As String = "inception_v3_2016_08_28_frozen.pb"

        Const inceptionGraphZip As String = inceptionFile & ".zip"
        'Const inceptionGraphUrl As String = "https://bit.ly/3nppmc2" ' v1
        Const inceptionGraphUrl As String = "https://bit.ly/3KjEiCH" ' v3

        Shared Sub Main(args() As String)

            Dim assetsRelativePath As String = "../../../assets"
            Dim assetsPath As String = GetAbsolutePath(assetsRelativePath)

            Dim commonDatasetsRelativePath As String = "../../../../../../../../datasets"
            Dim commonDatasetsPath As String = GetAbsolutePath(commonDatasetsRelativePath)
            Dim commonGraphsRelativePath As String = "../../../../../../../../graphs"
            Dim commonGraphsPath As String = GetAbsolutePath(commonGraphsRelativePath)

            GetResult(assetsPath, commonDatasetsPath, commonGraphsPath)

            ConsolePressAnyKey()

        End Sub

        Public Shared Function GetResult(
                myAssetsPath As String,
                myCommonDatasetsPath As String, myCommonGraphsPath As String) As Boolean

            Dim inceptionGraphPath = Path.Combine(myAssetsPath,
                "inputs", "tensorflow-pretrained-models", inceptionFile, inceptionGraphZip)
            Dim inceptionPb = Path.Combine(myAssetsPath, "inputs", "tensorflow-pretrained-models",
                inceptionFile, inceptionGraph)

            Dim imageClassifierZip = Path.Combine(myAssetsPath, "outputs", "imageClassifier.zip")

            Dim tagsTsv = Path.Combine(myAssetsPath, "inputs", "data", "tags.tsv")

            'DownloadGraph(inceptionPb, inceptionGraphPath, inceptionGraphUrl, commonInceptionGraphPath)
            Dim inceptionFolder As String = Path.Combine(myAssetsPath, "inputs", "tensorflow-pretrained-models")
            Dim fullIncepionFolderPath As String = Path.Combine(
                inceptionFolder, Path.GetFileNameWithoutExtension(inceptionGraphZip))
            Dim destFiles As New List(Of String) From {inceptionPb}
            DownloadBigFile(fullIncepionFolderPath, inceptionGraphUrl, inceptionGraphZip,
                myCommonGraphsPath, destFiles, destFolder:=fullIncepionFolderPath)

            'Dim imagesDownloadFolderPath As String = Path.Combine(myAssetsPath, "inputs", "images")
            Dim imagesFolder = Path.Combine(myAssetsPath, "inputs", "images")

            'Dim finalImagesFolderName As String = DownloadImageSet(imagesDownloadFolderPath)
            'DownloadImageSet(imagesFolder, imagesDatasetUrl, imagesDatasetZip, commonDatasetsPath)
            Dim fullImagesetFolderPath As String = Path.Combine(
                imagesFolder, Path.GetFileNameWithoutExtension(imagesDatasetZip))
            Dim image1 = Path.Combine(fullImagesetFolderPath, "daisy\286875003_f7c0e1882d.jpg")
            Dim destImagesFiles As New List(Of String) From {image1}
            DownloadBigFile(imagesFolder, imagesDatasetUrl, imagesDatasetZip, myCommonDatasetsPath,
                destImagesFiles, fullImagesetFolderPath)

            'Dim fullImagesetFolderPath = Path.Combine(imagesDownloadFolderPath, finalImagesFolderName)
            'Console.WriteLine($"Images folder: {fullImagesetFolderPath}")

            ' Single full dataset
            Dim allImages As IEnumerable(Of ImageData) =
                LoadImagesFromDirectory(folder:=fullImagesetFolderPath, useFolderNameasLabel:=True)
            Try
                Dim modelBuilder = New ModelBuilder(inceptionPb, imageClassifierZip)
                Dim score# = 0
                modelBuilder.BuildAndTrain(allImages, score)

                Dim scoreRounded = Math.Round(score, digits:=4) * 100
                Dim scoreExpected = 70
                Dim success = scoreRounded >= scoreExpected
                Console.WriteLine("Success: Score = " & scoreRounded.ToString("0.00") & " >= " & scoreExpected & " : " & success)

                Return success

            Catch ex As Exception
                ConsoleWriteException(ex.ToString())
                Return False
            End Try

        End Function

        Public Shared Iterator Function LoadImagesFromDirectory(folder As String,
                Optional useFolderNameasLabel As Boolean = True) As IEnumerable(Of ImageData)

            Dim files = Directory.GetFiles(folder, "*", searchOption:=SearchOption.AllDirectories)

            For Each file In files
                If (Path.GetExtension(file) <> ".jpg") AndAlso (Path.GetExtension(file) <> ".png") Then
                    Continue For
                End If

                Dim label = Path.GetFileName(file)
                If useFolderNameasLabel Then
                    label = Directory.GetParent(file).Name
                Else
                    Dim index As Integer = 0
                    Do While index < label.Length
                        If Not Char.IsLetter(label.Chars(index)) Then
                            label = label.Substring(0, index)
                            Exit Do
                        End If
                        index += 1
                    Loop
                End If

                Yield New ImageData With {
                    .ImagePath = file,
                    .Label = label
                }

            Next file

        End Function

        'Public Shared Sub DownloadGraph(inceptionPb As String, graphZipPath As String,
        '        inceptionZipUrl As String, commonGraphsPath As String)

        '    If Not File.Exists(graphZipPath) AndAlso File.Exists(commonGraphsPath) Then
        '        IO.File.Copy(commonGraphsPath, graphZipPath)
        '    ElseIf File.Exists(graphZipPath) AndAlso Not File.Exists(commonGraphsPath) Then
        '        IO.File.Copy(graphZipPath, commonGraphsPath)
        '    End If

        '    If File.Exists(inceptionPb) Then Exit Sub

        '    If Not File.Exists(graphZipPath) Then
        '        Console.WriteLine("====Downloading and extracting zip====")
        '        Using client = New MyWebClient
        '            ' The code below will download a dataset from a third-party,
        '            '  and may be governed by separate third-party terms
        '            ' By proceeding, you agree to those separate terms
        '            client.DownloadFile(inceptionZipUrl, graphZipPath)
        '        End Using
        '        Console.WriteLine("====Downloading is completed====")
        '        IO.File.Copy(graphZipPath, commonGraphsPath)
        '    End If

        '    If File.Exists(graphZipPath) Then
        '        Console.WriteLine("====Extracting zip====")
        '        Dim myFastZip As New FastZip()
        '        Dim actualDirectory = Path.GetDirectoryName(graphZipPath)
        '        Debug.WriteLine(Path.GetFullPath(graphZipPath))
        '        Debug.WriteLine(Path.GetFullPath(actualDirectory))
        '        Debug.WriteLine(Directory.Exists(actualDirectory))
        '        myFastZip.ExtractZip(graphZipPath, actualDirectory, fileFilter:=String.Empty)
        '        Console.WriteLine("====Extracting is completed====")
        '        If File.Exists(inceptionPb) Then
        '            Exit Sub
        '        Else
        '            Console.WriteLine("====Extracting: Fail!====")
        '            Stop
        '        End If
        '    End If

        'End Sub

        'Public Shared Sub DownloadImageSet(imagesFolder As String,
        '        imagesDatasetUrl As String, imagesDatasetZip As String, commonDatasetsPath As String)

        '    ' Get a set of images to teach the network about the new classes

        '    Dim zipPath = Path.Combine(imagesFolder, imagesDatasetZip)
        '    Dim commonPath = Path.Combine(commonDatasetsPath, imagesDatasetZip)

        '    If Not File.Exists(zipPath) AndAlso File.Exists(commonPath) Then
        '        IO.File.Copy(commonPath, zipPath)
        '    ElseIf File.Exists(zipPath) AndAlso Not File.Exists(commonPath) Then
        '        IO.File.Copy(zipPath, commonPath)
        '    End If

        '    Dim imagesDirectory = Path.Combine(
        '        imagesFolder, Path.GetFileNameWithoutExtension(imagesDatasetZip))
        '    If Directory.Exists(imagesDirectory) Then Exit Sub

        '    If Not File.Exists(zipPath) Then
        '        Console.WriteLine("====Downloading zip (images)====")
        '        Using client = New MyWebClient
        '            ' The code below will download a dataset from a third-party,
        '            '  and may be governed by separate third-party terms
        '            ' By proceeding, you agree to those separate terms
        '            client.DownloadFile(imagesDatasetUrl, zipPath)
        '        End Using
        '        Console.WriteLine("====Downloading is completed (images)====")
        '        IO.File.Copy(zipPath, commonPath)
        '    End If

        '    If File.Exists(zipPath) Then
        '        Console.WriteLine("====Extracting zip====")
        '        If zipPath.EndsWith(".tgz") Then
        '            Using inputStream = File.OpenRead(zipPath)
        '                Using gzipStream = New GZipInputStream(inputStream)
        '                    Using tarArchive As TarArchive = TarArchive.CreateInputTarArchive(
        '                            gzipStream, nameEncoding:=System.Text.Encoding.Default)
        '                        tarArchive.ExtractContents(imagesFolder)
        '                    End Using
        '                End Using
        '            End Using
        '        Else
        '            Dim myFastZip As New FastZip()
        '            myFastZip.ExtractZip(zipPath, imagesFolder, fileFilter:=String.Empty)
        '        End If
        '        Console.WriteLine("====Extracting is completed (images)====")
        '        If Directory.Exists(imagesDirectory) Then Exit Sub
        '        Console.WriteLine("====Extracting: Fail! (images)====")
        '        Stop
        '    End If

        '    'SMALL FLOWERS IMAGESET (200 files)
        '    'Dim fileName As String = "flower_photos_small_set.zip"
        '    'Dim url As String = $"https://mlnetfilestorage.file.core.windows.net/imagesets/flower_images/flower_photos_small_set.zip?st=2019-08-07T21%3A27%3A44Z&se=2030-08-08T21%3A27%3A00Z&sp=rl&sv=2018-03-28&sr=f&sig=SZ0UBX47pXD0F1rmrOM%2BfcwbPVob8hlgFtIlN89micM%3D"
        '    'Web.Download(url, imagesDownloadFolder, fileName)
        '    'Compress.UnZip(Path.Join(imagesDownloadFolder, fileName), imagesDownloadFolder)

        '    'FULL FLOWERS IMAGESET (3,600 files)
        '    'string fileName = "flower_photos.tgz";
        '    'string url = $"http://download.tensorflow.org/example_images/{fileName}";
        '    'Web.Download(url, imagesDownloadFolder, fileName);
        '    'Compress.ExtractTGZ(Path.Join(imagesDownloadFolder, fileName), imagesDownloadFolder);

        '    'Return Path.GetFileNameWithoutExtension(fileName)

        'End Sub

        Public Shared Function GetAbsolutePath(relativePath As String) As String
            Dim fullPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
            Return fullPath
        End Function

    End Class

End Namespace