using System;
using System.Collections.Generic;
using System.Linq;

namespace SAi_KR
{
    public class Genome
    {
        public double[] Genes { get; set; }
        public double Fitness { get; set; }

        public Genome(int length, Random rand)
        {
            Genes = new double[length];
            for (int i = 0; i < length; i++)
            {
                Genes[i] = rand.NextDouble() * 2.0 - 1.0;
            }
        }

        public Genome(double[] genes)
        {
            Genes = (double[])genes.Clone();
        }
    }

    public class GeneticTrainer
    {
        private readonly int _populationSize;
        private readonly double _mutationRate;
        private readonly double _mutationStrength;
        private readonly int _genomeLength;
        private readonly Random _rand = new Random();

        public List<Genome> Population { get; private set; }
        public Genome BestGenome { get; private set; } = null!;
        public int Generation { get; private set; } = 0;

        public GeneticTrainer(int populationSize, int genomeLength, double mutationRate = 0.05, double mutationStrength = 0.2)
        {
            _populationSize = populationSize;
            _genomeLength = genomeLength;
            _mutationRate = mutationRate;
            _mutationStrength = mutationStrength;

            Population = new List<Genome>();
            for (int i = 0; i < _populationSize; i++)
            {
                Population.Add(new Genome(_genomeLength, _rand));
            }
            BestGenome = Population[0];
        }

        public void Evolve()
        {
            Population = Population.OrderByDescending(g => g.Fitness).ToList();
            BestGenome = new Genome(Population[0].Genes) { Fitness = Population[0].Fitness };

            var newPopulation = new List<Genome>
            {
                new Genome(BestGenome.Genes)
            };

            while (newPopulation.Count < _populationSize)
            {
                Genome parent1 = TournamentSelect(3);
                Genome parent2 = TournamentSelect(3);

                Genome child = Crossover(parent1, parent2);
                Mutate(child);
                newPopulation.Add(child);
            }

            Population = newPopulation;
            Generation++;
        }

        private Genome TournamentSelect(int tournamentSize)
        {
            Genome? best = null;
            for (int i = 0; i < tournamentSize; i++)
            {
                Genome candidate = Population[_rand.Next(_populationSize)];
                if (best == null || candidate.Fitness > best.Fitness)
                {
                    best = candidate;
                }
            }
            return best ?? Population[0];
        }

        private Genome Crossover(Genome p1, Genome p2)
        {
            double[] childGenes = new double[_genomeLength];
            int crossoverPoint = _rand.Next(_genomeLength);

            for (int i = 0; i < _genomeLength; i++)
            {
                childGenes[i] = (i < crossoverPoint) ? p1.Genes[i] : p2.Genes[i];
            }

            return new Genome(childGenes);
        }

        private void Mutate(Genome genome)
        {
            for (int i = 0; i < _genomeLength; i++)
            {
                if (_rand.NextDouble() < _mutationRate)
                {
                    genome.Genes[i] += (_rand.NextDouble() * 2.0 - 1.0) * _mutationStrength;
                }
            }
        }
    }
}