using System.Collections.Generic;

namespace HyperNeatLib.Interfaces
{
    public interface IPopulation
    {
        int CurrentGeneration { get; set; }

        double MutationRate { get; set; }

        double CrossoverRate { get; set; }

        double SpecieEliteProportion { get; set; }

        double AsexualProportion { get; set; }

        double SelectionProportion { get; set; }

        double InterSpeciesMatingProportion { get; set; }

        double CopyDisjointGenesProbability { get; set; }

        int PopulationSize { get; set; }

        int InitialSpeciesSize { get; set; }
        

        List<INetwork>  Networks { get; set; }

        List<ISpecie> Species { get; set; }

        void SpeciateOffspring(List<INetwork> offspring);

        void Speciate();

        void SortByFitness();

        void CalculateNextPopulation();
    }
}