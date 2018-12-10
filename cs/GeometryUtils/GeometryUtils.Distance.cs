using System;
using System.Collections.Generic;
using System.Linq;

namespace Geometry
{
    public partial class GeometryUtil
    {

        // project each point of B onto A in the given direction, and return the 
        public static double? PolygonProjectionDistance(Polygon a, Polygon b, Vector direction)
        {
            var Boffsetx = b.offsetx;
            var Boffsety = b.offsety;

            var Aoffsetx = a.offsetx;
            var Aoffsety = a.offsety;

            var A = a.AsList();
            var B = b.AsList();

            // close the loop for polygons
            if (A[0] != A[A.Count() - 1])
            {
                A.Add(A[0]);
            }

            if (B[0] != B[B.Count() - 1])
            {
                B.Add(B[0]);
            }

            var edgeA = A;
            var edgeB = B;

            double? distance = null;
            Point p, s1, s2;
            double? d;


            for (var i = 0; i < edgeB.Count(); i++)
            {
                // the shortest/most negative projection of B onto A
                double? minprojection = null;
                //Point minp = null;
                for (var j = 0; j < edgeA.Count() - 1; j++)
                {
                    p = new Point
                    {
                        X = edgeB[i].X + Boffsetx,
                        Y = edgeB[i].Y + Boffsety
                    }
                    ;
                    s1 = new Point
                    {
                        X = edgeA[j].X + Aoffsetx,
                        Y = edgeA[j].Y + Aoffsety
                    }
                    ;
                    s2 = new Point
                    {
                        X = edgeA[j + 1].X + Aoffsetx,
                        Y = edgeA[j + 1].Y + Aoffsety
                    }
                    ;

                    if (Math.Abs((s2.Y - s1.Y) * direction.X - (s2.X - s1.X) * direction.Y) < _tol)
                    {
                        continue;
                    }

                    // project point, ignore edge boundaries
                    d = PointDistance(p, s1, s2, direction);

                    if (d != null && (minprojection == null || d < minprojection))
                    {
                        minprojection = d;
                        //minp = p;
                    }
                }

                if (minprojection != null && (distance == null || minprojection > distance))
                {
                    distance = minprojection;
                }
            }

            return distance;
        }

        public static double? PolygonSlideDistance(Polygon a, Polygon b,
            Vector direction, bool ignoreNegative)
        {

            Point A1, A2, B1, B2;
            double Aoffsetx, Aoffsety, Boffsetx, Boffsety;

            Aoffsetx = a.offsetx;
            Aoffsety = a.offsety;

            Boffsetx = b.offsetx;
            Boffsety = b.offsety;

            var A = a.AsList();
            var B = b.AsList();

            // close the loop for polygons
            if (A[0] != A[A.Count() - 1])
            {
                A.Add(A[0]);
            }

            if (B[0] != B[B.Count() - 1])
            {
                B.Add(B[0]);
            }

            var edgeA = A;
            var edgeB = B;

            double? distance = null;
            //Point p, s1, s2;
            double? d;


            var dir = NormalizeVector(direction);

            /*var normal = new Vector
            {
                X = dir.Y,
                Y = -dir.X
            };

            var reverse = new Vector
            {
                X = -dir.X,
                Y = -dir.Y,
            };*/

            for (var i = 0; i < edgeB.Count() - 1; i++)
            {
                for (var j = 0; j < edgeA.Count() - 1; j++)
                {
                    A1 = new Point
                    {
                        X = edgeA[j].X + Aoffsetx,
                        Y = edgeA[j].Y + Aoffsety
                    };
                    A2 = new Point
                    {
                        X = edgeA[j + 1].X + Aoffsetx,
                        Y = edgeA[j + 1].Y + Aoffsety
                    };
                    B1 = new Point
                    {
                        X = edgeB[i].X + Boffsetx,
                        Y = edgeB[i].Y + Boffsety
                    };
                    B2 = new Point
                    {
                        X = edgeB[i + 1].X + Boffsetx,
                        Y = edgeB[i + 1].Y + Boffsety
                    };

                    if ((AlmostEqual(A1.X, A2.X) && AlmostEqual(A1.Y, A2.Y)) ||
                        (AlmostEqual(B1.X, B2.X) && AlmostEqual(B1.Y, B2.Y)))
                    {
                        continue; // ignore extremely small lines
                    }

                    d = SegmentDistance(A1, A2, B1, B2, dir);

                    if (d != null && (distance == null || d < distance))
                    {
                        if (!ignoreNegative || d > 0 || AlmostEqual(d.Value, 0))
                        {
                            distance = d;
                        }
                    }
                }
            }

            return distance;
        }

        public static double? SegmentDistance(
            Point A, Point B,
            Point E, Point F, Vector direction)
        {
            var normal = new Vector
            {
                X = direction.Y,
                Y = -direction.X
            };

            var reverse = new Vector
            {
                X = -direction.X,
                Y = -direction.Y
            };

            var dotA = A.X * normal.X + A.Y * normal.Y;
            var dotB = B.X * normal.X + B.Y * normal.Y;
            var dotE = E.X * normal.X + E.Y * normal.Y;
            var dotF = F.X * normal.X + F.Y * normal.Y;

            var crossA = A.X * direction.X + A.Y * direction.Y;
            var crossB = B.X * direction.X + B.Y * direction.Y;
            var crossE = E.X * direction.X + E.Y * direction.Y;
            var crossF = F.X * direction.X + F.Y * direction.Y;

            //var crossABmin = Math.Min(crossA, crossB);
            //var crossABmax = Math.Max(crossA, crossB);

            //var crossEFmax = Math.Max(crossE, crossF);
            //var crossEFmin = Math.Min(crossE, crossF);

            var ABmin = Math.Min(dotA, dotB);
            var ABmax = Math.Max(dotA, dotB);

            var EFmax = Math.Max(dotE, dotF);
            var EFmin = Math.Min(dotE, dotF);

            // segments that will merely touch at one point
            if (AlmostEqual(ABmax, EFmin, _tol) || AlmostEqual(ABmin, EFmax, _tol))
            {
                return null;
            }

            // segments miss eachother completely
            if (ABmax < EFmin || ABmin > EFmax)
            {
                return null;
            }

            double overlap = 0;

            if ((ABmax > EFmax && ABmin < EFmin) || (EFmax > ABmax && EFmin < ABmin))
            {
                overlap = 1;
            }
            else
            {
                var minMax = Math.Min(ABmax, EFmax);
                var maxMin = Math.Max(ABmin, EFmin);

                var maxMax = Math.Max(ABmax, EFmax);
                var minMin = Math.Min(ABmin, EFmin);

                overlap = (minMax - maxMin) / (maxMax - minMin);
            }

            var crossABE = (E.Y - A.Y) * (B.X - A.X) - (E.X - A.X) * (B.Y - A.Y);
            var crossABF = (F.Y - A.Y) * (B.X - A.X) - (F.X - A.X) * (B.Y - A.Y);

            // lines are colinear
            if (AlmostEqual(crossABE, 0) && AlmostEqual(crossABF, 0))
            {

                var ABnorm = new Vector { X = B.Y - A.Y, Y = A.X - B.X };
                var EFnorm = new Vector { X = F.Y - E.Y, Y = E.X - F.X };

                var ABNormlength = Math.Sqrt(ABnorm.X * ABnorm.X + ABnorm.Y * ABnorm.Y);
                ABnorm.X /= ABNormlength;
                ABnorm.Y /= ABNormlength;

                var EFnormlength = Math.Sqrt(EFnorm.X * EFnorm.X + EFnorm.Y * EFnorm.Y);
                EFnorm.X /= EFnormlength;
                EFnorm.Y /= EFnormlength;

                // segment normals must point in opposite directions
                if (Math.Abs(ABnorm.Y * EFnorm.X - ABnorm.X * EFnorm.Y) < _tol &&
                    ABnorm.Y * EFnorm.Y + ABnorm.X * EFnorm.X < 0)
                {
                    // normal of AB segment must point in same direction as given direction vector
                    var normdot = ABnorm.Y * direction.Y + ABnorm.X * direction.X;
                    // the segments merely slide along eachother
                    if (AlmostEqual(normdot, 0, _tol))
                    {
                        return null;
                    }

                    if (normdot < 0)
                    {
                        return 0;
                    }
                }

                return null;
            }

            var distances = new List<double>();

            // coincident points
            if (AlmostEqual(dotA, dotE))
            {
                distances.Add(crossA - crossE);
            }
            else if (AlmostEqual(dotA, dotF))
            {
                distances.Add(crossA - crossF);
            }
            else if (dotA > EFmin && dotA < EFmax)
            {
                var d = PointDistance(A, E, F, reverse);
                if (d != null && AlmostEqual(d.Value, 0))
                {
                    //  A currently touches EF, but AB is moving away from EF
                    var dB = PointDistance(B, E, F, reverse, true);
                    if (dB < 0 || AlmostEqual(dB.Value * overlap, 0))
                    {
                        d = null;
                    }
                }

                if (d != null)
                {
                    distances.Add(d.Value);
                }
            }

            if (AlmostEqual(dotB, dotE))
            {
                distances.Add(crossB - crossE);
            }
            else if (AlmostEqual(dotB, dotF))
            {
                distances.Add(crossB - crossF);
            }
            else if (dotB > EFmin && dotB < EFmax)
            {
                var d = PointDistance(B, E, F, reverse);

                if (d != null && AlmostEqual(d.Value, 0))
                {
                    // crossA>crossB A currently touches EF, but AB is moving away from EF
                    var dA = PointDistance(A, E, F, reverse, true);
                    if (dA < 0 || AlmostEqual(dA.Value * overlap, 0))
                    {
                        d = null;
                    }
                }

                if (d != null)
                {
                    distances.Add(d.Value);
                }
            }

            if (dotE > ABmin && dotE < ABmax)
            {
                var d = PointDistance(E, A, B, direction);
                if (d != null && AlmostEqual(d.Value, 0))
                {
                    // crossF<crossE A currently touches EF, but AB is moving away from EF
                    var dF = PointDistance(F, A, B, direction, true);
                    if (dF < 0 || AlmostEqual(dF.Value * overlap, 0))
                    {
                        d = null;
                    }
                }

                if (d != null)
                {
                    distances.Add(d.Value);
                }
            }

            if (dotF > ABmin && dotF < ABmax)
            {
                var d = PointDistance(F, A, B, direction);
                if (d != null && AlmostEqual(d.Value, 0))
                {
                    // && crossE<crossF A currently touches EF, but AB is moving away from EF
                    var dE = PointDistance(E, A, B, direction, true);
                    if (dE < 0 || AlmostEqual(dE.Value * overlap, 0))
                    {
                        d = null;
                    }
                }

                if (d != null)
                {
                    distances.Add(d.Value);
                }
            }

            if (distances.Count() == 0)
            {
                return null;
            }

            return distances.Min(a => a);
        }

        public static double? PointDistance(Point p, Point s1, Point s2, Vector normal, bool infinite=false)
        {
            normal = NormalizeVector(normal);

            var dir = new Vector
            {
                X = normal.Y,
                Y = -normal.X
            };

            var pdot = p.X * dir.X + p.Y * dir.Y;
            var s1dot = s1.X * dir.X + s1.Y * dir.Y;
            var s2dot = s2.X * dir.X + s2.Y * dir.Y;

            var pdotnorm = p.X * normal.X + p.Y * normal.Y;
            var s1dotnorm = s1.X * normal.X + s1.Y * normal.Y;
            var s2dotnorm = s2.X * normal.X + s2.Y * normal.Y;

            if (!infinite)
            {
                if (((pdot < s1dot || AlmostEqual(pdot, s1dot)) && (pdot < s2dot || AlmostEqual(pdot, s2dot))) ||
                    ((pdot > s1dot || AlmostEqual(pdot, s1dot)) && (pdot > s2dot || AlmostEqual(pdot, s2dot))))
                {
                    return null; // dot doesn't collide with segment, or lies directly on the vertex
                }

                if ((AlmostEqual(pdot, s1dot) && AlmostEqual(pdot, s2dot)) &&
                    (pdotnorm > s1dotnorm && pdotnorm > s2dotnorm))
                {
                    return Math.Min(pdotnorm - s1dotnorm, pdotnorm - s2dotnorm);
                }

                if ((AlmostEqual(pdot, s1dot) && AlmostEqual(pdot, s2dot)) &&
                    (pdotnorm < s1dotnorm && pdotnorm < s2dotnorm))
                {
                    return -Math.Min(s1dotnorm - pdotnorm, s2dotnorm - pdotnorm);
                }
            }

            return -(pdotnorm - s1dotnorm + (s1dotnorm - s2dotnorm) * (s1dot - pdot) / (s1dot - s2dot));
        }

        // returns the normal distance from p to a line segment defined by s1 s2
        // this is basically algo 9 in [1], generalized for any vector direction
        // eg. normal of [-1, 0] returns the horizontal distance between the point and the line segment
        // sxinclusive: if true, include endpoints instead of excluding them

        public static double? PointLineDistance(Point p, Point s1, Point s2,
            Vector normal, bool s1inclusive, bool s2inclusive)
        {

            normal = NormalizeVector(normal);

            var dir = new Vector
            {
                X = normal.Y,
                Y = -normal.X
            };

            var pdot = p.X * dir.X + p.Y * dir.Y;
            var s1dot = s1.X * dir.X + s1.Y * dir.Y;
            var s2dot = s2.X * dir.X + s2.Y * dir.Y;

            var pdotnorm = p.X * normal.X + p.Y * normal.Y;
            var s1dotnorm = s1.X * normal.X + s1.Y * normal.Y;
            var s2dotnorm = s2.X * normal.X + s2.Y * normal.Y;


            // point is exactly along the edge in the normal direction
            if (AlmostEqual(pdot, s1dot) && AlmostEqual(pdot, s2dot))
            {
                // point lies on an endpoint
                if (AlmostEqual(pdotnorm, s1dotnorm))
                {
                    return null;
                }

                if (AlmostEqual(pdotnorm, s2dotnorm))
                {
                    return null;
                }

                // point is outside both endpoints
                if (pdotnorm > s1dotnorm && pdotnorm > s2dotnorm)
                {
                    return Math.Min(pdotnorm - s1dotnorm, pdotnorm - s2dotnorm);
                }

                if (pdotnorm < s1dotnorm && pdotnorm < s2dotnorm)
                {
                    return -Math.Min(s1dotnorm - pdotnorm, s2dotnorm - pdotnorm);
                }

                // point lies between endpoints
                var diff1 = pdotnorm - s1dotnorm;
                var diff2 = pdotnorm - s2dotnorm;
                if (diff1 > 0)
                {
                    return diff1;
                }
                else
                {
                    return diff2;
                }
            }
            // point 
            else if (AlmostEqual(pdot, s1dot))
            {
                if (s1inclusive)
                {
                    return pdotnorm - s1dotnorm;
                }
                else
                {
                    return null;
                }
            }
            else if (AlmostEqual(pdot, s2dot))
            {
                if (s2inclusive)
                {
                    return pdotnorm - s2dotnorm;
                }
                else
                {
                    return null;
                }
            }
            else if ((pdot < s1dot && pdot < s2dot) || (pdot > s1dot && pdot > s2dot))
            {
                return null; // point doesn't collide with segment
            }

            return (pdotnorm - s1dotnorm + (s1dotnorm - s2dotnorm) * (s1dot - pdot) / (s1dot - s2dot));
        }

        // returns true if points are within the given distance
        public static bool WithinDistance(Point p1, Point p2, double distance)
        {
            var dx = p1.X - p2.X;
            var dy = p1.Y - p2.Y;
            return ((dx * dx + dy * dy) < distance * distance);
        }
    }
}