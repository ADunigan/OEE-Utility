using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OEE_ExcelAddIn_2010
{
    static class DoubleExtensions
    {
        public static double StandardDeviation(this double[] _double)
        {
            double average = _double.Average();
            double sumOfSquaresofDifferences = _double.Select(val => (val - average) * (val - average)).Sum();
            return Math.Sqrt(sumOfSquaresofDifferences / (_double.Length - 1));
        }
    }
}
