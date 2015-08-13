using System;
using System.ComponentModel.Composition;

using HyperNeatLib.Interfaces;

namespace HyperNeatLib.ActivationFunctions
{
    [Export(typeof(IActivationFunction))]
    public class LinearActivationFunction : IActivationFunction
    {
        public double Calc(double input)
        {
            if (input < -1.0)
            {
                return -1.0;
            }

            if (input > 1.0)
            {
                return 1.0;
            }

            return input;
        }

        public bool AcceptsAuxValues
        {
            get
            {
                return false;
            }
        }

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
            return new LinearActivationFunction();
        }
    }
}