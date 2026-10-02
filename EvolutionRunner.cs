using System;

namespace SAi_KR
{
    public static class EvolutionRunner
    {
        public static double EvaluateGenome(Genome genome, SimulationEngine engine, int simulationSteps, double dt = 0.5, int seed = 42)
        {
            engine.ResetWithSeed(seed);
            engine.CurrentControlMode = ControlMode.IntelligentAgents;

            int weightsPerAgent = engine.Agents[0].Brain.TotalWeightsCount;
            int totalRequiredWeights = weightsPerAgent * engine.Agents.Count;

            if (genome.Genes.Length != totalRequiredWeights)
            {
                throw new ArgumentException($"Длина генома ({genome.Genes.Length}) не соответствует суммарному количеству весов агентов ({totalRequiredWeights}).");
            }

            for (int i = 0; i < engine.Agents.Count; i++)
            {
                double[] agentWeights = new double[weightsPerAgent];
                Array.Copy(genome.Genes, i * weightsPerAgent, agentWeights, 0, weightsPerAgent);
                engine.Agents[i].Brain.SetWeights(agentWeights);
            }

            for (int step = 0; step < simulationSteps; step++)
            {
                engine.Step(dt);
            }

            return engine.CalculateFitness();
        }
    }
}