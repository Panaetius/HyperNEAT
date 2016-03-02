using System;
using System.ComponentModel.Composition;

using HyperNeatLib.Interfaces;

namespace HyperNeatLib.ActivationFunctions
{
    [Export(typeof(IActivationFunction))]
    public class AbsoluteRootActivationFunction : IActivationFunction
    {
        public object Clone()
        {
            return new AbsoluteRootActivationFunction();
        }

        public double Calc(double input)
        {
            if (input < -1.0 || input > 1.0)
            {
                return 1.0;
            }

            return Math.Sqrt(Math.Abs(input));
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