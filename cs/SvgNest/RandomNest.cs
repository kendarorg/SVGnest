using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SvgNest
{
    public class NestRandom
    {
        private static Random _random = new Random();
        private static double _starter = 0.0001;

        public static double NextDouble()
        {
            //return _random.NextDouble();
            _starter += 0.0001;
            return _starter;
        }
    }
}
