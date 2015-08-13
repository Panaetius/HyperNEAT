namespace HyperNeatLib.Helpers
{
    public sealed class GenerationIdSingleton
    {
        private int connectionGeneration = 1;

        private int neuronGeneration = 1;

        public static GenerationIdSingleton Instance { get; } = new GenerationIdSingleton();

        public int NextConnectionGeneration => this.connectionGeneration++;

        public int NextNeuronGeneration => this.neuronGeneration++;
    }
}