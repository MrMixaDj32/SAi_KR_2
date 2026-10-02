using System;
using System.Linq;

namespace SAi_KR
{
    public class TrafficAgent
    {
        public Intersection Node { get; }
        public NeuralNetwork Brain { get; }
        public SafetyRuleEngine RuleEngine { get; }

        public TrafficAgent? NeighborWest { get; set; }
        public TrafficAgent? NeighborEast { get; set; }

        public string CurrentDecisionText { get; set; } = "Ожидание такта";
        public double CurrentBrainOutput { get; set; } = 0.0;
        public bool IsCoordinatingWave { get; set; } = false;
        public int LastOpposingQueue { get; set; } = 0;
        public int LastGreenQueue { get; set; } = 0;

        public TrafficAgent(Intersection node, Random rand)
        {
            Node = node;
            Brain = new NeuralNetwork(8, 8, 1, rand);
            RuleEngine = new SafetyRuleEngine();
        }

        public double[] CollectState()
        {
            double[] state = new double[8];
            int currentPhase = Node.CurrentPhaseIndex;

            // 1. Очередь на разрешенном (зеленом) направлении
            int greenQueue = 0;
            double greenSpeedSum = 0;
            int greenVehCount = 0;
            if (currentPhase < Node.Phases.Count)
            {
                foreach (int laneIdx in Node.Phases[currentPhase].GreenLaneIndices)
                {
                    if (laneIdx < Node.IncomingLanes.Count)
                    {
                        var l = Node.IncomingLanes[laneIdx];
                        greenQueue += l.GetQueueLength();
                        if (l.Vehicles.Count > 0)
                        {
                            greenSpeedSum += l.Vehicles.Sum(v => v.Speed);
                            greenVehCount += l.Vehicles.Count;
                        }
                    }
                }
            }
            state[0] = Math.Min(1.0, greenQueue / 30.0);

            // 2. Очередь на запрещенном (красном) направлении
            int opposingQueue = 0;
            double redSpeedSum = 0;
            int redVehCount = 0;
            for (int i = 0; i < Node.IncomingLanes.Count; i++)
            {
                if (currentPhase < Node.Phases.Count && !Node.Phases[currentPhase].GreenLaneIndices.Contains(i))
                {
                    var l = Node.IncomingLanes[i];
                    opposingQueue += l.GetQueueLength();
                    if (l.Vehicles.Count > 0)
                    {
                        redSpeedSum += l.Vehicles.Sum(v => v.Speed);
                        redVehCount += l.Vehicles.Count;
                    }
                }
            }
            state[1] = Math.Min(1.0, opposingQueue / 30.0);

            // 3. Средняя скорость на разрешенных полосах
            state[2] = greenVehCount > 0 ? Math.Min(1.0, (greenSpeedSum / greenVehCount) / 16.6) : 1.0;

            // 4. Средняя скорость на запрещенных полосах
            state[3] = redVehCount > 0 ? Math.Min(1.0, (redSpeedSum / redVehCount) / 16.6) : 0.0;

            // 5. Тип активной фазы (1.0 = Северная магистраль, 0.0 = боковая улица)
            bool isMajor = currentPhase == 0;
            state[4] = isMajor ? 1.0 : 0.0;

            // 6. Относительное время фазы
            state[5] = Math.Min(1.0, Node.TimeInCurrentPhase / 60.0);

            // 7. Очередь на соседних узлах по коридору
            int neighborIncomingQueue = 0;
            if (NeighborEast != null && NeighborEast.Node.IncomingLanes.Count >= 4)
                neighborIncomingQueue += NeighborEast.Node.IncomingLanes.Skip(2).Take(2).Sum(l => l.GetQueueLength());
            if (NeighborWest != null && NeighborWest.Node.IncomingLanes.Count >= 2)
                neighborIncomingQueue += NeighborWest.Node.IncomingLanes.Take(2).Sum(l => l.GetQueueLength());
            state[6] = Math.Min(1.0, neighborIncomingQueue / 40.0);

            // 8. Разность транспортного давления (градиент очереди): (очередь_красный - очередь_зеленый)
            double deltaQ = (opposingQueue - greenQueue) / 30.0;
            state[7] = Math.Max(0.0, Math.Min(1.0, (deltaQ + 1.0) / 2.0));

            return state;
        }

        public (bool SwitchRequested, string Reason) Decide()
        {
            double[] inputs = CollectState();
            double[] output = Brain.Forward(inputs);

            int currentPhase = Node.CurrentPhaseIndex;
            bool isMajor = currentPhase == 0;

            int opposingQueue = 0;
            for (int i = 0; i < Node.IncomingLanes.Count; i++)
            {
                if (currentPhase < Node.Phases.Count && !Node.Phases[currentPhase].GreenLaneIndices.Contains(i))
                    opposingQueue += Node.IncomingLanes[i].GetQueueLength();
            }

            int greenQueue = 0;
            int approachingPlatoon = 0;
            double greenSpeedSum = 0;
            int greenVehiclesCount = 0;
            if (currentPhase < Node.Phases.Count)
            {
                foreach (int laneIdx in Node.Phases[currentPhase].GreenLaneIndices)
                {
                    if (laneIdx < Node.IncomingLanes.Count)
                    {
                        var l = Node.IncomingLanes[laneIdx];
                        greenQueue += l.GetQueueLength();
                        approachingPlatoon += l.Vehicles.Count(v => v.Speed > 1.5);
                        if (l.Vehicles.Count > 0)
                        {
                            greenSpeedSum += l.Vehicles.Sum(v => v.Speed);
                            greenVehiclesCount += l.Vehicles.Count;
                        }
                    }
                }
            }
            double averageGreenSpeed = greenVehiclesCount > 0 ? (greenSpeedSum / greenVehiclesCount) : 0;

            // Адаптивные границы фазы:
            // Для магистрали Северной: сохраняем высокую пропускную способность (минимум 25с, максимум 150%)
            // Для второстепенных боковых улиц: гарантируем достаточный такт для полного сброса потока
            // Для Красной (32с база) minGreen = 20.8с, maxGreen = 43.2с; для Октябрьской (22с база) minGreen = 14.3с
            double baseDuration = (currentPhase < Node.Phases.Count) ? Node.Phases[currentPhase].Duration : 30.0;
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

            // Координация «зеленой волны»: определение реального приближения взвода от соседей
            // Проверяем не только фазу соседа, но и наличие движущихся машин на его зеленых полосах
            bool upstreamWaveApproaching = false;
            if (isMajor) // Координация волны только для магистральной фазы Северной
            {
                if (NeighborWest != null && NeighborWest.Node.CurrentPhaseIndex == 0 && NeighborWest.Node.TimeInCurrentPhase >= 4.0)
                {
                    // Проверяем, есть ли реально движущиеся машины на зеленых полосах соседа
                    int movingFromWest = 0;
                    if (NeighborWest.Node.Phases.Count > 0)
                    {
                        foreach (int li in NeighborWest.Node.Phases[0].GreenLaneIndices)
                        {
                            if (li < NeighborWest.Node.IncomingLanes.Count)
                                movingFromWest += NeighborWest.Node.IncomingLanes[li].Vehicles.Count(v => v.Speed > 1.5);
                        }
                    }
                    if (movingFromWest >= 2) upstreamWaveApproaching = true;
                }
                if (NeighborEast != null && NeighborEast.Node.CurrentPhaseIndex == 0 && NeighborEast.Node.TimeInCurrentPhase >= 4.0)
                {
                    int movingFromEast = 0;
                    if (NeighborEast.Node.Phases.Count > 0)
                    {
                        foreach (int li in NeighborEast.Node.Phases[0].GreenLaneIndices)
                        {
                            if (li < NeighborEast.Node.IncomingLanes.Count)
                                movingFromEast += NeighborEast.Node.IncomingLanes[li].Vehicles.Count(v => v.Speed > 1.5);
                        }
                    }
                    if (movingFromEast >= 2) upstreamWaveApproaching = true;
                }
            }

            var facts = new TrafficFacts
            {
                CurrentPhaseDuration = Node.TimeInCurrentPhase,
                MinPhaseDuration = minGreen,
                MaxPhaseDuration = maxGreen,
                IsYellowActive = Node.CurrentState == LightState.Yellow || Node.CurrentState == LightState.AllRed,
                OpposingQueueLength = opposingQueue,
                GreenQueueLength = greenQueue,
                ApproachingPlatoonCount = approachingPlatoon,
                NetworkDecisionOutput = output[0],
                AverageGreenLaneSpeed = averageGreenSpeed,
                IsMajorCorridorPhase = isMajor,
                IsUpstreamCorridorGreen = upstreamWaveApproaching,
                EmergencyVehicleDetected = false
            };

            bool switchApproved = RuleEngine.ValidatePhaseSwitch(facts, out string reason);
            CurrentDecisionText = reason;
            CurrentBrainOutput = output[0];
            IsCoordinatingWave = upstreamWaveApproaching;
            LastOpposingQueue = opposingQueue;
            LastGreenQueue = greenQueue;

            if (switchApproved)
            {
                Node.SwitchToNextPhase();
            }
            else if (Node.CurrentState == LightState.Green)
            {
                // Сообщаем перекрестку, что ИИ продлевает зеленый до максимального времени фазы
                // Без этого логика перекрестка ограничит фазу базовой длительностью по ГОСТ
                Node.ExtendedGreenLimit = maxGreen;
            }

            return (switchApproved, reason);
        }
    }
}