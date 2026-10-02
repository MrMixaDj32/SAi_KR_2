using System.Collections.Generic;
using System.Linq;

namespace SAi_KR
{
    public enum LightState
    {
        Red,
        Yellow,
        AllRed,
        Green
    }

    public class TrafficPhase
    {
        public List<int> GreenLaneIndices { get; } = new List<int>();
        public double Duration { get; set; }

        public TrafficPhase(double duration, params int[] greenLanes)
        {
            Duration = duration;
            GreenLaneIndices.AddRange(greenLanes);
        }
    }

    public class Intersection
    {
        public string Id { get; }
        public string Name { get; }
        public List<Lane> IncomingLanes { get; } = new List<Lane>();
        public List<Lane> OutgoingLanes { get; } = new List<Lane>();
        public List<TrafficPhase> Phases { get; } = new List<TrafficPhase>();

        public int CurrentPhaseIndex { get; private set; } = 0;
        public LightState CurrentState { get; private set; } = LightState.Green;
        public double TimeInCurrentPhase { get; private set; } = 0.0;
        public double YellowDuration { get; set; } = 3.0;
        public double AllRedDuration { get; set; } = 2.0;

        public double CycleDuration => Phases.Sum(p => p.Duration) + Phases.Count * (YellowDuration + AllRedDuration);

        public double PhaseProgress
        {
            get
            {
                if (Phases.Count == 0) return 0;
                
                double currentDuration = CurrentState switch
                {
                    LightState.Green => Phases[CurrentPhaseIndex].Duration,
                    LightState.Yellow => YellowDuration,
                    LightState.AllRed => AllRedDuration,
                    _ => 1.0
                };
                return currentDuration > 0 ? TimeInCurrentPhase / currentDuration : 0;
            }
        }

        public Intersection(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public void AddIncomingLane(Lane lane) => IncomingLanes.Add(lane);
        public void AddOutgoingLane(Lane lane) => OutgoingLanes.Add(lane);
        public void AddPhase(TrafficPhase phase) => Phases.Add(phase);

        public bool IsLaneBlocked(Lane lane)
        {
            int index = IncomingLanes.IndexOf(lane);
            if (index == -1) return false;

            // На желтый и полностью красный сигнал проезд запрещен всем направлениям
            if (CurrentState == LightState.Yellow || CurrentState == LightState.AllRed)
                return true;

            // На зеленый едут только полосы текущей активной фазы
            return !Phases[CurrentPhaseIndex].GreenLaneIndices.Contains(index);
        }

        /// <summary>
        /// Позволяет агенту ИИ динамически продлить текущую зелёную фазу.
        /// Значение 0 означает использование базовой длительности (ГОСТ-режим).
        /// В ИИ-режиме агент устанавливает значение > базовой длительности для удержания зелёного.
        /// Сбрасывается в 0 при переключении фазы.
        /// </summary>
        public double ExtendedGreenLimit { get; set; } = 0.0;

        public void Update(double dt)
        {
            if (Phases.Count == 0) return;

            TimeInCurrentPhase += dt;

            if (CurrentState == LightState.Green)
            {
                double effectiveDuration = ExtendedGreenLimit > 0 
                    ? ExtendedGreenLimit 
                    : Phases[CurrentPhaseIndex].Duration;
                if (TimeInCurrentPhase >= effectiveDuration)
                {
                    CurrentState = LightState.Yellow;
                    TimeInCurrentPhase = 0.0;
                    ExtendedGreenLimit = 0.0;
                }
            }
            else if (CurrentState == LightState.Yellow)
            {
                if (TimeInCurrentPhase >= YellowDuration)
                {
                    CurrentState = LightState.AllRed;
                    TimeInCurrentPhase = 0.0;
                }
            }
            else if (CurrentState == LightState.AllRed)
            {
                if (TimeInCurrentPhase >= AllRedDuration)
                {
                    CurrentState = LightState.Green;
                    CurrentPhaseIndex = (CurrentPhaseIndex + 1) % Phases.Count;
                    TimeInCurrentPhase = 0.0;
                    ExtendedGreenLimit = 0.0;
                }
            }
        }

        public void SwitchToNextPhase()
        {
            if (CurrentState == LightState.Green)
            {
                CurrentState = LightState.Yellow;
                TimeInCurrentPhase = 0.0;
                ExtendedGreenLimit = 0.0;
            }
        }
    }
}