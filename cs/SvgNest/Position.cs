using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClipperLib;

namespace SvgNest
{
    public class Position
    {
        public double X;
        public double Y;
        public int Id;
        public double Rotation;
        public List<List<IntPoint>> Nfp;
    }
}
