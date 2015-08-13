using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using HyperNeatLib.Factories;
using HyperNeatLib.Interfaces;

namespace HyperNeat.Bitcoin
{
    class Program
    {
        static void Main(string[] args)
        {
            var population = PopulationFactory.CreatePopulation(7, 3, 200);

            population.InitialSpeciesSize = 20;

            RunTrading(population);
        }

        private static void RunTrading(IPopulation population)
        {
            var lines = File.ReadAllLines("trades.txt");

            var random = new Random();

            var tradeHistory = lines.Select(
                l =>
                    {
                        var split = l.Split(';');
                        return new Tuple<DateTime, double, double>(
                            DateTime.Parse(split[1]),
                            double.Parse(split[0]),
                            double.Parse(split[2]));
                    })
                .OrderBy(t => t.Item1)
                .GroupBy(t => new DateTime(t.Item1.Year, t.Item1.Month, t.Item1.Day, t.Item1.Hour, t.Item1.Minute, 0))
                .Select(
                    g =>
                    Tuple.Create(
                        g.Key,
                        g.First().Item3,
                        g.Last().Item3,
                        g.Min(t => t.Item3),
                        g.Max(t => t.Item3),
                        g.Sum(t => t.Item2)))
                .ToList();

            var lockObject = new Object();

            int count = 1;

            var start = random.Next(0, (int)(tradeHistory.Count * 0.75));
            var end = random.Next(start + 20000, tradeHistory.Count);

            var trades = tradeHistory.Skip(start).Take(end - start).ToList();

            while (trades.First().Item3 > trades.Last().Item3)
            {
                start = random.Next(0, (int)(tradeHistory.Count * 0.75));
                end = random.Next(start + 20000, tradeHistory.Count);

                trades = tradeHistory.Skip(start).Take(end - start).ToList();
            }

            var ascending = false;

            while (true)
            {
                if (count%20 == 0)
                {
                    while ((ascending && trades.First().Item3 > trades.Last().Item3) || (!ascending && trades.First().Item3 < trades.Last().Item3))
                    {
                        start = random.Next(0, (int)(tradeHistory.Count * 0.75));
                        end = random.Next(start + 20000, tradeHistory.Count);

                        trades = tradeHistory.Skip(start).Take(end - start).ToList();
                    }

                    ascending = !ascending;
                }

                var bestGenomeFitness = 0.0;
                var bestGenomeBuys = 0;
                var bestGenomeSells = 0;
                var bestGenomeMoney = 0.0;

                var startPrice = trades.First().Item2;
                var endPrice = trades.Last().Item3;

                var priceDifference = startPrice / endPrice;

                Parallel.ForEach(
                    population.Networks,
                    network =>
                        {
                            var usd = 500.0;
                            var bitcoin = 0.0;

                            var buyCount = 0;
                            var sellCount = 0;
                            
                            var i = 0;

                            Tuple<DateTime, double, double, double, double, double> currentTrade = null;

                            foreach (var trade in trades)
                            {
                                currentTrade = trade;

                                network.SetInputs(trade.Item2, trade.Item3, trade.Item4, trade.Item5, trade.Item6, usd, bitcoin);

                                var output = network.GetOutputs();

                                if (i < 100)
                                {
                                    i++;
                                    continue;
                                }

                                if (output[0] > 0)
                                {
                                    var amount = output[1] * (usd + bitcoin * currentTrade.Item6 * 0.998);

                                    if (amount > 0)
                                    {
                                        // buy bitcoin
                                        if (amount > usd)
                                        {
                                            amount = usd;
                                        }

                                        var btcAmount = amount / trade.Item5 * 0.998;

                                        if (amount == 0 || btcAmount < 0.01)
                                        {
                                            continue;
                                        }

                                        buyCount++;

                                        usd -= amount;

                                        bitcoin += btcAmount;
                                    }
                                    else
                                    {
                                        //sell bitcoin
                                        amount *= -1;

                                        var btcAmount = amount / trade.Item4;

                                        if (btcAmount > bitcoin)
                                        {
                                            btcAmount = bitcoin;
                                        }

                                        if (btcAmount == 0 || btcAmount < 0.01)
                                        {
                                            continue;
                                        }

                                        sellCount++;

                                        bitcoin -= btcAmount;

                                        usd += btcAmount * trade.Item4 * 0.998;
                                    }
                                }

                                i++;
                            }

                            var totalAmount = usd + bitcoin * currentTrade.Item3 * 0.998;

                            //networks that don't trade at all get no fitness, networks that don't sell ever get a handycap
                            network.Fitness = buyCount == 0 ? 0 : totalAmount * priceDifference;

                            lock (lockObject)
                            {
                                if (network.Fitness > bestGenomeFitness)
                                {
                                    bestGenomeFitness = network.Fitness;
                                    bestGenomeBuys = buyCount;
                                    bestGenomeSells = sellCount;
                                    bestGenomeMoney = totalAmount;
                                }
                            }
                        });

                Console.WriteLine(
                    "Gen: {4}, Max Fit: {0:0.00000}, AVG Fit: {9:0.00000}, Money: {5:0.00000}, Buy: {1}, Sells: {2}, WP: {3:0.00000}, Start: {6}, End: {7}, Spec: {8}",
                    bestGenomeFitness,
                    bestGenomeBuys,
                    bestGenomeSells,
                    (Math.Pow(bestGenomeMoney / 500.0, (60.0 * 24.0 * 7.0) / trades.Count)  - 1) * 100,
                    population.CurrentGeneration,
                    bestGenomeMoney,
                    startPrice,
                    endPrice,
                    population.Species.Count(s => s.Networks.Any()),
                    population.Networks.Average(n => n.Fitness));

                File.AppendAllText(
                    "stats.txt",
                    string.Format(
                        "Gen: {4}, Max Fit: {0:0.00000}, AVG Fit: {9:0.00000}, Money: {5:0.00000}, Buy: {1}, Sells: {2}, WP: {3:0.00000}, Start: {6}, End: {7}, Spec: {8} \n",
                        bestGenomeFitness,
                        bestGenomeBuys,
                        bestGenomeSells,
                        (Math.Pow(bestGenomeMoney / 500.0, (60.0 * 24.0 * 7.0) / trades.Count) - 1) * 100,
                        population.CurrentGeneration,
                        bestGenomeMoney,
                    startPrice,
                    endPrice,
                    population.Species.Count(s => s.Networks.Any()),
                    population.Networks.Average(n => n.Fitness)));

                population.CalculateNextPopulation();

                count++;
            }
        }
    }
}
