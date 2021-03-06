﻿using System;
using System.ComponentModel.Composition;

using HyperNeatLib.Interfaces;

namespace HyperNeatLib.ActivationFunctions
{
    [Export(typeof(IActivationFunction))]
    public class SineActivationFunction : IActivationFunction
    {
        public object Clone()
        {
            return new SineActivationFunction();
        }

        public double Calc(double input)
        {
            return Math.Sin(2.0 * input);
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