using System.Collections.Generic;
using System.Linq;

using HyperNeatLib.Interfaces;

namespace HyperNeatLib.NEATImpl
{
    public class Specie : ISpecie
    {
        public Specie()
        {
            Networks = new List<INetwork>();
        }

        public List<INetwork> Networks { get; set; }

        public Dictionary<int, double> Centroid { get; set; }

        public int TargetSize { get; set; }

        public int EliteSize { get; set; }

        public int AsexualSize { get; set; }

        public int SexualSize { get; set; }

        public int SelectionSize { get; set; }

        public double CalculateTotalFitness()
        {
            return Networks.Sum(n => n.Fitness);
        }

        public double CalculateMeanFitness()
        {
            return Networks.Count == 0 ? 0 : this.CalculateTotalFitness() / Networks.Count;
        }

        public double CalculateTotalComplexity()
        {
            throw new System.NotImplementedException();
        }

        public double CalculateMeanComplexity()
        {
            throw new System.NotImplementedException();
        }

        public double MaxGenomeFitness => Networks.Count == 0 ? 0: Networks.Max(n => n.Fitness);

        public override string ToString()
        {
            return $"{this.Networks.Count}, {this.MaxGenomeFitness}";
        }
    }
}