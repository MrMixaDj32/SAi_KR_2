using System;
using System.Drawing;

namespace SAi_KR
{
    public enum VehicleType
    {
        Sedan,
        SUV,
        Truck
    }

    public class Vehicle
    {
        private static Random rnd => Random.Shared;
        private static readonly Color[] palette = new Color[] 
        { 
            Color.White, Color.Silver, Color.Black, Color.Red, 
            Color.Blue, Color.DarkGreen, Color.Gray, Color.Beige 
        };

        public int Id { get; }
        public VehicleType Type { get; set; }
        public Color VehicleColor { get; set; }
        public double Length { get; set; }
        public double VehicleWidth { get; set; }
        public double DirectionAngle { get; set; }

        public double Position { get; set; }     // Положение на полосе (м)
        public double Speed { get; set; }        // Скорость (м/с)
        public double Acceleration { get; set; } // Ускорение (м/с²)

        // Параметры IDM
        public double DesiredSpeed { get; set; } = 15.0; // 54 км/ч
        public double MinGap { get; set; } = 3.0;        // Дистанция остановки в заторе (3 м)
        public double SafeTimeHeadway { get; set; } = 1.4;
        public double MaxAcceleration { get; set; } = 2.0;
        public double ComfortableBraking { get; set; } = 2.5;
        public int Delta { get; set; } = 4;

        public double TotalWaitTime { get; set; } = 0.0;
        public double TotalTravelTime { get; set; } = 0.0;
        public int StopCount { get; set; } = 0;
        private bool _wasStopped = false;

        public Vehicle(int id, double initialSpeed, double position = 0.0)
        {
            Id = id;
            Speed = initialSpeed;
            Position = position;

            VehicleColor = palette[rnd.Next(palette.Length)];
            Type = (VehicleType)rnd.Next(3);

            if (Type == VehicleType.Truck)
            {
                Length = 6.0 + rnd.NextDouble() * 2.0;
                VehicleWidth = 2.1 + rnd.NextDouble() * 0.4;
            }
            else if (Type == VehicleType.SUV)
            {
                Length = 4.5 + rnd.NextDouble() * 1.0;
                VehicleWidth = 1.9 + rnd.NextDouble() * 0.2;
            }
            else
            {
                Length = 4.0 + rnd.NextDouble() * 1.5;
                VehicleWidth = 1.7 + rnd.NextDouble() * 0.2;
            }
        }

        public double CalculateAcceleration(double leaderGap, double leaderSpeed)
        {
            double deltaV = Speed - leaderSpeed;
            double desiredGap = MinGap + Math.Max(0.0,
                Speed * SafeTimeHeadway + (Speed * deltaV) / (2.0 * Math.Sqrt(MaxAcceleration * ComfortableBraking)));

            double freeRoadTerm = Math.Pow(Speed / DesiredSpeed, Delta);
            double interactionTerm = Math.Pow(desiredGap / Math.Max(leaderGap, 0.1), 2);

            return MaxAcceleration * (1.0 - freeRoadTerm - interactionTerm);
        }

        public void Update(double dt, double leaderGap, double leaderSpeed)
        {
            Acceleration = CalculateAcceleration(leaderGap, leaderSpeed);

            if (Acceleration < -8.0)
                Acceleration = -8.0;

            Speed += Acceleration * dt;
            if (Speed < 0.0)
            {
                Speed = 0.0;
                Acceleration = 0.0;
            }

            Position += Speed * dt;
            TotalTravelTime += dt;

            if (Speed < 0.5)
            {
                TotalWaitTime += dt;
                if (!_wasStopped)
                {
                    StopCount++;
                    _wasStopped = true;
                }
            }
            else if (Speed > 2.0)
            {
                _wasStopped = false;
            }
        }
    }
}