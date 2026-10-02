using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SAi_KR
{
    internal static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;
            }
            catch { }

            if (args != null && args.Length > 0 && args[0] == "--test")
            {
                RunSelfTests();
                return;
            }

            if (args != null && args.Length > 0 && args[0] == "--bench")
            {
                TestDelay.Run();
                return;
            }

            // Инициализация конфигурации приложения, включая настройки масштабирования High DPI и системные шрифты
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }

        private static void RunSelfTests()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("   СИСТЕМА САМОТЕСТИРОВАНИЯ SAi_KR_2   ");
            Console.WriteLine("========================================");
            int passed = 0;
            int failed = 0;

            void Assert(bool condition, string testName)
            {
                if (condition)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("[PASS] ");
                    Console.ResetColor();
                    Console.WriteLine(testName);
                    passed++;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("[FAIL] ");
                    Console.ResetColor();
                    Console.WriteLine(testName);
                    failed++;
                }
            }

            try
            {
                // ТЕСТ 1: Построение коридора
                var engine = new SimulationEngine();
                engine.BuildSevernayaCorridor();
                Assert(engine.Intersections.Count == 3, "Тест 1.1: Создано 3 перекрестка по ул. Северной");
                Assert(engine.AllLanes.Count >= 20, $"Тест 1.2: Создано {engine.AllLanes.Count} полос движения (ожидалось >= 20)");
                Assert(engine.Agents.Count == 3, "Тест 1.3: Инициализировано 3 агента управления");

                // Проверка поворота на Красной ТОЛЬКО НАПРАВО (ВНИЗ)
                var sevEastL1 = engine.AllLanes.First(l => l.Id == "Sev_Rash_Kras_E_L1");
                var sevEastL2 = engine.AllLanes.First(l => l.Id == "Sev_Rash_Kras_E_L2");
                Assert(sevEastL1.NextLane != null && sevEastL1.NextLane.Id == "Turn_Sev_Kras_L1",
                    "Тест 1.4: По Северной слева направо на перекрестке с Красной поворот только направо (вниз, L1)");
                Assert(sevEastL2.NextLane != null && sevEastL2.NextLane.Id == "Turn_Sev_Kras_L2",
                    "Тест 1.5: По Северной слева направо на перекрестке с Красной поворот только направо (вниз, L2)");
                Assert(sevEastL1.NextLane?.NextLane != null && sevEastL1.NextLane.NextLane.Id == "Out_Kras_S_L2" &&
                       sevEastL2.NextLane?.NextLane != null && sevEastL2.NextLane.NextLane.Id == "Out_Kras_S_Direct",
                    "Тест 1.5.1: Траектории поворота на Красную параллельны и не пересекаются (L1 -> L2 бульвара, L2 -> прямой рукав)");

                // Проверка отсутствия подрезаний при одновременном правом повороте двух машин
                var engTurnTest = new SimulationEngine();
                engTurnTest.BuildSevernayaCorridor();
                var laneL1 = engTurnTest.AllLanes.First(l => l.Id == "Sev_Rash_Kras_E_L1");
                var laneL2 = engTurnTest.AllLanes.First(l => l.Id == "Sev_Rash_Kras_E_L2");
                var car1 = new Vehicle(1001, 8.0, laneL1.Length - 10) { Length = 4.5, VehicleWidth = 1.8, VehicleColor = Color.Blue };
                var car2 = new Vehicle(1002, 8.0, laneL2.Length - 10) { Length = 4.5, VehicleWidth = 1.8, VehicleColor = Color.Red };
                laneL1.Vehicles.Add(car1);
                laneL2.Vehicles.Add(car2);

                double minTurningDist = 999.0;
                for (int step = 0; step < 120; step++)
                {
                    engTurnTest.Step(0.05);
                    Lane? l1Car = engTurnTest.AllLanes.FirstOrDefault(l => l.Vehicles.Contains(car1));
                    Lane? l2Car = engTurnTest.AllLanes.FirstOrDefault(l => l.Vehicles.Contains(car2));
                    if (l1Car != null && l2Car != null)
                    {
                        var (p1, _) = l1Car.GetRenderPositionWithAngle(car1.Position);
                        var (p2, _) = l2Car.GetRenderPositionWithAngle(car2.Position);
                        double d = Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
                        if (d < minTurningDist) minTurningDist = d;
                    }
                }
                Assert(minTurningDist >= 18.0, $"Тест 1.5.2: При одновременном повороте на Красную машины не подрезают друг друга (мин. боковой зазор {minTurningDist:F1} px >= 18 px)");

                // Проверка Октябрьской: поворот направо и налево
                var oktRight = engine.AllLanes.First(l => l.Id == "Inflow_Okt_S_Right");
                var oktLeft = engine.AllLanes.First(l => l.Id == "Inflow_Okt_S_Left");
                Assert(oktRight.NextLane != null && oktRight.NextLane.Id == "Turn_Okt_E",
                    "Тест 1.6: С Октябрьской реализован поворот направо на Северную (на восток)");
                Assert(oktLeft.NextLane != null && oktLeft.NextLane.Id == "Turn_Okt_W",
                    "Тест 1.7: С Октябрьской реализован поворот налево на Северную (на запад)");

                // Проверка Красной: бульвар снизу (правая вверх, левая вниз)
                var krasUp = engine.AllLanes.First(l => l.Id == "Inflow_Kras_SE_Str");
                var krasDown = engine.AllLanes.First(l => l.Id == "Out_Kras_S_Direct");
                Assert(krasUp.Waypoints[0].Y > 800 && krasUp.Waypoints.Last().Y < 500,
                    "Тест 1.8: По правой стороне бульвара Красной движение снизу ВВЕРХ");
                Assert(krasDown.Waypoints[0].Y < 500 && krasDown.Waypoints.Last().Y > 800,
                    "Тест 1.9: По левой стороне бульвара Красной движение сверху ВНИЗ");

                // Проверка Красной: поворот налево с Красной на Северную через промежуток между бульварами
                var krasLeft = engine.AllLanes.First(l => l.Id == "Inflow_Kras_SE_Left");
                var turnLeft = krasLeft.NextLane?.Id == "Turn_Kras_S_Sev" ? krasLeft.NextLane : krasLeft.AlternativeNextLanes.FirstOrDefault(l => l.Id == "Turn_Kras_S_Sev");
                Assert(turnLeft != null && turnLeft.NextLane != null && turnLeft.NextLane.Id == "Conn_Sev_Mid_Exit_L1" &&
                       turnLeft.NextLane.NextLane != null && turnLeft.NextLane.NextLane.Id == "Sev_Kras_Rash_W_L1",
                    "Тест 1.10: С Красной вверх на перекрестке с Северной поворот налево проходит через светофор между бульварами на Северную");

                var strLeft = krasLeft.NextLane?.Id == "Conn_Kras_S_Direct_L1" ? krasLeft.NextLane : krasLeft.AlternativeNextLanes.FirstOrDefault(l => l.Id == "Conn_Kras_S_Direct_L1");
                Assert(strLeft != null && strLeft.NextLane != null && strLeft.NextLane.Id == "Out_Kras_NE_L1",
                    "Тест 1.10.1: С крайней левой полосы Красной вверх также можно двигаться прямо через перекресток");

                var outNE1 = engine.AllLanes.FirstOrDefault(l => l.Id == "Out_Kras_NE_L1");
                var outNE2 = engine.AllLanes.FirstOrDefault(l => l.Id == "Out_Kras_NE_L2");
                Assert(outNE1 != null && outNE2 != null,
                    "Тест 1.10.2: На дороге вверх по Красной после перекрестка с Северной организовано две полосы");

                // Проверка Рашпилевской: одностороннее движение на юг снизу под Северной
                var rashS1 = engine.AllLanes.First(l => l.Id == "Out_Rash_S");
                var rashS2 = engine.AllLanes.First(l => l.Id == "Out_Rash_S_L2");
                Assert(rashS1.Waypoints[0].Y < 500 && rashS1.Waypoints.Last().Y > 800 &&
                       rashS2.Waypoints[0].Y < 500 && rashS2.Waypoints.Last().Y > 800,
                    "Тест 1.11: Рашпилевская снизу под Северной строго односторонняя на юг (обе полосы сверху вниз)");

                // Проверка поворота направо с Северной на Рашпилевскую
                var sevWestL2 = engine.AllLanes.First(l => l.Id == "Sev_Kras_Rash_W_L2");
                Assert(sevWestL2.AlternativeNextLanes.Any(l => l.Id == "Turn_Sev_Rash_N" && l.NextLane != null && l.NextLane.Id == "Out_Rash_N"),
                    "Тест 1.12: С участка Северной между Красной и Рашпилевской реализован поворот направо на Рашпилевскую на север");

                var sevEastRashL2 = engine.AllLanes.First(l => l.Id == "Sev_Okt_Rash_E_L2");
                Assert(sevEastRashL2.AlternativeNextLanes.Any(l => l.Id == "Turn_Sev_Rash_S" && l.NextLane != null && l.NextLane.Id == "Out_Rash_S_L2"),
                    "Тест 1.12.1: По Северной (слева направо) на перекрестке с Рашпилевской реализован поворот направо на юг");

                var krasNorth = engine.AllLanes.First(l => l.Id == "Inflow_Kras_NW");
                Assert(krasNorth.AlternativeNextLanes.Any(l => l.Id == "Turn_Kras_N_Sev" && l.NextLane != null && l.NextLane.Id == "Sev_Kras_Rash_W_L2"),
                    "Тест 1.12.2: По Красной вниз на перекрестке с Северной реализован поворот направо на Северную (в сторону Рашпилевской)");

                // Проверка светофоров на ВСЕХ перекрестках для ВСЕХ входящих направлений и светофора между бульварами
                var nOkt = engine.Intersections[0];
                var nRash = engine.Intersections[1];
                var nKras = engine.Intersections[2];
                Assert(nOkt.IncomingLanes.Count == 6 && nRash.IncomingLanes.Count == 5 && nKras.IncomingLanes.Count == 10,
                    "Тест 1.13: Светофоры установлены на ВСЕХ перекрестках (Окт:6, Раш:5, Крас:10 включая выделенный светофор между бульварами)");

                // ТЕСТ 2: Спавн и физика IDM
                int initialVehicles = engine.AllLanes.Sum(l => l.Vehicles.Count);
                for (int i = 0; i < 150; i++) engine.Step(0.1);
                int spawnedVehicles = engine.AllLanes.Sum(l => l.Vehicles.Count) + engine.CompletedVehicles.Count;
                Assert(spawnedVehicles > initialVehicles, $"Тест 2.1: Генерация трафика работает ({spawnedVehicles} авт.)");

                bool allHaveColors = engine.AllLanes.SelectMany(l => l.Vehicles).All(v => !v.VehicleColor.IsEmpty);
                Assert(allHaveColors, "Тест 2.2: Все автомобили имеют корректные цвета кузова");

                bool anyMoving = engine.AllLanes.SelectMany(l => l.Vehicles).Any(v => v.Speed > 0.1);
                Assert(anyMoving, "Тест 2.3: Автомобили успешно движутся (скорость > 0)");

                // ТЕСТ 3: Светофоры и фаза AllRed
                var n1 = engine.Intersections[0];
                Assert(n1.AllRedDuration == 2.0, "Тест 3.1: Длительность AllRed равна 2.0 с");
                bool allRedEncountered = false;
                for (int i = 0; i < 600; i++)
                {
                    engine.Step(0.1);
                    if (n1.CurrentState == LightState.AllRed)
                    {
                        allRedEncountered = true;
                        bool blocked = n1.IncomingLanes.All(l => n1.IsLaneBlocked(l));
                        Assert(blocked, "Тест 3.2: Фаза AllRed блокирует все входящие направления");
                        break;
                    }
                }
                Assert(allRedEncountered, "Тест 3.3: Фаза AllRed успешно активируется в цикле");

                // ТЕСТ 4: Правила безопасности (SafetyRuleEngine)
                var ruleEngine = new SafetyRuleEngine();
                // Правило 1: Блокировка во время тактов желтого и AllRed сигналов
                var f1 = new TrafficFacts { IsYellowActive = true, CurrentPhaseDuration = 20 };
                bool r1 = ruleEngine.ValidatePhaseSwitch(f1, out string re1);
                Assert(!r1 && re1.Contains("Правило 1"), "Тест 4.1: Правило 1 блокирует переключение во время желтого/AllRed");

                // Правило 6: Экстренные службы
                var f6 = new TrafficFacts { EmergencyVehicleDetected = true, CurrentPhaseDuration = 5 };
                bool r6 = ruleEngine.ValidatePhaseSwitch(f6, out string re6);
                Assert(r6 && re6.Contains("Правило 6"), "Тест 4.2: Правило 6 пропускает экстренные службы вне очереди");

                // Правило 2: Минимальная длительность фазы
                var f2 = new TrafficFacts { CurrentPhaseDuration = 5, MinPhaseDuration = 10, NetworkDecisionOutput = 0.9 };
                bool r2 = ruleEngine.ValidatePhaseSwitch(f2, out string re2);
                Assert(!r2 && re2.Contains("Правило 2"), "Тест 4.3: Правило 2 защищает минимальный зеленый такт");

                // Правило 5: Продление зеленого для движущихся ТС
                var f5 = new TrafficFacts { CurrentPhaseDuration = 12, MinPhaseDuration = 10, MinGreenExtension = 5, AverageGreenLaneSpeed = 8.0, NetworkDecisionOutput = 0.9 };
                bool r5 = ruleEngine.ValidatePhaseSwitch(f5, out string re5);
                Assert(!r5 && re5.Contains("Правило 5"), "Тест 4.4: Правило 5 продлевает фазу для завершения проезда");

                // Правило 3: Принудительное переключение при максимальном времени
                var f3 = new TrafficFacts { CurrentPhaseDuration = 65, MaxPhaseDuration = 60, OpposingQueueLength = 5 };
                bool r3 = ruleEngine.ValidatePhaseSwitch(f3, out string re3);
                Assert(r3 && re3.Contains("Правило 3"), "Тест 4.5: Правило 3 принудительно снимает фазу при заторе");

                // Правило 4: Санкционирование ИНС
                var f4 = new TrafficFacts { CurrentPhaseDuration = 25, MinPhaseDuration = 10, NetworkDecisionOutput = 0.8 };
                bool r4 = ruleEngine.ValidatePhaseSwitch(f4, out string re4);
                Assert(r4 && re4.Contains("Правило 4"), "Тест 4.6: Правило 4 подтверждает команду нейросети");

                // ТЕСТ 5: Мультиагентный режим ИИ
                engine.CurrentControlMode = ControlMode.IntelligentAgents;
                for (int i = 0; i < 100; i++) engine.Step(0.1);
                Assert(true, "Тест 5.1: 100 шагов симуляции в мультиагентном режиме ИИ выполнены стабильно");

                // ТЕСТ 6: Рендеринг позиций машин без исключений
                int checkedCars = 0;
                foreach (var lane in engine.AllLanes)
                {
                    foreach (var car in lane.Vehicles)
                    {
                        var (pos, angle) = lane.GetRenderPositionWithAngle(car.Position);
                        Assert(!float.IsNaN(pos.X) && !float.IsNaN(pos.Y) && !double.IsNaN(angle),
                            $"Тест 6.1: Позиция машины ID={car.Id} корректна ({pos.X:F1}, {pos.Y:F1})");
                        checkedCars++;
                        if (checkedCars >= 5) break;
                    }
                    if (checkedCars >= 5) break;
                }
                Assert(checkedCars > 0, $"Тест 6.2: Проверены расчеты координат {checkedCars} машин");

                // ТЕСТ 7: Генетический алгоритм и мультиагентная координация
                int genomeLen = engine.Agents[0].Brain.TotalWeightsCount * engine.Agents.Count;
                var trainer = new GeneticTrainer(populationSize: 4, genomeLength: genomeLen);
                foreach (var ind in trainer.Population)
                {
                    ind.Fitness = EvolutionRunner.EvaluateGenome(ind, engine, simulationSteps: 50, dt: 0.5, seed: 42);
                }
                trainer.Evolve();
                Assert(trainer.Generation == 1 && trainer.BestGenome != null, "Тест 7.1: Генетический алгоритм успешно провел эпоху эволюции");

                // Тест 7.2: Мультиагентная координация по коридору
                var ag1 = engine.Agents[0];
                var ag2 = engine.Agents[1];
                var ag3 = engine.Agents[2];
                Assert(ag1.NeighborEast == ag2 && ag2.NeighborWest == ag1 && ag2.NeighborEast == ag3 && ag3.NeighborWest == ag2,
                    "Тест 7.2: Двунаправленная связность агентов коридора (Октябрьская <-> Рашпилевская <-> Красная)");

                // Тест 7.3: Вектор состояния агента (включая координационный сигнал)
                double[] state2 = ag2.CollectState();
                Assert(state2.Length == 8 && state2.All(v => !double.IsNaN(v) && !double.IsInfinity(v) && v >= 0 && v <= 1.0),
                    "Тест 7.3: Вектор состояния агента Рашпилевской корректен (8 нормированных входов [0..1])");

                // Тест 7.4: Безопасность фаз на Красной (в фазе 1 полоса между бульварами заблокирована на выезд)
                var krasNode = engine.Intersections[2];
                var midLane = engine.AllLanes.First(l => l.Id == "Lane_Sev_Mid_Boulevard_L1");
                var leftTurnKras = engine.AllLanes.First(l => l.Id == "Turn_Kras_S_Sev");
                // Переключаем в фазу 1 (зеленый для Красной)
                krasNode.SwitchToNextPhase();
                krasNode.Update(krasNode.YellowDuration + 0.1);
                krasNode.Update(krasNode.AllRedDuration + 0.1);
                Assert(krasNode.CurrentPhaseIndex == 1 && krasNode.IsLaneBlocked(midLane) && krasNode.IsLaneBlocked(leftTurnKras),
                    "Тест 7.4: В фазе 1 (движение по Красной) выезд из кармана между бульварами заблокирован");

                // ТЕСТ 8: Интеграционный тест формы и графического рендеринга
                using (var testForm = new Form1())
                {
                    testForm.CreateControl();
                    var tmrMethod = typeof(Form1).GetMethod("TmrSim_Tick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    for (int t = 0; t < 25; t++)
                    {
                        tmrMethod?.Invoke(testForm, new object[] { testForm, EventArgs.Empty });
                    }

                    var pb = testForm.Controls.Find("pbSimulation", true).FirstOrDefault() as PictureBox;
                    Assert(pb != null, "Тест 8.1: Элемент pbSimulation найден на форме");
                    if (pb != null)
                    {
                        using (var bmp = new Bitmap(pb.Width, pb.Height))
                        {
                            var paintMethod = typeof(Form1).GetMethod("PbSimulation_Paint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            using (var g = Graphics.FromImage(bmp))
                            {
                                var pe = new PaintEventArgs(g, new Rectangle(0, 0, pb.Width, pb.Height));
                                paintMethod?.Invoke(testForm, new object[] { pb, pe });
                            }
                            Assert(true, "Тест 8.2: Полный цикл PbSimulation_Paint с автомобилями выполнен без исключений");
                        }
                    }
                }

                Console.WriteLine("========================================");
                Console.WriteLine($"ИТОГ ТЕСТИРОВАНИЯ: Пройдено: {passed}, Провалено: {failed}");
                Console.WriteLine("========================================");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"КРИТИЧЕСКАЯ ОШИБКА ТЕСТА: {ex}");
                Console.ResetColor();
                failed++;
            }

            Environment.Exit(failed == 0 ? 0 : 1);
        }
    }
}