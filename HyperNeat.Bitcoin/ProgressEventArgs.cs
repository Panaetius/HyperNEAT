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

        public string NetworkFingerprint;

        public int TradeCounts;

        public double BestGenomeTotalMoney;

        public string SpeciesOverview;

        public DateTime StartTime;

        public DateTime EndTime;
    }
}