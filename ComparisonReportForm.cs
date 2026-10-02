using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SAi_KR
{
    public class ComparisonReportForm : Form
    {
        private readonly ComparisonReport _report;
        private DataGridView _dgv = null!;
        private Panel _chartPanel = null!;
        private TextBox _txtConclusion = null!;
        private Button _btnCopy = null!;
        private Button _btnClose = null!;

        public ComparisonReportForm(ComparisonReport report)
        {
            _report = report;
            SetupUI();
            PopulateData();
        }

        private void SetupUI()
        {
            Text = "Отчет сравнительного анализа: ГОСТ Р 52289 и мультиагентное регулирование";
            Size = new Size(1600, 1020);
            MinimumSize = new Size(1100, 750);
            MaximumSize = new Size(2400, 1350);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(28, 32, 38);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // 1. Верхний заголовок
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(20, 24, 30),
                Padding = new Padding(20, 10, 20, 10)
            };
            var lblTitle = new Label
            {
                Text = "СРАВНИТЕЛЬНЫЙ АНАЛИЗ ЭФФЕКТИВНОСТИ СВЕТОФОРНОГО РЕГУЛИРОВАНИЯ",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 220, 130),
                AutoSize = true,
                Location = new Point(20, 15)
            };
            var lblSubtitle = new Label
            {
                Text = string.Format("Интенсивность: ул. Северная {0:F0} авт/ч, ул. Красная {1:F0} авт/ч | Время моделирования: {2:F0} с",
                    _report.FlowRateSevernaya, _report.FlowRateKrasnaya, _report.DurationSeconds),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(170, 180, 190),
                AutoSize = true,
                Location = new Point(20, 45)
            };
            headerPanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(lblSubtitle);
            Controls.Add(headerPanel);

            // 2. Нижняя панель с кнопками
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(20, 24, 30),
                Padding = new Padding(20, 10, 20, 10)
            };
            _btnCopy = new Button
            {
                Text = "Копировать отчет в буфер",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(260, 40),
                Location = new Point(20, 10),
                Cursor = Cursors.Hand
            };
            _btnCopy.FlatAppearance.BorderSize = 0;
            _btnCopy.Click += (s, e) =>
            {
                Clipboard.SetText(ComparisonRunner.GenerateReportPlainText(_report));
                _btnCopy.Text = "Отчет скопирован в буфер";
                _btnCopy.BackColor = Color.FromArgb(40, 167, 69);
            };

            _btnClose = new Button
            {
                Text = "Закрыть",
                Font = new Font("Segoe UI", 10f),
                BackColor = Color.FromArgb(60, 65, 75),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(130, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(1430, 10),
                Cursor = Cursors.Hand
            };
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.Click += (s, e) => Close();
            bottomPanel.Resize += (s, e) =>
            {
                _btnClose.Location = new Point(bottomPanel.ClientSize.Width - 150, 10);
            };

            bottomPanel.Controls.Add(_btnCopy);
            bottomPanel.Controls.Add(_btnClose);
            Controls.Add(bottomPanel);

            // 3. Основная область (Таблица + График + Вывод)
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(15),
                BackColor = Color.FromArgb(28, 32, 38)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54f));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40f));

            // Таблица показателей
            _dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(35, 40, 48),
                ForeColor = Color.White,
                GridColor = Color.FromArgb(55, 62, 74),
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _dgv.RowTemplate.Height = 34;
            _dgv.DefaultCellStyle.BackColor = Color.FromArgb(35, 40, 48);
            _dgv.DefaultCellStyle.ForeColor = Color.White;
            _dgv.DefaultCellStyle.Font = new Font("Segoe UI", 10f);
            _dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(50, 70, 95);
            _dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            _dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(22, 26, 32);
            _dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(0, 220, 130);
            _dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            _dgv.ColumnHeadersHeight = 38;
            _dgv.EnableHeadersVisualStyles = false;

            _dgv.Columns.Add("Param", "Показатель эффективности");
            _dgv.Columns.Add("Fixed", "ГОСТ Р 52289 (Фикс.)");
            _dgv.Columns.Add("AI", "Мультиагентный ИИ");
            _dgv.Columns.Add("Delta", "Эффект ИИ");
            _dgv.Columns[0].FillWeight = 42;
            _dgv.Columns[1].FillWeight = 20;
            _dgv.Columns[2].FillWeight = 20;
            _dgv.Columns[3].FillWeight = 18;

            mainLayout.Controls.Add(_dgv, 0, 0);

            // Панель графика (Bar Chart)
            _chartPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(35, 40, 48),
                Margin = new Padding(10, 0, 0, 0)
            };
            _chartPanel.Paint += ChartPanel_Paint;
            mainLayout.Controls.Add(_chartPanel, 1, 0);

            // Текстовое поле научного вывода
            _txtConclusion = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(22, 26, 32),
                ForeColor = Color.FromArgb(230, 235, 240),
                Font = new Font("Consolas", 10f),
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 10, 0, 0)
            };
            mainLayout.SetColumnSpan(_txtConclusion, 2);
            mainLayout.Controls.Add(_txtConclusion, 0, 1);

            Controls.Add(mainLayout);
        }

        private void PopulateData()
        {
            void AddRow(string param, string fixedVal, string aiVal, string delta, bool isPositive)
            {
                int idx = _dgv.Rows.Add(param, fixedVal, aiVal, delta);
                var row = _dgv.Rows[idx];
                row.Cells[3].Style.ForeColor = isPositive ? Color.FromArgb(0, 240, 120) : Color.FromArgb(255, 120, 120);
                row.Cells[3].Style.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            }

            string FormatReduction(double fixedVal, double aiVal, out bool isPositive)
            {
                double diff = fixedVal - aiVal;
                double pct = fixedVal > 0 ? (diff / fixedVal) * 100.0 : 0.0;
                if (pct >= 0)
                {
                    isPositive = true;
                    return $"-{pct:F1} %";
                }
                else
                {
                    isPositive = false;
                    return $"+{Math.Abs(pct):F1} %";
                }
            }

            string FormatIncrease(double fixedVal, double aiVal, out bool isPositive)
            {
                double diff = aiVal - fixedVal;
                double pct = fixedVal > 0 ? (diff / fixedVal) * 100.0 : 0.0;
                if (pct >= 0)
                {
                    isPositive = true;
                    return $"+{pct:F1} %";
                }
                else
                {
                    isPositive = false;
                    return $"-{Math.Abs(pct):F1} %";
                }
            }

            bool pos;
            AddRow("Средняя задержка на ТС", $"{_report.FixedAvgWait:F2} с", $"{_report.AIAvgWait:F2} с", FormatReduction(_report.FixedAvgWait, _report.AIAvgWait, out pos), pos);
            AddRow("Обслужено автомобилей", $"{_report.FixedTotalPassed} авт.", $"{_report.AITotalPassed} авт.", FormatIncrease(_report.FixedTotalPassed, _report.AITotalPassed, out pos), pos);
            AddRow("Средняя очередь", $"{_report.FixedAvgQueue:F1} авт.", $"{_report.AIAvgQueue:F1} авт.", FormatReduction(_report.FixedAvgQueue, _report.AIAvgQueue, out pos), pos);
            int peakDiff = _report.AIMaxQueue - _report.FixedMaxQueue;
            AddRow("Максимальная (пиковая) очередь", $"{_report.FixedMaxQueue} авт.", $"{_report.AIMaxQueue} авт.", $"{peakDiff:+#;-#;0} авт.", peakDiff <= 0);
            AddRow("Число остановок перед светофорами", $"{_report.FixedTotalStops} раз", $"{_report.AITotalStops} раз", FormatReduction(_report.FixedTotalStops, _report.AITotalStops, out pos), pos);
            AddRow("Безостановочный проезд («Зел.волна»)", $"{_report.FixedGreenWavePct:F1} %", $"{_report.AIGreenWavePct:F1} %", FormatIncrease(_report.FixedGreenWavePct, _report.AIGreenWavePct, out pos), pos);
            AddRow("  • Задержка: ул. Октябрьская", $"{_report.FixedDelayOkt:F2} с", $"{_report.AIDelayOkt:F2} с", FormatReduction(_report.FixedDelayOkt, _report.AIDelayOkt, out pos), pos);
            AddRow("  • Задержка: ул. Рашпилевская", $"{_report.FixedDelayRash:F2} с", $"{_report.AIDelayRash:F2} с", FormatReduction(_report.FixedDelayRash, _report.AIDelayRash, out pos), pos);
            AddRow("  • Задержка: ул. Красная", $"{_report.FixedDelayKras:F2} с", $"{_report.AIDelayKras:F2} с", FormatReduction(_report.FixedDelayKras, _report.AIDelayKras, out pos), pos);

            _txtConclusion.Text = ComparisonRunner.GenerateReportPlainText(_report);
        }

        private void ChartPanel_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = _chartPanel.ClientSize.Width;
            int h = _chartPanel.ClientSize.Height;

            // Заголовок диаграммы
            using (var titleFont = new Font("Segoe UI", 11.5f, FontStyle.Bold))
            {
                g.DrawString("Гистограмма сравнения задержек (с)", titleFont, Brushes.White, 15, 12);
            }

            // Легенда
            using (var legFont = new Font("Segoe UI", 9.5f))
            using (var legFixedBrush = new SolidBrush(Color.FromArgb(235, 130, 40)))
            using (var legAIBrush = new SolidBrush(Color.FromArgb(0, 200, 120)))
            {
                g.FillRectangle(legFixedBrush, 15, 38, 14, 14);
                g.DrawString("ГОСТ (Фиксированный)", legFont, Brushes.LightGray, 33, 37);

                g.FillRectangle(legAIBrush, 210, 38, 14, 14);
                g.DrawString("Мультиагентный ИИ", legFont, Brushes.LightGray, 228, 37);
            }

            // Данные для столбчатой диаграммы
            string[] categories = { "Общая", "Октябрьская", "Рашпилевская", "Красная" };
            double[] fixedVals = { _report.FixedAvgWait, _report.FixedDelayOkt, _report.FixedDelayRash, _report.FixedDelayKras };
            double[] aiVals = { _report.AIAvgWait, _report.AIDelayOkt, _report.AIDelayRash, _report.AIDelayKras };

            double maxVal = Math.Max(22.0, Math.Max(fixedVals.Length > 0 ? System.Linq.Enumerable.Max(fixedVals) : 0, 
                                                    aiVals.Length > 0 ? System.Linq.Enumerable.Max(aiVals) : 0) * 1.25);

            int chartTop = 70;
            int chartBottom = h - 45;
            int chartHeight = chartBottom - chartTop;
            int groupWidth = (w - 60) / categories.Length;
            int barWidth = Math.Max(16, (groupWidth - 25) / 2);

            using (var gridPen = new Pen(Color.FromArgb(50, 55, 65), 1))
            using (var axisFont = new Font("Segoe UI", 9.5f))
            using (var valFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var barFixedBrush = new SolidBrush(Color.FromArgb(235, 130, 40)))
            using (var barAIBrush = new SolidBrush(Color.FromArgb(0, 200, 120)))
            {
                // Горизонтальные направляющие
                for (int gridStep = 0; gridStep <= 4; gridStep++)
                {
                    float y = chartBottom - (chartHeight * gridStep / 4f);
                    g.DrawLine(gridPen, 45, y, w - 15, y);
                    double val = (maxVal * gridStep) / 4.0;
                    g.DrawString($"{val:F0}с", axisFont, Brushes.Gray, 10, y - 8);
                }

                // Столбцы категорий
                for (int i = 0; i < categories.Length; i++)
                {
                    int groupX = 55 + i * groupWidth;

                    // Столбец 1: Fixed
                    double fH = (fixedVals[i] / maxVal) * chartHeight;
                    float fY = chartBottom - (float)fH;
                    float fX = groupX;
                    g.FillRectangle(barFixedBrush, fX, fY, barWidth, (float)fH);
                    g.DrawString($"{fixedVals[i]:F1}", valFont, Brushes.White, fX - 2, fY - 18);

                    // Столбец 2: AI
                    double aH = (aiVals[i] / maxVal) * chartHeight;
                    float aY = chartBottom - (float)aH;
                    float aX = groupX + barWidth + 5;
                    g.FillRectangle(barAIBrush, aX, aY, barWidth, (float)aH);
                    g.DrawString($"{aiVals[i]:F1}", valFont, barAIBrush, aX - 2, aY - 18);

                    // Подпись категории
                    var sz = g.MeasureString(categories[i], axisFont);
                    g.DrawString(categories[i], axisFont, Brushes.White, groupX + (barWidth * 2 + 5 - sz.Width) / 2f, chartBottom + 8);
                }
            }
        }
    }
}
