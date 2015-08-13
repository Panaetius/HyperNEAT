
using System;

using HyperNeatLib.Interfaces;
using HyperNeatLib.NEATImpl;

namespace HyperNeatLib.Factories
{
    public static class PopulationFactory
    {
        public static IPopulation CreatePopulation(int inputs, int outputs, int populationSize)
        {
            var random = new Random();
            var population = new Population();

            population.PopulationSize = populationSize;

            var network = NetworkFactory.CreateNetwork(inputs, outputs);

            network.Generation = 1;

            population.Networks.Add(network);

            for (int i = 0; i < populationSize - 1; i++)
            {
                var newNetwork = (INetwork)network.Clone();

                newNetwork.Generation = 1;
                newNetwork.RandomizeConnectionWeights(random);

                population.Networks.Add(newNetwork);
            }

            return population;
        }
    }
}