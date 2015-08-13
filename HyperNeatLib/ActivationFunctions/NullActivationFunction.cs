using System;

using HyperNeatLib.Interfaces;

namespace HyperNeatLib.ActivationFunctions
{
    public class NullActivationFunction : IActivationFunction
    {
        public double Calc(double input)
        {
            return input;
        }

        public bool AcceptsAuxValues { get; }

        public double[] AuxValues { get; set; }

        public void RandomizeAuxValues()
        {
            throw new System.NotImplementedException();
        }

        public void MutateAuxValues(Random random)
        {
            throw new NotImplementedException();
        }

        public object Clone()
        {
            return new NullActivationFunction();
        }
    }
}