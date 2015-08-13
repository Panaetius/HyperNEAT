using System;
using System.ComponentModel.Composition;

using HyperNeatLib.Interfaces;

namespace HyperNeatLib.ActivationFunctions
{
    [Export(typeof(IActivationFunction))]
    public class GaussianActivationFunction : IActivationFunction
    {
        public object Clone()
        {
            return new PlainSigmoidActivationFunction();
        }

        public double Calc(double input)
        {
            return Math.Exp(-Math.Pow(input * 2.5, 2.0));
        }

        public bool AcceptsAuxValues => false;

        public double[] AuxValues { get; set; }

        public void RandomizeAuxValues()
        {
            throw new NotImplementedException();
        }

        public void MutateAuxValues(Random random)
        {
            throw new NotImplementedException();
        }
    }
}