using System;

namespace SAi_KR
{
    public class NeuralNetwork
    {
        private readonly int _inputSize;
        private readonly int _hiddenSize;
        private readonly int _outputSize;

        public double[,] WeightsInputHidden { get; set; }
        public double[] BiasHidden { get; set; }
        public double[,] WeightsHiddenOutput { get; set; }
        public double[] BiasOutput { get; set; }

        public int TotalWeightsCount => (_inputSize * _hiddenSize) + _hiddenSize + (_hiddenSize * _outputSize) + _outputSize;

        public NeuralNetwork(int inputSize, int hiddenSize, int outputSize, Random? rand = null)
        {
            _inputSize = inputSize;
            _hiddenSize = hiddenSize;
            _outputSize = outputSize;

            WeightsInputHidden = new double[inputSize, hiddenSize];
            BiasHidden = new double[hiddenSize];
            WeightsHiddenOutput = new double[hiddenSize, outputSize];
            BiasOutput = new double[outputSize];

            InitializeWeights(rand ?? new Random());
        }

        private void InitializeWeights(Random rand)
        {
            double limitHidden = Math.Sqrt(6.0 / (_inputSize + _hiddenSize));
            for (int i = 0; i < _inputSize; i++)
            {
                for (int j = 0; j < _hiddenSize; j++)
                {
                    WeightsInputHidden[i, j] = (rand.NextDouble() * 2.0 - 1.0) * limitHidden;
                }
            }

            for (int j = 0; j < _hiddenSize; j++)
            {
                BiasHidden[j] = 0.0;
            }

            double limitOutput = Math.Sqrt(6.0 / (_hiddenSize + _outputSize));
            for (int i = 0; i < _hiddenSize; i++)
            {
                for (int j = 0; j < _outputSize; j++)
                {
                    WeightsHiddenOutput[i, j] = (rand.NextDouble() * 2.0 - 1.0) * limitOutput;
                }
            }

            for (int k = 0; k < _outputSize; k++)
            {
                BiasOutput[k] = -0.8;
            }
        }

        public double[] Forward(double[] inputs)
        {
            if (inputs.Length != _inputSize)
                throw new ArgumentException($"Ожидался вектор входов размерности {_inputSize}, получено: {inputs.Length}");

            double[] hidden = new double[_hiddenSize];
            for (int j = 0; j < _hiddenSize; j++)
            {
                double sum = BiasHidden[j];
                for (int i = 0; i < _inputSize; i++)
                {
                    sum += inputs[i] * WeightsInputHidden[i, j];
                }
                hidden[j] = Math.Tanh(sum);
            }

            double[] outputs = new double[_outputSize];
            for (int k = 0; k < _outputSize; k++)
            {
                double sum = BiasOutput[k];
                for (int j = 0; j < _hiddenSize; j++)
                {
                    sum += hidden[j] * WeightsHiddenOutput[j, k];
                }
                outputs[k] = 1.0 / (1.0 + Math.Exp(-sum));
            }

            return outputs;
        }

        public double[] GetWeights()
        {
            double[] weights = new double[TotalWeightsCount];
            int idx = 0;

            for (int i = 0; i < _inputSize; i++)
                for (int j = 0; j < _hiddenSize; j++)
                    weights[idx++] = WeightsInputHidden[i, j];

            for (int j = 0; j < _hiddenSize; j++)
                weights[idx++] = BiasHidden[j];

            for (int j = 0; j < _hiddenSize; j++)
                for (int k = 0; k < _outputSize; k++)
                    weights[idx++] = WeightsHiddenOutput[j, k];

            for (int k = 0; k < _outputSize; k++)
                weights[idx++] = BiasOutput[k];

            return weights;
        }

        public void SetWeights(double[] weights)
        {
            if (weights.Length != TotalWeightsCount)
                throw new ArgumentException($"Ожидался массив весов длины {TotalWeightsCount}, получено: {weights.Length}");

            int idx = 0;

            for (int i = 0; i < _inputSize; i++)
                for (int j = 0; j < _hiddenSize; j++)
                    WeightsInputHidden[i, j] = weights[idx++];

            for (int j = 0; j < _hiddenSize; j++)
                BiasHidden[j] = weights[idx++];

            for (int j = 0; j < _hiddenSize; j++)
                for (int k = 0; k < _outputSize; k++)
                    WeightsHiddenOutput[j, k] = weights[idx++];

            for (int k = 0; k < _outputSize; k++)
                BiasOutput[k] = weights[idx++];
        }
    }
}