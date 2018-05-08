using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MathNet;

namespace OEE_WPF_Application
{
    class Program
    {
        private static void test(string[] args)
        {
            double[,] array = new double[500000,2];
            double mtbf = 1.0 / 100;
            double mttr = 1.0 / 2;
            MathNet.Numerics.Distributions.Exponential mtbf_expon = new MathNet.Numerics.Distributions.Exponential(mtbf);
            MathNet.Numerics.Distributions.Exponential mttr_expon = new MathNet.Numerics.Distributions.Exponential(mttr);
            for (int i = 0; i < 500000; i++)
            {
                array[i, 0] = mtbf_expon.Sample();
                array[i, 1] = mttr_expon.Sample();
            }
            //Console.WriteLine(array.Average().ToString());
            int j = 0;
        }
    }
}
