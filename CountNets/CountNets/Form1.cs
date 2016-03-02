using System;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CountNets
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            var regex = new Regex("^.*FP: ([^,]+),.*$", RegexOptions.Multiline);
            var matchCollection = regex.Matches(this.textBox1.Text);
            textBox2.Text = string.Join(
                "\r\n",
                matchCollection
                    .Cast<Match>()
                    .Select(m => m.Groups[1].Value)
                    .GroupBy(s => s)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key + "\t" + g.Count()));
        }
    }
}
