using System.Collections.Generic;

namespace Geometry
{
    // Bezier algos from http://algorithmist.net/docs/subdivision.pdf
    public class QuadraticBeziers
    {
        // returns the intersection of AB and EF
        // or null if there are no intersections or other numerical error
        // if the infinite flag is set, AE and EF describe infinite lines without endpoints, they are finite line segments otherwise
        public bool IsFlat(Point p1, Point p2, Point c1, double tol)
        {
            tol = 4 * tol * tol;

            var ux = 2 * c1.X - p1.X - p2.X;
            ux *= ux;

            var uy = 2 * c1.Y - p1.Y - p2.Y;
            uy *= uy;

            return (ux + uy <= tol);
        }

        // turn Bezier into line segments via de Casteljau, returns an array of points
        public List<Point> Linearize(Point p1, Point p2, Point c1, double tol)
        {
            var finished = new List<Point> { p1 }; // list of points to return
            var todo = new List<QuadraticBezier>
            {
                new QuadraticBezier {P1= p1, P2= p2, C1= c1}}; // list of Beziers to divide

            // recursion could stack overflow, loop instead
            while (todo.Count > 0)
            {
                var segment = todo[0];

                if (IsFlat(segment.P1, segment.P2, segment.C1, tol))
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
                    var divided = Subdivide(segment.P1, segment.P2, segment.C1, 0.5);
                    todo.Splice(0, 1, divided[0], divided[1]);
                }
            }
            return finished;
        }

        // subdivide a single Bezier
        // t is the percent along the Bezier to divide at. eg. 0.5
        public List<QuadraticBezier> Subdivide(Point p1, Point p2, Point c1, double t)
        {
            var mid1 = new Point
            {
                X = p1.X + (c1.X - p1.X) * t,
                Y = p1.Y + (c1.Y - p1.Y) * t
            };

            var mid2 = new Point
            {
                X = c1.X + (p2.X - c1.X) * t,
                Y = c1.Y + (p2.Y - c1.Y) * t
            };

            var mid3 = new Point
            {
                X = mid1.X + (mid2.X - mid1.X) * t,
                Y = mid1.Y + (mid2.Y - mid1.Y) * t
            };

            var seg1 = new QuadraticBezier { P1 = p1, P2 = mid3, C1 = mid1 };
            var seg2 = new QuadraticBezier { P1 = mid3, P2 = p2, C1 = mid2 };

            return new List<QuadraticBezier> { seg1, seg2 };
        }
    }
}