
Imports System.IO ' Path
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace MLSamples

    <TestClass>
    Public Class UnitTestMain

        <TestMethod>
        Sub Test01IrisClustering()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "Clustering_Iris\IrisClustering\Data"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim myModelPath$ = "..\..\..\..\getting-started\" &
                "AdvancedExperiment_AutoML\AdvancedTaxiFarePrediction\MLModels"
            Dim myModelFullPath$ = Path.GetFullPath(myModelPath)
            Dim res = Clustering_Iris.Clustering_Iris.GetResult(myDataFullPath, myModelFullPath)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test02HeartDiseaseDetection()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "BinaryClassification_HeartDiseaseDetection\HeartDiseaseDetection\Data"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim myModelePath$ = "..\..\..\..\getting-started\" &
                "BinaryClassification_HeartDiseaseDetection\HeartDiseaseDetection\MLModels"
            Dim myModeleFullPath$ = Path.GetFullPath(myModelePath)
            Dim res = HeartDiseaseDetection.HeartDiseasePredictionConsoleApp.
                Program.GetResult(myDataFullPath, myModeleFullPath)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test03MovieRecommendation()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "MatrixFactorization_MovieRecommendation\Data\"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim myCommonPath$ = "..\..\..\..\..\..\datasets"
            Dim myCommonFullPath$ = Path.GetFullPath(myCommonPath)
            Dim res = MovieRecommendation.MovieRecommendation.
                Program.GetResult(myDataFullPath, myCommonFullPath)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test04PowerAnomalyDetection()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "AnomalyDetection_PowerMeterReadings\PowerAnomalyDetection\Data"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim myModelePath$ = "..\..\..\..\getting-started\" &
                "AnomalyDetection_PowerMeterReadings\PowerAnomalyDetection"
            Dim myModeleFullPath$ = Path.GetFullPath(myModelePath)
            Dim res = PowerAnomalyDetection.PowerAnomalyDetection.
                Program.GetResult(myDataFullPath, myModeleFullPath, isTest:=True)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test05SpikeDetection()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "AnomalyDetection_Sales\SpikeDetection\Data"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim res = SpikeDetection.SpikeDetection.Program.GetResult(myDataFullPath)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test06CustomerSegmentationTrain()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "Clustering_CustomerSegmentation\CustomerSegmentation.Train\assets"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim res = CustomerSegmentation.Train.CustomerSegmentation.Train.
                Program.GetResult(myDataFullPath)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test07CustomerSegmentationPredict()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "Clustering_CustomerSegmentation\CustomerSegmentation.Predict\assets"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim assetsRelativeTrainPath = Path.Combine(myDataPath,
                "..\..\CustomerSegmentation.Train\assets\outputs")
            Dim assetsTrainPath = Path.GetFullPath(assetsRelativeTrainPath)
            Dim res = CustomerSegmentation.Predict.CustomerSegmentation.Predict.
                Program.GetResult(myDataFullPath, assetsTrainPath, isTest:=True)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test08Xor()

            Dim myDataPath2XOR$ = "..\..\..\..\getting-started\" &
                "XOR\XOR\Data\xor2-repeat3.txt"
            Dim myDataPath3XOR$ = "..\..\..\..\getting-started\" &
                "XOR\XOR\Data\xor3.txt"
            Dim myDataFullPath2XOR$ = Path.GetFullPath(myDataPath2XOR)
            Dim myDataFullPath3XOR$ = Path.GetFullPath(myDataPath3XOR)
            Dim myModelePath$ = "..\..\..\..\getting-started\" &
                "XOR\XOR\MLModels"
            Dim myModeleFullPath$ = Path.GetFullPath(myModelePath)

            Dim res1 = XORApp.XORApp.GetResult1XOR(myModeleFullPath)
            Assert.AreEqual(res1, True)

            Dim res2 = XORApp.XORApp.GetResult2XOR(myDataFullPath2XOR, myModeleFullPath)
            Assert.AreEqual(res2, True)

            Dim res3 = XORApp.XORApp.GetResult3XOR(myDataFullPath3XOR, myModeleFullPath)
            Assert.AreEqual(res3, True)

        End Sub

        <TestMethod>
        Sub Test09BikeSharingDemand()

            Dim myModelePath$ = "..\..\..\..\getting-started\" &
                "Regression_BikeSharingDemand\BikeSharingDemand\MLModels"
            Dim myModeleFullPath$ = Path.GetFullPath(myModelePath)
            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "Regression_BikeSharingDemand\BikeSharingDemand\Data"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim myCommonPath$ = "..\..\..\..\..\..\datasets"
            Dim myCommonFullPath$ = Path.GetFullPath(myCommonPath)
            Dim res = BikeSharingDemandConsoleApp.BikeSharingDemand.
                Program.GetResult(myDataFullPath, myModeleFullPath, myCommonFullPath)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test10CreditCardFraudDetection()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "AnomalyDetection_CreditCardFraudDetection\" &
                "CreditCardFraudDetection.Trainer\assets"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim myCommonPath$ = "..\..\..\..\..\..\datasets"
            Dim myCommonFullPath$ = Path.GetFullPath(myCommonPath)
            Dim res = CreditCardFraudDetection.Trainer.CreditCardFraudDetection.Trainer.
                Program.GetResult(myDataFullPath, myCommonFullPath)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test11CreditCardFraudDetectionPrediction()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "AnomalyDetection_CreditCardFraudDetection\" &
                "CreditCardFraudDetection.Trainer\assets"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim myTrainOutputPath$ = Path.Combine(myDataFullPath, "output")
            Dim myTrainOutputFullPath$ = Path.GetFullPath(myTrainOutputPath)
            Dim res = CreditCardFraudDetection.Predictor.CreditCardFraudDetection.Predictor.
                Program.GetResult(myDataFullPath, myTrainOutputFullPath)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test12SpamDetection()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "BinaryClassification_SpamDetection\SpamDetectionConsoleApp\Data\"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim myCommonPath$ = "..\..\..\..\..\..\datasets"
            Dim myCommonFullPath$ = Path.GetFullPath(myCommonPath)
            Dim res = SpamDetectionConsoleApp.SpamDetectionConsoleApp.
                Program.GetResult(myDataFullPath, myCommonFullPath)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test13TaxiFarePrediction()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "Regression_TaxiFarePrediction\TaxiFarePrediction\Data"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim myCommonPath$ = "..\..\..\..\..\..\datasets"
            Dim myCommonFullPath$ = Path.GetFullPath(myCommonPath)
            Dim myModelPath$ = "..\..\..\..\getting-started\" &
                "Regression_TaxiFarePrediction\TaxiFarePrediction\MLModels"
            Dim myModelFullPath$ = Path.GetFullPath(myModelPath)
            Dim res = TaxiFarePrediction.Regression_TaxiFarePrediction.
                Program.GetResult(myDataFullPath, myCommonFullPath, myModelFullPath, isTest:=True)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test14AdvancedTaxiFareAutoML()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "AdvancedExperiment_AutoML\AdvancedTaxiFarePrediction\Data"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim myCommonPath$ = "..\..\..\..\..\..\datasets"
            Dim myCommonFullPath$ = Path.GetFullPath(myCommonPath)
            Dim myModelPath$ = "..\..\..\..\getting-started\" &
                "AdvancedExperiment_AutoML\AdvancedTaxiFarePrediction\MLModels"
            Dim myModelFullPath$ = Path.GetFullPath(myModelPath)
            Dim res = AdvancedTaxiFarePrediction.AdvancedTaxiFarePrediction.
                Program.GetResult(myDataFullPath, myCommonFullPath, myModelFullPath, isTest:=True)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test15ObjectDetection()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "DeepLearning_ObjectDetection_Onnx\ObjectDetectionConsoleApp\assets\"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim myCommonPath$ = "..\..\..\..\..\..\datasets"
            Dim myCommonFullPath$ = Path.GetFullPath(myCommonPath)
            Dim myCommonGraphsPath$ = "..\..\..\..\..\..\graphs"
            Dim myCommonGraphsFullPath$ = Path.GetFullPath(myCommonGraphsPath)
            Dim res = ObjectDetection.ObjectDetection.
                Program.GetResult(myDataFullPath, myCommonFullPath, myCommonGraphsFullPath)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test16ImageClassificationTrain()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "DeepLearning_ImageClassification_TensorFlow\" &
                "ImageClassification\assets\"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim myCommonPath$ = "..\..\..\..\..\..\datasets"
            Dim myCommonFullPath$ = Path.GetFullPath(myCommonPath)
            Dim myCommonGraphsPath$ = "..\..\..\..\..\..\graphs"
            Dim myCommonGraphsFullPath$ = Path.GetFullPath(myCommonGraphsPath)
            Dim res = DLTFImageClassification.Score.DLTFImageClassification.Program.
                GetResult(myDataFullPath, myCommonFullPath, myCommonGraphsFullPath)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test17MNIST2()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "MulticlassClassification_mnist\mnist\Data"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim myModelePath$ = "..\..\..\..\getting-started\" &
                "MulticlassClassification_mnist\mnist\MLModels"
            Dim myModeleFullPath$ = Path.GetFullPath(myModelePath)
            Dim res = mnist2.mnist2.Program.GetResult(myDataFullPath, myModeleFullPath, isTest:=True)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test18IrisMulticlassClassification()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "MulticlassClassification_Iris\IrisClassification\Data"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim myModelePath$ = "..\..\..\..\getting-started\" &
                "MulticlassClassification_Iris\IrisClassification\MLModels"
            Dim myModeleFullPath$ = Path.GetFullPath(myModelePath)
            Dim res = MulticlassClassification_Iris.MulticlassClassification_Iris.
                Program.GetResult(myDataFullPath, myModeleFullPath, isTest:=True)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test19BCCreditCardFraudDetection()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "BinaryClassification_CreditCardFraudDetection\" &
                "CreditCardFraudDetection.Trainer\assets"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim myCommonPath$ = "..\..\..\..\..\..\datasets"
            Dim myCommonFullPath$ = Path.GetFullPath(myCommonPath)
            Dim res = BCCreditCardFraudDetection.Trainer.BCCreditCardFraudDetection.Trainer.
                Program.GetResult(myDataFullPath, myCommonFullPath)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test20BCCreditCardFraudDetectionPrediction()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "BinaryClassification_CreditCardFraudDetection\" &
                "CreditCardFraudDetection.Trainer\assets"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim myTrainOutputPath$ = Path.Combine(myDataFullPath, "output")
            Dim myTrainOutputFullPath$ = Path.GetFullPath(myTrainOutputPath)
            Dim res = BCCreditCardFraudDetection.Predictor.BCCreditCardFraudDetection.Predictor.
                Program.GetResult(myDataFullPath, myTrainOutputFullPath)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test21ProductRecommender()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "MatrixFactorization_ProductRecommendation\ProductRecommender\Data"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim myCommonPath$ = "..\..\..\..\..\..\datasets"
            Dim myCommonFullPath$ = Path.GetFullPath(myCommonPath)
            Dim res = ProductRecommender.ProductRecommender.
                Program.GetResult(myDataFullPath, myCommonFullPath)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test22DatabaseIntegration()

            Dim res = DatabaseIntegration.DatabaseIntegration.Program.GetResult()
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test23SentimentAnalysis()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "BinaryClassification_SentimentAnalysis\SentimentAnalysis\Data\"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim myCommonPath$ = "..\..\..\..\..\..\datasets"
            Dim myCommonFullPath$ = Path.GetFullPath(myCommonPath)
            Dim res = SentimentAnalysisConsoleApp.SentimentAnalysisConsoleApp.
                Program.GetResult(myDataFullPath, myCommonFullPath)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test24XorAutoML()

            Dim myDataPath1XOR$ = "..\..\..\..\getting-started\" &
                "XOR\XOR\Data\xorAutoML-repeat10.csv"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath1XOR)
            Dim myModelePath$ = "..\..\..\..\getting-started\" &
                "XOR\XOR\MLModels"
            Dim myModeleFullPath$ = Path.GetFullPath(myModelePath)

            Dim res = XORApp.XORApp.GetResult1XORAutoML(myDataFullPath, myModeleFullPath)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test25ImageClassificationTrain()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "DeepLearning_TensorFlowEstimator\" &
                "ImageClassification.Train\assets\"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim myCommonPath$ = "..\..\..\..\..\..\datasets"
            Dim myCommonFullPath$ = Path.GetFullPath(myCommonPath)
            Dim myCommonGraphsPath$ = "..\..\..\..\..\..\graphs"
            Dim myCommonGraphsFullPath$ = Path.GetFullPath(myCommonGraphsPath)
            Dim res = TFFEImageClassification.Train.TFFEImageClassification.Train.
                Program.GetResult(myDataFullPath, myCommonFullPath, myCommonGraphsFullPath)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test26ImageClassificationPredict()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "DeepLearning_TensorFlowEstimator\" &
                "ImageClassification.Predict\assets\"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim myModelePath$ = "..\..\..\..\getting-started\" &
                "DeepLearning_TensorFlowEstimator\" &
                "ImageClassification.Train\assets\"
            Dim myModeleFullPath$ = Path.GetFullPath(myModelePath)
            Dim res = TFFEImageClassification.Predict.TFFEImageClassification.Predict.
                Program.GetResult(myDataFullPath, myModeleFullPath)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test27DatabaseLoader()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "DatabaseLoader\DatabaseLoaderConsoleApp\SqlLocalDb"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim myCommonPath$ = "..\..\..\..\..\..\datasets"
            Dim myCommonFullPath$ = Path.GetFullPath(myCommonPath)
            Dim res = DatabaseLoaderConsoleApp.DatabaseLoaderConsoleApp.
                Program.GetResult(myDataFullPath, myCommonFullPath)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test28LargeDatasets()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "LargeDatasets\LargeDatasets\Data\OriginalUrlData\"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim myCommonPath$ = "..\..\..\..\..\..\datasets"
            Dim myCommonFullPath$ = Path.GetFullPath(myCommonPath)
            Dim res = LargeDatasets.LargeDatasets.
                Program.GetResult(myDataFullPath, myCommonFullPath)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test29MNISTAutoML()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "MulticlassClassification_AutoML\MNIST\Data"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim myModelePath$ = "..\..\..\..\getting-started\" &
                "MulticlassClassification_AutoML\MNIST\MLModels"
            Dim myModeleFullPath$ = Path.GetFullPath(myModelePath)
            Dim res = MNIST.MNIST.Program.GetResult(myDataFullPath, myModeleFullPath, isTest:=True)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test30SentimentAnalysisAutoML()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "BinaryClassification_AutoML\SentimentAnalysis\Data\"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim myModelPath$ = "..\..\..\..\getting-started\" &
                "BinaryClassification_AutoML\SentimentAnalysis\MLModels\"
            Dim myModelFullPath$ = Path.GetFullPath(myModelPath)
            Dim res = SentimentAnalysisAutoML.SentimentAnalysisAutoML.
                Program.GetResult(myDataFullPath, myModelFullPath, isTest:=True)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test31TaxiFarePredictionAutoML()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "Regression_AutoML\TaxiFarePrediction\Data"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim myCommonPath$ = "..\..\..\..\..\..\datasets"
            Dim myCommonFullPath$ = Path.GetFullPath(myCommonPath)
            Dim myModelePath$ = "..\..\..\..\getting-started\" &
                "Regression_AutoML\TaxiFarePrediction\MLModels"
            Dim myModeleFullPath$ = Path.GetFullPath(myModelePath)
            Dim res = TaxiFarePrediction2.TaxiFarePrediction2.
                Program.GetResult(myDataFullPath, myCommonFullPath, myModeleFullPath, isTest:=True)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test32WebRanking()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "Ranking_Web\WebRanking\Assets\Input"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim myCommonPath$ = "..\..\..\..\..\..\datasets"
            Dim myCommonFullPath$ = Path.GetFullPath(myCommonPath)
            'Dim myModelePath$ = "..\..\..\..\getting-started\" &
            '    "Ranking_Web\WebRanking\MLModels"
            Dim myModelePath$ = "..\..\..\..\getting-started\" &
                "Ranking_Web\WebRanking\Assets\Output"
            Dim myModeleFullPath$ = Path.GetFullPath(myModelePath)
            Dim res = WebRanking.WebRanking.
                Program.GetResult(myDataFullPath, myCommonFullPath, myModeleFullPath, isTest:=True)
            Assert.AreEqual(res, True)

        End Sub

    End Class

End Namespace

