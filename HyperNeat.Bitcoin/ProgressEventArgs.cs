using System;

namespace HyperNeat.Bitcoin
{
    public class ProgressEventArgs : EventArgs
    {
        public double BestGenomeFitness;

        public int BestGenomeBuys;

        public int BestGenomeSells;

        public double WeeklyProfit;

        public int CurrentGeneration;

        public double BestGenomeMoney;

        public double StartPrice;

        public double EndPrice;

        public int SpecieCount;

        public double AverageFitness;

        public string TradeHistory;
    }
}