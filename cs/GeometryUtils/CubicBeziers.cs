using System.Collections.Generic;
using System.Linq;

namespace Geometry
{
    public class CubicBeziers
    {
        public bool IsFlat(Point p1, Point p2, Point c1, Point c2, double tol)
        {
            tol = 16 * tol * tol;

            var ux = 3 * c1.X - 2 * p1.X - p2.X;
            ux *= ux;

            var uy = 3 * c1.Y - 2 * p1.Y - p2.Y;
            uy *= uy;

            var vx = 3 * c2.X - 2 * p2.X - p1.X;
            vx *= vx;

            var vy = 3 * c2.Y - 2 * p2.Y - p1.Y;
            vy *= vy;

            if (ux < vx)
            {
                ux = vx;
            }

            if (uy < vy)
            {
                uy = vy;
            }

            return (ux + uy <= tol);
        }

        public List<Point> Linearize(Point p1, Point p2, Point c1, Point c2, double tol)
        {
            var finished = new List<Point> { p1 }; // list of points to return
            var todo = new List<CubicBezier>
                {new CubicBezier {P1= p1, P2= p2, C1= c1, C2= c2}}; // list of Beziers to divide

            // recursion could stack overflow, loop instead

            while (todo.Count() > 0)
            {
                var segment = todo[0];

                if (IsFlat(segment.P1, segment.P2, segment.C1, segment.C2, tol))
                {
                    // reached subdivision limit
                    finished.Add(new Point
                    {
                        X = segment.P2.X,
                        Y = segment.P2.Y
                    });
                    todo.RemoveAt(0);
                }
                else
                {
                    var divided = Subdivide(segment.P1, segment.P2, segment.C1, segment.C2, 0.5);
                    todo.Splice(0, 1, divided[0], divided[1]);
                }
            }

            return finished;
        }

        public List<CubicBezier> Subdivide(Point p1, Point p2, Point c1, Point c2, double t)
        {
            var mid1 = new Point
            {
                X = p1.X + (c1.X - p1.X) * t,
                Y = p1.Y + (c1.Y - p1.Y) * t
            };

            var mid2 = new Point
            {
                X = c2.X + (p2.X - c2.X) * t,
                Y = c2.Y + (p2.Y - c2.Y) * t
            };

            var mid3 = new Point
            {
                X = c1.X + (c2.X - c1.X) * t,
                Y = c1.Y + (c2.Y - c1.Y) * t
            };

            var mida = new Point
            {
                X = mid1.X + (mid3.X - mid1.X) * t,
                Y = mid1.Y + (mid3.Y - mid1.Y) * t
            };

            var midb = new Point
            {
                X = mid3.X + (mid2.X - mid3.X) * t,
                Y = mid3.Y + (mid2.Y - mid3.Y) * t
            };

            var midx = new Point
            {
                X = mida.X + (midb.X - mida.X) * t,
                Y = mida.Y + (midb.Y - mida.Y) * t
            };

            var seg1 = new CubicBezier { P1 = p1, P2 = midx, C1 = mid1, C2 = mida };
            var seg2 = new CubicBezier { P1 = midx, P2 = p2, C1 = midb, C2 = mid2 };

            return new List<CubicBezier> { seg1, seg2 };
        }
    }
}