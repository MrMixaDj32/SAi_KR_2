using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SAi_KR
{
    public enum ControlMode
    {
        FixedTime,
        IntelligentAgents
    }

    public class SimulationEngine
    {
        public const double PxPerMeter = 2.5;

        public List<Intersection> Intersections { get; } = new List<Intersection>();
        public List<Lane> AllLanes { get; } = new List<Lane>();
        public List<Vehicle> CompletedVehicles { get; } = new List<Vehicle>();
        public List<TrafficAgent> Agents { get; } = new List<TrafficAgent>();

        public ControlMode CurrentControlMode { get; set; } = ControlMode.FixedTime;
        public double SimulationTime { get; private set; } = 0.0;

        private int _vehicleIdCounter = 1;
        private Random _rand = new Random(42);

        private Lane? _laneSevMidBoulevardL1;
        private Lane? _laneSevMidBoulevardL2;
        private Lane? _connSevKrasWEastL1;
        private Lane? _connSevKrasWEastL2;

        // Реалистичные потоки Краснодара (авт/час)
        public double InflowSevernayaWest { get; set; } = 1500;
        public double InflowSevernayaEast { get; set; } = 1350;
        public double InflowKrasnayaStraight { get; set; } = 400;
        public double InflowKrasnayaLeft { get; set; } = 250;
        public double InflowKrasnayaNorth { get; set; } = 550;
        public double InflowOktyabrskaya { get; set; } = 600;
        public double InflowRashpilevskayaNorth { get; set; } = 400; // С севера по односторонней на юг

        public void BuildSevernayaCorridor()
        {
            Intersections.Clear();
            AllLanes.Clear();
            Agents.Clear();
            CompletedVehicles.Clear();
            _vehicleIdCounter = 1;
            SimulationTime = 0.0;
            FixedModeStats.Reset();
            AIModeStats.Reset();

            var nodeOktyabrskaya = new Intersection("N1", "Северная / Октябрьская");
            var nodeRashpilevskaya = new Intersection("N2", "Северная / Рашпилевская");
            var nodeKrasnaya = new Intersection("N3", "Северная / Красная");

            // ====== 1. Ул. Северная — ВОСТОК (слева направо, Y=439 и Y=460) ======
            // Полоса 1 (левая, Y=439)
            var inWest_L1 = new Lane("Inflow_Sev_West_L1") { LaneIndex = 0 };
            inWest_L1.SetPath(new PointF(20, 439), new PointF(210, 439));
            var conn_Sev_Okt_E_L1 = new Lane("Conn_Sev_Okt_E_L1") { LaneIndex = 0 };
            conn_Sev_Okt_E_L1.SetPath(new PointF(210, 439), new PointF(295, 439));
            var laneSev12_E_L1 = new Lane("Sev_Okt_Rash_E_L1") { LaneIndex = 0 };
            laneSev12_E_L1.SetPath(new PointF(295, 439), new PointF(615, 439));
            var conn_Sev_Rash_E_L1 = new Lane("Conn_Sev_Rash_E_L1") { LaneIndex = 0 };
            conn_Sev_Rash_E_L1.SetPath(new PointF(615, 439), new PointF(700, 439));
            var laneSev23_E_L1 = new Lane("Sev_Rash_Kras_E_L1") { LaneIndex = 0 };
            laneSev23_E_L1.SetPath(new PointF(700, 439), new PointF(1015, 439));

            // Поворот на Красной ТОЛЬКО НАПРАВО (ВНИЗ)
            // Левый ряд (L1, Y=439) поворачивает по широкому радиусу в левый ряд бульвара (X=1065)
            var turnSev_Kras_L1 = new Lane("Turn_Sev_Kras_L1");
            turnSev_Kras_L1.SetPath(new PointF(1015, 439), new PointF(1036, 442), new PointF(1052, 453), new PointF(1063, 472), new PointF(1065, 495));
            var outKras_S_L2 = new Lane("Out_Kras_S_L2");
            outKras_S_L2.SetPath(new PointF(1065, 495), new PointF(1065, 940));

            inWest_L1.NextLane = conn_Sev_Okt_E_L1;
            conn_Sev_Okt_E_L1.NextLane = laneSev12_E_L1;
            laneSev12_E_L1.NextLane = conn_Sev_Rash_E_L1;
            conn_Sev_Rash_E_L1.NextLane = laneSev23_E_L1;
            laneSev23_E_L1.NextLane = turnSev_Kras_L1;
            turnSev_Kras_L1.NextLane = outKras_S_L2;

            // Полоса 2 (правая, Y=460)
            var inWest_L2 = new Lane("Inflow_Sev_West_L2") { LaneIndex = 1 };
            inWest_L2.SetPath(new PointF(20, 460), new PointF(210, 460));
            var conn_Sev_Okt_E_L2 = new Lane("Conn_Sev_Okt_E_L2") { LaneIndex = 1 };
            conn_Sev_Okt_E_L2.SetPath(new PointF(210, 460), new PointF(295, 460));
            var laneSev12_E_L2 = new Lane("Sev_Okt_Rash_E_L2") { LaneIndex = 1 };
            laneSev12_E_L2.SetPath(new PointF(295, 460), new PointF(615, 460));
            var conn_Sev_Rash_E_L2 = new Lane("Conn_Sev_Rash_E_L2") { LaneIndex = 1 };
            conn_Sev_Rash_E_L2.SetPath(new PointF(615, 460), new PointF(700, 460));
            var laneSev23_E_L2 = new Lane("Sev_Rash_Kras_E_L2") { LaneIndex = 1 };
            laneSev23_E_L2.SetPath(new PointF(700, 460), new PointF(1015, 460));

            // Правый ряд (L2, Y=460) поворачивает вдоль бордюра в правый ряд бульвара (X=1045)
            var turnSev_Kras_L2 = new Lane("Turn_Sev_Kras_L2");
            turnSev_Kras_L2.SetPath(new PointF(1015, 460), new PointF(1027, 463), new PointF(1038, 471), new PointF(1044, 482), new PointF(1045, 495));
            var outKras_S_Direct = new Lane("Out_Kras_S_Direct");
            outKras_S_Direct.SetPath(new PointF(1045, 495), new PointF(1045, 940));

            inWest_L2.NextLane = conn_Sev_Okt_E_L2;
            conn_Sev_Okt_E_L2.NextLane = laneSev12_E_L2;
            laneSev12_E_L2.NextLane = conn_Sev_Rash_E_L2;
            conn_Sev_Rash_E_L2.NextLane = laneSev23_E_L2;
            laneSev23_E_L2.NextLane = turnSev_Kras_L2;
            turnSev_Kras_L2.NextLane = outKras_S_Direct;

            // ====== 2. Ул. Северная — ЗАПАД (справа налево, Y=416 и Y=396) ======
            // Полоса 1 (левая, Y=416)
            var inEast_L1 = new Lane("Inflow_Sev_East_L1") { LaneIndex = 0 };
            inEast_L1.SetPath(new PointF(1650, 416), new PointF(1225, 416));
            var conn_Sev_Kras_W_East_L1 = new Lane("Conn_Sev_Kras_W_East_L1") { LaneIndex = 0 };
            conn_Sev_Kras_W_East_L1.SetPath(new PointF(1225, 416), new PointF(1175, 416));
            var lane_Sev_Mid_Boulevard_L1 = new Lane("Lane_Sev_Mid_Boulevard_L1") { LaneIndex = 0 };
            lane_Sev_Mid_Boulevard_L1.SetPath(new PointF(1175, 416), new PointF(1075, 416));
            var conn_Sev_Mid_Exit_L1 = new Lane("Conn_Sev_Mid_Exit_L1") { LaneIndex = 0 };
            conn_Sev_Mid_Exit_L1.SetPath(new PointF(1075, 416), new PointF(1015, 416));
            var laneSev32_W_L1 = new Lane("Sev_Kras_Rash_W_L1") { LaneIndex = 0 };
            laneSev32_W_L1.SetPath(new PointF(1015, 416), new PointF(700, 416));
            var conn_Sev_Rash_W_L1 = new Lane("Conn_Sev_Rash_W_L1") { LaneIndex = 0 };
            conn_Sev_Rash_W_L1.SetPath(new PointF(700, 416), new PointF(615, 416));
            var laneSev21_W_L1 = new Lane("Sev_Rash_Okt_W_L1") { LaneIndex = 0 };
            laneSev21_W_L1.SetPath(new PointF(615, 416), new PointF(295, 416));
            var conn_Sev_Okt_W_L1 = new Lane("Conn_Sev_Okt_W_L1") { LaneIndex = 0 };
            conn_Sev_Okt_W_L1.SetPath(new PointF(295, 416), new PointF(210, 416));
            var outWest_L1 = new Lane("Out_Sev_West_L1") { LaneIndex = 0 };
            outWest_L1.SetPath(new PointF(210, 416), new PointF(20, 416));

            inEast_L1.NextLane = conn_Sev_Kras_W_East_L1;
            conn_Sev_Kras_W_East_L1.NextLane = lane_Sev_Mid_Boulevard_L1;
            lane_Sev_Mid_Boulevard_L1.NextLane = conn_Sev_Mid_Exit_L1;
            conn_Sev_Mid_Exit_L1.NextLane = laneSev32_W_L1;
            laneSev32_W_L1.NextLane = conn_Sev_Rash_W_L1;
            conn_Sev_Rash_W_L1.NextLane = laneSev21_W_L1;
            laneSev21_W_L1.NextLane = conn_Sev_Okt_W_L1;
            conn_Sev_Okt_W_L1.NextLane = outWest_L1;

            // Полоса 2 (правая, Y=396)
            var inEast_L2 = new Lane("Inflow_Sev_East_L2") { LaneIndex = 1 };
            inEast_L2.SetPath(new PointF(1650, 396), new PointF(1225, 396));
            var conn_Sev_Kras_W_East_L2 = new Lane("Conn_Sev_Kras_W_East_L2") { LaneIndex = 1 };
            conn_Sev_Kras_W_East_L2.SetPath(new PointF(1225, 396), new PointF(1175, 396));
            var lane_Sev_Mid_Boulevard_L2 = new Lane("Lane_Sev_Mid_Boulevard_L2") { LaneIndex = 1 };
            lane_Sev_Mid_Boulevard_L2.SetPath(new PointF(1175, 396), new PointF(1075, 396));

            _laneSevMidBoulevardL1 = lane_Sev_Mid_Boulevard_L1;
            _laneSevMidBoulevardL2 = lane_Sev_Mid_Boulevard_L2;
            _connSevKrasWEastL1 = conn_Sev_Kras_W_East_L1;
            _connSevKrasWEastL2 = conn_Sev_Kras_W_East_L2;

            var conn_Sev_Mid_Exit_L2 = new Lane("Conn_Sev_Mid_Exit_L2") { LaneIndex = 1 };
            conn_Sev_Mid_Exit_L2.SetPath(new PointF(1075, 396), new PointF(1015, 396));
            var laneSev32_W_L2 = new Lane("Sev_Kras_Rash_W_L2") { LaneIndex = 1 };
            laneSev32_W_L2.SetPath(new PointF(1015, 396), new PointF(700, 396));
            var conn_Sev_Rash_W_L2 = new Lane("Conn_Sev_Rash_W_L2") { LaneIndex = 1 };
            conn_Sev_Rash_W_L2.SetPath(new PointF(700, 396), new PointF(615, 396));

            // Поворот направо с Северной (промежуток между Рашпилевской и Красной) на Рашпилевскую на север
            var turn_Sev_Rash_N = new Lane("Turn_Sev_Rash_N");
            turn_Sev_Rash_N.SetPath(new PointF(700, 396), new PointF(685, 385), new PointF(675, 375));
            var outRash_N = new Lane("Out_Rash_N");
            outRash_N.SetPath(new PointF(675, 375), new PointF(675, 30));
            turn_Sev_Rash_N.NextLane = outRash_N;

            var laneSev21_W_L2 = new Lane("Sev_Rash_Okt_W_L2") { LaneIndex = 1 };
            laneSev21_W_L2.SetPath(new PointF(615, 396), new PointF(295, 396));
            var conn_Sev_Okt_W_L2 = new Lane("Conn_Sev_Okt_W_L2") { LaneIndex = 1 };
            conn_Sev_Okt_W_L2.SetPath(new PointF(295, 396), new PointF(210, 396));
            var outWest_L2 = new Lane("Out_Sev_West_L2") { LaneIndex = 1 };
            outWest_L2.SetPath(new PointF(210, 396), new PointF(20, 396));

            inEast_L2.NextLane = conn_Sev_Kras_W_East_L2;
            conn_Sev_Kras_W_East_L2.NextLane = lane_Sev_Mid_Boulevard_L2;
            lane_Sev_Mid_Boulevard_L2.NextLane = conn_Sev_Mid_Exit_L2;
            conn_Sev_Mid_Exit_L2.NextLane = laneSev32_W_L2;
            laneSev32_W_L2.NextLane = conn_Sev_Rash_W_L2;
            laneSev32_W_L2.AlternativeNextLanes.Add(turn_Sev_Rash_N); // Поворот направо на Рашпилевскую
            conn_Sev_Rash_W_L2.NextLane = laneSev21_W_L2;
            laneSev21_W_L2.NextLane = conn_Sev_Okt_W_L2;
            conn_Sev_Okt_W_L2.NextLane = outWest_L2;

            // ====== 3. Ул. Октябрьская (выезд от ТРЦ Галерея — НАПРАВО и НАЛЕВО) ======
            // Правый ряд — поворот НАПРАВО на Северную (на восток)
            var inOkt_S_Right = new Lane("Inflow_Okt_S_Right");
            inOkt_S_Right.SetPath(new PointF(270, 750), new PointF(270, 480));
            var turnOkt_E = new Lane("Turn_Okt_E");
            turnOkt_E.SetPath(new PointF(270, 480), new PointF(280, 465), new PointF(295, 460));
            inOkt_S_Right.NextLane = turnOkt_E;
            turnOkt_E.NextLane = laneSev12_E_L2;

            // Левый ряд — поворот НАЛЕВО на Северную (на запад)
            var inOkt_S_Left = new Lane("Inflow_Okt_S_Left");
            inOkt_S_Left.SetPath(new PointF(245, 750), new PointF(245, 480));
            var turnOkt_W = new Lane("Turn_Okt_W");
            turnOkt_W.SetPath(new PointF(245, 480), new PointF(235, 435), new PointF(210, 416));
            inOkt_S_Left.NextLane = turnOkt_W;
            turnOkt_W.NextLane = outWest_L1;

            // ====== 4. Ул. Рашпилевская (СНИЗУ ПОД СЕВЕРНОЙ — ОДНОСТОРОННЯЯ НА ЮГ) ======
            // Въезд с севера по Рашпилевской к перекрестку
            var inRash_N = new Lane("Inflow_Rash_N");
            inRash_N.SetPath(new PointF(645, 30), new PointF(645, 375));
            var conn_Rash_S = new Lane("Conn_Rash_S");
            conn_Rash_S.SetPath(new PointF(645, 375), new PointF(645, 480));
            var outRash_S = new Lane("Out_Rash_S");
            outRash_S.SetPath(new PointF(645, 480), new PointF(645, 940));
            inRash_N.NextLane = conn_Rash_S;
            conn_Rash_S.NextLane = outRash_S;

            // Вторая полоса южной односторонней Рашпилевской
            var outRash_S_L2 = new Lane("Out_Rash_S_L2");
            outRash_S_L2.SetPath(new PointF(675, 480), new PointF(675, 940));

            // Поворот направо с Северной (восток) на Рашпилевскую на юг
            var turn_Sev_Rash_S = new Lane("Turn_Sev_Rash_S");
            turn_Sev_Rash_S.SetPath(new PointF(615, 460), new PointF(635, 462), new PointF(658, 470), new PointF(675, 480));
            turn_Sev_Rash_S.NextLane = outRash_S_L2;
            laneSev12_E_L2.AlternativeNextLanes.Add(turn_Sev_Rash_S);

            // ====== 5. Ул. Красная (БУЛЬВАР СВЕРХУ И СНИЗУ) ======
            // Северный левый рукав (движение ВНИЗ): прямо через перекресток в южный левый рукав
            var inKras_NW = new Lane("Inflow_Kras_NW");
            inKras_NW.SetPath(new PointF(1045, 30), new PointF(1045, 375));
            var conn_Kras_N_Direct = new Lane("Conn_Kras_N_Direct");
            conn_Kras_N_Direct.SetPath(new PointF(1045, 375), new PointF(1045, 495));
            inKras_NW.NextLane = conn_Kras_N_Direct;
            conn_Kras_N_Direct.NextLane = outKras_S_Direct;

            // Поворот направо с Красной (вниз) на Северную (на запад, в сторону Рашпилевской)
            var turn_Kras_N_Sev = new Lane("Turn_Kras_N_Sev");
            turn_Kras_N_Sev.SetPath(new PointF(1045, 375), new PointF(1040, 383), new PointF(1030, 392), new PointF(1015, 396));
            turn_Kras_N_Sev.NextLane = laneSev32_W_L2;
            inKras_NW.AlternativeNextLanes.Add(turn_Kras_N_Sev);

            // Северный правый рукав (движение ВВЕРХ после перекрестка): 2 полосы (L1: X=1185, L2: X=1205)
            var outKras_NE_L1 = new Lane("Out_Kras_NE_L1") { LaneIndex = 0 };
            outKras_NE_L1.SetPath(new PointF(1185, 375), new PointF(1185, 30));
            var outKras_NE_L2 = new Lane("Out_Kras_NE_L2") { LaneIndex = 1 };
            outKras_NE_L2.SetPath(new PointF(1205, 375), new PointF(1205, 30));

            // Соединители через перекресток для движения ВВЕРХ:
            var conn_Kras_S_Direct_L1 = new Lane("Conn_Kras_S_Direct_L1") { LaneIndex = 0 };
            conn_Kras_S_Direct_L1.SetPath(new PointF(1185, 480), new PointF(1185, 375));
            conn_Kras_S_Direct_L1.NextLane = outKras_NE_L1;

            var conn_Kras_S_Direct_L2 = new Lane("Conn_Kras_S_Direct_L2") { LaneIndex = 1 };
            conn_Kras_S_Direct_L2.SetPath(new PointF(1205, 480), new PointF(1205, 375));
            conn_Kras_S_Direct_L2.NextLane = outKras_NE_L2;

            // Южный правый рукав, правая полоса (X=1205): только прямо вверх в L2
            var inKras_SE_Straight = new Lane("Inflow_Kras_SE_Str") { LaneIndex = 1 };
            inKras_SE_Straight.SetPath(new PointF(1205, 940), new PointF(1205, 480));
            inKras_SE_Straight.NextLane = conn_Kras_S_Direct_L2;

            // Южный правый рукав, левая полоса (X=1185): прямо вверх в L1 И поворот налево на Северную
            var inKras_SE_Left = new Lane("Inflow_Kras_SE_Left") { LaneIndex = 0 };
            inKras_SE_Left.SetPath(new PointF(1185, 940), new PointF(1185, 480));
            var turnKras_S_Sev = new Lane("Turn_Kras_S_Sev");
            turnKras_S_Sev.SetPath(new PointF(1185, 480), new PointF(1165, 440), new PointF(1135, 416), new PointF(1075, 416));
            turnKras_S_Sev.NextLane = conn_Sev_Mid_Exit_L1; // Регулируется светофором между бульварами на X=1075

            inKras_SE_Left.NextLane = conn_Kras_S_Direct_L1; // Основное направление — прямо вверх в L1
            inKras_SE_Left.AlternativeNextLanes.Add(turnKras_S_Sev); // Налево на Северную через светофор между бульварами

            // ====== 6. Светофорные фазы ======
            // Октябрьская (Incoming: 0=SevE_L1, 1=SevE_L2, 2=SevW_L1, 3=SevW_L2, 4=OktRight, 5=OktLeft)
            nodeOktyabrskaya.AddIncomingLane(inWest_L1);       // 0
            nodeOktyabrskaya.AddIncomingLane(inWest_L2);       // 1
            nodeOktyabrskaya.AddIncomingLane(laneSev21_W_L1);  // 2
            nodeOktyabrskaya.AddIncomingLane(laneSev21_W_L2);  // 3
            nodeOktyabrskaya.AddIncomingLane(inOkt_S_Right);   // 4
            nodeOktyabrskaya.AddIncomingLane(inOkt_S_Left);    // 5
            nodeOktyabrskaya.AddPhase(new TrafficPhase(40.0, 0, 1, 2, 3));
            nodeOktyabrskaya.AddPhase(new TrafficPhase(22.0, 4, 5));

            // Рашпилевская (Incoming: 0=SevE_L1, 1=SevE_L2, 2=SevW_L1, 3=SevW_L2, 4=RashN)
            nodeRashpilevskaya.AddIncomingLane(laneSev12_E_L1); // 0
            nodeRashpilevskaya.AddIncomingLane(laneSev12_E_L2); // 1
            nodeRashpilevskaya.AddIncomingLane(laneSev32_W_L1); // 2
            nodeRashpilevskaya.AddIncomingLane(laneSev32_W_L2); // 3 (имеет правый поворот на Рашпилевскую)
            nodeRashpilevskaya.AddIncomingLane(inRash_N);       // 4
            nodeRashpilevskaya.AddPhase(new TrafficPhase(38.0, 0, 1, 2, 3));
            nodeRashpilevskaya.AddPhase(new TrafficPhase(20.0, 4));

            // Красная (Incoming: 0=SevE_L1 (направо вниз), 1=SevE_L2 (направо вниз),
            //                    2=SevW_East_L1, 3=SevW_East_L2, 4=KrasNW (вниз), 5=KrasSE_Str, 6=KrasSE_Left,
            //                    7=Turn_Kras_S_Sev (светофор между бульварами),
            //                    8=Lane_Sev_Mid_Boulevard_L1, 9=Lane_Sev_Mid_Boulevard_L2)
            nodeKrasnaya.AddIncomingLane(laneSev23_E_L1);            // 0
            nodeKrasnaya.AddIncomingLane(laneSev23_E_L2);            // 1
            nodeKrasnaya.AddIncomingLane(inEast_L1);                 // 2
            nodeKrasnaya.AddIncomingLane(inEast_L2);                 // 3
            nodeKrasnaya.AddIncomingLane(inKras_NW);                 // 4
            nodeKrasnaya.AddIncomingLane(inKras_SE_Straight);        // 5
            nodeKrasnaya.AddIncomingLane(inKras_SE_Left);            // 6
            nodeKrasnaya.AddIncomingLane(turnKras_S_Sev);            // 7: Светофор между бульварами (левый поворот)
            nodeKrasnaya.AddIncomingLane(lane_Sev_Mid_Boulevard_L1); // 8: Светофор между бульварами (сквозной L1)
            nodeKrasnaya.AddIncomingLane(lane_Sev_Mid_Boulevard_L2); // 9: Светофор между бульварами (сквозной L2)

            // Фаза 0: Зеленый для Северной и для светофора между бульварами (поток по Красной вниз ЗАКРЫТ)
            nodeKrasnaya.AddPhase(new TrafficPhase(45.0, 0, 1, 2, 3, 7, 8, 9));
            // Фаза 1: Зеленый для Красной (поток по Красной идет вниз, машины с левого поворота стоят на красном между бульварами)
            nodeKrasnaya.AddPhase(new TrafficPhase(32.0, 4, 5, 6));

            Intersections.AddRange(new[] { nodeOktyabrskaya, nodeRashpilevskaya, nodeKrasnaya });
            AllLanes.AddRange(new[] {
                inWest_L1, conn_Sev_Okt_E_L1, laneSev12_E_L1, conn_Sev_Rash_E_L1, laneSev23_E_L1, turnSev_Kras_L1, outKras_S_Direct,
                inWest_L2, conn_Sev_Okt_E_L2, laneSev12_E_L2, conn_Sev_Rash_E_L2, laneSev23_E_L2, turnSev_Kras_L2, outKras_S_L2,
                inEast_L1, conn_Sev_Kras_W_East_L1, lane_Sev_Mid_Boulevard_L1, conn_Sev_Mid_Exit_L1, laneSev32_W_L1, conn_Sev_Rash_W_L1, laneSev21_W_L1, conn_Sev_Okt_W_L1, outWest_L1,
                inEast_L2, conn_Sev_Kras_W_East_L2, lane_Sev_Mid_Boulevard_L2, conn_Sev_Mid_Exit_L2, laneSev32_W_L2, turn_Sev_Rash_N, outRash_N, conn_Sev_Rash_W_L2, laneSev21_W_L2, conn_Sev_Okt_W_L2, outWest_L2,
                inOkt_S_Right, turnOkt_E, inOkt_S_Left, turnOkt_W,
                inRash_N, conn_Rash_S, outRash_S, outRash_S_L2, turn_Sev_Rash_S,
                inKras_NW, conn_Kras_N_Direct, turn_Kras_N_Sev,
                inKras_SE_Straight, conn_Kras_S_Direct_L2, outKras_NE_L2,
                inKras_SE_Left, conn_Kras_S_Direct_L1, outKras_NE_L1, turnKras_S_Sev
            });

            var agent1 = new TrafficAgent(nodeOktyabrskaya, _rand);
            var agent2 = new TrafficAgent(nodeRashpilevskaya, _rand);
            var agent3 = new TrafficAgent(nodeKrasnaya, _rand);

            agent1.NeighborEast = agent2;
            agent2.NeighborWest = agent1;
            agent2.NeighborEast = agent3;
            agent3.NeighborWest = agent2;

            Agents.AddRange(new[] { agent1, agent2, agent3 });
        }

        public void Step(double dt)
        {
            SimulationTime += dt;
            SpawnTraffic(dt);

            // 1. Светофорное управление
            if (CurrentControlMode == ControlMode.FixedTime)
            {
                foreach (var node in Intersections)
                    node.Update(dt);
            }
            else
            {
                foreach (var agent in Agents)
                {
                    agent.Node.Update(dt);
                    agent.Decide();
                }
            }

            // 2. Движение по полосам с учетом светофоров
            var controlledLanes = new HashSet<Lane>(Intersections.SelectMany(n => n.IncomingLanes));
            foreach (var node in Intersections)
            {
                foreach (var lane in node.IncomingLanes)
                {
                    bool isBlocked = node.IsLaneBlocked(lane);

                    // ПДД РФ 13.2 (Anti-Gridlock): Запрет выезда на перекресток при заторе за ним
                    if (!isBlocked && (lane.Id == "Inflow_Sev_East_L1" || lane.Id == "Inflow_Sev_East_L2"))
                    {
                        var midLane = lane.Id.EndsWith("L1") ? _laneSevMidBoulevardL1 : _laneSevMidBoulevardL2;
                        var connLane = lane.Id.EndsWith("L1") ? _connSevKrasWEastL1 : _connSevKrasWEastL2;

                        if (midLane != null && connLane != null)
                        {
                            bool midBoulevardBlocked = midLane.Vehicles.Count > 0 && midLane.Vehicles.Last().Position < 12.0;
                            bool connBlocked = connLane.Vehicles.Count >= 2 ||
                                               (connLane.Vehicles.Count >= 1 && connLane.Vehicles.First().Speed < 2.0);

                            if (midBoulevardBlocked || connBlocked)
                            {
                                isBlocked = true;
                            }
                        }
                    }

                    lane.Update(dt, isBlocked);
                }
            }

            foreach (var lane in AllLanes)
            {
                if (!controlledLanes.Contains(lane))
                {
                    lane.Update(dt, false);
                }
            }

            int prevCompleted = CompletedVehicles.Count;
            // 3. Продвижение автомобилей (downstream к upstream)
            for (int i = AllLanes.Count - 1; i >= 0; i--)
            {
                AllLanes[i].AdvanceVehicles(CompletedVehicles);
            }
            int newlyCompleted = CompletedVehicles.Count - prevCompleted;

            // 4. Интеллектуальное предотвращение столкновений
            PreventIntersectionCollisions(dt);

            // 5. Обновление live статистики для текущего режима (сравнение в реальном времени)
            var activeStats = CurrentControlMode == ControlMode.FixedTime ? FixedModeStats : AIModeStats;
            activeStats.StepsCount++;
            int currentQ = GetTotalQueue();
            activeStats.QueueSum += currentQ;
            if (currentQ > activeStats.MaxQueue) activeStats.MaxQueue = currentQ;
            activeStats.CompletedCount += newlyCompleted;

            var activeVehicles = AllLanes.SelectMany(l => l.Vehicles).ToList();
            if (activeVehicles.Count > 0)
            {
                activeStats.TotalWaitTimeSum += activeVehicles.Sum(v => v.TotalWaitTime);
                activeStats.VehiclesSampled += activeVehicles.Count;
            }
        }

        private void PreventIntersectionCollisions(double dt)
        {
            var carItems = new List<(Lane Lane, Vehicle Car, PointF Pos, double Angle)>();
            foreach (var lane in AllLanes)
            {
                foreach (var car in lane.Vehicles)
                {
                    var (pt, angle) = lane.GetRenderPositionWithAngle(car.Position);
                    carItems.Add((lane, car, pt, angle));
                }
            }

            for (int i = 0; i < carItems.Count; i++)
            {
                for (int j = i + 1; j < carItems.Count; j++)
                {
                    var a = carItems[i];
                    var b = carItems[j];

                    if (a.Lane == b.Lane) continue;

                    // Параллельные полосы одного направления не сталкиваются
                    if ((a.Lane.Id.Contains("Kras_S_Direct") || a.Lane.Id.Contains("Out_Kras_NE") || a.Lane.Id.Contains("Inflow_Kras_SE")) &&
                        (b.Lane.Id.Contains("Kras_S_Direct") || b.Lane.Id.Contains("Out_Kras_NE") || b.Lane.Id.Contains("Inflow_Kras_SE")))
                    {
                        continue;
                    }

                    if (a.Lane.Id.Contains("Sev_Mid") && b.Lane.Id.Contains("Sev_Mid"))
                    {
                        continue;
                    }

                    float dx = b.Pos.X - a.Pos.X;
                    float dy = b.Pos.Y - a.Pos.Y;
                    float distSq = dx * dx + dy * dy;

                    // Быстрая грубая отсечка (> 25 px = ~10м)
                    if (distSq > 625f) continue;

                    // Преобразование вектора смещения в систему координат автомобиля A
                    double cosA = Math.Cos(a.Angle);
                    double sinA = Math.Sin(a.Angle);
                    double distLong = dx * cosA + dy * sinA;
                    double distLat = -dx * sinA + dy * cosA;

                    // Пороговые габариты с запасом безопасности
                    double safeLat = (a.Car.VehicleWidth + b.Car.VehicleWidth) * 0.5 * PxPerMeter + 2.5;
                    double safeLong = (a.Car.Length + b.Car.Length) * 0.5 * PxPerMeter + 5.0;

                    // Если автомобили параллельны и в соседних рядах — они не сталкиваются
                    if (Math.Abs(distLat) > safeLat || Math.Abs(distLong) > safeLong)
                    {
                        continue;
                    }

                    // Определение приоритета:
                    Vehicle yieldCar;

                    bool aIsKras = a.Lane.Id.Contains("Kras_");
                    bool bIsKras = b.Lane.Id.Contains("Kras_");
                    bool aIsSev = a.Lane.Id.Contains("Sev_");
                    bool bIsSev = b.Lane.Id.Contains("Sev_");

                    var nodeKras = Intersections.Count >= 3 ? Intersections[2] : null;
                    if (nodeKras != null && ((aIsKras && bIsSev) || (aIsSev && bIsKras)))
                    {
                        // На перекрестке с Красной приоритет определяется текущей фазой светофора
                        if (nodeKras.CurrentPhaseIndex == 1) // Зеленый для Красной
                        {
                            yieldCar = aIsSev ? a.Car : b.Car;
                        }
                        else // Зеленый для Северной
                        {
                            yieldCar = aIsKras ? a.Car : b.Car;
                        }
                    }
                    else
                    {
                        // Тот, кто сзади, уступает тому, кто впереди (distLong > 0 означает B впереди A)
                        yieldCar = (distLong > 0) ? a.Car : b.Car;
                    }

                    if (yieldCar != null)
                    {
                        yieldCar.Speed = Math.Max(0.0, yieldCar.Speed - 6.0 * dt);
                        if (distSq < 100f)
                        {
                            yieldCar.Speed = 0.0;
                        }
                    }
                }
            }
        }

        private void SpawnTraffic(double dt)
        {
            // Северная Восток (слева направо) — 2 полосы
            TrySpawnOnBestLane(new[] { "Inflow_Sev_West_L1", "Inflow_Sev_West_L2" }, InflowSevernayaWest, dt);
            // Северная Запад (справа налево) — 2 полосы
            TrySpawnOnBestLane(new[] { "Inflow_Sev_East_L1", "Inflow_Sev_East_L2" }, InflowSevernayaEast, dt);
            // Октябрьская — спавн направо и налево
            TrySpawnVehicle(AllLanes.First(l => l.Id == "Inflow_Okt_S_Right"), InflowOktyabrskaya * 0.55, dt);
            TrySpawnVehicle(AllLanes.First(l => l.Id == "Inflow_Okt_S_Left"), InflowOktyabrskaya * 0.45, dt);
            // Рашпилевская — только с севера по односторонней на юг
            TrySpawnVehicle(AllLanes.First(l => l.Id == "Inflow_Rash_N"), InflowRashpilevskayaNorth, dt);
            // Красная — по левому рукаву вниз, по правому рукаву вверх (прямо и поворот налево на Северную)
            TrySpawnVehicle(AllLanes.First(l => l.Id == "Inflow_Kras_NW"), InflowKrasnayaNorth, dt);
            TrySpawnVehicle(AllLanes.First(l => l.Id == "Inflow_Kras_SE_Str"), InflowKrasnayaStraight, dt);
            TrySpawnVehicle(AllLanes.First(l => l.Id == "Inflow_Kras_SE_Left"), InflowKrasnayaLeft, dt);
        }

        private void TrySpawnOnBestLane(string[] laneIds, double flowRatePerHour, double dt)
        {
            double lambda = flowRatePerHour / 3600.0;
            if (_rand.NextDouble() < lambda * dt)
            {
                var lanes = laneIds.Select(id => AllLanes.First(l => l.Id == id)).ToArray();
                var best = lanes.OrderBy(l => l.Vehicles.Count).First();
                var equal = lanes.Where(l => l.Vehicles.Count == best.Vehicles.Count).ToArray();
                if (equal.Length > 1) best = equal[_rand.Next(equal.Length)];

                if (best.Vehicles.Count == 0 || best.Vehicles.Last().Position > 12.0)
                {
                    var v = new Vehicle(_vehicleIdCounter++, 11.1, 0.0);
                    best.Vehicles.Add(v);
                }
            }
        }

        private void TrySpawnVehicle(Lane lane, double flowRatePerHour, double dt)
        {
            double lambda = flowRatePerHour / 3600.0;
            if (_rand.NextDouble() < lambda * dt)
            {
                if (lane.Vehicles.Count == 0 || lane.Vehicles.Last().Position > 12.0)
                {
                    var v = new Vehicle(_vehicleIdCounter++, 11.1, 0.0);
                    lane.Vehicles.Add(v);
                }
            }
        }

        public void ResetWithSeed(int seed)
        {
            _rand = new Random(seed);
            BuildSevernayaCorridor();
        }

        public double CalculateFitness()
        {
            var active = AllLanes.SelectMany(l => l.Vehicles);
            var all = active.Concat(CompletedVehicles).ToList();
            if (all.Count == 0) return 0.0;
            return -(all.Average(v => v.TotalWaitTime) * 1.5 + AllLanes.Sum(l => l.GetQueueLength()) * 0.5) + (CompletedVehicles.Count * 2.0);
        }

        public double GetAverageWaitTime()
        {
            var active = AllLanes.SelectMany(l => l.Vehicles);
            var all = active.Concat(CompletedVehicles).ToList();
            return all.Count == 0 ? 0.0 : all.Average(v => v.TotalWaitTime);
        }

        public class LiveModeStats
        {
            public double TotalWaitTimeSum { get; set; }
            public int VehiclesSampled { get; set; }
            public double QueueSum { get; set; }
            public int StepsCount { get; set; }
            public int CompletedCount { get; set; }
            public int MaxQueue { get; set; }

            public double AvgWait => VehiclesSampled > 0 ? TotalWaitTimeSum / VehiclesSampled : 0.0;
            public double AvgQueue => StepsCount > 0 ? QueueSum / StepsCount : 0.0;

            public void Reset()
            {
                TotalWaitTimeSum = 0;
                VehiclesSampled = 0;
                QueueSum = 0;
                StepsCount = 0;
                CompletedCount = 0;
                MaxQueue = 0;
            }
        }

        public LiveModeStats FixedModeStats { get; } = new LiveModeStats();
        public LiveModeStats AIModeStats { get; } = new LiveModeStats();

        public int GetIntersectionQueue(int nodeIndex)
        {
            if (nodeIndex >= Intersections.Count) return 0;
            return Intersections[nodeIndex].IncomingLanes.Sum(l => l.GetQueueLength());
        }

        public double GetIntersectionAvgWait(int nodeIndex)
        {
            if (nodeIndex >= Intersections.Count) return 0.0;
            var cars = Intersections[nodeIndex].IncomingLanes.SelectMany(l => l.Vehicles).ToList();
            return cars.Count > 0 ? cars.Average(c => c.TotalWaitTime) : 0.0;
        }

        public int GetTotalQueue() => AllLanes.Sum(l => l.GetQueueLength());
    }
}