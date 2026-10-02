using System.Collections.Generic;

namespace SAi_KR
{
    public class TrafficFacts
    {
        public double CurrentPhaseDuration { get; set; }
        public double MinPhaseDuration { get; set; } = 10.0; // Мин. зеленый по ГОСТ
        public double MaxPhaseDuration { get; set; } = 60.0; // Макс. удержание фазы
        public bool IsYellowActive { get; set; }
        public int OpposingQueueLength { get; set; }
        public int GreenQueueLength { get; set; }
        public int ApproachingPlatoonCount { get; set; }     // Автомобили во взводе, движущиеся на зеленый (v > 1.5 м/с)
        public double NetworkDecisionOutput { get; set; }   // Сигнал нейросети (> 0.5 — запрос смены)
        
        public double MinGreenExtension { get; set; } = 5.0;
        public double AverageGreenLaneSpeed { get; set; }
        public bool EmergencyVehicleDetected { get; set; }
        public bool IsMajorCorridorPhase { get; set; }
        public bool IsUpstreamCorridorGreen { get; set; }
    }

    public class SafetyRuleEngine
    {
        public List<string> FiredRules { get; } = new List<string>();

        /// <summary>
        /// Механизм логического вывода (прямой вывод) для верификации решений нейросети
        /// </summary>
        public bool ValidatePhaseSwitch(TrafficFacts facts, out string ruleTriggered)
        {
            FiredRules.Clear();

            // Правило 1: Запрет переключения во время переходного такта (желтого сигнала / AllRed)
            FiredRules.Add("Оценка Правила 1 (Желтый сигнал)");
            if (facts.IsYellowActive)
            {
                ruleTriggered = "Правило 1: Активен такт желтого сигнала. Переключение заблокировано.";
                FiredRules.Add("-> " + ruleTriggered);
                return false;
            }

            // Правило 6: Приоритетный проезд экстренных служб.
            FiredRules.Add("Оценка Правила 6 (Экстренные службы)");
            if (facts.EmergencyVehicleDetected)
            {
                ruleTriggered = "Правило 6: Приоритетный проезд экстренных служб.";
                FiredRules.Add("-> " + ruleTriggered);
                return true;
            }

            // Правило 2: Запрет переключения до истечения минимальной длительности фазы
            FiredRules.Add("Оценка Правила 2 (Минимальная длительность)");
            if (facts.CurrentPhaseDuration < facts.MinPhaseDuration)
            {
                ruleTriggered = "Правило 2: Минимальный интервал зеленого сигнала не достигнут.";
                FiredRules.Add("-> " + ruleTriggered);
                return false;
            }

            // Правило 3: Принудительное переключение при превышении лимита времени удержания
            FiredRules.Add("Оценка Правила 3 (Максимальное удержание)");
            if (facts.CurrentPhaseDuration >= facts.MaxPhaseDuration && facts.OpposingQueueLength > 0)
            {
                ruleTriggered = "Правило 3: Превышен лимит удержания фазы при наличии очереди на поперечном направлении.";
                FiredRules.Add("-> " + ruleTriggered);
                return true;
            }

            // Правило 5: Динамическая разгрузка магистрального коридора Северной и координация «зеленой волны»
            FiredRules.Add("Оценка Правила 5 (Удержание коридора Северной)");
            if (facts.CurrentPhaseDuration < (facts.MinPhaseDuration + facts.MinGreenExtension) && facts.AverageGreenLaneSpeed > 5.0)
            {
                ruleTriggered = "Правило 5: Продление зеленого для группы движущихся ТС (скорость > 5 м/с).";
                FiredRules.Add("-> " + ruleTriggered);
                return false;
            }

            if (facts.IsMajorCorridorPhase && facts.CurrentPhaseDuration < facts.MaxPhaseDuration)
            {
                if (facts.IsUpstreamCorridorGreen)
                {
                    ruleTriggered = "Правило 5: Координация «зеленой волны» — прием пакета автомобилей по коридору.";
                    FiredRules.Add("-> " + ruleTriggered);
                    return false;
                }
            }

            // Правило 7: Досрочный сброс второстепенной фазы при полном освобождении боковой улицы (досрочное завершение такта)
            // Фаза боковой улицы сбрасывается ТОЛЬКО если на ней действительно нет ни стоящих (GreenQueueLength == 0),
            // ни движущихся машин (ApproachingPlatoonCount == 0), и истек минимальный такт безопасности.
            FiredRules.Add("Оценка Правила 7 (Досрочный возврат на магистраль)");
            if (!facts.IsMajorCorridorPhase 
                && facts.CurrentPhaseDuration >= facts.MinPhaseDuration 
                && facts.GreenQueueLength == 0 
                && facts.ApproachingPlatoonCount == 0 
                && facts.OpposingQueueLength > 0)
            {
                ruleTriggered = "Правило 7: Боковая улица полностью свободна. Досрочный возврат зеленого на магистраль.";
                FiredRules.Add("-> " + ruleTriggered);
                return true;
            }

            // Правило 8: Принудительный возврат к магистрали при критическом накоплении на Северной
            // Срабатывает только если на Северной образовалась большая очередь (>= 12),
            // которая значительно превышает очередь на боковой улице, и боковая улица уже получила гарантированный зеленый.
            FiredRules.Add("Оценка Правила 8 (Приоритет магистрали)");
            if (!facts.IsMajorCorridorPhase 
                && facts.CurrentPhaseDuration >= facts.MinPhaseDuration
                && facts.OpposingQueueLength >= 12 
                && facts.OpposingQueueLength > facts.GreenQueueLength * 1.5)
            {
                ruleTriggered = "Правило 8: Критическое накопление очереди на магистрали Северной. Возврат зеленого сигнала.";
                FiredRules.Add("-> " + ruleTriggered);
                return true;
            }

            // Правило 4: Санкционирование решения нейросети
            FiredRules.Add("Оценка Правила 4 (Решение нейросети)");
            if (facts.NetworkDecisionOutput >= 0.5)
            {
                // Приоритет пропуска группы ТС для магистрального коридора
                if (facts.IsMajorCorridorPhase && facts.ApproachingPlatoonCount > 0 && facts.CurrentPhaseDuration < facts.MaxPhaseDuration)
                {
                    ruleTriggered = "Правило 4c: Решение ИНС отложено — пропуск приближающегося взвода по магистрали.";
                    FiredRules.Add("-> " + ruleTriggered);
                    return false;
                }

                // Удержание зеленого на боковой улице при наличии очереди
                if (!facts.IsMajorCorridorPhase && facts.GreenQueueLength > 0 && facts.GreenQueueLength >= facts.OpposingQueueLength)
                {
                    ruleTriggered = "Правило 4d: Решение ИНС отклонено — на боковой улице сохраняется очередь.";
                    FiredRules.Add("-> " + ruleTriggered);
                    return false;
                }

                if (facts.GreenQueueLength > 0 && facts.OpposingQueueLength == 0)
                {
                    ruleTriggered = "Правило 4b: Решение ИНС отклонено — на поперечном направлении нет машин.";
                    FiredRules.Add("-> " + ruleTriggered);
                    return false;
                }

                ruleTriggered = "Правило 4: Решение нейросети о смене фазы подтверждено базой знаний.";
                FiredRules.Add("-> " + ruleTriggered);
                return true;
            }

            ruleTriggered = "Правило по умолчанию: Продление текущей фазы.";
            FiredRules.Add("-> " + ruleTriggered);
            return false;
        }
    }
}