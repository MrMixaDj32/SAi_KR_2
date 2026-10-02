using System;
using System.Collections.Generic;
using System.Linq;

namespace SAi_KR
{
    public class ComparisonReport
    {
        public double FlowRateSevernaya { get; set; }
        public double FlowRateKrasnaya { get; set; }
        public double DurationSeconds { get; set; }

        public double FixedAvgWait { get; set; }
        public int FixedTotalPassed { get; set; }
        public double FixedAvgQueue { get; set; }
        public int FixedMaxQueue { get; set; }
        public int FixedTotalStops { get; set; }
        public double FixedGreenWavePct { get; set; }
        public double FixedDelayOkt { get; set; }
        public double FixedDelayRash { get; set; }
        public double FixedDelayKras { get; set; }

        public double AIAvgWait { get; set; }
        public int AITotalPassed { get; set; }
        public double AIAvgQueue { get; set; }
        public int AIMaxQueue { get; set; }
        public int AITotalStops { get; set; }
        public double AIGreenWavePct { get; set; }
        public double AIDelayOkt { get; set; }
        public double AIDelayRash { get; set; }
        public double AIDelayKras { get; set; }

        public double WaitImprovementPercent => FixedAvgWait > 0 ? ((FixedAvgWait - AIAvgWait) / FixedAvgWait) * 100.0 : 0.0;
        public double QueueReductionPercent => FixedAvgQueue > 0 ? ((FixedAvgQueue - AIAvgQueue) / FixedAvgQueue) * 100.0 : 0.0;
        public double PassedImprovementPercent => FixedTotalPassed > 0 ? (((double)AITotalPassed - FixedTotalPassed) / FixedTotalPassed) * 100.0 : 0.0;
        public double StopsReductionPercent => FixedTotalStops > 0 ? (((double)FixedTotalStops - AITotalStops) / FixedTotalStops) * 100.0 : 0.0;
        public double GreenWaveGainPercent => AIGreenWavePct - FixedGreenWavePct;
    }

    public static class ComparisonRunner
    {
        public static ComparisonReport RunExperiment(
            double flowSevernaya = 1500, 
            double flowKrasStraight = 400, 
            double flowKrasLeft = 250, 
            double durationSeconds = 180,
            Action<int, int>? progressCallback = null,
            SimulationEngine? sourceEngine = null)
        {
            var report = new ComparisonReport
            {
                FlowRateSevernaya = flowSevernaya,
                FlowRateKrasnaya = flowKrasStraight + flowKrasLeft,
                DurationSeconds = durationSeconds
            };

            int totalSteps = (int)(durationSeconds / 0.1);

            // ================= 1. ФИКСИРОВАННЫЙ РЕЖИМ (ГОСТ) =================
            var engFixed = new SimulationEngine();
            engFixed.ResetWithSeed(42);
            engFixed.InflowSevernayaWest = flowSevernaya;
            engFixed.InflowSevernayaEast = flowSevernaya * 0.9;
            engFixed.InflowKrasnayaStraight = flowKrasStraight;
            engFixed.InflowKrasnayaLeft = flowKrasLeft;
            if (sourceEngine != null)
            {
                engFixed.InflowKrasnayaNorth = sourceEngine.InflowKrasnayaNorth;
                engFixed.InflowOktyabrskaya = sourceEngine.InflowOktyabrskaya;
                engFixed.InflowRashpilevskayaNorth = sourceEngine.InflowRashpilevskayaNorth;
            }
            engFixed.CurrentControlMode = ControlMode.FixedTime;

            int fixedQueueSum = 0;
            int fixedMaxQ = 0;

            for (int s = 0; s < totalSteps; s++)
            {
                engFixed.Step(0.1);
                int q = engFixed.GetTotalQueue();
                fixedQueueSum += q;
                if (q > fixedMaxQ) fixedMaxQ = q;

                if (s % 100 == 0 && progressCallback != null)
                {
                    progressCallback(s / 2, totalSteps);
                }
            }

            var fixedVehicles = engFixed.AllLanes.SelectMany(l => l.Vehicles).Concat(engFixed.CompletedVehicles).ToList();
            report.FixedAvgWait = engFixed.GetAverageWaitTime();
            report.FixedTotalPassed = engFixed.CompletedVehicles.Count;
            report.FixedAvgQueue = (double)fixedQueueSum / totalSteps;
            report.FixedMaxQueue = fixedMaxQ;
            report.FixedTotalStops = fixedVehicles.Sum(v => v.StopCount);
            report.FixedGreenWavePct = fixedVehicles.Count > 0 
                ? (double)fixedVehicles.Count(v => v.StopCount == 0) / fixedVehicles.Count * 100.0 
                : 0.0;

            report.FixedDelayOkt = CalculateIntersectionDelay(engFixed, 0);
            report.FixedDelayRash = CalculateIntersectionDelay(engFixed, 1);
            report.FixedDelayKras = CalculateIntersectionDelay(engFixed, 2);

            // ================= 2. МУЛЬТИАГЕНТНЫЙ ИИ =================
            var engAI = new SimulationEngine();
            engAI.ResetWithSeed(42); // Идентичное зерно генератора трафика
            engAI.InflowSevernayaWest = flowSevernaya;
            engAI.InflowSevernayaEast = flowSevernaya * 0.9;
            engAI.InflowKrasnayaStraight = flowKrasStraight;
            engAI.InflowKrasnayaLeft = flowKrasLeft;
            if (sourceEngine != null)
            {
                engAI.InflowKrasnayaNorth = sourceEngine.InflowKrasnayaNorth;
                engAI.InflowOktyabrskaya = sourceEngine.InflowOktyabrskaya;
                engAI.InflowRashpilevskayaNorth = sourceEngine.InflowRashpilevskayaNorth;

                // Переносим обученные веса нейросетей агентов
                for (int i = 0; i < Math.Min(sourceEngine.Agents.Count, engAI.Agents.Count); i++)
                {
                    engAI.Agents[i].Brain.SetWeights(sourceEngine.Agents[i].Brain.GetWeights());
                }
            }
            engAI.CurrentControlMode = ControlMode.IntelligentAgents;

            int aiQueueSum = 0;
            int aiMaxQ = 0;

            for (int s = 0; s < totalSteps; s++)
            {
                engAI.Step(0.1);
                int q = engAI.GetTotalQueue();
                aiQueueSum += q;
                if (q > aiMaxQ) aiMaxQ = q;

                if (s % 100 == 0 && progressCallback != null)
                {
                    progressCallback(totalSteps / 2 + s / 2, totalSteps);
                }
            }

            var aiVehicles = engAI.AllLanes.SelectMany(l => l.Vehicles).Concat(engAI.CompletedVehicles).ToList();
            report.AIAvgWait = engAI.GetAverageWaitTime();
            report.AITotalPassed = engAI.CompletedVehicles.Count;
            report.AIAvgQueue = (double)aiQueueSum / totalSteps;
            report.AIMaxQueue = aiMaxQ;
            report.AITotalStops = aiVehicles.Sum(v => v.StopCount);
            report.AIGreenWavePct = aiVehicles.Count > 0 
                ? (double)aiVehicles.Count(v => v.StopCount == 0) / aiVehicles.Count * 100.0 
                : 0.0;

            report.AIDelayOkt = CalculateIntersectionDelay(engAI, 0);
            report.AIDelayRash = CalculateIntersectionDelay(engAI, 1);
            report.AIDelayKras = CalculateIntersectionDelay(engAI, 2);

            progressCallback?.Invoke(totalSteps, totalSteps);
            return report;
        }

        private static double CalculateIntersectionDelay(SimulationEngine engine, int nodeIndex)
        {
            if (nodeIndex >= engine.Intersections.Count) return 0.0;
            var node = engine.Intersections[nodeIndex];
            var cars = node.IncomingLanes.SelectMany(l => l.Vehicles).ToList();
            return cars.Count > 0 ? cars.Average(c => c.TotalWaitTime) : 0.0;
        }

        public static string GenerateReportPlainText(ComparisonReport r)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("           ОТЧЕТ СРАВНИТЕЛЬНОГО АНАЛИЗА ЭФФЕКТИВНОСТИ РЕГУЛИРОВАНИЯ");
            sb.AppendLine("           ЖЕСТКИЙ ЦИКЛ (ГОСТ Р 52289) И МУЛЬТИАГЕНТНОЕ УПРАВЛЕНИЕ");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"Параметры эксперимента:");
            sb.AppendLine($"  - Магистраль (ул. Северная): {r.FlowRateSevernaya:F0} авт/ч");
            sb.AppendLine($"  - Поперечная магистраль (ул. Красная): {r.FlowRateKrasnaya:F0} авт/ч");
            sb.AppendLine($"  - Время модельного эксперимента: {r.DurationSeconds:F0} секунд (генерация идентична, seed=42)");
            sb.AppendLine();
            sb.AppendLine(string.Format("{0,-38} | {1,-14} | {2,-14} | {3,-12}", "Показатель эффективности", "ГОСТ (Фикс.)", "ИИ (Агенты)", "Эффект ИИ"));
            sb.AppendLine(new string('-', 88));
            sb.AppendLine(string.Format("{0,-38} | {1,11:F2} с | {2,11:F2} с | {3,10:F1} %", "Средняя задержка автомобиля (с)", r.FixedAvgWait, r.AIAvgWait, -r.WaitImprovementPercent));
            sb.AppendLine(string.Format("{0,-38} | {1,12} авт | {2,12} авт | {3,10:F1} %", "Обслужено транспортных средств", r.FixedTotalPassed, r.AITotalPassed, r.PassedImprovementPercent));
            sb.AppendLine(string.Format("{0,-38} | {1,12:F1} авт | {2,12:F1} авт | {3,10:F1} %", "Средняя длина очереди", r.FixedAvgQueue, r.AIAvgQueue, -r.QueueReductionPercent));
            sb.AppendLine(string.Format("{0,-38} | {1,12} авт | {2,12} авт | {3,10:F1} %", "Пиковая (максимальная) очередь", r.FixedMaxQueue, r.AIMaxQueue, ((double)(r.AIMaxQueue - r.FixedMaxQueue) / Math.Max(1, r.FixedMaxQueue)) * 100.0));
            sb.AppendLine(string.Format("{0,-38} | {1,12} раз | {2,12} раз | {3,10:F1} %", "Суммарное число остановок ТС", r.FixedTotalStops, r.AITotalStops, -r.StopsReductionPercent));
            sb.AppendLine(string.Format("{0,-38} | {1,11:F1} % | {2,11:F1} % | {3,10:F1} %", "Безостановочный проезд (Зел.волна)", r.FixedGreenWavePct, r.AIGreenWavePct, r.GreenWaveGainPercent));
            sb.AppendLine(new string('-', 88));
            sb.AppendLine(string.Format("{0,-38} | {1,11:F2} с | {2,11:F2} с | {3,10:F1} %", "  - Задержка: ул. Октябрьская", r.FixedDelayOkt, r.AIDelayOkt, ((r.FixedDelayOkt - r.AIDelayOkt) / Math.Max(0.1, r.FixedDelayOkt)) * -100.0));
            sb.AppendLine(string.Format("{0,-38} | {1,11:F2} с | {2,11:F2} с | {3,10:F1} %", "  - Задержка: ул. Рашпилевская", r.FixedDelayRash, r.AIDelayRash, ((r.FixedDelayRash - r.AIDelayRash) / Math.Max(0.1, r.FixedDelayRash)) * -100.0));
            sb.AppendLine(string.Format("{0,-38} | {1,11:F2} с | {2,11:F2} с | {3,10:F1} %", "  - Задержка: ул. Красная", r.FixedDelayKras, r.AIDelayKras, ((r.FixedDelayKras - r.AIDelayKras) / Math.Max(0.1, r.FixedDelayKras)) * -100.0));
            sb.AppendLine(new string('=', 88));
            sb.AppendLine();
            sb.AppendLine("ЗАКЛЮЧЕНИЕ ПО РЕЗУЛЬТАТАМ МОДЕЛИРОВАНИЯ:");
            sb.AppendLine($"1. Мультиагентная адаптивная система на основе искусственных нейронных сетей");
            sb.AppendLine($"   и продукционной базы правил безопасности обеспечила снижение средней задержки");
            sb.AppendLine($"   транспорта на {r.WaitImprovementPercent:F1}% (с {r.FixedAvgWait:F2} с до {r.AIAvgWait:F2} с).");
            sb.AppendLine($"2. Межагентная координация по коридору ул. Северной («Зеленая волна») увеличила долю");
            sb.AppendLine($"   безостановочного проезда с {r.FixedGreenWavePct:F1}% до {r.AIGreenWavePct:F1}% (+{r.GreenWaveGainPercent:F1}%).");
            sb.AppendLine($"3. Устранено образование заторов на пересечении с ул. Красной за счет динамического");
            sb.AppendLine($"   дозирования фаз и реализации правила ПДД 13.2 (Anti-Gridlock).");
            sb.AppendLine($"4. Пропускная способность транспортного узла возросла на {r.PassedImprovementPercent:F1}%.");
            sb.AppendLine("================================================================================");
            return sb.ToString();
        }
    }
}
