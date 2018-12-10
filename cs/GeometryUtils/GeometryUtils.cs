using System;
using System.Collections.Generic;
using System.Linq;

namespace Geometry
{
    public partial class GeometryUtil
    {
        // floating point comparison tolerance
        private static double _tol = Math.Pow(10, -9); // Floating point error is likely to be above 1 epsilon

        public static bool AlmostEqual(double a, double b, double? tolerance = null)
        {
            if (tolerance == null)
            {
                tolerance = _tol;
            }

            return Math.Abs(a - b) < tolerance;
        }

        public static double DegreesToRadians(double angle)
        {
            return angle * (Math.PI / 180);
        }

        public static double RadiansToDegrees(double angle)
        {
            return angle * (180 / Math.PI);
        }

        // normalize vector into a unit vector
        public static Vector NormalizeVector(Vector v)
        {
            if (AlmostEqual(v.X * v.X + v.Y * v.Y, 1))
            {
                return v; // given vector was already a unit vector
            }

            var len = Math.Sqrt(v.X * v.X + v.Y * v.Y);
            var inverse = 1 / len;

            return new Vector
            {
                X = v.X * inverse,
                Y = v.Y * inverse
            };
        }







        // returns true if point already exists in the given nfp
        private static bool InNfp(Point p, List<Polygon> nfp)
        {
            if (null == nfp || nfp.Count() == 0)
            {
                return false;
            }

            for (var i = 0; i < nfp.Count(); i++)
            {
                for (var j = 0; j < nfp[i].Count; j++)
                {
                    if (AlmostEqual(p.X, nfp[i][j].X) && AlmostEqual(p.Y, nfp[i][j].Y))
                    {
                        return true;
                    }
                }
            }

            return false;
        }


        private static bool IsFinite(double p0)
        {
            return !double.IsInfinity(p0) && !double.IsNaN(p0);
        }
    }
}