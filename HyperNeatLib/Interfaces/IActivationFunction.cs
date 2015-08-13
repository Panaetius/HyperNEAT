using System;

namespace HyperNeatLib.Interfaces
{
    public interface IActivationFunction:ICloneable
    {
        double Calc(double input);

        bool AcceptsAuxValues { get; }

        double[] AuxValues { get; set; }

        void RandomizeAuxValues();

        void MutateAuxValues(Random random);
    }
}