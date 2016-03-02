using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using HyperNeatLib.Factories;
using HyperNeatLib.Helpers;
using HyperNeatLib.Interfaces;

namespace HyperNeatLib.NEATImpl
{
    public class Network : INetwork
    {
        public Network()
        {
            this.HiddenNodes = new List<INeuron>();
            Connections = new List<IConnection>();
            EnabledConnections = new List<IConnection>();
            Inputs = new List<INeuron>();
            Outputs = new List<INeuron>();
        }

        private const int ActivationSteps = 5;

        public List<INeuron> HiddenNodes { get; set; }

        public List<IConnection> Connections { get; set; }

        public List<IConnection> EnabledConnections { get; set; }

        public List<INeuron> Inputs { get; set; }

        public List<INeuron> Outputs { get; set; }

        public List<INeuron> Neurons
        {
            get
            {
                return Inputs.Concat(Outputs).Concat(HiddenNodes).Concat(new List<INeuron>() { BiasNeuron }).ToList();
            }
        }

        public INeuron BiasNeuron { get; set; }

        public Dictionary<int, double> Position
        {
            get
            {
                return Connections.ToDictionary(c => c.Id, c => c.Weight);
            }
        }

        public double Fitness { get; set; }

        public double Score { get; set; }

        public int Generation { get; set; }

        public ISpecie Specie { get; set; }

        public void SetInputs(params double[] inputs)
        {
            foreach (var t in this.Inputs.Zip(inputs, Tuple.Create))
            {
                t.Item1.Input = t.Item2;
            }

            foreach (var node in Inputs)
            {
                node.Calculate();
            }

            BiasNeuron.Calculate();
        }

        public double[] GetOutputs()
        {
            for (int i = 0; i < ActivationSteps; i++)
            {
                foreach (var connection in EnabledConnections)
                {
                    connection.Calculate();
                }

                foreach (var node in this.HiddenNodes)
                {
                    node.Calculate();
                    node.Input = 0.0;
                }

                foreach (var node in Outputs)
                {
                    node.Calculate();
                    node.Input = 0.0;
                }
            }

            return Outputs.Select(o => o.Output).ToArray();
        }

        public void RandomizeConnectionWeights(Random random)
        {
            foreach (var connection in Connections)
            {
                connection.Weight = random.NextDouble() * 2 - 1;
            }
        }

        public void Mutate(Random random)
        {
            var max = MutationParameterSingleton.MutateConnectionWeightsChance
                      + MutationParameterSingleton.AddConnectionChance + MutationParameterSingleton.AddNeuronChance
                      + MutationParameterSingleton.MutateAuxChance + MutationParameterSingleton.MutateNeuronChance;

            while (true)
            {
                var chance = random.NextDouble() * max;

                if (chance < MutationParameterSingleton.MutateConnectionWeightsChance)
                {
                    this.MutateConnection(random);
                    return;
                }

                if (chance < MutationParameterSingleton.MutateConnectionWeightsChance + MutationParameterSingleton.AddConnectionChance)
                {
                    if (MutateAddConnection(random))
                    {
                        return;
                    }
                }

                if (chance < MutationParameterSingleton.MutateConnectionWeightsChance + MutationParameterSingleton.AddConnectionChance
                    + MutationParameterSingleton.AddNeuronChance)
                {
                    MutateAddNeuron(random);
                    return;
                }

                if (chance < MutationParameterSingleton.MutateConnectionWeightsChance + MutationParameterSingleton.AddConnectionChance
                    + MutationParameterSingleton.AddNeuronChance + MutationParameterSingleton.MutateNeuronChance)
                {
                    if (MutateNeuron(random))
                    {
                        return;
                    }
                }

                if (chance < MutationParameterSingleton.MutateConnectionWeightsChance + MutationParameterSingleton.AddConnectionChance
                    + MutationParameterSingleton.AddNeuronChance + MutationParameterSingleton.MutateNeuronChance + MutationParameterSingleton.MutateAuxChance)
                {
                    if (MutateAuxWeights(random))
                    {
                        return;
                    }
                }
            }
        }

        private bool MutateNeuron(Random random)
        {
            if (!HiddenNodes.Any())
            {
                return false;
            }

            var neuron = HiddenNodes[random.Next(0, HiddenNodes.Count)];

            neuron.ActivationFunction = ActivationFunctionFactory.Instance.GetRandomActivationFunction();

            return true;
        }

        public void Reset()
        {
            foreach (var neuron in Neurons.Except(new List<INeuron>() { BiasNeuron}))
            {
                neuron.Input = 0.0;
                neuron.Output = 0.0;
            }
        }

        public string Fingerprint()
        {
            var intArray =
                HiddenNodes.OrderBy(n => n.Id)
                    .Select(n => n.Id)
                    .Concat(Connections.OrderBy(c => c.Id).Select(c => c.Id))
                    .SelectMany(BitConverter.GetBytes)
                    .Where(b => b != 0)
                    .ToArray();

            var hex = new StringBuilder(intArray.Length * 2);

            foreach (byte b in intArray)
            {
                hex.AppendFormat("{0:x}", b);
            }

            return hex.ToString();
        }

        private bool MutateAuxWeights(Random random)
        {
            var neuronsWithAuxWeights = Neurons.Where(n => n.AcceptsAuxValues).ToList();

            if (!neuronsWithAuxWeights.Any())
            {
                return false;
            }

            var activationFunction = neuronsWithAuxWeights[random.Next(0, neuronsWithAuxWeights.Count())].ActivationFunction;

            activationFunction.MutateAuxValues(random);

            return true;
        }

        private void MutateAddNeuron(Random random)
        {
            //We don't want to mutate bias connections
            var possibleConnections = Connections.Where(c => c.InputNode.Id != BiasNeuron.Id).ToList();

            var connection = possibleConnections[random.Next(0, possibleConnections.Count)];

            Connections.Remove(connection);

            var result = NeuronFactory.SplitConnection(connection, BiasNeuron, Neurons);

            if (Neurons.Any(n => n.Id == result.Item3.Id))
            {
                throw new Exception("Connection Already Added, WTF?");
            }

            HiddenNodes.Add(result.Item3);

            if (Connections.Any(c => c.Id == result.Item2.Id))
            {
                throw new Exception("Connection Already Added, WTF?");
            }

            Connections.Add(result.Item2);

            if (Connections.Any(c => c.Id == result.Item1.Id))
            {
                throw new Exception("Connection Already Added, WTF?");
            }

            Connections.Add(result.Item1);

            if (result.Item4 != null)
            {
                Connections.Add(result.Item4);
            }
        }

        private bool MutateAddConnection(Random random)
        {
            var validSources = Inputs.Concat(HiddenNodes).ToList();
            var validTargets = Outputs.Concat(HiddenNodes).ToList();

            for (int i = 0; i < 5; i++)
            {
                var source = validSources[random.Next(0, validSources.Count)];

                var target = validTargets[random.Next(0, validTargets.Count())];

                //check that they're not connected already
                if (Connections.Any(c => c.InputNode.Id == source.Id && c.OutputNode.Id == target.Id))
                {
                    continue;
                }

                var newConnection = ConnectionFactory.CreateConnection(source, target);

                if (Connections.Any(c => c.Id == newConnection.Id))
                {
                    throw new Exception("Connection Already Added, WTF?");
                }

                this.Connections.Add(newConnection);

                if (newConnection.IsEnabled)
                {
                    EnabledConnections.Add(newConnection);
                }

                return true;
            }

            return false;
        }

        private void MutateConnection(Random random)
        {
            var connection = Connections[random.Next(0, Connections.Count)];

            if (random.NextDouble() < MutationParameterSingleton.DisableConnectionChance)
            {
                connection.IsEnabled = !connection.IsEnabled;

                if (!connection.IsEnabled)
                {
                    EnabledConnections.Remove(connection);
                }
                else
                {
                    EnabledConnections.Add(connection);
                }
            }
            else
            {
                connection.Weight += MutationParameterSingleton.GaussianSampler.NextSample(0, 0.02);

                connection.Weight = connection.Weight > 5 ? 5 : connection.Weight < -5 ? -5 : connection.Weight;
            }
        }

        public object Clone()
        {
            var network = new Network();

            foreach (var input in Inputs)
            {
                network.Inputs.Add((INeuron)input.Clone());
            }

            foreach (var output in Outputs)
            {
                network.Outputs.Add((INeuron)output.Clone());
            }

            foreach (var node in this.HiddenNodes)
            {
                network.HiddenNodes.Add((INeuron)node.Clone());
            }

            network.BiasNeuron = (INeuron)BiasNeuron.Clone();

            foreach (var connection in Connections)
            {
                var newConnection =
                    ConnectionFactory.CreateConnection(
                        network.Neurons.Single(n => n.Id == connection.InputNode.Id),
                        network.Neurons.Single(n => n.Id == connection.OutputNode.Id),
                        connection.Weight,
                        connection.IsEnabled,
                        connection.Id);

                if (network.Connections.Any(c => c.Id == newConnection.Id))
                {
                    throw new Exception("Connection Already Added, WTF?");
                }

                network.Connections.Add(newConnection);

                if (newConnection.IsEnabled)
                {
                    EnabledConnections.Add(newConnection);
                }
            }

            return network;
        }

        public override string ToString()
        {
            return $"{this.HiddenNodes.Count}:{this.Connections.Count}, {this.Generation}, {this.Fitness}";
        }
    }
}