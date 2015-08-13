using System;
using System.Collections.Generic;

namespace HyperNeatLib.Interfaces
{
    public interface INetwork : ICloneable
    {
        List<INeuron> HiddenNodes { get; set; }
        
        List<IConnection> Connections { get; set; }

        List<INeuron> Inputs { get; set; }

        List<INeuron> Outputs { get; set; }

        List<INeuron> Neurons { get; }

        INeuron BiasNeuron { get; set; }

        Dictionary<int, double> Position { get; } 

        double Fitness { get; set; }

        int Generation { get; set; }

        ISpecie Specie { get; set; }

        void SetInputs(params double[] inputs);

        double[] GetOutputs();

        void RandomizeConnectionWeights(Random random);

        void Mutate(Random random);
    }
}