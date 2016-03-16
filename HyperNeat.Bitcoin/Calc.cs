using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Threading.Tasks;

using HyperNeatLib.Factories;
using HyperNeatLib.Helpers;
using HyperNeatLib.Interfaces;

using Newtonsoft.Json;

namespace HyperNeat.Bitcoin
{
    using System.Threading;

    public class Calc
    {
        public event EventHandler ProgressUpdate;

        public bool DoSerialize { get; set; } = false;

        public bool Stop { get; set; } = false;

        public long TradeCount { get; set; }

        public const int TradePeriod = 1;

        public const int TradeMonths = 3;

        public int TradeLength
        {
            get
            {
                return (TradeMonths * 30 * 24 * 60 / TradePeriod);
            }
        }

        public void StartTrading(object input)
        {
            GenerationIdSingleton.Instance.Reset();

            var population = PopulationFactory.CreatePopulation(9, 5, 100);

            population.InitialSpeciesSize = 10;

            if (!Directory.Exists("old_nets"))
            {
                Directory.CreateDirectory("old_nets");
            }

            foreach (var file in Directory.GetFiles("old_nets"))
            {
                File.Delete(file);
            }

            RunTrading(population);
        }

        public void RunTrading(IPopulation population)
        {
            //var lines = File.ReadAllLines("trades.txt");

            if (File.Exists("PopulationDevelopment.txt"))
            {
                File.Delete("PopulationDevelopment.txt");
            }

            var random = new Random();

            //var tradeHistory = lines.Select(
            //    l =>
            //    {
            //        var split = l.Split(';');
            //        return new Tuple<DateTime, double, double>(
            //            DateTime.Parse(split[1]),
            //            double.Parse(split[0]),
            //            double.Parse(split[2]));
            //    })
            //    .OrderBy(t => t.Item1)
            //    .GroupBy(t => new DateTime(t.Item1.Year, t.Item1.Month, t.Item1.Day, t.Item1.Hour, (t.Item1.Minute / 5) * 5, 0))
            //    .Select(
            //        g =>
            //        Tuple.Create(
            //            g.Key,
            //            g.First().Item3,
            //            g.Last().Item3,
            //            g.Min(t => t.Item3),
            //            g.Max(t => t.Item3),
            //            g.Sum(t => t.Item2)))
            //    .ToList();

            //TradeCount = 0;

            //using (StreamReader sr = new StreamReader("btceUSD.csv"))
            //{
            //    String line;
            //    // Read and display lines from the file until the end of 
            //    // the file is reached.
            //    while ((line = sr.ReadLine()) != null)
            //    {
            //        TradeCount++;
            //    }
            //}

            var lockObject = new Object();

            int count = 1;

            int minUnixTime = 1313331280;
            int maxUnixTime = 1453204641;

            var start = random.Next(minUnixTime, (int)(maxUnixTime - (TradeMonths * 30 * 24 * 60 * 60)));
            var end = random.Next(start + ((TradeMonths * 30 * 24 * 60 * 60) / 2), start + (TradeMonths * 30 * 24 * 60 * 60));

            var trades = GetTradeEntries(start, end, false);

            while (trades == null || trades.First().FirstPrice < trades.Last().LastPrice)
            {
                trades = null;

                GC.Collect();

                start = random.Next(minUnixTime, (int)(maxUnixTime - (TradeMonths * 30 * 24 * 60 * 60)));
                end = random.Next(start + ((TradeMonths * 30 * 24 * 60 * 60) / 2), start + (TradeMonths * 30 * 24 * 60 * 60));

                trades = GetTradeEntries(start, end, false);
            }

            var ascending = false;

            File.Delete("stats.txt");

            var endWeight = 0.1;

            double bestGenomeOverallFitness = 0.0;
            int lastBestGenomeOverallChange = 1;

            //Calculate exonential moving average start
            var startEma = trades.First().FirstPrice;

            foreach (var tr in trades.Take(10))
            {
                startEma = startEma + 0.18 * (tr.FirstPrice - startEma);
            }

            trades = trades.Skip(10).ToList();

            List<TradeEntry> newTrades = null;

            var thread = new Thread(
                () =>
                {
                    while (newTrades == null || newTrades.Count < 8000
                           || (ascending && newTrades.First().FirstPrice <= newTrades.Last().LastPrice)
                           || (!ascending && newTrades.First().FirstPrice > newTrades.Last().LastPrice))
                    {
                        newTrades = null;

                        GC.Collect();

                        start = random.Next(minUnixTime, (int)(maxUnixTime - (TradeMonths * 30 * 24 * 60 * 60)));
                        end = random.Next(
                            start + ((TradeMonths * 30 * 24 * 60 * 60) / 2),
                            start + (TradeMonths * 30 * 24 * 60 * 60));

                        newTrades = GetTradeEntries(start, end, !ascending);
                    }

                    ascending = !ascending;
                });

            thread.Start();

            while (true)
            {
                if (count - lastBestGenomeOverallChange > 10)
                {
                    thread.Join();

                    startEma = newTrades.First().FirstPrice;

                    foreach (var tr in newTrades.Take(10))
                    {
                        startEma = startEma + 0.18 * (tr.FirstPrice - startEma);
                    }

                    newTrades = newTrades.Skip(10).ToList();

                    trades = newTrades;

                    bestGenomeOverallFitness = 0.0;
                    lastBestGenomeOverallChange = count;

                    thread = new Thread(
                        () =>
                        {
                            while (newTrades == null || newTrades.Count < 8000
                                   || (ascending && newTrades.First().FirstPrice <= newTrades.Last().LastPrice)
                                   || (!ascending && newTrades.First().FirstPrice > newTrades.Last().LastPrice))
                            {
                                newTrades = null;

                                GC.Collect();

                                start = random.Next(minUnixTime, (int)(maxUnixTime - (TradeMonths * 30 * 24 * 60 * 60)));
                                end = random.Next(
                                    start + ((TradeMonths * 30 * 24 * 60 * 60) / 2),
                                    start + (TradeMonths * 30 * 24 * 60 * 60));

                                newTrades = GetTradeEntries(start, end, !ascending);
                            }

                            ascending = !ascending;
                        });

                    thread.Start();
                }

                var turnWeightDifference = (1 - endWeight) / trades.Count;

                var bestGenomeFitness = 0.0;
                var bestGenomeBuys = 0;
                var bestGenomeSells = 0;
                var bestGenomeMoney = 0.0;
                var bestGenomeTradeHistory = string.Empty;
                INetwork bestNetwork = population.Networks.First();

                var startPrice = trades.First().FirstPrice;
                var endPrice = trades.Last().LastPrice;

                var priceDifference = startPrice / endPrice;

                var tradeCount = trades.Count;

                Parallel.ForEach(
                    population.Networks,
                    network =>
                    {
                        var usd = 1000.0;
                        var bitcoin = 0.0;// 1000.0 / trades.First().MaxPrice;

                        TradeEntry previousTrade = trades.First();

                        var buyCount = 0;
                        var sellCount = 0;

                        var profits = 0.0;

                        var losses = 0.0;

                        var netTradeHistory = string.Empty;

                        var i = 0;

                        TradeEntry currentTrade = trades.First();

                        var buys = new Stack<Tuple<double, double>>();

                        //buys.Push(Tuple.Create(1000.0 / trades.First().MaxPrice, trades.First().MaxPrice));

                        var averageMoney = 0.0;

                        var currFitness = 0.0;

                        var ema = startEma;

                        var queue = new Queue<double>();

                        var buySignals = new List<Signal>();
                        var sellSignals = new List<Signal>();
                        var ignoreSignals = new List<Signal>();
                        var historicalBuySignals = 0.0;
                        var historicalSellSignals = 0.0;
                        var historicalIgnoreSignals = 0.0;


                        for (int j = 0; j < 5; j++)
                        {
                            queue.Enqueue((trades[j].FirstPrice / ema - 1) * 100);
                            ema = ema + 0.18 * (trades[j].FirstPrice - ema);
                        }

                        foreach (var trade in trades.Skip(5).ToList())
                        {
                            if (Stop)
                            {
                                return;
                            }

                            ema = ema + 0.18 * (trade.FirstPrice - ema);

                            currentTrade = trade;

                            historicalBuySignals += buySignals.Where(s => i - s.Index > 30).Sum(b => b.Fitness);
                            historicalSellSignals += sellSignals.Where(s => i - s.Index > 30).Sum(b => b.Fitness);
                            historicalIgnoreSignals += ignoreSignals.Where(s => i - s.Index > 30).Sum(b => b.Fitness);

                            buySignals = buySignals.Where(s => i - s.Index <= 30).ToList();
                            sellSignals = sellSignals.Where(s => i - s.Index <= 30).ToList();
                            ignoreSignals = ignoreSignals.Where(s => i - s.Index <= 30).ToList();


                            foreach (Signal signal in buySignals)
                            {
                                signal.Fitness += (trade.LastPrice / signal.Price - 1)
                                             * Math.Pow(0.9, i - signal.Index) * signal.Confidence;
                            }

                            foreach (Signal signal in sellSignals)
                            {
                                signal.Fitness += (signal.Price / trade.LastPrice - 1)
                                             * Math.Pow(0.9, i - signal.Index)*signal.Confidence;
                            }

                            foreach (Signal signal in ignoreSignals)
                            {
                                var res = signal.Price / trade.LastPrice - 1;
                                if (res < 0.0035 && res > -0.0035)
                                {
                                    signal.Fitness += 0.0035 * Math.Pow(0.9, i - signal.Index);
                                }
                                else
                                {
                                    signal.Fitness -= Math.Abs(res) * Math.Pow(0.9, i - signal.Index);
                                }
                            }

                            var newEl = (previousTrade.LastPrice / ema - 1) * 100;

                            network.SetInputs(
                                queue.ElementAt(0),
                                queue.ElementAt(1),
                                queue.ElementAt(2),
                                queue.ElementAt(3),
                                queue.ElementAt(4),
                                newEl,
                                usd,
                                bitcoin * previousTrade.LastPrice,
                                previousTrade.Volume);

                            queue.Enqueue(newEl);
                            queue.Dequeue();

                            var output = network.GetOutputs();

                            if (i < 50)
                            {
                                previousTrade = trade;
                                i++; //do nothing first 50 rounds so networks can initialize if necessary
                                continue;
                            }
                            var ignoreSignal = output[0];
                            var buySignal = output[1];
                            var buyConfidence = (output[2] +1 ) / 2;
                            var sellSignal = output[3];
                            var sellConfidence = (output[4] +1)/2;

                            if (buySignal > ignoreSignal && buySignal > sellSignal && buyConfidence > 0.2)
                            {
                                buySignals.Add(
                                    new Signal()
                                        {
                                            Index = i,
                                            Price = trade.LastPrice,
                                            Confidence = buyConfidence,
                                            Fitness = 0.0
                                        });

                                var amount = 1 * usd;

                                // buy bitcoin
                                var btcAmount = amount / trade.MaxPrice * 0.998;

                                if (amount <= usd && amount != 0 && btcAmount >= 0.01)
                                {
                                    netTradeHistory +=
                                        $"B{{{trade.Key:yyyy-MM-dd HH:mm},{amount},{trade.MaxPrice},{usd},{bitcoin}}} ";
                                    buyCount++;

                                    buys.Push(Tuple.Create(btcAmount, trade.MaxPrice));

                                    usd -= amount;

                                    bitcoin += btcAmount;
                                }
                            }
                            else if (sellSignal > ignoreSignal && sellSignal > buySignal && sellConfidence > 0.2)
                            {
                                //sell bitcoin
                                sellSignals.Add(
                                    new Signal()
                                    {
                                        Index = i,
                                        Price = trade.LastPrice,
                                        Confidence = sellConfidence,
                                        Fitness = 0.0
                                    });

                                var btcAmount = 1 * bitcoin;

                                if (btcAmount <= bitcoin && btcAmount != 0 && btcAmount >= 0.01)
                                {
                                    SellBtcAmount(btcAmount, buys, trade, ref profits, ref losses);

                                    netTradeHistory +=
                                        $"S{{{trade.Key:yyyy-MM-dd HH:mm},{btcAmount * trade.MinPrice},{trade.MinPrice},{usd},{bitcoin}}} ";
                                    sellCount++;

                                    bitcoin -= btcAmount;

                                    usd += btcAmount * trade.MinPrice * 0.998;

                                    if (!buys.Any())
                                    {
                                        bitcoin = 0;
                                    }
                                }
                            }
                            else
                            {
                                ignoreSignals.Add(
                                    new Signal()
                                    {
                                        Index = i,
                                        Price = trade.LastPrice,
                                        Confidence = 0,
                                        Fitness = 0.0
                                    });
                            }

                            //var currAmount = usd + bitcoin * currentTrade.Item4 * 0.998;
                            //var holdPrice = 500 + (500.0 / startPrice) * currentTrade.Item4 * 0.998;

                            //var difference = currAmount
                            //                 - holdPrice;

                            //difference *= (1 - i * turnWeightDifference);

                            //averageMoney = (averageMoney * (i / (i + 1.0)))
                            //               + (difference / (i + 1));

                            previousTrade = trade;

                            i++;
                        }

                        var prefProfits = profits;

                        SellBtcAmount(bitcoin, buys, currentTrade, ref profits, ref losses);

                        //profits = (profits - prefProfits) * 0.8 + prefProfits;

                        usd += bitcoin * currentTrade.MinPrice * 0.998;

                        var totalAmount = usd;

                        if (sellCount < 1)
                        {
                            losses = 1000;
                        }

                        historicalBuySignals += buySignals.Sum(b => b.Fitness);
                        historicalSellSignals += sellSignals.Sum(b => b.Fitness);
                        historicalIgnoreSignals += ignoreSignals.Sum(b => b.Fitness);

                        //networks that don't trade at all get no fitness, networks that don't sell ever get a handycap
                        var val = 0.0;//Math.Log(1 + Math.Exp(0.01 * (profits - losses) * startPrice / endPrice));
                        val += Math.Log(1 + Math.Exp(0.001 * (historicalBuySignals+ historicalSellSignals+ historicalIgnoreSignals)));
                        //val += Math.Log(1 + Math.Exp(0.01 * historicalSellSignals));
                        //val += Math.Log(1 + Math.Exp(0.01 * historicalIgnoreSignals));
                        network.Fitness = val; //Math.Max(0.0, 400 / (1 + Math.Exp(-0.02 * (val - 200))) - 6);

                        //if (network.HiddenNodes.Count == 0)
                        //{
                        //    network.Fitness = Math.Min(network.Fitness/1.5, 4);
                        //}

                        network.Score = totalAmount;

                        lock (lockObject)
                        {
                            if (network.Fitness > bestGenomeFitness || network.Fitness == bestGenomeFitness && network.Score > bestGenomeMoney)
                            {
                                bestGenomeFitness = network.Fitness;
                                bestGenomeBuys = buyCount;
                                bestGenomeSells = sellCount;
                                bestGenomeMoney = network.Score;
                                bestGenomeTradeHistory = netTradeHistory;
                                bestNetwork = network;
                            }
                        }
                    });

                if (bestGenomeFitness / bestGenomeOverallFitness > 1.1)
                {
                    bestGenomeOverallFitness = bestGenomeFitness;
                    lastBestGenomeOverallChange = count;
                }

                File.AppendAllText(
                    "PopulationDevelopment.txt",
                    string.Join(
                        ";",
                        population.Networks.Select(n => n.Fingerprint())
                        .GroupBy(f => f)
                        .Select(g => g.Key + "\t" + g.Count())) + "\r\n");

                if (Stop)
                {
                    return;
                }

                //var minFitness = population.Networks.Min(n => n.Fitness);

                //foreach (var network in population.Networks)
                //{
                //    network.Fitness -= minFitness - 0.01;
                //}

                var usde = 500.0;
                var bitcoine = 0.0;
                var buyCounte = 0;
                var sellCounte = 0;
                var netTradeHistorye = string.Empty;

                //bestNetwork.Reset();

                //foreach (var trade in tradeHistory)
                //{
                //    bestNetwork.SetInputs(trade.Item2, trade.Item3, trade.Item4, trade.Item5, trade.Item6, usde, bitcoine);

                //    var output = bestNetwork.GetOutputs();

                //    if (output[0] > 0)
                //    {
                //        var amount = output[1] * (usde + bitcoine * trade.Item3 * 0.998); ;

                //        if (amount > 0)
                //        {
                //            // buy bitcoin
                //            var btcAmount = amount / trade.Item5 * 0.998;

                //            if (amount <= usde && amount != 0 && btcAmount >= 0.01)
                //            {
                //                netTradeHistorye +=
                //                    $"B{{{trade.Item1:yyyy-MM-dd HH:mm},{amount},{trade.Item5}}} ";
                //                buyCounte++;

                //                usde -= amount;

                //                bitcoine += btcAmount;
                //            }
                //        }
                //        else
                //        {
                //            //sell bitcoin
                //            amount *= -1;

                //            var btcAmount = amount / trade.Item4;

                //            if (btcAmount <= bitcoine && btcAmount != 0 && btcAmount >= 0.01)
                //            {
                //                netTradeHistorye +=
                //                    $"S{{{trade.Item1:yyyy-MM-dd HH:mm},{amount},{trade.Item4}}} ";
                //                sellCounte++;

                //                bitcoine -= btcAmount;

                //                usde += btcAmount * trade.Item4 * 0.998;
                //            }
                //        }
                //    }
                //}

                var bestGenomeTotalMoney = 0;//usde + bitcoine * tradeHistory.Last().Item4 * 0.998;

                if (ProgressUpdate != null)
                {
                    ProgressUpdate(this, new ProgressEventArgs()
                    {
                        BestGenomeFitness = bestGenomeFitness,
                        BestGenomeBuys = bestGenomeBuys,
                        BestGenomeSells = bestGenomeSells,
                        WeeklyProfit = (Math.Pow(bestGenomeMoney / 1000.0, (60 * 24.0 * 30 * TradeMonths) / trades.Count) - 1) * 100,
                        CurrentGeneration = population.CurrentGeneration,
                        BestGenomeMoney = bestGenomeMoney,
                        StartPrice = startPrice,
                        EndPrice = endPrice,
                        SpecieCount = population.Species.Count(s => s.Networks.Any()),
                        AverageFitness = population.Networks.Average(n => n.Fitness),
                        TradeHistory = bestGenomeTradeHistory,
                        NetworkFingerprint = bestNetwork.Fingerprint(),
                        TradeCounts = trades.Count,
                        BestGenomeTotalMoney = bestGenomeTotalMoney,
                        SpeciesOverview = string.Join(
                            "\r\n",
                            population.Species
                                .OrderByDescending(s => s.Networks.Count)
                                .ThenByDescending(s => s.MaxGenomeFitness)
                                .Select(s => s.Networks.Count + ", " + s.MaxGenomeFitness)),
                        StartTime = trades.First().Key,
                        EndTime = trades.Last().Key
                    });
                }

                Console.WriteLine(
                    "Gen: {4}, Max Fit: {0:0.00000}, AVG Fit: {9:0.00000}, Money: {5:0.00}, Best Money: {11:0.00}, Buy: {1}, Sells: {2}, WP: {3:0.00000}, Start: {6}, End: {7}, Spec: {8}, Trade History: {10}",
                    bestGenomeFitness,
                    bestGenomeBuys,
                    bestGenomeSells,
                    (Math.Pow(bestGenomeMoney / 1000.0, (60 * 24.0 * 30 * TradeMonths) / trades.Count) - 1) * 100,
                    population.CurrentGeneration,
                    bestGenomeMoney,
                    startPrice,
                    endPrice,
                    population.Species.Count(s => s.Networks.Any()),
                    population.Networks.Average(n => n.Fitness),
                    bestGenomeTradeHistory,
                    bestGenomeTotalMoney
                    );

                File.AppendAllText(
                    "stats.txt",
                    string.Format(
                        "Time: {11:yyyy-MM-dd HH:mm:ss}, Gen: {4}, Max Fit: {0:0.00000}, AVG Fit: {9:0.00000}, Money: {5:0.00}, Best Money: {14:0.00}, Buy: {1}, Sells: {2}, YP: {3:0.00000}, Start: {6}, End: {7}, Count: {13}, Spec: {8}, FP:{12}, Trade History: {10} \n",
                        bestGenomeFitness,
                        bestGenomeBuys,
                        bestGenomeSells,
                        (Math.Pow(bestGenomeMoney / 1000.0, (60 * 24.0 * 30 * TradeMonths) / trades.Count) - 1) * 100,
                        population.CurrentGeneration,
                        bestGenomeMoney,
                        startPrice,
                        endPrice,
                        population.Species.Count(s => s.Networks.Any()),
                        population.Networks.Average(n => n.Fitness),
                        bestGenomeTradeHistory,
                        DateTime.Now,
                        bestNetwork.Fingerprint(),
                        trades.Count,
                        bestGenomeTotalMoney
                        ));

                //serialize best net for future generations
                JsonSerializer serializer = new JsonSerializer();

                if (bestNetwork.Score > 500)
                {
                    serializer.NullValueHandling = NullValueHandling.Include;
                    //serializer.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    serializer.TypeNameAssemblyFormat =
                        FormatterAssemblyStyle.Simple;
                    serializer.TypeNameHandling = TypeNameHandling.Objects;
                    serializer.Formatting = Formatting.Indented;
                    serializer.PreserveReferencesHandling = PreserveReferencesHandling.Objects;

                    using (
                        StreamWriter sw =
                            new StreamWriter(string.Format(@"old_nets/net_{0}.json", population.CurrentGeneration)))
                    using (JsonWriter writer = new JsonTextWriter(sw))
                    {
                        serializer.Serialize(writer, bestNetwork);
                    }
                }

                if (DoSerialize)
                {
                    DoSerialize = false;

                    File.Delete("SerializedNetworks.json");

                    //population.Networks = population.Networks.OrderByDescending(n => n.Fitness).ToList();

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
                    serializer = new JsonSerializer();
                    serializer.NullValueHandling = NullValueHandling.Include;
                    //serializer.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    serializer.TypeNameAssemblyFormat =
                        System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
                    serializer.TypeNameHandling = TypeNameHandling.Objects;
                    serializer.Formatting = Formatting.Indented;
                    serializer.PreserveReferencesHandling = PreserveReferencesHandling.Objects;

                    using (StreamWriter sw = new StreamWriter(@"SerializedNetworks.json"))
                    using (JsonWriter writer = new JsonTextWriter(sw))
                    {
                        serializer.Serialize(writer, bestNetwork);
                    }
                }

                if (Stop)
                {
                    return;
                }

                population.CalculateNextPopulation();

                foreach (var network in population.Networks)
                {
                    network.Reset();
                }

                if (Stop)
                {
                    return;
                }

                count++;
            }
        }

        private static void SellBtcAmount(double btcAmount, Stack<Tuple<double, double>> buys, TradeEntry trade, ref double profits, ref double losses)
        {
            var btcTradeAmount = btcAmount;

            while (btcTradeAmount > 0)
            {
                var currTrade = buys.Pop();

                if (currTrade.Item1 <= btcTradeAmount)
                {
                    var amt = (currTrade.Item1 * trade.MinPrice * 0.998) - (currTrade.Item1 / 0.998 * currTrade.Item2);
                    if (amt > 0)
                    {
                        profits += amt;
                    }
                    else
                    {
                        losses += -1 * amt;
                    }

                    btcTradeAmount -= currTrade.Item1;

                    if (btcTradeAmount < 0.000001)
                    {
                        btcTradeAmount = 0;
                    }
                }
                else
                {
                    var amt = (btcTradeAmount * trade.MinPrice * 0.998) - (btcTradeAmount / 0.998 * currTrade.Item2);

                    if (amt > 0)
                    {
                        profits += amt;
                    }
                    else
                    {
                        losses += -1 * amt;
                    }

                    buys.Push(Tuple.Create(currTrade.Item1 - btcTradeAmount, currTrade.Item2));

                    btcTradeAmount = 0;
                }
            }
        }

        public List<TradeEntry> GetTradeEntries(long start, long end, bool ascending)
        {
            long count = 0;

            double startPrice = 0.0;
            double endPrice = 0.0;

            using (StreamReader sr = new StreamReader("btceUSD.csv"))
            {
                String line;
                // Read and display lines from the file until the end of 
                // the file is reached.

                while ((line = sr.ReadLine()) != null)
                {
                    var sp = line.Split(',');
                    var time = int.Parse(sp[0]);

                    if (time >= start && startPrice == 0.0)
                    {
                        startPrice = double.Parse(sp[1]);
                    }
                    else if (startPrice > 0.0 && time >= end)
                    {
                        endPrice = double.Parse(sp[1]);
                        break;
                    }

                    count++;
                }
            }

            if ((ascending && startPrice >= endPrice) || (!ascending && startPrice < endPrice))
            {
                return null;
            }

            var history = new List<string>();

            using (StreamReader sr = new StreamReader("btceUSD.csv"))
            {
                String line;
                // Read and display lines from the file until the end of 
                // the file is reached.
                while ((line = sr.ReadLine()) != null)
                {
                    var time = int.Parse(line.Split(',')[0]);

                    if (time >= start && time < end)
                    {
                        history.Add(line);
                    }
                    else if (time >= end)
                    {
                        break;
                    }

                    count++;
                }
            }

            var unixTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

            return history.Select(
                l =>
                {
                    var split = l.Split(',');
                    return new Tuple<DateTime, double, double>(
                        unixTime.AddSeconds(int.Parse(split[0])),
                        double.Parse(split[2]),
                        double.Parse(split[1]));
                })
                .OrderBy(t => t.Item1)
                .GroupBy(t => new DateTime(t.Item1.Year, t.Item1.Month, t.Item1.Day, t.Item1.Hour, (t.Item1.Minute / TradePeriod) * TradePeriod, 0))
                .Select(
                    g =>
                    new TradeEntry()
                    {
                        Key = g.Key,
                        FirstPrice = g.First().Item3,
                        LastPrice = g.Last().Item3,
                        MinPrice = g.Min(t => t.Item3),
                        MaxPrice = g.Max(t => t.Item3),
                        Volume = g.Sum(t => t.Item2)
                    })
                .ToList();
        }
    }

    public class Signal
    {
        public int Index;

        public double Price;

        public double Confidence;

        public double Fitness;
    }

    public struct TradeEntry
    {
        public DateTime Key;

        public double FirstPrice;

        public double LastPrice;

        public double MinPrice;

        public double MaxPrice;

        public double Volume;
    }
}