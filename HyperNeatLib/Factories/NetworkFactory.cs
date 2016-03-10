using System;
using System.Collections.Generic;
using System.Linq;

using HyperNeatLib.Interfaces;
using HyperNeatLib.NEATImpl;

namespace HyperNeatLib.Factories
{
    public static class NetworkFactory
    {
        public static Network CreateNetwork(int inputs, int outputs)
        {
            var network = new Network();

            //create neurons
            for (int i = 0; i < inputs; i++)
            {
                network.Inputs.Add(NeuronFactory.CreateNeuron(NeuronType.Input));
            }

            for (int i = 0; i < outputs; i++)
            {
                network.Outputs.Add(NeuronFactory.CreateNeuron(NeuronType.Output));
            }

            network.BiasNeuron = NeuronFactory.CreateNeuron(NeuronType.Bias);

            //create connections

            var random = new Random();

            foreach (var input in network.Inputs.Concat(new List<INeuron>() { network.BiasNeuron }))
            {
                foreach (var output in network.Outputs)
                {
                    var conn = ConnectionFactory.CreateConnection(input, output, random.NextDouble());
                    network.Connections.Add(conn);
                }
            }

            network.RandomizeConnectionWeights(random);

            return network;
        }

        public static Network CreateEmptyNetworkFromExisting(INetwork existing)
        {
            var network = new Network();

            network.Inputs = existing.Inputs.Select(i => (INeuron)i.Clone()).ToList();
            network.Outputs = existing.Outputs.Select(i => (INeuron)i.Clone()).ToList();

            network.BiasNeuron = (INeuron)existing.BiasNeuron.Clone();

            //create connections

            var random = new Random();

            foreach (var input in network.Inputs.Concat(new List<INeuron>() { network.BiasNeuron }))
            {
                foreach (var output in network.Outputs)
                {
                    var conn = ConnectionFactory.CreateConnection(input, output, random.NextDouble());
                    network.Connections.Add(conn);
                }
            }

            network.RandomizeConnectionWeights(random);

            return network;
        }

        internal static INetwork CreateNetwork(List<INeuron> neurons, List<IConnection> connections)
        {
            var network = new Network();

            network.Inputs = neurons.Where(n => n.Type == NeuronType.Input).ToList();
            network.Outputs = neurons.Where(n => n.Type == NeuronType.Output).ToList();
            network.BiasNeuron = neurons.SingleOrDefault(n => n.Type == NeuronType.Bias);
            network.HiddenNodes = neurons.Where(n => n.Type == NeuronType.Hidden).ToList();

            network.Connections = connections;

            return network;
        }
    }
}