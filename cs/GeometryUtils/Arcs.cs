using System;
using System.Collections.Generic;
using System.Linq;

namespace Geometry
{
    public class Arcs
    {
        private GeometryUtil GeometryUtil;

        public Arcs()
        {
            GeometryUtil = new GeometryUtil();
        }

        public List<Point> Linearize(Point p1, Point p2,
            double rx, double ry,
            double angle, bool largearc, bool sweep,
            double tol)
        {

            var finished = new List<Point> { p2 }; // list of points to return

            var arc = SvgToCenter(p1, p2, rx, ry, angle, largearc, sweep);
            var todo = new List<Arc> { arc }; // list of arcs to divide

            // recursion could stack overflow, loop instead
            while (todo.Count() > 0)
            {
                arc = todo[0];

                var fullarc = CenterToSvg(arc.Center, arc.Rx, arc.Ry, arc.Theta, arc.Extent, arc.Angle);
                var subarc = CenterToSvg(arc.Center, arc.Rx, arc.Ry, arc.Theta, 0.5 * arc.Extent, arc.Angle);
                var arcmid = subarc.P2;

                var mid = new Point
                {
                    X = 0.5 * (fullarc.P1.X + fullarc.P2.X),
                    Y = 0.5 * (fullarc.P1.Y + fullarc.P2.Y)
                };

                // compare midpoint of line with midpoint of arc
                // this is not 100% accurate, but should be a good heuristic for flatness in most cases
                if (GeometryUtil.WithinDistance(mid, arcmid, tol))
                {
                    finished.Insert(0, fullarc.P2);
                    todo.RemoveAt(0);
                }
                else
                {
                    var arc1 = new Arc
                    {
                        Center = arc.Center,
                        Rx = arc.Rx,
                        Ry = arc.Ry,
                        Theta = arc.Theta,
                        Extent = 0.5 * arc.Extent,
                        Angle = arc.Angle
                    };
                    var arc2 = new Arc
                    {
                        Center = arc.Center,
                        Rx = arc.Rx,
                        Ry = arc.Ry,
                        Theta = arc.Theta + 0.5 * arc.Extent,
                        Extent = 0.5 * arc.Extent,
                        Angle = arc.Angle
                    };
                    todo.Splice(0, 1, arc1, arc2);
                }
            }

            return finished;
        }

        // convert from center point/angle sweep definition to SVG point and flag definition of arcs
        // ported from http://commons.oreilly.com/wiki/index.php/SVG_Essentials/Paths
        public Arc CenterToSvg(Point center,
            double rx, double ry,
            double theta1, double extent, double angleDegrees)
        {

            var theta2 = theta1 + extent;

            theta1 = GeometryUtil.DegreesToRadians(theta1);
            theta2 = GeometryUtil.DegreesToRadians(theta2);
            var angle = GeometryUtil.DegreesToRadians(angleDegrees);

            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);

            var t1cos = Math.Cos(theta1);
            var t1sin = Math.Sin(theta1);

            var t2cos = Math.Cos(theta2);
            var t2sin = Math.Sin(theta2);

            var x0 = center.X + cos * rx * t1cos + (-sin) * ry * t1sin;
            var y0 = center.Y + sin * rx * t1cos + cos * ry * t1sin;

            var x1 = center.X + cos * rx * t2cos + (-sin) * ry * t2sin;
            var y1 = center.Y + sin * rx * t2cos + cos * ry * t2sin;

            var largearc = (extent > 180) ? true:false;
            var sweep = (extent > 0) ? true : false;

            return new Arc
            {
                P1 = new Point { X = x0, Y = y0 },
                P2 = new Point { X = x1, Y = y1 },
                Rx = rx,
                Ry = ry,
                Angle = angle,
                LargeArc = largearc,
                Sweep = sweep
            };
        }

        // convert from SVG format arc to center point arc
        public Arc SvgToCenter(
            Point p1, Point p2,
            double rx, double ry, double angleDegrees, bool largearc, bool sweep)
        {

            var mid = new Point
            {
                X = 0.5 * (p1.X + p2.X),
                Y = 0.5 * (p1.Y + p2.Y)
            };

            var diff = new Point
            {
                X = 0.5 * (p2.X - p1.X),
                Y = 0.5 * (p2.Y - p1.Y)
            };

            var angle = GeometryUtil.DegreesToRadians(angleDegrees % 360);

            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);

            var x1 = cos * diff.X + sin * diff.Y;
            var y1 = -sin * diff.X + cos * diff.Y;

            rx = Math.Abs(rx);
            ry = Math.Abs(ry);
            var Prx = rx * rx;
            var Pry = ry * ry;
            var Px1 = x1 * x1;
            var Py1 = y1 * y1;

            var radiiCheck = Px1 / Prx + Py1 / Pry;
            var radiiSqrt = Math.Sqrt(radiiCheck);
            if (radiiCheck > 1)
            {
                rx = radiiSqrt * rx;
                ry = radiiSqrt * ry;
                Prx = rx * rx;
                Pry = ry * ry;
            }

            var sign = (largearc != sweep) ? -1 : 1;
            var sq = ((Prx * Pry) - (Prx * Py1) - (Pry * Px1)) / ((Prx * Py1) + (Pry * Px1));

            sq = (sq < 0) ? 0 : sq;

            var coef = sign * Math.Sqrt(sq);
            var cx1 = coef * ((rx * y1) / ry);
            var cy1 = coef * -((ry * x1) / rx);

            var cx = mid.X + (cos * cx1 - sin * cy1);
            var cy = mid.Y + (sin * cx1 + cos * cy1);

            var ux = (x1 - cx1) / rx;
            var uy = (y1 - cy1) / ry;
            var vx = (-x1 - cx1) / rx;
            var vy = (-y1 - cy1) / ry;
            var n = Math.Sqrt((ux * ux) + (uy * uy));
            var p = ux;
            sign = (uy < 0) ? -1 : 1;

            var theta = sign * Math.Acos(p / n);
            theta = GeometryUtil.RadiansToDegrees(theta);

            n = Math.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy));
            p = ux * vx + uy * vy;
            sign = ((ux * vy - uy * vx) < 0) ? -1 : 1;
            var delta = sign * Math.Acos(p / n);
            delta = GeometryUtil.RadiansToDegrees(delta);

            if (sweep  && delta > 0)
            {
                delta -= 360;
            }
            else if (!sweep && delta < 0)
            {
                delta += 360;
            }

            delta %= 360;
            theta %= 360;

            return new Arc
            {
                Center = new Point { X = cx, Y = cy },
                Rx = rx,
                Ry = ry,
                Theta = theta,
                Extent = delta,
                Angle = angleDegrees
            };
        }

    }
}