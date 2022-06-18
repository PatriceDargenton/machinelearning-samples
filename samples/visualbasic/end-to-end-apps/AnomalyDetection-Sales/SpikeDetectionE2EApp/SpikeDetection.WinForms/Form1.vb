﻿
Imports System.IO
Imports System.Windows.Forms.DataVisualization.Charting
Imports Microsoft.ML
Imports Microsoft.WindowsAPICodePack.Dialogs ' CommonOpenFileDialog

Namespace SpikeDetection.WinForms

    Partial Public Class Form1 : Inherits Form

        Private dataTable As DataTable = Nothing
        Private filePath As String = ""
        Private tup As Tuple(Of String, String) = Nothing
        Private dict As New Dictionary(Of Integer, Tuple(Of String, String))
        Private Shared BaseModelsRelativePath As String = "../../../MLModels"
        Private Shared ModelRelativePath1 As String = $"{BaseModelsRelativePath}/ProductSalesSpikeModel.zip"
        Private Shared ModelRelativePath2 As String = $"{BaseModelsRelativePath}/ProductSalesChangePointModel.zip"

        Private Shared spikeModelPath As String = GetAbsolutePath(ModelRelativePath1)
        Private Shared changePointModelPath As String = GetAbsolutePath(ModelRelativePath2)

        Public Sub New()

            InitializeComponent()

            'filePathTextbox.Text = Path.Combine(Application.StartupPath & "/../../../Data")
            filePathTextbox.Text = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(
                Application.StartupPath))) & "\Data"

        End Sub

        ' Find file button
        Private Sub button1_Click(sender As Object, e As EventArgs) Handles button1.Click

            ' Open File Explorer
            'Dim result As DialogResult = openFileExplorer.ShowDialog()
            Dim result As New CommonOpenFileDialog()
            result.InitialDirectory = filePathTextbox.Text

            ' Set text in file path textbox to file path from file explorer
            If result.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
                filePathTextbox.Text = result.FileName
            End If

        End Sub

        ' Go button
        Private Sub button2_Click(sender As Object, e As EventArgs) Handles button2.Click

            ' Set filepath from text from filepath textbox
            filePath = filePathTextbox.Text

            ' Checkk if file exists
            If File.Exists(filePath) Then

                dict = New Dictionary(Of Integer, Tuple(Of String, String))

                If filePath <> "" Then
                    ' Reset text in anomaly textbox
                    anomalyText.Text = ""

                    ' Display preview of dataset and graph
                    displayDataTableAndGraph()

                    ' Load a trained model to detect anomalies and then mark them on the graph
                    detectAnomalies()

                    ' If file path textbox is empty, prompt user to input file path
                Else
                    MessageBox.Show("Please input file path.")
                End If

            Else
                MessageBox.Show("File does not exist. Try finding the file again.")
            End If

        End Sub

        Private Sub displayDataTableAndGraph()

            dataTable = New DataTable
            Dim dataCol() As String = Nothing
            Dim a As Integer = 0
            Dim xAxis As String = ""
            Dim yAxis As String = ""

            Dim dataset() As String = File.ReadAllLines(filePath)
            dataCol = If(commaSeparatedRadio.Checked, dataset(0).Split(","c), dataset(0).Split(CChar(vbTab)))

            dataTable.Columns.Add(dataCol(0))
            dataTable.Columns.Add(dataCol(1))
            xAxis = dataCol(0)
            yAxis = dataCol(1)

            For Each line As String In dataset.Skip(1)
                ' Add next row of data
                dataCol = If(commaSeparatedRadio.Checked, line.Split(","c), line.Split(CChar(vbTab)))
                dataTable.Rows.Add(dataCol)

                tup = New Tuple(Of String, String)(dataCol(0), dataCol(1))
                dict.Add(a, tup)

                a += 1
            Next line

            ' Set data view preview source
            dataGridView1.DataSource = dataTable

            ' Update y axis min and max values
            Dim yMax As Double = Convert.ToDouble(dataTable.Compute("max([" & yAxis & "])", String.Empty))
            Dim yMin As Double = Convert.ToDouble(dataTable.Compute("min([" & yAxis & "])", String.Empty))

            ' Set graph source
            graph.DataSource = dataTable

            ' Set graph options
            graph.Series("Series1").ChartType = SeriesChartType.Line

            graph.Series("Series1").XValueMember = xAxis
            graph.Series("Series1").YValueMembers = yAxis

            graph.Legends("Legend1").Enabled = True

            graph.ChartAreas("ChartArea1").AxisX.MajorGrid.LineWidth = 0
            graph.ChartAreas("ChartArea1").AxisX.Interval = a \ 10

            graph.ChartAreas("ChartArea1").AxisY.Maximum = yMax
            graph.ChartAreas("ChartArea1").AxisY.Minimum = yMin
            graph.ChartAreas("ChartArea1").AxisY.Interval = yMax / 10

            graph.DataBind()

        End Sub

        Private Sub detectAnomalies()

            ' Create MLContext to be shared across the model creation workflow objects 
            Dim mlcontext = New MLContext

            ' STEP 1: Load the data into IDataView.
            Dim dataView As IDataView = mlcontext.Data.LoadFromTextFile(Of ProductSalesData)(
                path:=filePath, hasHeader:=True, separatorChar:=CChar(If(commaSeparatedRadio.Checked, ","c, vbTab)))

            ' Step 2: Load & use model
            ' Note -- The model is trained with the product-sales dataset in a separate console app
            ' (see AnomalyDetectionConsoleApp)            
            If spikeDet.Checked Then
                If File.Exists(spikeModelPath) Then
                    loadAndUseModel(mlcontext, dataView, spikeModelPath, "Spike", Color.DarkRed)
                Else
                    MessageBox.Show(
                        "Spike detection model does not exist. Please run model training console app first.")
                End If
            End If
            If changePointDet.Checked Then

                If File.Exists(changePointModelPath) Then
                    loadAndUseModel(mlcontext, dataView, changePointModelPath, "Change point", Color.DarkBlue)
                Else
                    MessageBox.Show(
                        "Change point detection model does not exist. Please run model training console app first.")
                End If
            End If

        End Sub

        Public Shared Function GetAbsolutePath(relativePath As String) As String
            Dim fullPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
            Return fullPath
        End Function

        Private Sub loadAndUseModel(mlcontext As MLContext, dataView As IDataView, modelPath As String,
                                    type As String, color As Color)

            Dim modelInputSchema As DataViewSchema = Nothing
            Dim tansformedModel As ITransformer = mlcontext.Model.Load(modelPath, modelInputSchema)

            ' Step 3: Apply data transformation to create predictions
            Dim transformedData As IDataView = tansformedModel.Transform(dataView)
            Dim predictions = mlcontext.Data.CreateEnumerable(Of ProductSalesPrediction)(
                transformedData, reuseRowObject:=False)

            ' Index key for dictionary (date, sales)
            Dim a As Integer = 0

            For Each prediction In predictions
                ' Check if anomaly is predicted (indicated by an alert)
                If prediction.Prediction(0) = 1 Then
                    ' Get the date (year-month) where spike is detected
                    Dim xAxisDate = dict(a).Item1
                    ' Get the number of sales which was detected to be a spike
                    Dim yAxisSalesNum = dict(a).Item2

                    ' Add anomaly points to graph
                    '  and set point/marker options
                    graph.Series("Series1").Points(a).SetValueXY(a, yAxisSalesNum)
                    graph.Series("Series1").Points(a).MarkerStyle = MarkerStyle.Star4
                    graph.Series("Series1").Points(a).MarkerSize = 10
                    graph.Series("Series1").Points(a).MarkerColor = color

                    ' Print out anomalies as text for user &
                    '  change color of text accordingly
                    Dim text As String = type & " detected in " & xAxisDate & ": " & yAxisSalesNum & vbLf
                    anomalyText.SelectionColor = color
                    anomalyText.AppendText(text)

                    ' Change row color in table where anomalies occur
                    Dim row As DataGridViewRow = dataGridView1.Rows(a)
                    row.DefaultCellStyle.BackColor = color
                    row.DefaultCellStyle.ForeColor = System.Drawing.Color.White
                End If
                a += 1
            Next prediction

        End Sub

    End Class

End Namespace