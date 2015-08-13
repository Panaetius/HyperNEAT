using System;
using System.ComponentModel.Composition;

using HyperNeatLib.Interfaces;

namespace HyperNeatLib.ActivationFunctions
{
    [Export(typeof(IActivationFunction))]
    public class PlainSigmoidActivationFunction : IActivationFunction
    {
        public object Clone()
        {
            return new PlainSigmoidActivationFunction();
        }

        public double Calc(double input)
        {
            return 1.0 / (1.0 + Math.Exp(-input));
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