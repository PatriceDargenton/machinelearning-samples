
Imports System.IO ' Path
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace MLSamples

    <TestClass>
    Public Class UnitTestLegacy

        <TestMethod>
        Sub Test1ImageClassificationTrain()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "DeepLearning_ImageClassification_Training\" &
                "ImageClassification.Train\assets\"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim myCommonPath$ = "..\..\..\..\..\..\datasets"
            Dim myCommonFullPath$ = Path.GetFullPath(myCommonPath)
            Dim res = ImageClassification.Train.ImageClassificationTrain.
                Program.GetResult(myDataFullPath, myCommonFullPath)
            Assert.AreEqual(res, True)

        End Sub

        <TestMethod>
        Sub Test2ImageClassificationPredict()

            Dim myDataPath$ = "..\..\..\..\getting-started\" &
                "DeepLearning_ImageClassification_Training\" &
                "ImageClassification.Predict\assets\"
            Dim myDataFullPath$ = Path.GetFullPath(myDataPath)
            Dim myModelPath$ = "..\..\..\..\getting-started\" &
                "DeepLearning_ImageClassification_Training\" &
                "ImageClassification.Train\assets\"
            Dim myModelFullPath$ = Path.GetFullPath(myModelPath)
            Dim res = ImageClassification.Predict.ImageClassificationPredict.Program.
                GetResult(myDataFullPath, myModelFullPath)
            Assert.AreEqual(res, True)

        End Sub

    End Class

End Namespace

