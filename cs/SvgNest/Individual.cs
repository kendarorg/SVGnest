using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Geometry;

namespace SvgNest
{
    public class Individual
    {
        public List<Polygon> Placements;
        public List<double> Rotations;
        public double? Fitness = null;
    }
}
