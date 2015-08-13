using System.Collections.Generic;

namespace HyperNeatLib.Interfaces
{
    public interface ISpecie
    {
        List<INetwork> Networks { get; set; }
        
        Dictionary<int, double> Centroid { get; set; }

        int TargetSize { get; set; }

        int EliteSize { get; set; }

        int AsexualSize { get; set; }

        int SexualSize { get; set; }

        int SelectionSize { get; set; }

        double CalculateTotalFitness();

        double CalculateMeanFitness();

        double CalculateTotalComplexity();

        double CalculateMeanComplexity();

        double MaxGenomeFitness { get; }
    }
}