using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Reflection;

namespace SAi_KR
{
    public partial class Form1 : Form
    {
        private readonly SimulationEngine _engine = new SimulationEngine();
        private double _simulatedTime = 0.0;
        private int _logCounter = 0;

        private Button _btnCompareModes = null!;
        private Button _btnScenarioNormal = null!;
        private Button _btnScenarioRush = null!;
        private Button _btnScenarioKrasnayaJam = null!;

        private Panel _cardFixed = null!;
        private Panel _cardAI = null!;
        private Panel _cardDelta = null!;

        private Label _lblFixedWaitVal = null!;
        private Label _lblFixedQueueVal = null!;
        private Label _lblFixedPassedVal = null!;

        private Label _lblAIWaitVal = null!;
        private Label _lblAIQueueVal = null!;
        private Label _lblAIPassedVal = null!;

        private Label _lblDeltaWaitVal = null!;
        private Label _lblDeltaQueueVal = null!;
        private Label _lblDeltaPassedVal = null!;
        private Label _lblScenarioHint = null!;

        public Form1()
        {
            InitializeComponent();
            SetupCustomUi();
            _engine.BuildSevernayaCorridor();
        }

        private void SetupCustomUi()
        {
            dgvTelemetry.Columns.Clear();
            dgvTelemetry.Columns.Add("colTime", "Время (с)");
            dgvTelemetry.Columns.Add("colNode", "Перекресток");
            dgvTelemetry.Columns.Add("colQueue", "Очередь (авт)");
            dgvTelemetry.Columns.Add("colPhase", "Фаза");
            dgvTelemetry.Columns.Add("colSignal", "Сигнал ИНС");
            dgvTelemetry.Columns.Add("colRule", "Заключение БЗ (Правило)");

            dgvTelemetry.Columns[0].Width = 150;
            dgvTelemetry.Columns[1].Width = 320;
            dgvTelemetry.Columns[2].Width = 180;
            dgvTelemetry.Columns[3].Width = 110;
            dgvTelemetry.Columns[4].Width = 170;
            dgvTelemetry.Columns[5].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            typeof(PictureBox).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic,
                null, pbSimulation, new object[] { true });

            pbSimulation.Paint += PbSimulation_Paint;
            tmrSim.Tick += TmrSim_Tick;
            btnStartPause.Click += BtnStartPause_Click;
            btnReset.Click += BtnReset_Click;
            btnTrain.Click += BtnTrain_Click;
            rbFixedTime.CheckedChanged += RbControlMode_CheckedChanged;
            rbAI.CheckedChanged += RbControlMode_CheckedChanged;
            tbSpeed.ValueChanged += (s, e) => lblSpeed.Text = $"Скорость симуляции: x{tbSpeed.Value}";
            nudFlow.ValueChanged += (s, e) =>
            {
                _engine.InflowSevernayaWest = (double)nudFlow.Value;
                _engine.InflowSevernayaEast = (double)nudFlow.Value * 0.9;
            };

            // Кнопки предустановленных сценариев нагрузки в gbControl
            var lblScenarios = new Label
            {
                Text = "Предустановленные сценарии потока:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(36, 455),
                AutoSize = true
            };
            _btnScenarioNormal = new Button
            {
                Text = "1. Базовый поток",
                Location = new Point(30, 490),
                Size = new Size(200, 48),
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            _btnScenarioRush = new Button
            {
                Text = "2. Час пик (Северная)",
                Location = new Point(240, 490),
                Size = new Size(200, 48),
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            _btnScenarioKrasnayaJam = new Button
            {
                Text = "3. Затор (Красная)",
                Location = new Point(450, 490),
                Size = new Size(230, 48),
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };

            _btnScenarioNormal.Click += (s, e) => ApplyScenario(1400, 400, 250, "Базовый поток по коридору ул. Северной (1400 авт/ч)");
            _btnScenarioRush.Click += (s, e) => ApplyScenario(2000, 500, 300, "Час пик по ул. Северной (2000 авт/ч)");
            _btnScenarioKrasnayaJam.Click += (s, e) => ApplyScenario(1300, 750, 450, "Повышенная загрузка ул. Красной (750 авт/ч)");

            gbControl.Controls.Add(lblScenarios);
            gbControl.Controls.Add(_btnScenarioNormal);
            gbControl.Controls.Add(_btnScenarioRush);
            gbControl.Controls.Add(_btnScenarioKrasnayaJam);

            // Индикатор статуса обучения агентов
            lblTrainStatus.Location = new Point(30, 555);
            lblTrainStatus.Size = new Size(650, 75);
            lblTrainStatus.Font = new Font("Segoe UI", 9f);
            lblTrainStatus.Text = "Статус агентов: исходные веса";
            gbStats.Controls.Remove(lblTrainStatus);
            gbControl.Controls.Add(lblTrainStatus);

            // Настройка панели сравнительной эффективности
            gbStats.Controls.Clear();
            gbStats.Text = "Показатели эффективности регулирования";

            _btnCompareModes = new Button
            {
                Text = "Сравнить режимы (A/B эксперимент)",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(15, 30),
                Size = new Size(680, 50),
                Cursor = Cursors.Hand
            };
            _btnCompareModes.FlatAppearance.BorderSize = 0;
            _btnCompareModes.Click += BtnCompareModes_Click;
            gbStats.Controls.Add(_btnCompareModes);

            // Карточка 1: ГОСТ (Фиксированный)
            _cardFixed = new Panel
            {
                Location = new Point(15, 90),
                Size = new Size(335, 140),
                BackColor = Color.FromArgb(245, 245, 248),
                BorderStyle = BorderStyle.FixedSingle
            };
            var lblFixedTitle = new Label { Text = "Фиксированный цикл (ГОСТ)", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(210, 105, 30), Location = new Point(10, 8), AutoSize = true };
            _lblFixedWaitVal = new Label { Text = "Задержка: — с", Font = new Font("Segoe UI", 9f), Location = new Point(10, 35), AutoSize = true };
            _lblFixedQueueVal = new Label { Text = "Очередь: — авт.", Font = new Font("Segoe UI", 9f), Location = new Point(10, 60), AutoSize = true };
            _lblFixedPassedVal = new Label { Text = "Пропущено: 0 авт.", Font = new Font("Segoe UI", 9f), Location = new Point(10, 85), AutoSize = true };
            var lblFixedSub = new Label { Text = "Жесткий тактовый цикл", Font = new Font("Segoe UI", 7.5f), ForeColor = Color.Gray, Location = new Point(10, 115), AutoSize = true };
            _cardFixed.Controls.AddRange(new Control[] { lblFixedTitle, _lblFixedWaitVal, _lblFixedQueueVal, _lblFixedPassedVal, lblFixedSub });
            gbStats.Controls.Add(_cardFixed);

            // Карточка 2: Мультиагентный ИИ
            _cardAI = new Panel
            {
                Location = new Point(360, 90),
                Size = new Size(335, 140),
                BackColor = Color.FromArgb(240, 248, 245),
                BorderStyle = BorderStyle.FixedSingle
            };
            var lblAITitle = new Label { Text = "Мультиагентный ИИ", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 140, 80), Location = new Point(10, 8), AutoSize = true };
            _lblAIWaitVal = new Label { Text = "Задержка: — с", Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 140, 80), Location = new Point(10, 35), AutoSize = true };
            _lblAIQueueVal = new Label { Text = "Очередь: — авт.", Font = new Font("Segoe UI", 9f), Location = new Point(10, 60), AutoSize = true };
            _lblAIPassedVal = new Label { Text = "Пропущено: 0 авт.", Font = new Font("Segoe UI", 9f), Location = new Point(10, 85), AutoSize = true };
            var lblAISub = new Label { Text = "ИНС + База правил + Волна", Font = new Font("Segoe UI", 7.5f), ForeColor = Color.Gray, Location = new Point(10, 115), AutoSize = true };
            _cardAI.Controls.AddRange(new Control[] { lblAITitle, _lblAIWaitVal, _lblAIQueueVal, _lblAIPassedVal, lblAISub });
            gbStats.Controls.Add(_cardAI);

            // Карточка 3: Разница показателей
            _cardDelta = new Panel
            {
                Location = new Point(15, 240),
                Size = new Size(680, 145),
                BackColor = Color.FromArgb(235, 252, 242),
                BorderStyle = BorderStyle.FixedSingle
            };
            var lblDeltaTitle = new Label { Text = "Сравнительный эффект режимов:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 130, 60), Location = new Point(12, 8), AutoSize = true };
            _lblDeltaWaitVal = new Label { Text = "Средняя задержка: —", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 140, 60), Location = new Point(12, 38), AutoSize = true };
            _lblDeltaQueueVal = new Label { Text = "Суммарная очередь: —", Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(30, 80, 50), Location = new Point(12, 68), AutoSize = true };
            _lblDeltaPassedVal = new Label { Text = "Обслужено автомобилей: —", Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(30, 80, 50), Location = new Point(12, 98), AutoSize = true };
            _cardDelta.Controls.AddRange(new Control[] { lblDeltaTitle, _lblDeltaWaitVal, _lblDeltaQueueVal, _lblDeltaPassedVal });
            gbStats.Controls.Add(_cardDelta);

            // Описание текущего сценария
            _lblScenarioHint = new Label
            {
                Location = new Point(15, 395),
                Size = new Size(680, 200),
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(70, 75, 85),
                Text = "Текущий сценарий: базовый поток по коридору ул. Северной."
            };
            gbStats.Controls.Add(_lblScenarioHint);
        }

        private async void BtnCompareModes_Click(object? sender, EventArgs e)
        {
            bool wasRunning = tmrSim.Enabled;
            tmrSim.Enabled = false;
            _btnCompareModes.Enabled = false;
            _btnCompareModes.Text = "Вычисление эксперимента...";
            _btnCompareModes.BackColor = Color.FromArgb(235, 130, 40);

            ComparisonReport? report = null;
            double flow = (double)nudFlow.Value;
            double krasStr = _engine.InflowKrasnayaStraight;
            double krasLeft = _engine.InflowKrasnayaLeft;

            await Task.Run(() =>
            {
                report = ComparisonRunner.RunExperiment(
                    flowSevernaya: flow,
                    flowKrasStraight: krasStr,
                    flowKrasLeft: krasLeft,
                    durationSeconds: 150,
                    progressCallback: (cur, total) =>
                    {
                        int pct = (int)((double)cur / total * 100);
                        Invoke(() =>
                        {
                            _btnCompareModes.Text = $"Вычисление эксперимента ({pct}%)...";
                        });
                    },
                    sourceEngine: _engine);
            });

            _btnCompareModes.Text = "Сравнить режимы (A/B эксперимент)";
            _btnCompareModes.BackColor = Color.FromArgb(0, 120, 215);
            _btnCompareModes.Enabled = true;

            if (report != null)
            {
                using var dlg = new ComparisonReportForm(report);
                dlg.ShowDialog(this);
            }

            if (wasRunning) tmrSim.Enabled = true;
        }

        private void ApplyScenario(double sevFlow, double krasStr, double krasLeft, string scenarioName)
        {
            nudFlow.Value = (decimal)sevFlow;
            _engine.InflowSevernayaWest = sevFlow;
            _engine.InflowSevernayaEast = sevFlow * 0.9;
            _engine.InflowKrasnayaStraight = krasStr;
            _engine.InflowKrasnayaLeft = krasLeft;
            _lblScenarioHint.Text = $"Текущий сценарий: {scenarioName}.";
            _lblScenarioHint.ForeColor = Color.FromArgb(40, 50, 60);
        }

        private void BtnStartPause_Click(object? sender, EventArgs e)
        {
            tmrSim.Enabled = !tmrSim.Enabled;
            btnStartPause.Text = tmrSim.Enabled ? "Пауза" : "Старт";
        }

        private void BtnReset_Click(object? sender, EventArgs e)
        {
            tmrSim.Enabled = false;
            btnStartPause.Text = "Старт";
            _simulatedTime = 0.0;
            _engine.BuildSevernayaCorridor();
            dgvTelemetry.Rows.Clear();
            UpdateStatsDisplay();
            pbSimulation.Invalidate();
        }

        private void RbControlMode_CheckedChanged(object? sender, EventArgs e)
        {
            _engine.CurrentControlMode = rbAI.Checked ? ControlMode.IntelligentAgents : ControlMode.FixedTime;
            UpdateStatsDisplay();
            pbSimulation.Invalidate();
        }

        private void TmrSim_Tick(object? sender, EventArgs e)
        {
            int subSteps = tbSpeed.Value;
            double dt = 0.1;

            for (int step = 0; step < subSteps; step++)
            {
                _engine.Step(dt);
                _simulatedTime += dt;
            }

            _logCounter++;
            if (_logCounter % 10 == 0)
            {
                LogTelemetry();
            }

            UpdateStatsDisplay();
            pbSimulation.Invalidate();
        }

        private void UpdateStatsDisplay()
        {
            double curWait = _engine.GetAverageWaitTime();
            int curQueue = _engine.GetTotalQueue();
            int curPassed = _engine.CompletedVehicles.Count;

            if (rbFixedTime.Checked)
            {
                _lblFixedWaitVal.Text = $"Задержка: {curWait:F2} с";
                _lblFixedQueueVal.Text = $"Очередь: {curQueue} авт.";
                _lblFixedPassedVal.Text = $"Пропущено: {curPassed} авт.";
                _cardFixed.BackColor = Color.FromArgb(255, 245, 235);
                _cardAI.BackColor = Color.FromArgb(245, 245, 248);
            }
            else
            {
                _lblAIWaitVal.Text = $"Задержка: {curWait:F2} с";
                _lblAIQueueVal.Text = $"Очередь: {curQueue} авт.";
                _lblAIPassedVal.Text = $"Пропущено: {curPassed} авт.";
                _cardAI.BackColor = Color.FromArgb(235, 255, 245);
                _cardFixed.BackColor = Color.FromArgb(245, 245, 248);
            }

            if (_simulatedTime < 1.0)
            {
                _lblFixedWaitVal.Text = "Задержка: — с";
                _lblFixedQueueVal.Text = "Очередь: — авт.";
                _lblFixedPassedVal.Text = "Пропущено: 0 авт.";

                _lblAIWaitVal.Text = "Задержка: — с";
                _lblAIQueueVal.Text = "Очередь: — авт.";
                _lblAIPassedVal.Text = "Пропущено: 0 авт.";

                _lblDeltaWaitVal.Text = "Средняя задержка: расчет при запуске";
                _lblDeltaWaitVal.ForeColor = Color.FromArgb(70, 75, 85);
                _lblDeltaQueueVal.Text = "Суммарная очередь: расчет при запуске";
                _lblDeltaQueueVal.ForeColor = Color.FromArgb(70, 75, 85);
                _lblDeltaPassedVal.Text = "Обслужено автомобилей: расчет при запуске";
                _lblDeltaPassedVal.ForeColor = Color.FromArgb(70, 75, 85);
                return;
            }

            if (_engine.FixedModeStats.VehiclesSampled > 0 && _engine.AIModeStats.VehiclesSampled > 0)
            {
                double fixedW = _engine.FixedModeStats.AvgWait;
                double aiW = _engine.AIModeStats.AvgWait;
                double deltaWait = fixedW > 0 ? ((fixedW - aiW) / fixedW) * 100.0 : 0.0;
                _lblDeltaWaitVal.Text = string.Format("Средняя задержка: {0:+#;-#;0.0}% ({1:F1} с против {2:F1} с)", -deltaWait, aiW, fixedW);
                _lblDeltaWaitVal.ForeColor = deltaWait >= 0 ? Color.FromArgb(0, 140, 60) : Color.FromArgb(200, 40, 40);

                double fixedQ = _engine.FixedModeStats.AvgQueue;
                double aiQ = _engine.AIModeStats.AvgQueue;
                double deltaQ = fixedQ > 0 ? ((fixedQ - aiQ) / fixedQ) * 100.0 : 0.0;
                _lblDeltaQueueVal.Text = string.Format("Суммарная очередь: {0:+#;-#;0.0}% ({1:F1} против {2:F1} авт)", -deltaQ, aiQ, fixedQ);
                _lblDeltaQueueVal.ForeColor = deltaQ >= 0 ? Color.FromArgb(0, 140, 60) : Color.FromArgb(200, 40, 40);

                int fixedP = _engine.FixedModeStats.CompletedCount;
                int aiP = _engine.AIModeStats.CompletedCount;
                double deltaP = fixedP > 0 ? (((double)aiP - fixedP) / fixedP) * 100.0 : 0.0;
                _lblDeltaPassedVal.Text = string.Format("Обслужено автомобилей: {0:+#;-#;0.0}% ({1} против {2} авт)", deltaP, aiP, fixedP);
                _lblDeltaPassedVal.ForeColor = deltaP >= 0 ? Color.FromArgb(0, 140, 60) : Color.FromArgb(200, 40, 40);
            }
            else if (rbFixedTime.Checked)
            {
                _lblDeltaWaitVal.Text = $"Задержка (ГОСТ): {curWait:F1} с";
                _lblDeltaWaitVal.ForeColor = Color.FromArgb(210, 105, 30);
                _lblDeltaQueueVal.Text = $"Текущая очередь: {curQueue} авт.";
                _lblDeltaQueueVal.ForeColor = Color.FromArgb(210, 105, 30);
                _lblDeltaPassedVal.Text = $"Обслужено ТС: {curPassed} авт.";
                _lblDeltaPassedVal.ForeColor = Color.FromArgb(60, 65, 75);
            }
            else
            {
                _lblDeltaWaitVal.Text = $"Задержка (ИИ): {curWait:F1} с";
                _lblDeltaWaitVal.ForeColor = Color.FromArgb(0, 140, 60);
                _lblDeltaQueueVal.Text = $"Текущая очередь: {curQueue} авт.";
                _lblDeltaQueueVal.ForeColor = Color.FromArgb(0, 140, 60);
                _lblDeltaPassedVal.Text = $"Обслужено ТС: {curPassed} авт.";
                _lblDeltaPassedVal.ForeColor = Color.FromArgb(60, 65, 75);
            }
        }

        private void LogTelemetry()
        {
            foreach (var agent in _engine.Agents)
            {
                double[] state = agent.CollectState();
                double[] brainOut = agent.Brain.Forward(state);
                int queue = 0;
                int currentPhase = agent.Node.CurrentPhaseIndex;

                for (int i = 0; i < agent.Node.IncomingLanes.Count; i++)
                {
                    if (currentPhase < agent.Node.Phases.Count && !agent.Node.Phases[currentPhase].GreenLaneIndices.Contains(i))
                        queue += agent.Node.IncomingLanes[i].GetQueueLength();
                }

                double greenSpeedSum = 0;
                int greenVehiclesCount = 0;
                int greenQueue = 0;
                if (currentPhase < agent.Node.Phases.Count)
                {
                    foreach (int laneIdx in agent.Node.Phases[currentPhase].GreenLaneIndices)
                    {
                        if (laneIdx < agent.Node.IncomingLanes.Count)
                        {
                            var l = agent.Node.IncomingLanes[laneIdx];
                            greenQueue += l.GetQueueLength();
                            if (l.Vehicles.Count > 0)
                            {
                                greenSpeedSum += l.Vehicles.Sum(v => v.Speed);
                                greenVehiclesCount += l.Vehicles.Count;
                            }
                        }
                    }
                }
                double avgGreenSpeed = greenVehiclesCount > 0 ? (greenSpeedSum / greenVehiclesCount) : 0;
                bool isMajor = currentPhase == 0;
                double baseDuration = (currentPhase < agent.Node.Phases.Count) ? agent.Node.Phases[currentPhase].Duration : 30.0;
                double minGreen;
                double maxGreen;
                if (isMajor)
                {
                    minGreen = Math.Max(25.0, baseDuration * 0.65);
                    maxGreen = baseDuration * 1.5;
                }
                else
                {
                    minGreen = Math.Max(14.0, baseDuration * 0.65);
                    maxGreen = Math.Max(18.0, baseDuration * 1.35);
                }

                bool upstreamWaveApproaching = false;
                if (agent.NeighborWest != null && agent.NeighborWest.Node.CurrentPhaseIndex == 0 && agent.NeighborWest.Node.TimeInCurrentPhase >= 4.0)
                {
                    upstreamWaveApproaching = true;
                }
                if (agent.NeighborEast != null && agent.NeighborEast.Node.CurrentPhaseIndex == 0 && agent.NeighborEast.Node.TimeInCurrentPhase >= 4.0)
                {
                    upstreamWaveApproaching = true;
                }

                int approachingPlatoon = 0;
                if (currentPhase < agent.Node.Phases.Count)
                {
                    foreach (int laneIdx in agent.Node.Phases[currentPhase].GreenLaneIndices)
                    {
                        if (laneIdx < agent.Node.IncomingLanes.Count)
                        {
                            var l = agent.Node.IncomingLanes[laneIdx];
                            approachingPlatoon += l.Vehicles.Count(v => v.Speed > 1.5);
                        }
                    }
                }

                var facts = new TrafficFacts
                {
                    CurrentPhaseDuration = agent.Node.TimeInCurrentPhase,
                    MinPhaseDuration = minGreen,
                    MaxPhaseDuration = maxGreen,
                    IsYellowActive = agent.Node.CurrentState == LightState.Yellow || agent.Node.CurrentState == LightState.AllRed,
                    OpposingQueueLength = queue,
                    GreenQueueLength = greenQueue,
                    ApproachingPlatoonCount = approachingPlatoon,
                    NetworkDecisionOutput = brainOut[0],
                    AverageGreenLaneSpeed = avgGreenSpeed,
                    IsMajorCorridorPhase = isMajor,
                    IsUpstreamCorridorGreen = upstreamWaveApproaching
                };

                agent.RuleEngine.ValidatePhaseSwitch(facts, out string reason);

                dgvTelemetry.Rows.Insert(0,
                    _simulatedTime.ToString("F1"),
                    agent.Node.Name,
                    queue,
                    agent.Node.CurrentPhaseIndex + 1,
                    brainOut[0].ToString("F3"),
                    reason);

                if (dgvTelemetry.Rows.Count > 100)
                    dgvTelemetry.Rows.RemoveAt(dgvTelemetry.Rows.Count - 1);
            }
        }

        private async void BtnTrain_Click(object? sender, EventArgs e)
        {
            bool wasRunning = tmrSim.Enabled;
            tmrSim.Enabled = false;
            btnStartPause.Enabled = false;
            btnTrain.Enabled = false;
            lblTrainStatus.Text = "Обучение алгоритмом ГА...";

            int genomeLength = _engine.Agents[0].Brain.TotalWeightsCount * _engine.Agents.Count;
            var simCopy = new SimulationEngine();
            simCopy.InflowSevernayaWest = _engine.InflowSevernayaWest;
            simCopy.InflowSevernayaEast = _engine.InflowSevernayaEast;
            simCopy.InflowKrasnayaStraight = _engine.InflowKrasnayaStraight;
            simCopy.InflowKrasnayaLeft = _engine.InflowKrasnayaLeft;
            simCopy.InflowKrasnayaNorth = _engine.InflowKrasnayaNorth;
            simCopy.InflowOktyabrskaya = _engine.InflowOktyabrskaya;
            simCopy.InflowRashpilevskayaNorth = _engine.InflowRashpilevskayaNorth;
            var trainer = new GeneticTrainer(populationSize: 20, genomeLength: genomeLength, mutationRate: 0.08, mutationStrength: 0.25);

            await Task.Run(() =>
            {
                for (int gen = 0; gen < 15; gen++)
                {
                    int seed = 42 + gen * 17;
                    foreach (var ind in trainer.Population)
                    {
                        ind.Fitness = EvolutionRunner.EvaluateGenome(ind, simCopy, simulationSteps: 600, dt: 0.5, seed: seed);
                    }
                    trainer.Evolve();

                    int currentGen = gen + 1;
                    double bestFit = trainer.BestGenome.Fitness;
                    Invoke(() =>
                    {
                        lblTrainStatus.Text = string.Format("Обучение ГА: поколение {0}/15, функция: {1:F1}", currentGen, bestFit);
                    });
                }
            });

            int wPerAgent = _engine.Agents[0].Brain.TotalWeightsCount;
            for (int i = 0; i < _engine.Agents.Count; i++)
            {
                double[] bestWeights = new double[wPerAgent];
                Array.Copy(trainer.BestGenome.Genes, i * wPerAgent, bestWeights, 0, wPerAgent);
                _engine.Agents[i].Brain.SetWeights(bestWeights);
            }

            lblTrainStatus.Text = string.Format("Обучение завершено ({0} пок.). Значение функции: {1:F1}", trainer.Generation, trainer.BestGenome.Fitness);
            btnStartPause.Enabled = true;
            btnTrain.Enabled = true;

            if (wasRunning) tmrSim.Enabled = true;
        }

        private void PbSimulation_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 1. Задний план и окружающая территория
            g.Clear(Color.FromArgb(45, 52, 45));

            // Фоновые здания кварталов (прямоугольники с контуром)
            using (var bBrush = new SolidBrush(Color.FromArgb(35, 40, 35)))
            using (var bPen = new Pen(Color.FromArgb(30, 35, 30), 1))
            {
                Rectangle[] blocks = {
                    new Rectangle(0, 0, 120, 375),
                    new Rectangle(380, 0, 240, 375),
                    new Rectangle(700, 0, 325, 375),
                    new Rectangle(1225, 0, 495, 375),
                    new Rectangle(0, 480, 220, 460),
                    new Rectangle(620, 480, 405, 460),
                    new Rectangle(1225, 480, 495, 460)
                };
                foreach (var r in blocks)
                {
                    g.FillRectangle(bBrush, r);
                    g.DrawRectangle(bPen, r);
                }
            }

            // Зеленые парковые зоны
            using (var parkBrush = new SolidBrush(Color.FromArgb(30, 60, 35)))
            using (var treeBrush = new SolidBrush(Color.FromArgb(20, 80, 30)))
            using (var alleyBrush = new SolidBrush(Color.FromArgb(85, 90, 85)))
            {
                // Сквер Памяти Героев
                Rectangle sqGeroev = new Rectangle(130, 20, 240, 350);
                g.FillRectangle(parkBrush, sqGeroev);
                for (int tx = 140; tx < 360; tx += 20)
                    for (int ty = 30; ty < 360; ty += 25)
                        g.FillEllipse(treeBrush, tx + (ty % 2) * 10, ty, 15, 15);

                // Александровский бульвар СЕВЕР (Y: 20..375)
                Rectangle alBulvarNorth = new Rectangle(1075, 20, 100, 355);
                g.FillRectangle(parkBrush, alBulvarNorth);
                g.FillRectangle(alleyBrush, 1115, 20, 20, 355);
                for (int tx = 1080; tx < 1165; tx += 25)
                    for (int ty = 30; ty < 365; ty += 24)
                    {
                        if (tx < 1110 || tx > 1135)
                            g.FillEllipse(treeBrush, tx, ty + (tx % 2) * 8, 16, 16);
                    }

                // Александровский бульвар ЮГ (Y: 470..конец) — БУЛЬВАР СНИЗУ ПО КРАСНОЙ
                Rectangle alBulvarSouth = new Rectangle(1075, 470, 100, pbSimulation.Height - 470);
                g.FillRectangle(parkBrush, alBulvarSouth);
                g.FillRectangle(alleyBrush, 1115, 470, 20, pbSimulation.Height - 470);
                for (int tx = 1080; tx < 1165; tx += 25)
                    for (int ty = 485; ty < pbSimulation.Height - 20; ty += 26)
                    {
                        if (tx < 1110 || tx > 1135)
                            g.FillEllipse(treeBrush, tx, ty + (tx % 2) * 8, 16, 16);
                    }
            }

            // ТРЦ «Галерея»
            using (var mallBrush = new SolidBrush(Color.FromArgb(50, 45, 55)))
            using (var mallPen = new Pen(Color.FromArgb(40, 35, 45), 2))
            {
                Rectangle mall = new Rectangle(300, 480, 310, 200);
                g.FillRectangle(mallBrush, mall);
                g.DrawRectangle(mallPen, mall);
            }

            // 2. Дорожное полотно и разметка
            using (var roadBrush = new SolidBrush(Color.FromArgb(55, 55, 55)))
            using (var dashPen = new Pen(Color.White, 2) { DashStyle = DashStyle.Dash })
            using (var solidWhitePen = new Pen(Color.White, 2))
            using (var doubleYellow = new Pen(Color.FromArgb(240, 200, 50), 3))
            using (var turnPen = new Pen(Color.FromArgb(200, 255, 255, 255), 2) { DashStyle = DashStyle.Dash })
            {
                // Полотно ул. Северная (Y: 385..470)
                // От 0 до перекрестка с Красной (1015) полотно идет полностью в 4 полосы
                g.FillRectangle(roadBrush, 0, 385, 1015, 85);
                // На восток за Красной идет только встречная западная полоса (Y: 385..427)
                g.FillRectangle(roadBrush, 1015, 385, pbSimulation.Width - 1015, 42);

                // Границы проезжей части Северной
                g.DrawLine(solidWhitePen, 0, 385, pbSimulation.Width, 385);
                g.DrawLine(solidWhitePen, 0, 470, 1035, 470);

                // Двойная сплошная Северной
                g.DrawLine(doubleYellow, 0, 426, 1015, 426);
                g.DrawLine(doubleYellow, 0, 429, 1015, 429);

                // Прерывистые разделительные полос Северной
                g.DrawLine(dashPen, 0, 406, 1015, 406); // западная
                g.DrawLine(dashPen, 0, 449, 1015, 449); // восточная
                g.DrawLine(dashPen, 1215, 406, pbSimulation.Width, 406); // продолжение западной на восток

                // Закругления поворотов на Северной:
                // 1. Поворот с Северной на Красную направо (вниз) — две параллельные направляющие и разделитель рядов
                g.DrawBezier(turnPen,
                    new PointF(1015, 460),
                    new PointF(1035, 460),
                    new PointF(1045, 475),
                    new PointF(1045, 495)); // правый ряд вдоль бордюра
                g.DrawBezier(turnPen,
                    new PointF(1015, 449.5f),
                    new PointF(1042.5f, 449.5f),
                    new PointF(1055, 472.5f),
                    new PointF(1055, 495)); // разделитель между рядами поворота
                g.DrawBezier(turnPen,
                    new PointF(1015, 439),
                    new PointF(1050, 439),
                    new PointF(1065, 470),
                    new PointF(1065, 495)); // левый ряд по внешнему радиусу

                // 2. Поворот с Северной (между Рашпилевской и Красной) направо на Рашпилевскую на север
                g.DrawArc(turnPen, 660, 365, 55, 55, 0, 90);

                // Поворот с Северной (восток) направо на Рашпилевскую на юг
                g.DrawBezier(turnPen,
                    new PointF(615, 460),
                    new PointF(635, 460),
                    new PointF(660, 470),
                    new PointF(675, 480));

                // Поворот с Красной (вниз) направо на Северную на запад (в сторону Рашпилевской)
                g.DrawBezier(turnPen,
                    new PointF(1045, 375),
                    new PointF(1040, 386),
                    new PointF(1028, 396),
                    new PointF(1015, 396));

                // 3. Поворот с Красной налево на Северную в накопитель между бульварами к светофору на X=1075
                g.DrawBezier(turnPen,
                    new PointF(1185, 480),
                    new PointF(1185, 435),
                    new PointF(1135, 416),
                    new PointF(1075, 416));

                // 4. Повороты с Октябрьской (направо на восток и налево на запад)
                g.DrawArc(turnPen, 260, 450, 40, 40, 180, 90); // направо
                g.DrawBezier(turnPen,
                    new PointF(245, 480),
                    new PointF(245, 435),
                    new PointF(230, 416),
                    new PointF(210, 416)); // налево

                // Ул. Октябрьская (230..285): Левый ряд — налево, Правый ряд — направо
                g.FillRectangle(roadBrush, 230, 470, 55, pbSimulation.Height - 470);
                g.DrawLine(solidWhitePen, 230, 470, 230, pbSimulation.Height);
                g.DrawLine(solidWhitePen, 285, 470, 285, pbSimulation.Height);
                g.DrawLine(dashPen, 257, 470, 257, pbSimulation.Height);

                // Ул. Рашпилевская (630..690)
                // Северная часть: ДВУСТОРОННЯЯ (левый ряд вниз к Северной, правый ряд вверх от Северной)
                g.FillRectangle(roadBrush, 630, 0, 60, 385);
                g.DrawLine(solidWhitePen, 630, 0, 630, 385);
                g.DrawLine(solidWhitePen, 690, 0, 690, 385);
                g.DrawLine(dashPen, 660, 0, 660, 385);

                // Зона перекрестка Рашпилевской
                g.FillRectangle(roadBrush, 630, 385, 60, 85);

                // Южная часть: ОДНОСТОРОННЕЕ ДВИЖЕНИЕ НА ЮГ (обе полосы идут вниз!)
                g.FillRectangle(roadBrush, 630, 470, 60, pbSimulation.Height - 470);
                g.DrawLine(solidWhitePen, 630, 470, 630, pbSimulation.Height);
                g.DrawLine(solidWhitePen, 690, 470, 690, pbSimulation.Height);
                g.DrawLine(dashPen, 660, 470, 660, pbSimulation.Height);

                // Ул. Красная (БУЛЬВАР СВЕРХУ И СНИЗУ):
                // Левый рукав (движение ВНИЗ): 1035..1075
                g.FillRectangle(roadBrush, 1035, 0, 40, 385);
                g.DrawLine(solidWhitePen, 1035, 0, 1035, 385);
                g.DrawLine(solidWhitePen, 1075, 0, 1075, 385);

                g.FillRectangle(roadBrush, 1035, 385, 40, 85);

                g.FillRectangle(roadBrush, 1035, 470, 40, pbSimulation.Height - 470);
                g.DrawLine(solidWhitePen, 1035, 470, 1035, pbSimulation.Height);
                g.DrawLine(solidWhitePen, 1075, 470, 1075, pbSimulation.Height);
                g.DrawLine(dashPen, 1055, 470, 1055, pbSimulation.Height); // 2 ряда вниз

                // Правый рукав (движение ВВЕРХ): 1175..1215
                g.FillRectangle(roadBrush, 1175, 0, 40, 385);
                g.DrawLine(solidWhitePen, 1175, 0, 1175, 385);
                g.DrawLine(solidWhitePen, 1215, 0, 1215, 385);
                g.DrawLine(dashPen, 1195, 0, 1195, 385); // 2 ряда вверх

                g.FillRectangle(roadBrush, 1175, 385, 40, 42); // пересечение с западной полосой

                g.FillRectangle(roadBrush, 1175, 470, 40, pbSimulation.Height - 470);
                g.DrawLine(solidWhitePen, 1175, 470, 1175, pbSimulation.Height);
                g.DrawLine(solidWhitePen, 1215, 470, 1215, pbSimulation.Height);
                g.DrawLine(dashPen, 1195, 470, 1195, pbSimulation.Height); // 2 ряда: налево и прямо
            }

            // Дорожная стрелочная разметка на асфальте (ГОСТ 51256, разметка 1.18)
            // 1. Ул. Северная — Восток (слева направо, угол = 0)
            DrawRoadArrow(g, 140, 439, 0, RoadManeuver.Straight);
            DrawRoadArrow(g, 140, 460, 0, RoadManeuver.Straight);
            DrawRoadArrow(g, 540, 439, 0, RoadManeuver.Straight);
            DrawRoadArrow(g, 540, 460, 0, RoadManeuver.StraightAndRight); // перед Рашпилевской: прямо и направо на юг
            DrawRoadArrow(g, 970, 439, 0, RoadManeuver.Right); // перед Красной: только направо (вниз)
            DrawRoadArrow(g, 970, 460, 0, RoadManeuver.Right); // перед Красной: только направо (вниз)

            // 2. Ул. Северная — Запад (справа налево, угол = 180)
            DrawRoadArrow(g, 1350, 396, 180, RoadManeuver.Straight);
            DrawRoadArrow(g, 1350, 416, 180, RoadManeuver.Straight);
            DrawRoadArrow(g, 1120, 396, 180, RoadManeuver.Straight);
            DrawRoadArrow(g, 1120, 416, 180, RoadManeuver.Straight);
            DrawRoadArrow(g, 740, 396, 180, RoadManeuver.StraightAndRight); // правый ряд: прямо и направо на Рашпилевскую
            DrawRoadArrow(g, 740, 416, 180, RoadManeuver.Straight);         // левый ряд: только прямо
            DrawRoadArrow(g, 350, 396, 180, RoadManeuver.Straight);
            DrawRoadArrow(g, 350, 416, 180, RoadManeuver.Straight);

            // 3. Ул. Октябрьская — выезд от Галереи (снизу вверх, угол = -90)
            DrawRoadArrow(g, 245, 540, -90, RoadManeuver.Left);  // левый ряд: налево на Северную
            DrawRoadArrow(g, 270, 540, -90, RoadManeuver.Right); // правый ряд: направо на Северную

            // 4. Ул. Рашпилевская — Север (двусторонняя)
            DrawRoadArrow(g, 645, 230, 90, RoadManeuver.Straight);  // въезд на юг (угол = 90)
            DrawRoadArrow(g, 675, 230, -90, RoadManeuver.Straight); // выезд на север (угол = -90)

            // 5. Ул. Рашпилевская — Юг (односторонняя на юг, обе полосы вниз, угол = 90)
            DrawRoadArrow(g, 645, 540, 90, RoadManeuver.Straight);
            DrawRoadArrow(g, 675, 540, 90, RoadManeuver.Straight);
            DrawRoadArrow(g, 645, 720, 90, RoadManeuver.Straight);
            DrawRoadArrow(g, 675, 720, 90, RoadManeuver.Straight);

            // 6. Ул. Красная — Северные рукава
            DrawRoadArrow(g, 1045, 230, 90, RoadManeuver.StraightAndRight); // левый рукав вниз: прямо и направо на Северную
            DrawRoadArrow(g, 1185, 230, -90, RoadManeuver.Straight);        // правый рукав вверх (полоса 1, угол = -90)
            DrawRoadArrow(g, 1205, 230, -90, RoadManeuver.Straight);        // правый рукав вверх (полоса 2, угол = -90)

            // 7. Ул. Красная — Южный левый рукав (движение вниз, угол = 90)
            DrawRoadArrow(g, 1045, 540, 90, RoadManeuver.Straight);
            DrawRoadArrow(g, 1065, 540, 90, RoadManeuver.Straight);
            DrawRoadArrow(g, 1045, 720, 90, RoadManeuver.Straight);
            DrawRoadArrow(g, 1065, 720, 90, RoadManeuver.Straight);

            // 8. Ул. Красная — Южный правый рукав (движение вверх, угол = -90)
            DrawRoadArrow(g, 1185, 540, -90, RoadManeuver.StraightAndLeft); // левый ряд: прямо и налево на Северную
            DrawRoadArrow(g, 1205, 540, -90, RoadManeuver.Straight);        // правый ряд: прямо вверх
            DrawRoadArrow(g, 1185, 720, -90, RoadManeuver.StraightAndLeft);
            DrawRoadArrow(g, 1205, 720, -90, RoadManeuver.Straight);

            // Пешеходные переходы (разметка 1.14 «Зебра»)
            using (var zebraPen = new Pen(Color.White, 3) { DashStyle = DashStyle.Dash })
            {
                // Ул. Октябрьская
                g.DrawLine(zebraPen, 230, 480, 285, 480);
                g.DrawLine(zebraPen, 205, 385, 205, 470);
                g.DrawLine(zebraPen, 300, 385, 300, 470);

                // Ул. Рашпилевская
                g.DrawLine(zebraPen, 630, 375, 690, 375);
                g.DrawLine(zebraPen, 630, 480, 690, 480);
                g.DrawLine(zebraPen, 610, 385, 610, 470);
                g.DrawLine(zebraPen, 705, 385, 705, 470);

                // Ул. Красная (рукава бульвара и переход между бульварами)
                g.DrawLine(zebraPen, 1035, 375, 1075, 375);
                g.DrawLine(zebraPen, 1175, 375, 1215, 375);
                g.DrawLine(zebraPen, 1035, 480, 1075, 480);
                g.DrawLine(zebraPen, 1175, 480, 1215, 480);
                g.DrawLine(zebraPen, 1010, 385, 1010, 470);
                g.DrawLine(zebraPen, 1230, 385, 1230, 427);

                // Пешеходная аллея Александровского бульвара через Северную
                g.DrawLine(zebraPen, 1075, 375, 1075, 470);
                g.DrawLine(zebraPen, 1175, 375, 1175, 470);
            }

            // Стоп-линии (разметка 1.12)
            using (var stopPen = new Pen(Color.White, 4))
            {
                // Ул. Октябрьская
                g.DrawLine(stopPen, 210, 429, 210, 470); // Северная Восток
                g.DrawLine(stopPen, 295, 385, 295, 426); // Северная Запад
                g.DrawLine(stopPen, 230, 480, 285, 480); // Октябрьская

                // Ул. Рашпилевская
                g.DrawLine(stopPen, 615, 429, 615, 470); // Северная Восток
                g.DrawLine(stopPen, 700, 385, 700, 426); // Северная Запад
                g.DrawLine(stopPen, 630, 375, 660, 375); // Рашпилевская Север (левый входящий ряд)

                // Ул. Красная
                g.DrawLine(stopPen, 1015, 429, 1015, 470); // Северная Восток (перед поворотом направо)
                g.DrawLine(stopPen, 1225, 385, 1225, 426); // Северная Запад
                g.DrawLine(stopPen, 1035, 375, 1075, 375); // Красная Север (левый рукав, движение вниз)
                g.DrawLine(stopPen, 1175, 480, 1215, 480); // Красная Юг (правый рукав, движение вверх)

                // СТОП-ЛИНИЯ МЕЖДУ БУЛЬВАРАМИ (перед пересечением с потоком по Красной вниз)
                g.DrawLine(stopPen, 1075, 385, 1075, 426);
            }

            // 3. Светофоры (на всех перекрестках и направлениях)
            if (_engine.Intersections.Count >= 3)
            {
                // --- ПЕРЕКРЕСТОК 1: СЕВЕРНАЯ / ОКТЯБРЬСКАЯ ---
                var n1 = _engine.Intersections[0];
                bool n1_SevActive = (n1.CurrentPhaseIndex == 0);
                DrawTrafficLightBox(g, 205, 475, GetLightState(n1, n1_SevActive));
                DrawTrafficLightBox(g, 295, 315, GetLightState(n1, n1_SevActive));
                DrawTrafficLightBox(g, 295, 480, GetLightState(n1, !n1_SevActive));

                // --- ПЕРЕКРЕСТОК 2: СЕВЕРНАЯ / РАШПИЛЕВСКАЯ ---
                var n2 = _engine.Intersections[1];
                bool n2_SevActive = (n2.CurrentPhaseIndex == 0);
                DrawTrafficLightBox(g, 610, 475, GetLightState(n2, n2_SevActive));
                DrawTrafficLightBox(g, 700, 315, GetLightState(n2, n2_SevActive));
                DrawTrafficLightBox(g, 615, 315, GetLightState(n2, !n2_SevActive));

                // --- ПЕРЕКРЕСТОК 3: СЕВЕРНАЯ / КРАСНАЯ ---
                var n3 = _engine.Intersections[2];
                bool n3_SevActive = (n3.CurrentPhaseIndex == 0);
                DrawTrafficLightBox(g, 1010, 475, GetLightState(n3, n3_SevActive));
                DrawTrafficLightBox(g, 1230, 315, GetLightState(n3, n3_SevActive));
                DrawTrafficLightBox(g, 1020, 315, GetLightState(n3, !n3_SevActive));
                DrawTrafficLightBox(g, 1220, 480, GetLightState(n3, !n3_SevActive));
                DrawTrafficLightBox(g, 1080, 315, GetLightState(n3, n3_SevActive));
            }

            // 4. Отрисовка транспортных средств
            foreach (var lane in _engine.AllLanes)
            {
                foreach (var car in lane.Vehicles)
                {
                    DrawVehicle(g, lane, car);
                }
            }

            // 5. Наименования улиц (только чистые названия улиц без лишних надписей)
            using (var signFont = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            using (var signBg = new SolidBrush(Color.FromArgb(225, 20, 25, 35)))
            {
                DrawStreetLabel(g, "ул. Северная", 450, 355, signFont, signBg);
                DrawStreetLabel(g, "ул. Северная", 1330, 355, signFont, signBg);
                DrawStreetLabel(g, "ул. Октябрьская", 185, 720, signFont, signBg);
                DrawStreetLabel(g, "ул. Рашпилевская", 575, 50, signFont, signBg);
                DrawStreetLabel(g, "ул. Рашпилевская", 575, 720, signFont, signBg);
                DrawStreetLabel(g, "ул. Красная", 1085, 50, signFont, signBg);
                DrawStreetLabel(g, "ул. Красная", 1085, 720, signFont, signBg);
            }

            using (var infoFont = new Font("Segoe UI", 14, FontStyle.Bold))
            using (var infoBg = new SolidBrush(Color.FromArgb(200, 0, 0, 0)))
            {
                string timeTxt = string.Format("Время: {0:F1} с", _simulatedTime);
                SizeF sz = g.MeasureString(timeTxt, infoFont);
                float tx = pbSimulation.Width - sz.Width - 20;
                float ty = 20;
                g.FillRectangle(infoBg, tx - 10, ty - 5, sz.Width + 20, sz.Height + 10);
                g.DrawString(timeTxt, infoFont, Brushes.White, tx, ty);
            }

            // 6. Верхний баннер активного режима
            DrawModeBanner(g);

            // 7. Межагентная координация («Зеленая волна»)
            if (rbAI.Checked)
            {
                DrawCoordinationWave(g);
            }

            // 8. Интеллектуальные HUD-плашки над перекрестками
            DrawIntersectionHUDs(g);

            // 9. Индикаторы очередей на стоп-линиях
            DrawQueuePills(g);
        }

        private void DrawModeBanner(Graphics g)
        {
            bool isAI = rbAI.Checked;
            RectangleF box = new RectangleF(25, 15, 620, 46);

            using (var bgBrush = new SolidBrush(Color.FromArgb(225, 18, 22, 28)))
            using (var borderPen = new Pen(isAI ? Color.FromArgb(0, 220, 130) : Color.FromArgb(240, 130, 40), 1.8f))
            using (var path = GetRoundedRect(box, 5f))
            {
                g.FillPath(bgBrush, path);
                g.DrawPath(borderPen, path);
            }

            using (var titleFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (var subFont = new Font("Segoe UI", 8f))
            {
                if (isAI)
                {
                    g.FillEllipse(Brushes.LimeGreen, 37, 24, 10, 10);
                    g.DrawString("РЕЖИМ: МУЛЬТИАГЕНТНЫЙ ИИ (ИНС + ПРАВИЛА БЕЗОПАСНОСТИ + ЗЕЛЕНАЯ ВОЛНА)", titleFont, Brushes.White, 55, 18);
                    g.DrawString("3 взаимодействующих агента | Адаптивное продление фаз | Координация коридора активна", subFont, Brushes.LightGray, 55, 36);
                }
                else
                {
                    using var dotBrush = new SolidBrush(Color.FromArgb(240, 130, 40));
                    g.FillRectangle(dotBrush, 37, 24, 10, 10);
                    g.DrawString("РЕЖИМ: ФИКСИРОВАННЫЙ ЦИКЛ ПО ГОСТ Р 52289 (ТАЙМЕР)", titleFont, Brushes.White, 55, 18);
                    g.DrawString("Жесткие такты: Северная 40-45с, Боковые 20-32с | Адаптивное продление отключено", subFont, Brushes.LightGray, 55, 36);
                }
            }
        }

        private void DrawCoordinationWave(Graphics g)
        {
            float y = 345f;
            float x1 = 250f;
            float x2 = 655f;
            float x3 = 1110f;

            using (var wavePen = new Pen(Color.FromArgb(160, 0, 210, 255), 2.2f) { DashStyle = DashStyle.Dash })
            {
                g.DrawLine(wavePen, x1, y, x2, y);
                g.DrawLine(wavePen, x2, y, x3, y);
            }

            // Светящиеся узлы координации
            float pulse = (float)(Math.Sin(_simulatedTime * 4.0) * 2.0);
            using (var glowBrush = new SolidBrush(Color.FromArgb(100, 0, 220, 255)))
            using (var dotBrush = new SolidBrush(Color.FromArgb(0, 240, 255)))
            {
                foreach (float x in new[] { x1, x2, x3 })
                {
                    g.FillEllipse(glowBrush, x - 8f - pulse, y - 8f - pulse, 16f + pulse * 2, 16f + pulse * 2);
                    g.FillEllipse(dotBrush, x - 4f, y - 4f, 8f, 8f);
                }
            }

            // Центральный информационный шильдик
            RectangleF badge = new RectangleF(390, y - 12, 275, 24);
            using (var bgBrush = new SolidBrush(Color.FromArgb(230, 15, 25, 35)))
            using (var borderPen = new Pen(Color.FromArgb(0, 200, 255), 1.2f))
            using (var path = GetRoundedRect(badge, 4f))
            using (var font = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            {
                g.FillPath(bgBrush, path);
                g.DrawPath(borderPen, path);
                g.DrawString("Координация: «Зеленая волна» (Окт ↔ Раш ↔ Крас)", font, Brushes.Cyan, badge.X + 8, badge.Y + 4);
            }
        }

        private void DrawIntersectionHUDs(Graphics g)
        {
            if (_engine.Intersections.Count < 3) return;

            (float X, string Name, Intersection Node, TrafficAgent? Agent)[] items = {
                (250f, "УЗЕЛ 1: Октябрьская", _engine.Intersections[0], _engine.Agents.Count > 0 ? _engine.Agents[0] : null),
                (655f, "УЗЕЛ 2: Рашпилевская", _engine.Intersections[1], _engine.Agents.Count > 1 ? _engine.Agents[1] : null),
                (1110f, "УЗЕЛ 3: Красная", _engine.Intersections[2], _engine.Agents.Count > 2 ? _engine.Agents[2] : null)
            };

            float y = 250f;
            float w = 175f;
            float h = 60f;

            using (var boxBrush = new SolidBrush(Color.FromArgb(220, 20, 25, 32)))
            using (var borderPen = new Pen(Color.FromArgb(80, 95, 115), 1f))
            using (var titleFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (var infoFont = new Font("Segoe UI", 7.5f))
            using (var decFont = new Font("Segoe UI", 7f, FontStyle.Italic))
            {
                foreach (var item in items)
                {
                    RectangleF r = new RectangleF(item.X - w / 2f, y, w, h);
                    using (var path = GetRoundedRect(r, 4f))
                    {
                        g.FillPath(boxBrush, path);
                        g.DrawPath(borderPen, path);
                    }

                    int q = item.Node.IncomingLanes.Sum(l => l.GetQueueLength());
                    double t = item.Node.TimeInCurrentPhase;
                    int ph = item.Node.CurrentPhaseIndex + 1;

                    // Заголовок узла
                    g.DrawString(item.Name, titleFont, Brushes.White, r.X + 6, r.Y + 4);

                    // Номер фазы и текущая очередь
                    string pqTxt = $"Фаза {ph}: {t:F0}с | Очередь: {q} авт.";
                    g.DrawString(pqTxt, infoFont, Brushes.LightGray, r.X + 6, r.Y + 22);

                    // Обоснование принятого решения
                    string decTxt;
                    Brush decBrush;
                    if (rbAI.Checked && item.Agent != null)
                    {
                        string reason = item.Agent.CurrentDecisionText;
                        if (reason.Length > 24) reason = reason.Substring(0, 22) + "..";
                        decTxt = $"[ИИ] {reason}";
                        decBrush = Brushes.PaleGreen;
                    }
                    else
                    {
                        double maxD = item.Node.Phases.Count > item.Node.CurrentPhaseIndex ? item.Node.Phases[item.Node.CurrentPhaseIndex].Duration : 40.0;
                        double rem = Math.Max(0.0, maxD - t);
                        decTxt = $"[ГОСТ] Таймер: ост. {rem:F0}с";
                        decBrush = Brushes.PeachPuff;
                    }
                    g.DrawString(decTxt, decFont, decBrush, r.X + 6, r.Y + 38);

                    // Полоса очереди внизу панели
                    float barW = w - 12f;
                    float barFill = Math.Min(barW, (q / 15f) * barW);
                    Color barCol = q <= 4 ? Color.FromArgb(0, 200, 100) : (q <= 8 ? Color.FromArgb(235, 180, 20) : Color.FromArgb(235, 60, 50));
                    using (var barBgBrush = new SolidBrush(Color.FromArgb(40, 45, 55)))
                    using (var barBrush = new SolidBrush(barCol))
                    {
                        g.FillRectangle(barBgBrush, r.X + 6, r.Bottom - 6, barW, 3);
                        g.FillRectangle(barBrush, r.X + 6, r.Bottom - 6, barFill, 3);
                    }
                }
            }
        }

        private void DrawQueuePills(Graphics g)
        {
            (float X, float Y, string LaneId)[] checkPoints = {
                (1195f, 495f, "Inflow_Kras_SE_Str"),
                (1045f, 360f, "Inflow_Kras_NW"),
                (1240f, 400f, "Inflow_Sev_East_L1"),
                (255f, 495f, "Inflow_Okt_S_Right"),
                (645f, 360f, "Inflow_Rash_N")
            };

            using (var font = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            {
                foreach (var cp in checkPoints)
                {
                    var lane = _engine.AllLanes.FirstOrDefault(l => l.Id == cp.LaneId);
                    if (lane == null) continue;
                    int q = lane.GetQueueLength();
                    if (q <= 0) continue;

                    string txt = $"{q} авт.";
                    SizeF sz = g.MeasureString(txt, font);
                    RectangleF pill = new RectangleF(cp.X - sz.Width / 2f - 4f, cp.Y - sz.Height / 2f - 2f, sz.Width + 8f, sz.Height + 4f);

                    Color bg = q >= 6 ? Color.FromArgb(220, 220, 40, 40) : Color.FromArgb(220, 220, 150, 20);
                    using (var pillBrush = new SolidBrush(bg))
                    using (var path = GetRoundedRect(pill, 4f))
                    {
                        g.FillPath(pillBrush, path);
                        g.DrawString(txt, font, Brushes.White, pill.X + 4f, pill.Y + 2f);
                    }
                }
            }
        }

        private LightState GetLightState(Intersection node, bool isPhaseActive)
        {
            if (node.CurrentState == LightState.AllRed) return LightState.AllRed;
            if (node.CurrentState == LightState.Yellow) return LightState.Yellow;
            return isPhaseActive ? LightState.Green : LightState.Red;
        }

        private void DrawTrafficLightBox(Graphics g, float x, float y, LightState state, string? label = null)
        {
            // Габариты корпуса светофора
            float boxW = 20f;
            float boxH = 54f;
            RectangleF box = new RectangleF(x - boxW / 2f, y, boxW, boxH);

            // Опора (столб)
            using (var polePen = new Pen(Color.FromArgb(120, 120, 125), 2.5f))
            {
                g.DrawLine(polePen, x, y + boxH, x, y + boxH + 20f);
            }

            // Корпус светофора
            using (var boxBrush = new SolidBrush(Color.FromArgb(24, 26, 28)))
            using (var boxPen = new Pen(Color.FromArgb(90, 95, 100), 1f))
            using (var path = GetRoundedRect(box, 3.5f))
            {
                g.FillPath(boxBrush, path);
                g.DrawPath(boxPen, path);
            }

            // Сигналы: красный, желтый, зеленый
            float sRedY = y + 9f;
            float sYelY = y + 27f;
            float sGrnY = y + 45f;

            DrawSignal(g, x, sRedY, Color.FromArgb(255, 45, 45), state == LightState.Red || state == LightState.AllRed);
            DrawSignal(g, x, sYelY, Color.FromArgb(255, 200, 20), state == LightState.Yellow);
            DrawSignal(g, x, sGrnY, Color.FromArgb(30, 230, 60), state == LightState.Green);

            // Информационная табличка
            if (!string.IsNullOrEmpty(label))
            {
                using (var badgeFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                using (var badgeBg = new SolidBrush(Color.FromArgb(220, 15, 18, 22)))
                using (var badgeBorder = new Pen(Color.FromArgb(100, 120, 140), 1f))
                {
                    SizeF bSz = g.MeasureString(label, badgeFont);
                    float bx = x - bSz.Width / 2f - 2f;
                    float by = y - bSz.Height - 2f;
                    RectangleF badgeRect = new RectangleF(bx, by, bSz.Width + 4f, bSz.Height + 2f);
                    g.FillRectangle(badgeBg, badgeRect);
                    g.DrawRectangle(badgeBorder, badgeRect.X, badgeRect.Y, badgeRect.Width, badgeRect.Height);
                    g.DrawString(label, badgeFont, Brushes.White, bx + 2f, by + 1f);
                }
            }
        }

        private void DrawSignal(Graphics g, float x, float y, Color baseColor, bool active)
        {
            float r = 5.0f;
            if (active)
            {
                // Свечение ореола сигнала
                using (var glowBrush = new SolidBrush(Color.FromArgb(90, baseColor.R, baseColor.G, baseColor.B)))
                {
                    g.FillEllipse(glowBrush, x - r - 4f, y - r - 4f, (r + 4f) * 2f, (r + 4f) * 2f);
                }
                // Яркое ядро активного сигнала
                using (var activeBrush = new SolidBrush(baseColor))
                {
                    g.FillEllipse(activeBrush, x - r, y - r, r * 2f, r * 2f);
                }
                // Блик на линзе светофора
                using (var glintBrush = new SolidBrush(Color.FromArgb(220, 255, 255, 255)))
                {
                    g.FillEllipse(glintBrush, x - r + 1.5f, y - r + 1.5f, 2.5f, 2.5f);
                }
            }
            else
            {
                // Затемненная линза неактивного сигнала
                using (var dimBrush = new SolidBrush(Color.FromArgb(45, baseColor.R / 4, baseColor.G / 4, baseColor.B / 4)))
                using (var dimPen = new Pen(Color.FromArgb(30, 30, 35), 0.8f))
                {
                    g.FillEllipse(dimBrush, x - r, y - r, r * 2f, r * 2f);
                    g.DrawEllipse(dimPen, x - r, y - r, r * 2f, r * 2f);
                }
            }
        }

        private GraphicsPath GetRoundedRect(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2f;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void DrawStreetLabel(Graphics g, string text, float x, float y, Font font, Brush bgBrush)
        {
            SizeF sz = g.MeasureString(text, font);
            RectangleF rect = new RectangleF(x, y, sz.Width + 14, sz.Height + 6);
            using (var path = GetRoundedRect(rect, 4f))
            using (var borderPen = new Pen(Color.FromArgb(90, 110, 130), 1f))
            {
                g.FillPath(bgBrush, path);
                g.DrawPath(borderPen, path);
            }
            g.DrawString(text, font, Brushes.White, x + 7, y + 3);
        }

        private enum RoadManeuver
        {
            Straight,
            Right,
            Left,
            StraightAndRight,
            StraightAndLeft
        }

        private void DrawRoadArrow(Graphics g, float cx, float cy, float rotationDegrees, RoadManeuver maneuver)
        {
            var state = g.Save();
            g.TranslateTransform(cx, cy);
            g.RotateTransform(rotationDegrees);

            using (var brush = new SolidBrush(Color.FromArgb(235, 255, 255, 255)))
            using (var pen = new Pen(Color.FromArgb(235, 255, 255, 255), 2.5f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                switch (maneuver)
                {
                    case RoadManeuver.Straight:
                        g.DrawLine(pen, -10f, 0f, 4f, 0f);
                        PointF[] straightHead = {
                            new PointF(4f, -5f),
                            new PointF(13f, 0f),
                            new PointF(4f, 5f)
                        };
                        g.FillPolygon(brush, straightHead);
                        break;

                    case RoadManeuver.Right:
                        using (var turnPen = new Pen(Color.FromArgb(235, 255, 255, 255), 2.5f))
                        {
                            turnPen.StartCap = LineCap.Round;
                            g.DrawArc(turnPen, -10f, 0f, 16f, 16f, 270f, 90f);
                            g.DrawLine(turnPen, -2f, 8f, -2f, 10f);
                        }
                        PointF[] rightHead = {
                            new PointF(-7f, 9f),
                            new PointF(-2f, 16f),
                            new PointF(3f, 9f)
                        };
                        g.FillPolygon(brush, rightHead);
                        break;

                    case RoadManeuver.Left:
                        using (var turnPen = new Pen(Color.FromArgb(235, 255, 255, 255), 2.5f))
                        {
                            turnPen.StartCap = LineCap.Round;
                            g.DrawArc(turnPen, -10f, -16f, 16f, 16f, 0f, 90f);
                            g.DrawLine(turnPen, -2f, -8f, -2f, -10f);
                        }
                        PointF[] leftHead = {
                            new PointF(-7f, -9f),
                            new PointF(-2f, -16f),
                            new PointF(3f, -9f)
                        };
                        g.FillPolygon(brush, leftHead);
                        break;

                    case RoadManeuver.StraightAndRight:
                        g.DrawLine(pen, -10f, 0f, 4f, 0f);
                        PointF[] sHead = {
                            new PointF(4f, -5f),
                            new PointF(13f, 0f),
                            new PointF(4f, 5f)
                        };
                        g.FillPolygon(brush, sHead);
                        using (var branchPen = new Pen(Color.FromArgb(235, 255, 255, 255), 2.2f))
                        {
                            branchPen.StartCap = LineCap.Round;
                            g.DrawArc(branchPen, -4f, 0f, 14f, 14f, 270f, 90f);
                        }
                        PointF[] rHead = {
                            new PointF(-1f, 7f),
                            new PointF(3f, 14f),
                            new PointF(7f, 7f)
                        };
                        g.FillPolygon(brush, rHead);
                        break;

                    case RoadManeuver.StraightAndLeft:
                        g.DrawLine(pen, -10f, 0f, 4f, 0f);
                        PointF[] slHead = {
                            new PointF(4f, -5f),
                            new PointF(13f, 0f),
                            new PointF(4f, 5f)
                        };
                        g.FillPolygon(brush, slHead);
                        using (var branchPen = new Pen(Color.FromArgb(235, 255, 255, 255), 2.2f))
                        {
                            branchPen.StartCap = LineCap.Round;
                            g.DrawArc(branchPen, -4f, -14f, 14f, 14f, 0f, 90f);
                        }
                        PointF[] lHead = {
                            new PointF(-1f, -7f),
                            new PointF(3f, -14f),
                            new PointF(7f, -7f)
                        };
                        g.FillPolygon(brush, lHead);
                        break;
                }
            }

            g.Restore(state);
        }

        private void DrawVehicle(Graphics g, Lane lane, Vehicle car)
        {
            var (renderPos, angleRad) = lane.GetRenderPositionWithAngle(car.Position);

            // Метод Graphics.RotateTransform принимает градусы, перевод из радианов
            float angleDeg = (float)(angleRad * 180.0 / Math.PI);

            // Масштабирование габаритов автомобиля в экранные пиксели
            float vLength = (float)(car.Length * SimulationEngine.PxPerMeter);
            float vWidth = (float)(car.VehicleWidth * SimulationEngine.PxPerMeter);
            if (vLength < 12f) vLength = 12f;
            if (vWidth < 6.5f) vWidth = 6.5f;

            var state = g.Save();
            g.TranslateTransform(renderPos.X, renderPos.Y);
            g.RotateTransform(angleDeg);

            // 1. Тень под автомобилем
            using (var shadowBrush = new SolidBrush(Color.FromArgb(80, 15, 20, 15)))
            {
                g.FillRectangle(shadowBrush, -vLength / 2f + 1f, -vWidth / 2f + 1.5f, vLength, vWidth);
            }

            // 2. Кузов автомобиля
            Color bodyColor = car.VehicleColor;
            if (bodyColor.IsEmpty)
                bodyColor = car.Speed < 0.5 ? Color.OrangeRed : Color.DeepSkyBlue;

            Color strokeColor = Color.FromArgb(
                Math.Max(0, bodyColor.R - 65),
                Math.Max(0, bodyColor.G - 65),
                Math.Max(0, bodyColor.B - 65));

            RectangleF carRect = new RectangleF(-vLength / 2f, -vWidth / 2f, vLength, vWidth);
            using (var carBrush = new SolidBrush(bodyColor))
            using (var carPen = new Pen(strokeColor, 1.2f))
            using (var path = GetRoundedRect(carRect, 2.5f))
            {
                g.FillPath(carBrush, path);
                g.DrawPath(carPen, path);
            }

            // 3. Остекление салона и крыша
            float roofLength = vLength * 0.42f;
            float roofWidth = vWidth * 0.72f;

            using (var glassBrush = new SolidBrush(Color.FromArgb(230, 20, 30, 40)))
            using (var roofBrush = new SolidBrush(Color.FromArgb(
                Math.Max(0, bodyColor.R - 25),
                Math.Max(0, bodyColor.G - 25),
                Math.Max(0, bodyColor.B - 25))))
            {
                // Область остекления салона
                float cabX = -vLength * 0.22f;
                float cabW = vLength * 0.58f;
                float cabH = vWidth * 0.76f;
                g.FillRectangle(glassBrush, cabX, -cabH / 2f, cabW, cabH);

                // Крыша автомобиля
                g.FillRectangle(roofBrush, -roofLength / 2f - 0.5f, -roofWidth / 2f, roofLength, roofWidth);
            }

            // 4. Передние фары
            using (var hlBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 210)))
            {
                float hlW = 2.2f;
                float hlH = 2.0f;
                g.FillRectangle(hlBrush, vLength / 2f - 2f, -vWidth / 2f + 0.8f, hlW, hlH);
                g.FillRectangle(hlBrush, vLength / 2f - 2f, vWidth / 2f - 2.8f, hlW, hlH);
            }

            // 5. Задние фонари и стоп-сигналы
            if (car.Speed < 0.5)
            {
                // Активные стоп-сигналы при торможении со свечением
                using (var glowBrake = new SolidBrush(Color.FromArgb(120, 255, 0, 0)))
                using (var brakeBrush = new SolidBrush(Color.FromArgb(255, 255, 30, 30)))
                {
                    g.FillEllipse(glowBrake, -vLength / 2f - 3f, -vWidth / 2f - 1f, 5f, 5f);
                    g.FillEllipse(glowBrake, -vLength / 2f - 3f, vWidth / 2f - 4f, 5f, 5f);

                    g.FillRectangle(brakeBrush, -vLength / 2f - 0.5f, -vWidth / 2f + 0.8f, 2f, 2.2f);
                    g.FillRectangle(brakeBrush, -vLength / 2f - 0.5f, vWidth / 2f - 3.0f, 2f, 2.2f);
                }
            }
            else
            {
                // Стандартные задние габаритные огни
                using (var tailBrush = new SolidBrush(Color.FromArgb(210, 170, 20, 20)))
                {
                    g.FillRectangle(tailBrush, -vLength / 2f, -vWidth / 2f + 0.8f, 1.8f, 2.0f);
                    g.FillRectangle(tailBrush, -vLength / 2f, vWidth / 2f - 2.8f, 1.8f, 2.0f);
                }
            }

            g.Restore(state);
        }
    }
}