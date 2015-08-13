using System;

namespace HyperNeatLib.Interfaces
{
    public interface IConnection:ICloneable
    {
        int Id { get; set; }

        INeuron InputNode { get; set; } 

        INeuron OutputNode { get; set; }

        double Weight { get; set; }

        bool IsEnabled { get; set; }

        void Calculate();
    }
}