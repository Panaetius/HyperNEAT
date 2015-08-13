using System;
using System.Threading.Tasks;

using HyperNeatLib.Factories;
using HyperNeatLib.Interfaces;

namespace HyperNeat.XorTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var population = PopulationFactory.CreatePopulation(2, 1, 1000);

            population.InitialSpeciesSize = 50;

            RunTrading(population);
        }

        private static void RunTrading(IPopulation population)
        {
            var random = new Random();

            var lockObject = new Object();

            while (true)
            {
                var bestGenomeFitness = 0.0;

                INetwork bestGenome = null;

                Parallel.ForEach(
                    population.Networks,
                    network =>
                        {
                            var fitness = 0.0;
                            var correctSide = 0;

                            network.SetInputs(0,0);

                            var output = network.GetOutputs()[0];

                            fitness += 1 - Math.Abs(0 - output);
                            correctSide += output < 0.5 ? 1 : 0;

                            network.SetInputs(0, 1);

                            output = network.GetOutputs()[0];

                            fitness += 1 - Math.Abs(1 - output);
                            correctSide += output >= 0.5 ? 1 : 0;

                            network.SetInputs(1, 0);

                            output = network.GetOutputs()[0];

                            fitness += 1 - Math.Abs(1 - output);
                            correctSide += output >= 0.5 ? 1 : 0;

                            network.SetInputs(1, 1);

                            output = network.GetOutputs()[0];

                            fitness += 1 - Math.Abs(0 - output);
                            correctSide += output < 0.5 ? 1 : 0;

                            network.Fitness = fitness + (correctSide == 4 ? 10 : 0);

                            lock (lockObject)
                            {
                                if (network.Fitness > bestGenomeFitness)
                                {
                                    bestGenomeFitness = network.Fitness;
                                    bestGenome = network;
                                }
                            }
                        });

                Console.WriteLine("Gen: {0}, Best Genome Fitness: {1}", population.CurrentGeneration, bestGenomeFitness);

                population.CalculateNextPopulation();
            }
        }
    }
}
