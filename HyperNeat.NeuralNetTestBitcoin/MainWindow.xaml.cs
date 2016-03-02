using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

using HyperNeat.Bitcoin;

using HyperNeatLib.Interfaces;
using HyperNeatLib.NEATImpl;

using Microsoft.Win32;

using Newtonsoft.Json;

namespace HyperNeat.NeuralNetTestBitcoin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                selectedFile.Text = openFileDialog.FileName;
            }
        }

        private void doIt_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(selectedFile.Text))
            {
                return;
            }

            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Include;
            //serializer.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            serializer.TypeNameAssemblyFormat =
                System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
            serializer.TypeNameHandling = TypeNameHandling.Objects;
            serializer.Formatting = Formatting.Indented;
            serializer.PreserveReferencesHandling = PreserveReferencesHandling.Objects;

            INetwork network;

            using (StreamReader sr = new StreamReader(selectedFile.Text))
            using (JsonTextReader reader = new JsonTextReader(sr))
            {
                network = serializer.Deserialize<Network>(reader);
            }

            var usd = 500.0;
            var bitcoin = 0.0;

            var buyCount = 0;
            var sellCount = 0;

            var profits = 0.01;

            var losses = 1.0;

            var netTradeHistory = string.Empty;

            var i = 0;

            TradeEntry currentTrade = null;

            var buys = new Stack<Tuple<double, double>>();

            var averageMoney = 0.0;

            var currFitness = 0.0;

            using (StreamReader sr = new StreamReader("btceUSD.csv"))
            {
                String line;
                var done = false;
                int start = int.Parse(sr.ReadLine().Split(',')[0]);
                var unixTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

                while (!done)
                {
                    var lines = new List<String>();

                    bool end = true;

                    while ((line = sr.ReadLine()) != null)
                    {
                        lines.Add(line);

                        if (int.Parse(line.Split(',')[0]) > start + 300)
                        {
                            end = false;
                            start = int.Parse(line.Split(',')[0]);
                            break;
                        }
                    }

                    done = end;

                    if (!lines.Any())
                    {
                        continue;
                    }

                    var tempTrade = lines.Select(
                        l =>
                            {
                                var split = l.Split(',');
                                return new Tuple<DateTime, double, double>(
                                    unixTime.AddSeconds(int.Parse(split[0])),
                                    double.Parse(split[2]),
                                    double.Parse(split[1]));
                            }).OrderBy(t => t.Item1);

                    var trade = new TradeEntry()
                                    {
                                        Key =
                                            tempTrade.Select(
                                                t =>
                                                new DateTime(
                                                    t.Item1.Year,
                                                    t.Item1.Month,
                                                    t.Item1.Day,
                                                    t.Item1.Hour,
                                                    (t.Item1.Minute / 5) * 5,
                                                    0)).First(),
                                        FirstPrice = tempTrade.First().Item3,
                                        LastPrice = tempTrade.Last().Item3,
                                        MinPrice = tempTrade.Min(t => t.Item3),
                                        MaxPrice = tempTrade.Max(t => t.Item3),
                                        Volume = tempTrade.Sum(t => t.Item2)
                                    };

                    currentTrade = trade;

                    network.SetInputs(
                        trade.FirstPrice,
                        trade.LastPrice,
                        trade.MinPrice,
                        trade.MaxPrice,
                        trade.Volume,
                        usd,
                        bitcoin);

                    var output = network.GetOutputs();

                    if (output[0] > 0)
                    {
                        var amount = output[1] * (usd + bitcoin * currentTrade.LastPrice * 0.998);
                        ;

                        if (amount > 0)
                        {
                            // buy bitcoin
                            var btcAmount = amount / trade.MaxPrice * 0.998;

                            if (amount <= usd && amount != 0 && btcAmount >= 0.01)
                            {
                                netTradeHistory += $"B{{{trade.Key:yyyy-MM-dd HH:mm},{amount},{trade.MaxPrice}}} ";
                                buyCount++;

                                buys.Push(Tuple.Create(btcAmount, trade.MaxPrice));

                                usd -= amount;

                                bitcoin += btcAmount;
                            }
                        }
                        else
                        {
                            //sell bitcoin
                            amount *= -1;

                            var btcAmount = amount / trade.MinPrice;

                            if (btcAmount <= bitcoin && btcAmount != 0 && btcAmount >= 0.01)
                            {
                                var btcTradeAmount = btcAmount;

                                while (btcTradeAmount > 0)
                                {
                                    var currTrade = buys.Pop();

                                    if (currTrade.Item1 <= btcTradeAmount)
                                    {
                                        var amt = (currTrade.Item1 * trade.MinPrice * 0.998)
                                                  - (currTrade.Item1 / 0.998 * currTrade.Item2);
                                        if (amt > 0)
                                        {
                                            profits += amt;
                                        }
                                        else
                                        {
                                            losses += -1 * amt;
                                        }

                                        btcTradeAmount -= currTrade.Item1;
                                    }
                                    else
                                    {
                                        var amt = (btcTradeAmount * trade.MinPrice * 0.998)
                                                  - (btcTradeAmount / 0.998 * currTrade.Item2);

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

                                netTradeHistory += $"S{{{trade.Key:yyyy-MM-dd HH:mm},{amount},{trade.MinPrice}}} ";
                                sellCount++;

                                bitcoin -= btcAmount;

                                usd += btcAmount * trade.MinPrice * 0.998;
                            }
                        }
                    }

                    i++;
                }

                var totalAmount = usd + (buys.Any() ? buys.Sum(b => b.Item1 * b.Item2 * 0.998) : 0);

                //networks that don't trade at all get no fitness, networks that don't sell ever get a handycap
                var val = profits / losses;
                network.Fitness = Math.Max(0.0, 50 / (1 + Math.Exp(-0.08 * (val - 30))) - 4.1486);
                network.Score = totalAmount;

                TotalMoney.Content = totalAmount;
                ProfitRatio.Content = val;
                Fitness.Content = network.Fitness;
                TradeHistory.Text = netTradeHistory;
                Buys.Content = buyCount;
                Sells.Content = sellCount;
            }
        }
    }
}
