using System;
using System.Collections.Generic;
using System.Linq;

using HyperNeatLib.ActivationFunctions;
using HyperNeatLib.Helpers;
using HyperNeatLib.Interfaces;
using HyperNeatLib.NEATImpl;

namespace HyperNeatLib.Factories
{
    public static class NeuronFactory
    {
        /// <summary>
        /// key = original connection id
        /// Tuple:
        ///     1: new source connection id
        ///     2: new target connection id
        ///     3: new neuron id
        /// </summary>
        public static Dictionary<int, List<Tuple<int, int, int, int>>> SplitConnections = new Dictionary<int, List<Tuple<int, int, int, int>>>();

        public static INeuron CreateNeuron(NeuronType type, int? id = null)
        {
            Neuron neuron;

            switch (type)
            {
                case NeuronType.Input:
                    neuron= new Neuron();
                    neuron.ActivationFunction = new NullActivationFunction();
                    neuron.Id = id ?? GenerationIdSingleton.Instance.NextNeuronGeneration;
                    neuron.Type = type;
                    break;
                case NeuronType.Bias:
                    neuron = new Neuron();
                    neuron.ActivationFunction = new BiasActivationFunction();
                    neuron.Id = id ?? GenerationIdSingleton.Instance.NextNeuronGeneration;
                    neuron.Type = type;
                    break;
                case NeuronType.Output:
                    neuron = new Neuron();
                    neuron.ActivationFunction = new LinearActivationFunction();
                    neuron.Id = id ?? GenerationIdSingleton.Instance.NextNeuronGeneration;
                    neuron.Type = type;
                    break;
                case NeuronType.Hidden:
                    neuron = new Neuron();
                    neuron.ActivationFunction = ActivationFunctionFactory.Instance.GetRandomActivationFunction();
                    neuron.Id = id ?? GenerationIdSingleton.Instance.NextNeuronGeneration;
                    neuron.Type = type;
                    break;
                default:
                    throw new NotImplementedException();
            }

            return neuron;
        }

        public static Tuple<IConnection, IConnection, INeuron, IConnection> SplitConnection(IConnection connection, INeuron bias, List<INeuron> neurons)
        {
            INeuron neuron;
            IConnection connection1, connection2, connection3 = null;

            Tuple<int, int, int, int> existing = null;

            if (SplitConnections.ContainsKey(connection.Id))
            {
                //mutation of this connection already occurred
                existing = SplitConnections[connection.Id].FirstOrDefault(t => neurons.All(n => n.Id != t.Item3));
            }

            if (existing != null)
            {
                neuron = CreateNeuron(NeuronType.Hidden, existing.Item3);

                connection1 = ConnectionFactory.CreateConnection(connection.InputNode, neuron, id: existing.Item1);

                connection2 = ConnectionFactory.CreateConnection(neuron, connection.OutputNode, id: existing.Item2);

                if (connection.InputNode.Type != NeuronType.Bias)
                {
                    connection3 = ConnectionFactory.CreateConnection(bias, neuron, id: existing.Item4);
                }
            }
            else
            {
                neuron = CreateNeuron(NeuronType.Hidden);

                connection1 = ConnectionFactory.CreateConnection(connection.InputNode, neuron);

                connection2 = ConnectionFactory.CreateConnection(neuron, connection.OutputNode);

                if (connection.InputNode.Type != NeuronType.Bias)
                {
                    connection3 = ConnectionFactory.CreateConnection(bias, neuron);
                }

                if (!SplitConnections.ContainsKey(connection.Id))
                {
                    SplitConnections.Add(connection.Id, new List<Tuple<int, int, int, int>>());
                }

                SplitConnections[connection.Id].Add(Tuple.Create(connection1.Id, connection2.Id, neuron.Id, connection3?.Id ?? -1));
            }

            return Tuple.Create(connection1, connection2, neuron, connection3);
        }
    }
}