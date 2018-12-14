using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Geometry;

namespace SvgNest
{
    public class Placement
    {
        public double? Fitness = null;
        public List<List<Position>> Placements;
        public List<Polygon> Paths;
        public double Area;
    }
}
