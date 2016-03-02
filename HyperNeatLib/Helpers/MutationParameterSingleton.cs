namespace HyperNeatLib.Helpers
{
    public static class MutationParameterSingleton
    {
        public const double MutateConnectionWeightsChance = 0.988;

        public const double AddConnectionChance = 0.02;

        public const  double AddNeuronChance = 0.02;

        public const double MutateAuxChance = 0.2;

        public const double DisableConnectionChance = 0.01;

        public static ZigguratGaussianSampler GaussianSampler = new ZigguratGaussianSampler();

        public const double MutateNeuronChance = 0.01;
    }
}