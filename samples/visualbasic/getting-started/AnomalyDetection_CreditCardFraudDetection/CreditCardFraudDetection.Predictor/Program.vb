
Imports System.IO
Imports CreditCardFraudDetection.Common.CreditCardFraudDetection.Common

Namespace CreditCardFraudDetection.Predictor

    Public Class Program

        Shared Sub Main(args() As String)

            Dim assetsPath As String = GetAbsolutePath("../../../assets")
            Dim trainOutput As String = GetAbsolutePath("../../../../CreditCardFraudDetection.Trainer/assets/output")

            GetResult(assetsPath, trainOutput)

            Console.WriteLine("=============== Press any key ===============")
            Console.ReadKey()

        End Sub

        Public Shared Function GetResult(
                myAssetsPath As String, mytrainOutput As String) As Boolean

            Dim inputDatasetForPredictions = Path.Combine(myAssetsPath, "input", "testData.csv")
            Dim modelFilePath = Path.Combine(myAssetsPath, "input", "randomizedPca.zip")

            ' Always copy the trained model from the trainer project just in case there's a new version trained
            CopyModelAndDatasetFromTrainingProject(mytrainOutput, myAssetsPath)

            ' Create model predictor to perform a few predictions
            Dim modelPredictor = New Predictor(modelFilePath, inputDatasetForPredictions)

            Dim success = False
            modelPredictor.RunMultiplePredictions(numberOfPredictions:=5, success)
            Console.WriteLine("Success: " & success)

            Return success

        End Function

        Public Shared Sub CopyModelAndDatasetFromTrainingProject(trainOutput As String, assetsPath As String)

            If Not File.Exists(Path.Combine(trainOutput, "testData.csv")) OrElse
               Not File.Exists(Path.Combine(trainOutput, "randomizedPca.zip")) Then
                Console.WriteLine("***** YOU NEED TO RUN THE TRAINING PROJECT FIRST *****")
                Console.WriteLine("=============== Press any key ===============")
                Console.ReadKey()
                Environment.Exit(0)
            End If

            ' Copy files from train output
            Directory.CreateDirectory(assetsPath)

            For Each file In Directory.GetFiles(trainOutput)
                Dim fileDestination = Path.Combine(Path.Combine(assetsPath, "input"), Path.GetFileName(file))

                If System.IO.File.Exists(fileDestination) Then
                    LocalConsoleHelper.DeleteAssets(fileDestination)
                End If

                ' Only copy the files we need for the scoring project
                If (Path.GetFileName(file) = "testData.csv") OrElse
                   (Path.GetFileName(file) = "randomizedPca.zip") Then
                    System.IO.File.Copy(file,
                        Path.Combine(Path.Combine(assetsPath, "input"), Path.GetFileName(file)))
                End If
            Next file

        End Sub

        Public Shared Function GetAbsolutePath(relativePath As String) As String
            Dim fullPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
            Return fullPath
        End Function

    End Class

End Namespace