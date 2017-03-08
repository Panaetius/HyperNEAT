using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace GraphPopulation
{
    public partial class Form1 : Form
    {
        private const int MinCount = 5;
        
        public Form1()
        {
            InitializeComponent();

            fileSystemWatcher1.Changed += FileSystemWatcher1OnChanged;
            this.chart1.GetToolTipText += this.chart1_GetToolTipText;
        }

        private void chart1_GetToolTipText(object sender, ToolTipEventArgs e)
        {
            switch (e.HitTestResult.ChartElementType)
            {
                case ChartElementType.DataPoint:
                    var dataPoint = e.HitTestResult.Series.Points[e.HitTestResult.PointIndex];
                    e.Text = string.Format("{2}\r\n X:\t{0}\nY:\t{1}", dataPoint.XValue, dataPoint.YValues[0], e.HitTestResult.Series.Name);
                    foreach (var series in chart1.Series.Where(s => s.BorderWidth > 1))
                    {
                        series.BorderWidth = 1;
                    }
                    e.HitTestResult.Series.BorderWidth = 3;
                    break;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var result = openFileDialog1.ShowDialog();

            if (result != DialogResult.OK)
            {
                return;
            }

            var file = new FileInfo(openFileDialog1.FileName);

            this.fileSystemWatcher1.Path = file.DirectoryName;
            fileSystemWatcher1.Filter = file.Name;

            FileSystemWatcher1OnChanged(null, null);
        }

        private void FileSystemWatcher1OnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            fileSystemWatcher1.EnableRaisingEvents = false;

            while (IsFileLocked(openFileDialog1.FileName))
            {
                Thread.Sleep(10);
            }

            var count = int.Parse(tbMaxCount.Text);
            chart1.ChartAreas[0].AxisY.Maximum = count;
            
            var lines = File.ReadAllLines(openFileDialog1.FileName);

            var entries = new Dictionary<string, Dictionary<int, int>>();

            int i;

            for (i = 0; i < lines.Count(); i++)
            {
                var regex = new Regex("([^\t;]+)\t([^;]+)");
                var matches = regex.Matches(lines[i]);

                foreach (Match match in matches)
                {
                    var val = int.Parse(match.Groups[2].Value);

                    if (!entries.ContainsKey(match.Groups[1].Value))
                    {
                        entries.Add(match.Groups[1].Value, new Dictionary<int, int>() );
                    }

                    entries[match.Groups[1].Value].Add(i + 1, val);
                }
            }

            chart1.Width = Math.Max(1900, i * 10);

            if (lines.Count() > 1000)
            {
                entries =
                    entries.Where(e => e.Value.Values.Any(v => v > MinCount) && e.Value.Values.Count > 50)
                        .ToDictionary(e => e.Key, e => e.Value);
            }
            else
            {
                entries =
                    entries.Where(e => e.Value.Values.Any(v => v > MinCount))
                        .ToDictionary(e => e.Key, e => e.Value);
            }

            chart1.Series.Clear();

            int c = 1;

            foreach (var entry in entries.ToList())
            {
                foreach (var tuple in entry.Value.ToList())
                {
                    if (tuple.Key < i && !entry.Value.ContainsKey(tuple.Key + 1))
                    {
                        entry.Value.Add(tuple.Key + 1, 0);
                    }

                    if (tuple.Key > 1 && !entry.Value.ContainsKey(tuple.Key - 1))
                    {
                        entry.Value.Add(tuple.Key - 1, 0);
                    }
                }

                var serie = new Series();
                serie.ChartType = SeriesChartType.Line;
                serie.ToolTip = "Name #SERIESNAME";
                serie.IsVisibleInLegend = false;
                serie.Name = entry.Key;

                foreach (var tuple in entry.Value.OrderBy(t => t.Key))
                {
                    serie.Points.AddXY(tuple.Key, tuple.Value);
                }

                chart1.Series.Add(serie);

                if (c%10 == 0)
                {
                    Application.DoEvents();
                }
                c++;
            }

            //fileSystemWatcher1.EnableRaisingEvents = true;
        }

        protected virtual bool IsFileLocked(string file)
        {
            FileStream stream = null;

            try
            {
                stream = (new FileInfo(file)).Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }
    }
}
