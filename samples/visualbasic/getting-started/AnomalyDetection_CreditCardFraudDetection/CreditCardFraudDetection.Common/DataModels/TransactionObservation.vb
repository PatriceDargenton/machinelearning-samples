﻿
Imports Microsoft.ML.Data

Namespace CreditCardFraudDetection.Common.DataModels

    Public Interface IModelEntity
        Sub PrintToConsole()
    End Interface

    Public Class TransactionObservation : Implements IModelEntity

        <LoadColumn(0)>
        Public Time As Single

        <LoadColumn(1)>
        Public V1 As Single

        <LoadColumn(2)>
        Public V2 As Single

        <LoadColumn(3)>
        Public V3 As Single

        <LoadColumn(4)>
        Public V4 As Single

        <LoadColumn(5)>
        Public V5 As Single

        <LoadColumn(6)>
        Public V6 As Single

        <LoadColumn(7)>
        Public V7 As Single

        <LoadColumn(8)>
        Public V8 As Single

        <LoadColumn(9)>
        Public V9 As Single

        <LoadColumn(10)>
        Public V10 As Single

        <LoadColumn(11)>
        Public V11 As Single

        <LoadColumn(12)>
        Public V12 As Single

        <LoadColumn(13)>
        Public V13 As Single

        <LoadColumn(14)>
        Public V14 As Single

        <LoadColumn(15)>
        Public V15 As Single

        <LoadColumn(16)>
        Public V16 As Single

        <LoadColumn(17)>
        Public V17 As Single

        <LoadColumn(18)>
        Public V18 As Single

        <LoadColumn(19)>
        Public V19 As Single

        <LoadColumn(20)>
        Public V20 As Single

        <LoadColumn(21)>
        Public V21 As Single

        <LoadColumn(22)>
        Public V22 As Single

        <LoadColumn(23)>
        Public V23 As Single

        <LoadColumn(24)>
        Public V24 As Single

        <LoadColumn(25)>
        Public V25 As Single

        <LoadColumn(26)>
        Public V26 As Single

        <LoadColumn(27)>
        Public V27 As Single

        <LoadColumn(28)>
        Public V28 As Single

        <LoadColumn(29)>
        Public Amount As Single

        <LoadColumn(30)>
        Public Label As Single

        Public Sub PrintToConsole() Implements IModelEntity.PrintToConsole
            Console.WriteLine($"Label: {Label}")
            Console.WriteLine($"Features: [V1] {V1} [V2] {V2} [V3] {V3} ... [V28] {V28} Amount: {Amount}")
        End Sub

    End Class

End Namespace