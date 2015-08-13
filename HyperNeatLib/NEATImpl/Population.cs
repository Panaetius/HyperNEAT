using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using HyperNeatLib.Factories;
using HyperNeatLib.Interfaces;

namespace HyperNeatLib.NEATImpl
{
    public class Population : IPopulation
    {
        public Population()
        {
            Networks = new List<INetwork>();
            Species = new List<ISpecie>();
            CurrentGeneration = 1;
        }

        public int CurrentGeneration { get; set; }

        private const int MaxKmeansLoop = 5;

        public double MutationRate { get; set; }

        public double CrossoverRate { get; set; }

        public double SpecieEliteProportion { get; set; } = 0.2;

        public double AsexualProportion { get; set; } = 0.5;

        public double SelectionProportion { get; set; } = 0.2;

        public double InterSpeciesMatingProportion { get; set; } = 0.01;

        public double CopyDisjointGenesProbability { get; set; } = 0.1;

        public int InitialSpeciesSize { get; set; } = 5;

        public List<INetwork> Networks { get; set; }

        public List<ISpecie> Species { get; set; }

        public int PopulationSize { get; set; }

        public void SpeciateOffspring(List<INetwork> offspring)
        {
            Parallel.ForEach(Species, CalculateCentroid);

            foreach (var o in offspring)
            {
                var closestSpecie = FindClosestSpecie(o.Position);

                o.Specie?.Networks.Remove(o);
                closestSpecie.Networks.Add(o);
                o.Specie = closestSpecie;
            }

            Parallel.ForEach(Species, CalculateCentroid);

            Networks = Species.SelectMany(s => s.Networks).ToList();

            this.KMeansSpeciate();
        }

        public void Speciate()
        {
            var random = new Random();

            var genomes = Networks.OrderBy(_ => random.NextDouble()).ToList();

            for (int i = 0; i < this.InitialSpeciesSize; i++)
            {
                var specie = new Specie();
                specie.Centroid = genomes[i].Position;
                specie.Networks.Add(genomes[i]);
                genomes[i].Specie?.Networks.Remove(genomes[i]);
                genomes[i].Specie = specie;

                Species.Add(specie);
            }

            //assign each remaining genome to closest specie
            for (int i = this.InitialSpeciesSize; i < PopulationSize; i++)
            {
                var genome = genomes[i];
                var position = genome.Position;
                var closestSpecie = this.FindClosestSpecie(position);

                closestSpecie.Networks.Add(genome);
                genome.Specie?.Networks.Remove(genome);
                genome.Specie = closestSpecie;
            }

            //calculate new centroid for each specie
            Parallel.ForEach(Species, CalculateCentroid);

            this.KMeansSpeciate();
        }

        private void KMeansSpeciate()
        {
            for (int i = 0; i < MaxKmeansLoop; i++)
            {
                bool modified = false;

                foreach (var genome in Networks)
                {
                    var closestSpecie = this.FindClosestSpecie(genome.Position);
                    if (closestSpecie != genome.Specie)
                    {
                        genome.Specie?.Networks.Remove(genome);
                        closestSpecie.Networks.Add(genome);
                        genome.Specie = closestSpecie;

                        modified = true;
                    }
                }

                if (!modified)
                {
                    break;
                }

                Parallel.ForEach(this.Species.Where(s => s.Networks.Any()), CalculateCentroid);

                var genomeMaxDistance = this.Networks.OrderByDescending(n => this.MeasureDistance(n.Position, n.Specie.Centroid)).ToList();

                var emptySpecies = this.Species.Where(s => !s.Networks.Any()).ToList();

                int currentGenome = -1;

                var modifiedSpecies = new List<ISpecie>();

                foreach (var emptySpecie in emptySpecies)
                {
                    //find first genome in a species with more than 1 genome
                    while (genomeMaxDistance[++currentGenome].Specie.Networks.Count == 1)
                    {
                    }

                    var genome = genomeMaxDistance[currentGenome];

                    modifiedSpecies.Add(genome.Specie);
                    genome.Specie?.Networks.Remove(genome);
                    emptySpecie.Networks.Add(genome);
                    genome.Specie = emptySpecie;
                }

                Parallel.ForEach(modifiedSpecies.Concat(emptySpecies), CalculateCentroid);

                if (!modifiedSpecies.Any() && !emptySpecies.Any())
                {
                    break;
                }
            }
        }

        private ISpecie FindClosestSpecie(Dictionary<int, double> position)
        {
            var closestSpecie = this.Species[0];
            var closestDistance = this.MeasureDistance(position, closestSpecie.Centroid);

            foreach (var specie in this.Species)
            {
                var distance = this.MeasureDistance(position, specie.Centroid);
                if (distance < closestDistance)
                {
                    closestSpecie = specie;
                    closestDistance = distance;
                }
            }
            return closestSpecie;
        }

        private static void CalculateCentroid(ISpecie s)
        {
            if (s.Networks.Count == 1)
            {
                s.Centroid = s.Networks[0].Position;
            }
            else
            {
                s.Centroid = s.Networks.SelectMany(n => n.Position)
                    .GroupBy(p => p.Key)
                    .ToDictionary(g => g.Key, g => g.Sum(p => p.Value) / g.Count());
            }
        }

        private double MeasureDistance(Dictionary<int, double> position1, Dictionary<int, double> position2)
        {
            var distance = 0.0;

            if (!position1.Any() && !position2.Any())
            {
                return distance;
            }

            if (!position1.Any())
            {
                return position2.Count * 10.0;
            }

            if (!position2.Any())
            {
                return position1.Count * 10.0;
            }

            int pos1 = 0;
            int pos2 = 0;

            var first = position1.ElementAt(pos1);
            var second = position2.ElementAt(pos2);

            while (true)
            {
                if (first.Key < second.Key)
                {
                    distance += 10.0;

                    pos1++;
                }
                else if (first.Key > second.Key)
                {
                    distance += 10.0;

                    pos2++;
                }
                else
                {
                    distance += Math.Abs(first.Value - second.Value);
                    pos1++;
                    pos2++;
                }

                if (pos1 == position1.Count)
                {
                    distance += 10.0 * (position2.Count - pos2);

                    return distance;
                }

                if (pos2 == position2.Count)
                {
                    distance += 10.0 * (position1.Count - pos1);

                    return distance;
                }

                first = position1.ElementAt(pos1);
                second = position2.ElementAt(pos2);
            }
        }

        public void SortByFitness()
        {
            Networks = Networks.OrderByDescending(n => n.Fitness).ThenByDescending(n => n.Generation).ToList();
        }

        public void CalculateNextPopulation()
        {
            var random = new Random();

            CurrentGeneration++;

            if (!Species.Any())
            {
                this.Speciate();
            }

            this.AdjustSpeciesTargetSizesAccordingToFitness();

            int totalOffspringCount = 0;

            var nonZeroSelectionCount = 0;

            foreach (var specie in Species)
            {
                specie.EliteSize = Math.Min(ProbabilisticRound(specie.Networks.Count * SpecieEliteProportion, random), specie.TargetSize);

                var offspringCount = specie.TargetSize - specie.EliteSize;

                totalOffspringCount += offspringCount;

                specie.AsexualSize = ProbabilisticRound(offspringCount * AsexualProportion, random);
                specie.SexualSize = offspringCount - specie.AsexualSize;

                specie.SelectionSize = Math.Max(ProbabilisticRound(specie.Networks.Count * SelectionProportion, random), 1);

                if (specie.SelectionSize > 0)
                {
                    nonZeroSelectionCount++;
                }
            }

            var offspringList = new List<INetwork>();

            INetwork offspring;

            foreach (var specie in Species)
            {
                var speciesTotalFitness = specie.CalculateTotalFitness();
                //asexual reproduction
                for (int i = 0; i < specie.AsexualSize; i++)
                {
                    offspring = ReproduceAsexually(specie, speciesTotalFitness, random);

                    offspring.Generation = CurrentGeneration;

                    offspringList.Add(offspring);
                }

                var crossSpecieMatings = nonZeroSelectionCount == 1
                                             ? 0
                                             : ProbabilisticRound(
                                                 specie.SexualSize * InterSpeciesMatingProportion,
                                                 random);

                if (Species.Count(s => s.Networks.Any()) > 1)
                {
                    for (int i = 0; i < crossSpecieMatings; i++)
                    {
                        //cross specie matings
                        offspring = ReproduceSexuallyInterSpecies(specie, speciesTotalFitness, random);

                        offspring.Generation = CurrentGeneration;

                        offspringList.Add(offspring);
                    }
                }
                else
                {
                    crossSpecieMatings = 0;
                }

                //regular sexual reproduction
                if (specie.SelectionSize == 1)
                {
                    //fallback to asexual
                    for (int i = 0; i < specie.SexualSize - crossSpecieMatings; i++)
                    {
                        offspring = ReproduceAsexually(specie, speciesTotalFitness, random);

                        offspring.Generation = CurrentGeneration;

                        offspringList.Add(offspring);
                    }
                }
                else
                {
                    for (int i = 0; i < specie.SexualSize - crossSpecieMatings; i++)
                    {
                        offspring = ReproduceSexuallyIntraSpecies(specie, speciesTotalFitness, random);

                        offspring.Generation = CurrentGeneration;

                        offspringList.Add(offspring);
                    }
                }
            }

            //remove non-elites from species
            foreach (var specie in Species)
            {
                specie.Networks = specie.Networks.OrderByDescending(n => n.Fitness).Take(specie.EliteSize).ToList();
            }

            SpeciateOffspring(offspringList);

            Networks.Clear();

            foreach (var specie in Species)
            {
                Networks.AddRange(specie.Networks);
            }
        }

        private INetwork ReproduceSexuallyInterSpecies(ISpecie specie, double speciesTotalFitness, Random random)
        {
            for (var i = 0; i < 5; i++)
            {
                var val = random.NextDouble() * (Species.Sum(s => s.CalculateTotalFitness()) - speciesTotalFitness);

                var curVal = 0.0;

                ISpecie specie2 = null;

                foreach (var curSpecie in Species.Where(s => s.Networks.Any()))
                {
                    if (curSpecie == specie)
                    {
                        continue;
                    }

                    curVal += curSpecie.CalculateTotalFitness();

                    if (curVal >= val)
                    {
                        specie2 = curSpecie;
                        break;
                    }
                }

                if (specie2 == null)
                {
                    continue;
                }

                for (var j = 0; i < 5; i++)
                {
                    INetwork parent1 = null;

                    INetwork parent2 = null;

                    val = random.NextDouble() * speciesTotalFitness;

                    curVal = 0.0;

                    foreach (var network in specie.Networks)
                    {
                        curVal += network.Fitness;

                        if (curVal >= val)
                        {
                            parent1 = network;
                            break;
                        }
                    }

                    val = random.NextDouble() * specie2.CalculateTotalFitness();

                    curVal = 0.0;

                    foreach (var network in specie2.Networks)
                    {
                        curVal += network.Fitness;

                        if (curVal >= val)
                        {
                            parent2 = network;
                            break;
                        }
                    }

                    if (parent1 == null || parent2 == null || parent1 == parent2)
                    {
                        continue;
                    }

                    return this.CreateOffspring(random, parent2, parent1);
                }
            }

            throw new Exception("Couldn't find suitable parents with random selection, something's wrong");
        }

        private INetwork ReproduceSexuallyIntraSpecies(ISpecie specie, double speciesTotalFitness, Random random)
        {
            for (var i = 0; i < 10; i++)
            {
                INetwork parent1 = null;

                INetwork parent2 = null;

                var val = random.NextDouble() * speciesTotalFitness;

                var curVal = 0.0;

                foreach (var network in specie.Networks)
                {
                    curVal += network.Fitness;

                    if (curVal >= val)
                    {
                        parent1 = network;
                        break;
                    }
                }

                val = random.NextDouble() * (speciesTotalFitness - parent1.Fitness);

                curVal = 0.0;

                foreach (var network in specie.Networks)
                {
                    if (network == parent1)
                    {
                        continue;
                    }
                    curVal += network.Fitness;

                    if (curVal >= val)
                    {
                        parent2 = network;
                        break;
                    }
                }

                if (parent1 == null || parent2 == null || parent1 == parent2)
                {
                    continue;
                }

                return this.CreateOffspring(random, parent2, parent1);
            }

            throw new Exception("Couldn't find suitable parents with random selection, something's wrong");
        }

        private INetwork CreateOffspring(Random random, INetwork parent2, INetwork parent1)
        {
            //make sure parent1 is the fitter one
            if (parent2.Fitness > parent1.Fitness)
            {
                var temp = parent2;
                parent2 = parent1;
                parent1 = temp;
            }

            var copyDisjointGenesFlag = random.NextDouble() < this.CopyDisjointGenesProbability;

            //copy neurons
            var childNeurons = new List<INeuron>();

            childNeurons.AddRange(parent1.Inputs.Select(n => (INeuron)n.Clone()));
            childNeurons.AddRange(parent1.Outputs.Select(n => (INeuron)n.Clone()));
            childNeurons.Add((INeuron)parent1.BiasNeuron.Clone());

            foreach (var matchingNeuron in parent1.HiddenNodes.Where(n1 => parent2.HiddenNodes.Any(n2 => n2.Id == n1.Id)))
            {
                var parentNeuron = matchingNeuron;
                if (random.Next(2) == 0)
                {
                    parentNeuron = parent2.HiddenNodes.First(n => n.Id == matchingNeuron.Id);
                }

                childNeurons.Add((INeuron)parentNeuron.Clone());
            }

            foreach (var parentNeuron in parent1.HiddenNodes.Where(n1 => parent2.HiddenNodes.All(n2 => n1.Id != n2.Id)))
            {
                childNeurons.Add((INeuron)parentNeuron.Clone());
            }

            if (copyDisjointGenesFlag)
            {
                foreach (var parentNeuron in parent2.HiddenNodes.Where(n1 => parent1.HiddenNodes.All(n2 => n1.Id != n2.Id)))
                {
                    childNeurons.Add((INeuron)parentNeuron.Clone());
                }
            }

            var childConnections = new List<IConnection>();

            //copy connections
            foreach (var matchingConnection in parent1.Connections.Where(c1 => parent2.Connections.Any(c2 => c1.Id == c2.Id)))
            {
                var parentConnection = matchingConnection;
                if (random.Next(2) == 0)
                {
                    parentConnection = parent2.Connections.First(n => n.Id == matchingConnection.Id);
                }

                var childConnection = (IConnection)parentConnection.Clone();

                childConnection.InputNode = childNeurons.First(n => n.Id == childConnection.InputNode.Id);
                childConnection.OutputNode = childNeurons.First(n => n.Id == childConnection.OutputNode.Id);

                if (childConnections.Any(c => c.Id == childConnection.Id))
                {
                    throw new Exception("Connection Already Added, WTF?");
                }

                childConnections.Add(childConnection);
            }

            foreach (var disjointConnection in parent1.Connections.Where(c1 => parent2.Connections.All(c2 => c1.Id != c2.Id)))
            {
                var childConnection = (IConnection)disjointConnection.Clone();

                childConnection.InputNode = childNeurons.First(n => n.Id == childConnection.InputNode.Id);
                childConnection.OutputNode = childNeurons.First(n => n.Id == childConnection.OutputNode.Id);

                if (childConnections.Any(c => c.Id == childConnection.Id))
                {
                    throw new Exception("Connection Already Added, WTF?");
                }

                childConnections.Add(childConnection);
            }

            if (copyDisjointGenesFlag)
            {
                foreach (var disjointConnection in parent2.Connections.Where(c1 => parent1.Connections.All(c2 => c1.Id != c2.Id)))
                {
                    var childConnection = (IConnection)disjointConnection.Clone();

                    childConnection.InputNode = childNeurons.First(n => n.Id == childConnection.InputNode.Id);
                    childConnection.OutputNode = childNeurons.First(n => n.Id == childConnection.OutputNode.Id);

                    if (childConnections.Any(c => c.Id == childConnection.Id))
                    {
                        throw new Exception("Connection Already Added, WTF?");
                    }

                    childConnections.Add(childConnection);
                }
            }

            return NetworkFactory.CreateNetwork(childNeurons, childConnections);
        }

        private static INetwork ReproduceAsexually(ISpecie specie, double speciesTotalFitness, Random random)
        {
            var val = random.NextDouble() * speciesTotalFitness;

            var curVal = 0.0;

            foreach (var network in specie.Networks)
            {
                curVal += network.Fitness;

                if (curVal >= val)
                {
                    var networkClone = (INetwork)network.Clone();

                    networkClone.Mutate(random);

                    return networkClone;
                }
            }

            throw new Exception("Couldn't find a single network with random selection, something's wrong");
        }

        private void AdjustSpeciesTargetSizesAccordingToFitness()
        {
            var random = new Random();

            var specieMeanFitness = Species.ToDictionary(s => s, s => s.CalculateMeanFitness());

            var totalMeanFitness = specieMeanFitness.Sum(s => s.Value);

            var totalTargetSize = 0;

            var targetSizes = new Dictionary<ISpecie, Tuple<int, double, double>>();

            if (totalMeanFitness == 0)
            {
                //if no specie has any fitness, just make the next generation the same size
                foreach (var specie in Species)
                {
                    specie.TargetSize = specie.Networks.Count;
                }


                return;
            }

            foreach (var specie in this.Species)
            {
                var targetSize = (specieMeanFitness[specie] / totalMeanFitness * this.PopulationSize);

                var intTargetSize = ProbabilisticRound(targetSize, random);

                targetSizes.Add(specie, Tuple.Create(intTargetSize, targetSize, targetSize - intTargetSize));
                specie.TargetSize = intTargetSize;
                totalTargetSize += specie.TargetSize;
            }

            var deltaTargetSize = totalTargetSize - this.PopulationSize;

            if (deltaTargetSize < 0)
            {
                deltaTargetSize *= -1;

                var totalPositiveDeltaTargetSize = targetSizes.Sum(t => t.Value.Item3 > 0 ? t.Value.Item3 : 0);

                for (int i = 0; i < deltaTargetSize; i++)
                {
                    var val = random.NextDouble() * totalPositiveDeltaTargetSize;

                    var result = new KeyValuePair<ISpecie, Tuple<int, double, double>>();
                    var curVal = 0.0;

                    foreach (var targetSize in targetSizes.Where(t => t.Value.Item3 > 0))
                    {
                        curVal += targetSize.Value.Item3;

                        if (result.Key == null && curVal >= val)
                        {
                            result = targetSize;
                            break;
                        }
                    }

                    totalPositiveDeltaTargetSize -= result.Value.Item3;

                    result.Key.TargetSize += 1;

                    targetSizes[result.Key] = new Tuple<int, double, double>(
                        result.Value.Item1 - 1,
                        result.Value.Item2 - 1,
                        result.Value.Item3 - 1);
                }
            }
            else if (deltaTargetSize > 0)
            {
                var totalnegativeDeltaTargetSize = targetSizes.Sum(t => t.Value.Item3 < 0 ? t.Value.Item3 : 0) * -1;

                for (int i = 0; i < deltaTargetSize; i++)
                {
                    var val = random.NextDouble() * totalnegativeDeltaTargetSize;

                    var result = new KeyValuePair<ISpecie, Tuple<int, double, double>>();
                    var curVal = 0.0;

                    foreach (var targetSize in targetSizes.Where(t => t.Value.Item3 < 0))
                    {
                        curVal += targetSize.Value.Item3 * -1;

                        if (result.Key == null && curVal >= val)
                        {
                            result = targetSize;
                            break;
                        }
                    }

                    totalnegativeDeltaTargetSize += result.Value.Item3;

                    result.Key.TargetSize += 1;

                    targetSizes[result.Key] = new Tuple<int, double, double>(
                        result.Value.Item1 + 1,
                        result.Value.Item2 + 1,
                        result.Value.Item3 + 1);
                }
            }

            var maxFitness = Species.Max(s => s.MaxGenomeFitness);

            //make sure best specie has at least size 1
            var bestSpecie = Species.First(s => s.MaxGenomeFitness == maxFitness);

            if (bestSpecie.TargetSize == 0)
            {
                var val = random.Next(0, Species.Count - 1);
                var spec = Species.ElementAt(val);

                while (spec == bestSpecie || spec.TargetSize < 1)
                {
                    spec = Species.ElementAt(++val);
                }

                spec.TargetSize -= 1;
                bestSpecie.TargetSize += 1;
            }
        }

        private static int ProbabilisticRound(double val, Random random)
        {
            var intVal = (int)Math.Floor(val);

            var fractionalPart = val - intVal;

            intVal = random.NextDouble() < fractionalPart ? intVal + 1 : intVal;
            return intVal;
        }
    }
}