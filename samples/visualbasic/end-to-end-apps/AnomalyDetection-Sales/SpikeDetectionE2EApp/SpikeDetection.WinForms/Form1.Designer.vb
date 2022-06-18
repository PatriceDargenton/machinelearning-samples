Namespace SpikeDetection.WinForms
	Partial Public Class Form1
		''' <summary>
		''' Required designer variable.
		''' </summary>
		Private components As System.ComponentModel.IContainer = Nothing

		''' <summary>
		''' Clean up any resources being used.
		''' </summary>
		''' <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		Protected Overrides Sub Dispose(disposing As Boolean)
			If disposing AndAlso (components IsNot Nothing) Then
				components.Dispose()
			End If
			MyBase.Dispose(disposing)
		End Sub

		#Region "Windows Form Designer generated code"

		''' <summary>
		''' Required method for Designer support - do not modify
		''' the contents of this method with the code editor.
		''' </summary>
		Private Sub InitializeComponent()
            Me.components = New System.ComponentModel.Container()
            Dim ChartArea4 As System.Windows.Forms.DataVisualization.Charting.ChartArea = New System.Windows.Forms.DataVisualization.Charting.ChartArea()
            Dim Legend4 As System.Windows.Forms.DataVisualization.Charting.Legend = New System.Windows.Forms.DataVisualization.Charting.Legend()
            Dim LegendItem7 As System.Windows.Forms.DataVisualization.Charting.LegendItem = New System.Windows.Forms.DataVisualization.Charting.LegendItem()
            Dim LegendItem8 As System.Windows.Forms.DataVisualization.Charting.LegendItem = New System.Windows.Forms.DataVisualization.Charting.LegendItem()
            Dim Series7 As System.Windows.Forms.DataVisualization.Charting.Series = New System.Windows.Forms.DataVisualization.Charting.Series()
            Dim Series8 As System.Windows.Forms.DataVisualization.Charting.Series = New System.Windows.Forms.DataVisualization.Charting.Series()
            Me.debugInstructionsLabel = New System.Windows.Forms.Label()
            Me.button1 = New System.Windows.Forms.Button()
            Me.helloWorldLabel = New System.Windows.Forms.Label()
            Me.filePathTextbox = New System.Windows.Forms.TextBox()
            Me.dataGridView1 = New System.Windows.Forms.DataGridView()
            Me.button2 = New System.Windows.Forms.Button()
            Me.commaSeparatedRadio = New System.Windows.Forms.RadioButton()
            Me.tabSeparatedRadio = New System.Windows.Forms.RadioButton()
            Me.backgroundWorker1 = New System.ComponentModel.BackgroundWorker()
            Me.label1 = New System.Windows.Forms.Label()
            Me.label2 = New System.Windows.Forms.Label()
            Me.backgroundWorker2 = New System.ComponentModel.BackgroundWorker()
            Me.contextMenuStrip1 = New System.Windows.Forms.ContextMenuStrip(Me.components)
            Me.contextMenuStrip2 = New System.Windows.Forms.ContextMenuStrip(Me.components)
            Me.anomalyText = New System.Windows.Forms.RichTextBox()
            Me.label3 = New System.Windows.Forms.Label()
            Me.graph = New System.Windows.Forms.DataVisualization.Charting.Chart()
            Me.openFileExplorer = New System.Windows.Forms.OpenFileDialog()
            Me.panel1 = New System.Windows.Forms.Panel()
            Me.panel2 = New System.Windows.Forms.Panel()
            Me.changePointDet = New System.Windows.Forms.CheckBox()
            Me.spikeDet = New System.Windows.Forms.CheckBox()
            CType(Me.dataGridView1, System.ComponentModel.ISupportInitialize).BeginInit()
            CType(Me.graph, System.ComponentModel.ISupportInitialize).BeginInit()
            Me.panel1.SuspendLayout()
            Me.panel2.SuspendLayout()
            Me.SuspendLayout()
            '
            'debugInstructionsLabel
            '
            Me.debugInstructionsLabel.AutoSize = True
            Me.debugInstructionsLabel.Location = New System.Drawing.Point(8, 55)
            Me.debugInstructionsLabel.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
            Me.debugInstructionsLabel.Name = "debugInstructionsLabel"
            Me.debugInstructionsLabel.Size = New System.Drawing.Size(77, 13)
            Me.debugInstructionsLabel.TabIndex = 1
            Me.debugInstructionsLabel.Text = "Data File Path:"
            '
            'button1
            '
            Me.button1.Location = New System.Drawing.Point(212, 68)
            Me.button1.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
            Me.button1.Name = "button1"
            Me.button1.Size = New System.Drawing.Size(60, 24)
            Me.button1.TabIndex = 2
            Me.button1.Text = "Find"
            Me.button1.UseVisualStyleBackColor = True
            '
            'helloWorldLabel
            '
            Me.helloWorldLabel.AutoSize = True
            Me.helloWorldLabel.Font = New System.Drawing.Font("Microsoft Sans Serif", 16.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
            Me.helloWorldLabel.Location = New System.Drawing.Point(6, 13)
            Me.helloWorldLabel.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
            Me.helloWorldLabel.Name = "helloWorldLabel"
            Me.helloWorldLabel.Size = New System.Drawing.Size(196, 26)
            Me.helloWorldLabel.TabIndex = 3
            Me.helloWorldLabel.Text = "Anomaly Detection"
            '
            'filePathTextbox
            '
            Me.filePathTextbox.Location = New System.Drawing.Point(11, 72)
            Me.filePathTextbox.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
            Me.filePathTextbox.Name = "filePathTextbox"
            Me.filePathTextbox.Size = New System.Drawing.Size(199, 20)
            Me.filePathTextbox.TabIndex = 4
            '
            'dataGridView1
            '
            Me.dataGridView1.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
            Me.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
            Me.dataGridView1.Location = New System.Drawing.Point(9, 202)
            Me.dataGridView1.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
            Me.dataGridView1.Name = "dataGridView1"
            Me.dataGridView1.RowTemplate.Height = 33
            Me.dataGridView1.Size = New System.Drawing.Size(263, 422)
            Me.dataGridView1.TabIndex = 5
            '
            'button2
            '
            Me.button2.Location = New System.Drawing.Point(7, 161)
            Me.button2.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
            Me.button2.Name = "button2"
            Me.button2.Size = New System.Drawing.Size(272, 24)
            Me.button2.TabIndex = 6
            Me.button2.Text = "Go"
            Me.button2.UseVisualStyleBackColor = True
            '
            'commaSeparatedRadio
            '
            Me.commaSeparatedRadio.AutoSize = True
            Me.commaSeparatedRadio.Checked = True
            Me.commaSeparatedRadio.Location = New System.Drawing.Point(6, 10)
            Me.commaSeparatedRadio.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
            Me.commaSeparatedRadio.Name = "commaSeparatedRadio"
            Me.commaSeparatedRadio.Size = New System.Drawing.Size(112, 17)
            Me.commaSeparatedRadio.TabIndex = 9
            Me.commaSeparatedRadio.TabStop = True
            Me.commaSeparatedRadio.Text = "Comma Separated"
            Me.commaSeparatedRadio.UseVisualStyleBackColor = True
            '
            'tabSeparatedRadio
            '
            Me.tabSeparatedRadio.AutoSize = True
            Me.tabSeparatedRadio.Location = New System.Drawing.Point(6, 34)
            Me.tabSeparatedRadio.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
            Me.tabSeparatedRadio.Name = "tabSeparatedRadio"
            Me.tabSeparatedRadio.Size = New System.Drawing.Size(96, 17)
            Me.tabSeparatedRadio.TabIndex = 10
            Me.tabSeparatedRadio.Text = "Tab Separated"
            Me.tabSeparatedRadio.UseVisualStyleBackColor = True
            '
            'label1
            '
            Me.label1.AutoSize = True
            Me.label1.Location = New System.Drawing.Point(6, 187)
            Me.label1.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
            Me.label1.Name = "label1"
            Me.label1.Size = New System.Drawing.Size(100, 13)
            Me.label1.TabIndex = 11
            Me.label1.Text = "Data View Preview:"
            '
            'label2
            '
            Me.label2.AutoSize = True
            Me.label2.Location = New System.Drawing.Point(300, 68)
            Me.label2.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
            Me.label2.Name = "label2"
            Me.label2.Size = New System.Drawing.Size(105, 13)
            Me.label2.TabIndex = 13
            Me.label2.Text = "Anomalies Detected:"
            '
            'contextMenuStrip1
            '
            Me.contextMenuStrip1.ImageScalingSize = New System.Drawing.Size(32, 32)
            Me.contextMenuStrip1.Name = "contextMenuStrip1"
            Me.contextMenuStrip1.Size = New System.Drawing.Size(61, 4)
            '
            'contextMenuStrip2
            '
            Me.contextMenuStrip2.ImageScalingSize = New System.Drawing.Size(32, 32)
            Me.contextMenuStrip2.Name = "contextMenuStrip2"
            Me.contextMenuStrip2.Size = New System.Drawing.Size(61, 4)
            '
            'anomalyText
            '
            Me.anomalyText.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.anomalyText.ForeColor = System.Drawing.Color.Black
            Me.anomalyText.Location = New System.Drawing.Point(302, 87)
            Me.anomalyText.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
            Me.anomalyText.Name = "anomalyText"
            Me.anomalyText.Size = New System.Drawing.Size(599, 92)
            Me.anomalyText.TabIndex = 17
            Me.anomalyText.Text = ""
            '
            'label3
            '
            Me.label3.AutoSize = True
            Me.label3.Location = New System.Drawing.Point(300, 187)
            Me.label3.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
            Me.label3.Name = "label3"
            Me.label3.Size = New System.Drawing.Size(39, 13)
            Me.label3.TabIndex = 18
            Me.label3.Text = "Graph:"
            '
            'graph
            '
            Me.graph.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            ChartArea4.AxisX.Title = "Month"
            ChartArea4.AxisY.Maximum = 700.0R
            ChartArea4.AxisY.Minimum = 0R
            ChartArea4.AxisY.Title = "Sales"
            ChartArea4.Name = "ChartArea1"
            Me.graph.ChartAreas.Add(ChartArea4)
            LegendItem7.ImageStyle = System.Windows.Forms.DataVisualization.Charting.LegendImageStyle.Marker
            LegendItem7.MarkerBorderColor = System.Drawing.Color.DarkRed
            LegendItem7.MarkerBorderWidth = 0
            LegendItem7.MarkerColor = System.Drawing.Color.DarkRed
            LegendItem7.MarkerSize = 15
            LegendItem7.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Star4
            LegendItem7.Name = "Spike"
            LegendItem8.ImageStyle = System.Windows.Forms.DataVisualization.Charting.LegendImageStyle.Marker
            LegendItem8.MarkerBorderColor = System.Drawing.Color.DarkBlue
            LegendItem8.MarkerBorderWidth = 0
            LegendItem8.MarkerColor = System.Drawing.Color.DarkBlue
            LegendItem8.MarkerSize = 15
            LegendItem8.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Star4
            LegendItem8.Name = "Change point"
            Legend4.CustomItems.Add(LegendItem7)
            Legend4.CustomItems.Add(LegendItem8)
            Legend4.Enabled = False
            Legend4.Name = "Legend1"
            Me.graph.Legends.Add(Legend4)
            Me.graph.Location = New System.Drawing.Point(302, 202)
            Me.graph.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
            Me.graph.Name = "graph"
            Me.graph.Palette = System.Windows.Forms.DataVisualization.Charting.ChartColorPalette.None
            Series7.ChartArea = "ChartArea1"
            Series7.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line
            Series7.Color = System.Drawing.Color.DimGray
            Series7.IsVisibleInLegend = False
            Series7.IsXValueIndexed = True
            Series7.Legend = "Legend1"
            Series7.Name = "Series1"
            Series8.ChartArea = "ChartArea1"
            Series8.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Point
            Series8.IsVisibleInLegend = False
            Series8.Legend = "Legend1"
            Series8.Name = "Series2"
            Me.graph.Series.Add(Series7)
            Me.graph.Series.Add(Series8)
            Me.graph.Size = New System.Drawing.Size(597, 422)
            Me.graph.TabIndex = 19
            Me.graph.Text = "graph"
            '
            'panel1
            '
            Me.panel1.Controls.Add(Me.commaSeparatedRadio)
            Me.panel1.Controls.Add(Me.tabSeparatedRadio)
            Me.panel1.Location = New System.Drawing.Point(7, 95)
            Me.panel1.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
            Me.panel1.Name = "panel1"
            Me.panel1.Size = New System.Drawing.Size(120, 60)
            Me.panel1.TabIndex = 22
            '
            'panel2
            '
            Me.panel2.Controls.Add(Me.changePointDet)
            Me.panel2.Controls.Add(Me.spikeDet)
            Me.panel2.Location = New System.Drawing.Point(130, 95)
            Me.panel2.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
            Me.panel2.Name = "panel2"
            Me.panel2.Size = New System.Drawing.Size(148, 60)
            Me.panel2.TabIndex = 23
            '
            'changePointDet
            '
            Me.changePointDet.AutoSize = True
            Me.changePointDet.Location = New System.Drawing.Point(12, 34)
            Me.changePointDet.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
            Me.changePointDet.Name = "changePointDet"
            Me.changePointDet.Size = New System.Drawing.Size(139, 17)
            Me.changePointDet.TabIndex = 25
            Me.changePointDet.Text = "Change Point Detection"
            Me.changePointDet.UseVisualStyleBackColor = True
            '
            'spikeDet
            '
            Me.spikeDet.AutoSize = True
            Me.spikeDet.Location = New System.Drawing.Point(12, 10)
            Me.spikeDet.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
            Me.spikeDet.Name = "spikeDet"
            Me.spikeDet.Size = New System.Drawing.Size(102, 17)
            Me.spikeDet.TabIndex = 24
            Me.spikeDet.Text = "Spike Detection"
            Me.spikeDet.UseVisualStyleBackColor = True
            '
            'Form1
            '
            Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.AutoSize = True
            Me.ClientSize = New System.Drawing.Size(909, 632)
            Me.Controls.Add(Me.panel2)
            Me.Controls.Add(Me.panel1)
            Me.Controls.Add(Me.graph)
            Me.Controls.Add(Me.label3)
            Me.Controls.Add(Me.anomalyText)
            Me.Controls.Add(Me.label2)
            Me.Controls.Add(Me.label1)
            Me.Controls.Add(Me.button2)
            Me.Controls.Add(Me.dataGridView1)
            Me.Controls.Add(Me.filePathTextbox)
            Me.Controls.Add(Me.helloWorldLabel)
            Me.Controls.Add(Me.button1)
            Me.Controls.Add(Me.debugInstructionsLabel)
            Me.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
            Me.Name = "Form1"
            Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
            Me.Text = "Anomaly Detection"
            CType(Me.dataGridView1, System.ComponentModel.ISupportInitialize).EndInit()
            CType(Me.graph, System.ComponentModel.ISupportInitialize).EndInit()
            Me.panel1.ResumeLayout(False)
            Me.panel1.PerformLayout()
            Me.panel2.ResumeLayout(False)
            Me.panel2.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub

#End Region
        Private debugInstructionsLabel As System.Windows.Forms.Label
		Private WithEvents button1 As System.Windows.Forms.Button
		Private helloWorldLabel As System.Windows.Forms.Label
		Private filePathTextbox As System.Windows.Forms.TextBox
		Private dataGridView1 As System.Windows.Forms.DataGridView
		Private WithEvents button2 As System.Windows.Forms.Button
        Private commaSeparatedRadio As System.Windows.Forms.RadioButton
        Private tabSeparatedRadio As System.Windows.Forms.RadioButton
		Private backgroundWorker1 As System.ComponentModel.BackgroundWorker
		Private label1 As System.Windows.Forms.Label
		Private label2 As System.Windows.Forms.Label
		Private backgroundWorker2 As System.ComponentModel.BackgroundWorker
		Private contextMenuStrip1 As System.Windows.Forms.ContextMenuStrip
		Private contextMenuStrip2 As System.Windows.Forms.ContextMenuStrip
		Private anomalyText As System.Windows.Forms.RichTextBox
		Private label3 As System.Windows.Forms.Label
		Private graph As System.Windows.Forms.DataVisualization.Charting.Chart
		Private openFileExplorer As System.Windows.Forms.OpenFileDialog
		Private panel1 As System.Windows.Forms.Panel
		Private panel2 As System.Windows.Forms.Panel
		Private spikeDet As System.Windows.Forms.CheckBox
		Private changePointDet As System.Windows.Forms.CheckBox
	End Class
End Namespace

