using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SAi_KR
{
    public class Lane
    {
        public string Id { get; }
        public double Length { get; private set; }
        public double SpeedLimit { get; }
        public List<Vehicle> Vehicles { get; } = new List<Vehicle>();
        public Lane? NextLane { get; set; }
        public List<Lane> AlternativeNextLanes { get; } = new List<Lane>();
        
        public int LaneIndex { get; set; }

        public List<PointF> Waypoints { get; } = new List<PointF>();

        private double _cachedPixelLength = 0;

        public Lane(string id, double speedLimit = 16.6)
        {
            Id = id;
            SpeedLimit = speedLimit;
        }

        public void SetPath(params PointF[] points)
        {
            Waypoints.Clear();
            Waypoints.AddRange(points);
            _cachedPixelLength = GetTotalPixelLength();
            Length = Math.Max(5.0, _cachedPixelLength / SimulationEngine.PxPerMeter);
        }

        public double GetTotalPixelLength()
        {
            double len = 0;
            for (int i = 0; i < Waypoints.Count - 1; i++)
            {
                float dx = Waypoints[i + 1].X - Waypoints[i].X;
                float dy = Waypoints[i + 1].Y - Waypoints[i].Y;
                len += Math.Sqrt(dx * dx + dy * dy);
            }
            return len;
        }

        [Obsolete("Use GetRenderPositionWithAngle instead for better oriented rendering.")]
        public (PointF Position, bool IsVertical) GetRenderPosition(double distance)
        {
            if (Waypoints.Count < 2)
                return (new PointF(0, 0), false);

            double dPx = Math.Max(0.0, Math.Min(distance * SimulationEngine.PxPerMeter, GetTotalPixelLength()));

            for (int i = 0; i < Waypoints.Count - 1; i++)
            {
                float dx = Waypoints[i + 1].X - Waypoints[i].X;
                float dy = Waypoints[i + 1].Y - Waypoints[i].Y;
                double segLen = Math.Sqrt(dx * dx + dy * dy);

                if (dPx <= segLen || i == Waypoints.Count - 2)
                {
                    float t = segLen > 0.001 ? (float)(dPx / segLen) : 0f;
                    t = Math.Max(0f, Math.Min(1f, t));
                    PointF pt = new PointF(Waypoints[i].X + dx * t, Waypoints[i].Y + dy * t);
                    bool isVert = Math.Abs(dy) > Math.Abs(dx);
                    return (pt, isVert);
                }
                dPx -= segLen;
            }

            return (Waypoints.Last(), false);
        }

        public (PointF Position, double Angle) GetRenderPositionWithAngle(double distance)
        {
            if (Waypoints.Count < 2)
                return (new PointF(0, 0), 0.0);

            double totalPx = _cachedPixelLength > 0 ? _cachedPixelLength : GetTotalPixelLength();
            double dPx = Math.Max(0.0, Math.Min(distance * SimulationEngine.PxPerMeter, totalPx));

            for (int i = 0; i < Waypoints.Count - 1; i++)
            {
                float dx = Waypoints[i + 1].X - Waypoints[i].X;
                float dy = Waypoints[i + 1].Y - Waypoints[i].Y;
                double segLen = Math.Sqrt(dx * dx + dy * dy);

                if (dPx <= segLen || i == Waypoints.Count - 2)
                {
                    float t = segLen > 0.001 ? (float)(dPx / segLen) : 0f;
                    t = Math.Max(0f, Math.Min(1f, t));
                    PointF pt = new PointF(Waypoints[i].X + dx * t, Waypoints[i].Y + dy * t);
                    double angle = Math.Atan2(dy, dx);
                    return (pt, angle);
                }
                dPx -= segLen;
            }

            if (Waypoints.Count >= 2)
            {
                float dx = Waypoints[Waypoints.Count - 1].X - Waypoints[Waypoints.Count - 2].X;
                float dy = Waypoints[Waypoints.Count - 1].Y - Waypoints[Waypoints.Count - 2].Y;
                return (Waypoints.Last(), Math.Atan2(dy, dx));
            }

            return (Waypoints.Last(), 0.0);
        }

        public void Update(double dt, bool isBlocked)
        {
            for (int i = 0; i < Vehicles.Count; i++)
            {
                Vehicle current = Vehicles[i];
                double leaderGap;
                double leaderSpeed;

                if (i > 0)
                {
                    Vehicle leader = Vehicles[i - 1];
                    leaderGap = leader.Position - current.Position - leader.Length;
                    leaderSpeed = leader.Speed;

                    double minGap = (leader.Length / 2.0) + 1.5;
                    if (current.Position >= leader.Position - leader.Length - minGap)
                    {
                        current.Position = Math.Max(0.0, leader.Position - leader.Length - minGap);
                        current.Speed = Math.Min(current.Speed, leader.Speed);
                    }
                }
                else
                {
                    if (isBlocked)
                    {
                        double stopLineBuffer = current.Length / 2.0;
                        leaderGap = Math.Max(0.1, Length - current.Position - stopLineBuffer);
                        leaderSpeed = 0.0;

                        if (current.Position >= Length - stopLineBuffer)
                        {
                            current.Position = Length - stopLineBuffer;
                            current.Speed = 0.0;
                        }
                    }
                    else
                    {
                        Lane? lookAheadLane = (AlternativeNextLanes.Count > 0 && (current.Id % 3 == 0)) ? AlternativeNextLanes[0] : NextLane;
                        if (lookAheadLane != null && lookAheadLane.Vehicles.Count > 0)
                        {
                            Vehicle nextLeader = lookAheadLane.Vehicles.Last();
                            double gap = (Length - current.Position) + nextLeader.Position - nextLeader.Length;
                            leaderGap = Math.Max(0.5, gap);
                            leaderSpeed = nextLeader.Speed;
                        }
                        else
                        {
                            leaderGap = 1000.0;
                            leaderSpeed = current.DesiredSpeed;
                        }
                    }
                }

                current.Update(dt, Math.Max(0.1, leaderGap), leaderSpeed);
            }
        }

        public void AdvanceVehicles(List<Vehicle> finishedVehicles)
        {
            while (Vehicles.Count > 0 && Vehicles[0].Position >= Length)
            {
                Vehicle lead = Vehicles[0];
                Lane? targetNext = (AlternativeNextLanes.Count > 0 && (lead.Id % 3 == 0)) ? AlternativeNextLanes[0] : NextLane;

                if (targetNext != null)
                {
                    if (targetNext.Vehicles.Count > 0 && targetNext.Vehicles.Last().Position < 6.0)
                    {
                        lead.Position = Length;
                        lead.Speed = 0.0;
                        break;
                    }

                    Vehicles.RemoveAt(0);
                    lead.Position = Math.Max(0.0, lead.Position - Length);
                    targetNext.Vehicles.Add(lead);
                }
                else
                {
                    Vehicles.RemoveAt(0);
                    finishedVehicles.Add(lead);
                }
            }
        }

        public int GetQueueLength()
        {
            return Vehicles.Count(v => v.Speed < 0.5);
        }
    }
}