using System;
using System.ComponentModel.Composition;

using HyperNeatLib.Interfaces;

namespace HyperNeatLib.ActivationFunctions
{
    [Export(typeof(IActivationFunction))]
    public class StepActivationFunction : IActivationFunction
    {
        public double Calc(double input)
        {
            if (input < 0.0)
            {
                return 0.0;
            }

            return 1.0;
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
            return new StepActivationFunction();
        }
    }
}