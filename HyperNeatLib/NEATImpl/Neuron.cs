using HyperNeatLib.Interfaces;

namespace HyperNeatLib.NEATImpl
{
    class Neuron : INeuron
    {
        public int Id { get; set; }

        public NeuronType Type { get; set; }

        public IActivationFunction ActivationFunction { get; set; }

        public double[] AuxValues { get; set; }

        public bool AcceptsAuxValues
        {
            get
            {
                return ActivationFunction.AcceptsAuxValues;
            }
        }

        public double Input { get; set; }

        public void Calculate()
        {
            Output = ActivationFunction.Calc(this.Input);
        }

        public double Output { get; set; }

        public object Clone()
        {
            var neuron = new Neuron();
            neuron.Id = Id;
            neuron.ActivationFunction = (IActivationFunction)ActivationFunction.Clone();
            neuron.Input = Input;
            neuron.Output = Output;
            neuron.Type = Type;

            return neuron;
        }

        public override string ToString()
        {
            return $"{this.Id}, {this.Type}, {this.ActivationFunction.GetType().Name}";
        }
    }
}