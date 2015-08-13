using System;
using System.ComponentModel.Composition;

using HyperNeatLib.Interfaces;

namespace HyperNeatLib.ActivationFunctions
{
    [Export(typeof(IActivationFunction))]
    public class BiasActivationFunction : IActivationFunction
    {
        public double Calc(double input)
        {
            return 1;
        }

        public bool AcceptsAuxValues => false;

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
            return new BiasActivationFunction();
        }
    }
}