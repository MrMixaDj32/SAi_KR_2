using System;
using System.Linq;
using SAi_KR;

namespace SAi_KR
{
    class TestDelay
    {
        public static void Run()
        {
            Console.WriteLine("=== ТЕСТ ПРОИЗВОДИТЕЛЬНОСТИ С РАЗНЫМИ BIAS / ВЕСАМИ ===");

            var engineFixed = new SimulationEngine();
            engineFixed.ResetWithSeed(42);
            engineFixed.InflowSevernayaWest = 1600;
            engineFixed.InflowSevernayaEast = 1400;
            engineFixed.CurrentControlMode = ControlMode.FixedTime;

            for (int i = 0; i < 3000; i++) engineFixed.Step(0.1);
            Console.WriteLine($"[1] ФИКСИРОВАННЫЙ: Задержка = {engineFixed.GetAverageWaitTime():F2} с | Очередь = {engineFixed.GetTotalQueue()} | Проехало = {engineFixed.CompletedVehicles.Count}");

            // Test AI with default BiasOutput = -0.8
            var engineAI = new SimulationEngine();
            engineAI.ResetWithSeed(42);
            engineAI.InflowSevernayaWest = 1600;
            engineAI.InflowSevernayaEast = 1400;
            engineAI.CurrentControlMode = ControlMode.IntelligentAgents;

            for (int i = 0; i < 3000; i++) engineAI.Step(0.1);
            Console.WriteLine($"[2] ИИ (По умолчанию): Задержка = {engineAI.GetAverageWaitTime():F2} с | Очередь = {engineAI.GetTotalQueue()} | Проехало = {engineAI.CompletedVehicles.Count}");

            Console.WriteLine("=== ЛОГ ПЕРЕКЛЮЧЕНИЙ КРАСНОЙ (УЗЕЛ 2) В ИИ РЕЖИМЕ ===");
            var testKras = new SimulationEngine();
            testKras.ResetWithSeed(42);
            testKras.CurrentControlMode = ControlMode.IntelligentAgents;
            int lastPh = -1;
            double lastT = 0;
            for (int i = 0; i < 2000; i++)
            {
                testKras.Step(0.1);
                var nodeKras = testKras.Intersections[2];
                if (nodeKras.CurrentPhaseIndex != lastPh)
                {
                    if (lastPh != -1)
                    {
                        double dur = testKras.SimulationTime - lastT;
                        int qKras = nodeKras.IncomingLanes.Skip(4).Take(3).Sum(l => l.GetQueueLength());
                        int qSev = nodeKras.IncomingLanes.Take(4).Sum(l => l.GetQueueLength());
                        Console.WriteLine($"[T={testKras.SimulationTime,5:F1}с] Красная: Фаза {lastPh} длилась {dur,4:F1}с -> Фаза {nodeKras.CurrentPhaseIndex} (Очередь Красной: {qKras}, Очередь Северной: {qSev})");
                    }
                    lastPh = nodeKras.CurrentPhaseIndex;
                    lastT = testKras.SimulationTime;
                }
            }

            Console.WriteLine("=== ТЕСТ ПОТОКОВ: 1000, 1400, 1800, 2000 АВТ/Ч ===");
            double[] testFlows = { 1000, 1400, 1800, 2000 };
            foreach (double flow in testFlows)
            {
                var ef = new SimulationEngine();
                ef.ResetWithSeed(42);
                ef.InflowSevernayaWest = flow;
                ef.InflowSevernayaEast = flow * 0.9;
                ef.CurrentControlMode = ControlMode.FixedTime;
                for (int i = 0; i < 3000; i++) ef.Step(0.1);

                var eAI = new SimulationEngine();
                eAI.ResetWithSeed(42);
                eAI.InflowSevernayaWest = flow;
                eAI.InflowSevernayaEast = flow * 0.9;
                eAI.CurrentControlMode = ControlMode.IntelligentAgents;
                for (int i = 0; i < 3000; i++) eAI.Step(0.1);

                Console.WriteLine($"[ПОТОК {flow,4}] ФИКС: Задержка = {ef.GetAverageWaitTime():F2}с, Проехало = {ef.CompletedVehicles.Count,3} | ИИ: Задержка = {eAI.GetAverageWaitTime():F2}с, Проехало = {eAI.CompletedVehicles.Count,3}");
            }
        }
    }
}
