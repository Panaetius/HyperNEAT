using System;
using System.ComponentModel.Composition;

using HyperNeatLib.Interfaces;

namespace HyperNeatLib.ActivationFunctions
{
    [Export(typeof(IActivationFunction))]
    public class BipolarGaussianActivationFunction : IActivationFunction
    {
        public object Clone()
        {
            return new BipolarGaussianActivationFunction();
        }

        public double Calc(double input)
        {
            return (2.0 * Math.Exp(-Math.Pow(input * 2.5, 2.0))) - 1.0;
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