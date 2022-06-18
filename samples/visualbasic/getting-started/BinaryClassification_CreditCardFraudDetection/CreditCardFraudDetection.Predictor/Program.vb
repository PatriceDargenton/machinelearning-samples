
Imports System.IO
Imports BCCreditCardFraudDetection.Common.BCCreditCardFraudDetection.Common

Namespace BCCreditCardFraudDetection.Predictor

    Public Class Program

        Shared Sub Main(args() As String)

            Dim assetsPath As String = GetAbsolutePath("../../../assets")
            Dim trainOutput As String = GetAbsolutePath(
                "../../../../CreditCardFraudDetection.Trainer/assets/output")

            GetResult(assetsPath, trainOutput)

            Console.WriteLine("=============== Press any key ===============")
            Console.ReadKey()

        End Sub

        Public Shared Function GetResult(
                myAssetsPath As String, mytrainOutput As String) As Boolean

            CopyModelAndDatasetFromTrainingProject(mytrainOutput, myAssetsPath)

            Dim inputDatasetForPredictions = Path.Combine(myAssetsPath, "input", "testData.csv")
            Dim modelFilePath = Path.Combine(myAssetsPath, "input", "fastTree.zip")

            ' Create model predictor to perform a few predictions
            Dim modelPredictor = New Predictor(modelFilePath, inputDatasetForPredictions)

            Dim success = False
            modelPredictor.RunMultiplePredictions(numberOfPredictions:=5, success)
            Console.WriteLine("Success: " & success)

            Return success

        End Function

        Public Shared Sub CopyModelAndDatasetFromTrainingProject(trainOutput As String, assetsPath As String)

            If Not File.Exists(Path.Combine(trainOutput, "testData.csv")) OrElse
               Not File.Exists(Path.Combine(trainOutput, "fastTree.zip")) Then
                Console.WriteLine("***** YOU NEED TO RUN THE TRAINING PROJECT IN THE FIRST PLACE *****")
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

                System.IO.File.Copy(file,
                    Path.Combine(Path.Combine(assetsPath, "input"), Path.GetFileName(file)))
            Next file

        End Sub

        Public Shared Function GetAbsolutePath(relativePath As String) As String
            Dim fullPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
            Return fullPath
        End Function

    End Class

End Namespace