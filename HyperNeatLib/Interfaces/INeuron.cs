using System;

namespace HyperNeatLib.Interfaces
{
    public interface INeuron : ICloneable
    {
        int Id { get; set; }

        NeuronType Type { get; set; }

        IActivationFunction ActivationFunction { get; set; }

        double[] AuxValues { get; set; }

        bool AcceptsAuxValues { get; }

        double Input { get; set; }

        double Output { get; set; }

        void Calculate();
    }
}