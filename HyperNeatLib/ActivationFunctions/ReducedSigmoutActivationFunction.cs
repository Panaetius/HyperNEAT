﻿using System;
using System.ComponentModel.Composition;

using HyperNeatLib.Interfaces;

namespace HyperNeatLib.ActivationFunctions
{
    [Export(typeof(IActivationFunction))]
    public class ReducedSigmoutActivationFunction : IActivationFunction
    {
        public object Clone()
        {
            return new ReducedSigmoutActivationFunction();
        }

        public double Calc(double input)
        {
            return 1.0 / (1.0 + Math.Exp(-0.5 * input));
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