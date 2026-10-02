namespace SAi_KR
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            pbSimulation = new PictureBox();
            dgvTelemetry = new DataGridView();
            gbControl = new GroupBox();
            btnStartPause = new Button();
            btnTrain = new Button();
            nudFlow = new NumericUpDown();
            lblFlow = new Label();
            tbSpeed = new TrackBar();
            lblSpeed = new Label();
            rbAI = new RadioButton();
            rbFixedTime = new RadioButton();
            btnReset = new Button();
            gbStats = new GroupBox();
            lblTrainStatus = new Label();
            lblPassed = new Label();
            lblTotalQueue = new Label();
            lblAvgWait = new Label();
            tmrSim = new System.Windows.Forms.Timer(components);
            ((System.ComponentModel.ISupportInitialize)pbSimulation).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvTelemetry).BeginInit();
            gbControl.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudFlow).BeginInit();
            ((System.ComponentModel.ISupportInitialize)tbSpeed).BeginInit();
            gbStats.SuspendLayout();
            SuspendLayout();
            // 
            // pbSimulation
            // 
            pbSimulation.BackColor = Color.FromArgb(30, 30, 30);
            pbSimulation.BorderStyle = BorderStyle.FixedSingle;
            pbSimulation.Location = new Point(24, 24);
            pbSimulation.Name = "pbSimulation";
            pbSimulation.Size = new Size(1720, 940);
            pbSimulation.TabIndex = 0;
            pbSimulation.TabStop = false;
            // 
            // dgvTelemetry
            // 
            dgvTelemetry.AllowUserToAddRows = false;
            dgvTelemetry.BackgroundColor = Color.White;
            dgvTelemetry.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvTelemetry.Location = new Point(24, 980);
            dgvTelemetry.Name = "dgvTelemetry";
            dgvTelemetry.ReadOnly = true;
            dgvTelemetry.RowHeadersVisible = false;
            dgvTelemetry.RowHeadersWidth = 82;
            dgvTelemetry.Size = new Size(1720, 377);
            dgvTelemetry.TabIndex = 1;
            // 
            // gbControl
            // 
            gbControl.Controls.Add(btnStartPause);
            gbControl.Controls.Add(btnTrain);
            gbControl.Controls.Add(nudFlow);
            gbControl.Controls.Add(lblFlow);
            gbControl.Controls.Add(tbSpeed);
            gbControl.Controls.Add(lblSpeed);
            gbControl.Controls.Add(rbAI);
            gbControl.Controls.Add(rbFixedTime);
            gbControl.Controls.Add(btnReset);
            gbControl.Location = new Point(1770, 24);
            gbControl.Name = "gbControl";
            gbControl.Size = new Size(712, 710);
            gbControl.TabIndex = 2;
            gbControl.TabStop = false;
            gbControl.Text = "Управление симуляцией";
            // 
            // btnStartPause
            // 
            btnStartPause.Font = new Font("Segoe UI", 10.875F, FontStyle.Bold, GraphicsUnit.Point, 204);
            btnStartPause.Location = new Point(30, 50);
            btnStartPause.Name = "btnStartPause";
            btnStartPause.Size = new Size(310, 70);
            btnStartPause.TabIndex = 10;
            btnStartPause.Text = "Старт";
            btnStartPause.UseVisualStyleBackColor = true;
            // 
            // btnTrain
            // 
            btnTrain.Location = new Point(30, 640);
            btnTrain.Name = "btnTrain";
            btnTrain.Size = new Size(650, 55);
            btnTrain.TabIndex = 9;
            btnTrain.Text = "Обучить агентов (Генетический алгоритм)";
            btnTrain.UseVisualStyleBackColor = true;
            // 
            // nudFlow
            // 
            nudFlow.Increment = new decimal(new int[] { 100, 0, 0, 0 });
            nudFlow.Location = new Point(40, 305);
            nudFlow.Maximum = new decimal(new int[] { 3000, 0, 0, 0 });
            nudFlow.Minimum = new decimal(new int[] { 300, 0, 0, 0 });
            nudFlow.Name = "nudFlow";
            nudFlow.Size = new Size(240, 39);
            nudFlow.TabIndex = 8;
            nudFlow.Value = new decimal(new int[] { 1200, 0, 0, 0 });
            // 
            // lblFlow
            // 
            lblFlow.AutoSize = true;
            lblFlow.Location = new Point(36, 265);
            lblFlow.Name = "lblFlow";
            lblFlow.Size = new Size(418, 32);
            lblFlow.TabIndex = 7;
            lblFlow.Text = "Интенсивность по Северной (авт/ч):";
            // 
            // tbSpeed
            // 
            tbSpeed.Location = new Point(30, 185);
            tbSpeed.Minimum = 1;
            tbSpeed.Name = "tbSpeed";
            tbSpeed.Size = new Size(650, 65);
            tbSpeed.TabIndex = 6;
            tbSpeed.Value = 1;
            // 
            // lblSpeed
            // 
            lblSpeed.AutoSize = true;
            lblSpeed.Location = new Point(36, 145);
            lblSpeed.Name = "lblSpeed";
            lblSpeed.Size = new Size(280, 32);
            lblSpeed.TabIndex = 3;
            lblSpeed.Text = "Скорость симуляции: x1";
            // 
            // rbAI
            // 
            rbAI.AutoSize = true;
            rbAI.Location = new Point(40, 400);
            rbAI.Name = "rbAI";
            rbAI.Size = new Size(447, 36);
            rbAI.TabIndex = 5;
            rbAI.TabStop = true;
            rbAI.Text = "Мультиагентная ИНС + База правил";
            rbAI.UseVisualStyleBackColor = true;
            // 
            // rbFixedTime
            // 
            rbFixedTime.AutoSize = true;
            rbFixedTime.Checked = true;
            rbFixedTime.Location = new Point(40, 360);
            rbFixedTime.Name = "rbFixedTime";
            rbFixedTime.Size = new Size(363, 36);
            rbFixedTime.TabIndex = 4;
            rbFixedTime.TabStop = true;
            rbFixedTime.Text = "Фиксированный цикл (ГОСТ)";
            rbFixedTime.UseVisualStyleBackColor = true;
            // 
            // btnReset
            // 
            btnReset.Font = new Font("Segoe UI", 10.875F, FontStyle.Regular, GraphicsUnit.Point, 204);
            btnReset.Location = new Point(375, 50);
            btnReset.Name = "btnReset";
            btnReset.Size = new Size(310, 70);
            btnReset.TabIndex = 3;
            btnReset.Text = "Сброс";
            btnReset.UseVisualStyleBackColor = true;
            // 
            // gbStats
            // 
            gbStats.Controls.Add(lblTrainStatus);
            gbStats.Controls.Add(lblPassed);
            gbStats.Controls.Add(lblTotalQueue);
            gbStats.Controls.Add(lblAvgWait);
            gbStats.Location = new Point(1770, 750);
            gbStats.Name = "gbStats";
            gbStats.Size = new Size(712, 607);
            gbStats.TabIndex = 3;
            gbStats.TabStop = false;
            gbStats.Text = "Показатели эффективности";
            // 
            // lblTrainStatus
            // 
            lblTrainStatus.Font = new Font("Segoe UI", 16.875F, FontStyle.Regular, GraphicsUnit.Point, 204);
            lblTrainStatus.Location = new Point(40, 290);
            lblTrainStatus.Name = "lblTrainStatus";
            lblTrainStatus.Size = new Size(572, 326);
            lblTrainStatus.TabIndex = 3;
            lblTrainStatus.Text = "Статус ИИ: Исходные веса";
            // 
            // lblPassed
            // 
            lblPassed.AutoSize = true;
            lblPassed.Font = new Font("Segoe UI", 10.125F, FontStyle.Regular, GraphicsUnit.Point, 204);
            lblPassed.Location = new Point(40, 210);
            lblPassed.Name = "lblPassed";
            lblPassed.Size = new Size(287, 37);
            lblPassed.TabIndex = 2;
            lblPassed.Text = "Пропущено машин: 0";
            // 
            // lblTotalQueue
            // 
            lblTotalQueue.AutoSize = true;
            lblTotalQueue.Font = new Font("Segoe UI", 10.125F, FontStyle.Regular, GraphicsUnit.Point, 204);
            lblTotalQueue.Location = new Point(40, 140);
            lblTotalQueue.Name = "lblTotalQueue";
            lblTotalQueue.Size = new Size(350, 37);
            lblTotalQueue.TabIndex = 1;
            lblTotalQueue.Text = "Суммарная очередь: 0 авт.";
            // 
            // lblAvgWait
            // 
            lblAvgWait.AutoSize = true;
            lblAvgWait.Font = new Font("Segoe UI", 10.125F, FontStyle.Bold, GraphicsUnit.Point, 204);
            lblAvgWait.Location = new Point(40, 70);
            lblAvgWait.Name = "lblAvgWait";
            lblAvgWait.Size = new Size(356, 37);
            lblAvgWait.TabIndex = 0;
            lblAvgWait.Text = "Средняя задержка: 0.00 с";
            // 
            // tmrSim
            // 
            tmrSim.Interval = 40;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(2494, 1369);
            Controls.Add(gbStats);
            Controls.Add(gbControl);
            Controls.Add(dgvTelemetry);
            Controls.Add(pbSimulation);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximumSize = new Size(2520, 1440);
            MinimizeBox = false;
            MinimumSize = new Size(2520, 1440);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Система управления автотраффиком";
            ((System.ComponentModel.ISupportInitialize)pbSimulation).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvTelemetry).EndInit();
            gbControl.ResumeLayout(false);
            gbControl.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudFlow).EndInit();
            ((System.ComponentModel.ISupportInitialize)tbSpeed).EndInit();
            gbStats.ResumeLayout(false);
            gbStats.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private PictureBox pbSimulation;
        private DataGridView dgvTelemetry;
        private GroupBox gbControl;
        private Button btnReset;
        private TrackBar tbSpeed;
        private Label lblSpeed;
        private RadioButton rbAI;
        private RadioButton rbFixedTime;
        private Button btnTrain;
        private NumericUpDown nudFlow;
        private Label lblFlow;
        private GroupBox gbStats;
        private Label lblTotalQueue;
        private Label lblAvgWait;
        private Label lblTrainStatus;
        private Label lblPassed;
        private System.Windows.Forms.Timer tmrSim;
        private Button btnStartPause;
    }
}
