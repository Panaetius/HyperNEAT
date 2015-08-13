using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using HyperNeatLib.Factories;
using HyperNeatLib.Interfaces;

using Newtonsoft.Json;

namespace HyperNeat.Bitcoin
{
    public class Calc
    {
        public event EventHandler ProgressUpdate;

        public bool DoSerialize { get; set; } = false;

        public void StartTrading(object input)
        {
            var population = PopulationFactory.CreatePopulation(7, 3, 1000);

            population.InitialSpeciesSize = 20;

            RunTrading(population);
        }

        public void RunTrading(IPopulation population)
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
                .GroupBy(t => new DateTime(t.Item1.Year, t.Item1.Month, t.Item1.Day, t.Item1.Hour, (t.Item1.Minute / 15) * 15, 0))
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
            var end = random.Next(start + 1000, tradeHistory.Count);

            var trades = tradeHistory.Skip(start).Take(end - start).ToList();

            while (trades.First().Item3 > trades.Last().Item3)
            {
                start = random.Next(0, (int)(tradeHistory.Count * 0.75));
                end = random.Next(start + 1000, tradeHistory.Count);

                trades = tradeHistory.Skip(start).Take(end - start).ToList();
            }

            var ascending = false;

            File.Delete("stats.txt");

            while (true)
            {
                if (count % 10 == 0)
                {
                    while ((ascending && trades.First().Item2 > trades.Last().Item3) || (!ascending && trades.First().Item2 < trades.Last().Item3))
                    {
                        start = random.Next(0, (int)(tradeHistory.Count * 0.75));
                        end = random.Next(start + 1000, tradeHistory.Count);

                        trades = tradeHistory.Skip(start).Take(end - start).ToList();
                    }

                    ascending = !ascending;
                }

                var bestGenomeFitness = 0.0;
                var bestGenomeBuys = 0;
                var bestGenomeSells = 0;
                var bestGenomeMoney = 0.0;
                var bestGenomeTradeHistory = string.Empty;

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

                        var netTradeHistory = string.Empty;

                        var i = 0;

                        Tuple<DateTime, double, double, double, double, double> currentTrade = null;

                        foreach (var trade in trades)
                        {
                            currentTrade = trade;

                            network.SetInputs(trade.Item2, trade.Item3, trade.Item4, trade.Item5, trade.Item6, usd, bitcoin);

                            var output = network.GetOutputs();

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

                                    netTradeHistory += "1";
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

                                    netTradeHistory += "0";
                                    sellCount++;

                                    bitcoin -= btcAmount;

                                    usd += btcAmount * trade.Item4 * 0.998;
                                }
                            }

                            i++;
                        }

                        var totalAmount = usd + bitcoin * currentTrade.Item3 * 0.998;
                        var fitness = usd + bitcoin * currentTrade.Item3 * 0.998 * 0.75;

                        //networks that don't trade at all get no fitness, networks that don't sell ever get a handycap
                        network.Fitness = buyCount == 0 ? 0 : fitness * priceDifference;

                        lock (lockObject)
                        {
                            if (network.Fitness > bestGenomeFitness)
                            {
                                bestGenomeFitness = network.Fitness;
                                bestGenomeBuys = buyCount;
                                bestGenomeSells = sellCount;
                                bestGenomeMoney = totalAmount;
                                bestGenomeTradeHistory = netTradeHistory;
                            }
                        }
                    });

                if (ProgressUpdate != null)
                {
                    ProgressUpdate(this, new ProgressEventArgs()
                    {
                        BestGenomeFitness = bestGenomeFitness,
                        BestGenomeBuys = bestGenomeBuys,
                        BestGenomeSells = bestGenomeSells,
                        WeeklyProfit = (Math.Pow(bestGenomeMoney / 500.0, (4.0 * 24.0 * 365.0) / trades.Count) - 1) * 100,
                        CurrentGeneration = population.CurrentGeneration,
                        BestGenomeMoney = bestGenomeMoney,
                        StartPrice = startPrice,
                        EndPrice = endPrice,
                        SpecieCount = population.Species.Count(s => s.Networks.Any()),
                        AverageFitness = population.Networks.Average(n => n.Fitness),
                        TradeHistory = bestGenomeTradeHistory
                    });
                }

                Console.WriteLine(
                    "Gen: {4}, Max Fit: {0:0.00000}, AVG Fit: {9:0.00000}, Money: {5:0.00000}, Buy: {1}, Sells: {2}, WP: {3:0.00000}, Start: {6}, End: {7}, Spec: {8}, Trade History: {10}",
                    bestGenomeFitness,
                    bestGenomeBuys,
                    bestGenomeSells,
                    (Math.Pow(bestGenomeMoney / 500.0, (4.0 * 24.0 * 365.0) / trades.Count) - 1) * 100,
                    population.CurrentGeneration,
                    bestGenomeMoney,
                    startPrice,
                    endPrice,
                    population.Species.Count(s => s.Networks.Any()),
                    population.Networks.Average(n => n.Fitness),
                    bestGenomeTradeHistory);

                File.AppendAllText(
                    "stats.txt",
                    string.Format(
                        "Time: {11:yyyy-MM-dd HH:mm:ss}, Gen: {4}, Max Fit: {0:0.00000}, AVG Fit: {9:0.00000}, Money: {5:0.00000}, Buy: {1}, Sells: {2}, WP: {3:0.00000}, Start: {6}, End: {7}, Spec: {8}, Trade History: {10} \n",
                        bestGenomeFitness,
                        bestGenomeBuys,
                        bestGenomeSells,
                        (Math.Pow(bestGenomeMoney / 500.0, (4.0 * 24.0 * 365.0) / trades.Count) - 1) * 100,
                        population.CurrentGeneration,
                        bestGenomeMoney,
                        startPrice,
                        endPrice,
                        population.Species.Count(s => s.Networks.Any()),
                        population.Networks.Average(n => n.Fitness),
                        bestGenomeTradeHistory,
                        DateTime.Now));

                if (DoSerialize)
                {
                    DoSerialize = false;

                    File.Delete("SerializedNetworks.json");

                    population.Networks = population.Networks.OrderByDescending(n => n.Fitness).ToList();

                    //File.WriteAllText(
                    //    "SerializedNetworks.json",
                    //    JsonConvert.SerializeObject(
                    //        population.Networks.First(),
                    //        Formatting.None,
                    //        new JsonSerializerSettings()
                    //        {
                    //            TypeNameHandling = TypeNameHandling.Objects,
                    //            TypeNameAssemblyFormat =
                    //                    System.Runtime.Serialization.Formatters
                    //                    .FormatterAssemblyStyle.Simple,
                    //            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    //        }));
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.NullValueHandling = NullValueHandling.Include;
                    serializer.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    serializer.TypeNameAssemblyFormat =
                        System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
                    serializer.TypeNameHandling = TypeNameHandling.Objects;
                    serializer.Formatting = Formatting.Indented;

                    using (StreamWriter sw = new StreamWriter(@"SerializedNetworks.json"))
                    using (JsonWriter writer = new JsonTextWriter(sw))
                    {
                        serializer.Serialize(writer, population.Networks.First());
                    }
                }

                population.CalculateNextPopulation();

                foreach (var network in population.Networks)
                {
                    network.Reset();
                }

                count++;
            }
        }
    }
}