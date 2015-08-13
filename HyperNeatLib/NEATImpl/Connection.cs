using HyperNeatLib.Factories;
using HyperNeatLib.Interfaces;

namespace HyperNeatLib.NEATImpl
{
    public class Connection : IConnection
    {
        public int Id { get; set; }

        public INeuron InputNode { get; set; }

        public INeuron OutputNode { get; set; }

        public double Weight { get; set; }

        public bool IsEnabled { get; set; }

        public void Calculate()
        {
            if (!IsEnabled)
            {
                return;
            }

            OutputNode.Input += InputNode.Output * Weight;
        }

        public object Clone()
        {
            return ConnectionFactory.CreateConnection(InputNode, OutputNode, Weight, IsEnabled, Id);
        }

        public override string ToString()
        {
            return $"{this.Id}, {this.InputNode.Id}->{this.OutputNode.Id}, {this.Weight}, {this.IsEnabled}";
        }
    }
}