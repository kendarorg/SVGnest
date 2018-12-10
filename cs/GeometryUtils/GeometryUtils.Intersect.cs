using System;

namespace Geometry
{
    public partial class GeometryUtil
    {

        // todo: swap this for a more efficient sweep-line implementation
        // returnEdges: if set, return all edges on A that have intersections

        public static bool Intersect(Polygon a, Polygon b)
        {
            var aOffsetx = a.offsetx;
            var aOffsety = a.offsety;

            var bOffsetx = b.offsetx;
            var bOffsety = b.offsety;

            var aClone = a.Clone();
            var bClone = b.Clone();

            for (var i = 0; i < aClone.Count - 1; i++)
            {
                for (var j = 0; j < bClone.Count - 1; j++)
                {
                    var a1 = new Point{X= aClone[i].X+aOffsetx, Y=aClone[i].Y+aOffsety};
                    var a2 = new Point { X= aClone[i+1].X+aOffsetx, Y=aClone[i+1].Y+aOffsety};
                    var b1 = new Point { X= bClone[j].X+bOffsetx, Y= bClone[j].Y+bOffsety};
                    var b2 = new Point { X= bClone[j+1].X+bOffsetx, Y= bClone[j+1].Y+bOffsety};

                    var prevbindex = (j == 0) ? bClone.Count - 1 : j - 1;
                    var prevaindex = (i == 0) ? aClone.Count - 1 : i - 1;
                    var nextbindex = (j + 1 == bClone.Count - 1) ? 0 : j + 2;
                    var nextaindex = (i + 1 == aClone.Count - 1) ? 0 : i + 2;

                    // go even further back if we happen to hit on a loop End point
                    if (bClone[prevbindex] == bClone[j] ||
                        (AlmostEqual(bClone[prevbindex].X, bClone[j].X) && AlmostEqual(bClone[prevbindex].Y, bClone[j].Y)))
                    {
                        prevbindex = (prevbindex == 0) ? bClone.Count - 1 : prevbindex - 1;
                    }

                    if (aClone[prevaindex] == aClone[i] ||
                        (AlmostEqual(aClone[prevaindex].X, aClone[i].X) && AlmostEqual(aClone[prevaindex].Y, aClone[i].Y)))
                    {
                        prevaindex = (prevaindex == 0) ? aClone.Count - 1 : prevaindex - 1;
                    }

                    // go even further forward if we happen to hit on a loop End point
                    if (bClone[nextbindex] == bClone[j + 1] ||
                        (AlmostEqual(bClone[nextbindex].X, bClone[j + 1].X) && AlmostEqual(bClone[nextbindex].Y, bClone[j + 1].Y)))
                    {
                        nextbindex = (nextbindex == bClone.Count - 1) ? 0 : nextbindex + 1;
                    }

                    if (aClone[nextaindex] == aClone[i + 1] ||
                        (AlmostEqual(aClone[nextaindex].X, aClone[i + 1].X) && AlmostEqual(aClone[nextaindex].Y, aClone[i + 1].Y)))
                    {
                        nextaindex = (nextaindex == aClone.Count - 1) ? 0 : nextaindex + 1;
                    }

                    var a0 = new Point {X= aClone[prevaindex].X + aOffsetx, Y= aClone[prevaindex].Y + aOffsety};
                    var b0 = new Point {X= bClone[prevbindex].X + bOffsetx, Y= bClone[prevbindex].Y + bOffsety};

                    var a3 = new Point {X= aClone[nextaindex].X + aOffsetx, Y= aClone[nextaindex].Y + aOffsety};
                    var b3 = new Point {X= bClone[nextbindex].X + bOffsetx, Y= bClone[nextbindex].Y + bOffsety};

                    if (OnSegment(a1, a2, b1) || (AlmostEqual(a1.X, b1.X) && AlmostEqual(a1.Y, b1.Y)))
                    {
                        // if a point is on a segment, it could intersect or it could not. Check via the neighboring points
                        var b0in = PointInPolygon(b0, aClone);
                        var b2in = PointInPolygon(b2, aClone);
                        if ((b0in == true && b2in == false) || (b0in == false && b2in == true))
                        {
                            return true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (OnSegment(a1, a2, b2) || (AlmostEqual(a2.X, b2.X) && AlmostEqual(a2.Y, b2.Y)))
                    {
                        // if a point is on a segment, it could intersect or it could not. Check via the neighboring points
                        var b1in = PointInPolygon(b1, aClone);
                        var b3in = PointInPolygon(b3, aClone);

                        if ((b1in == true && b3in == false) || (b1in == false && b3in == true))
                        {
                            return true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (OnSegment(b1, b2, a1) || (AlmostEqual(a1.X, b2.X) && AlmostEqual(a1.Y, b2.Y)))
                    {
                        // if a point is on a segment, it could intersect or it could not. Check via the neighboring points
                        var a0in = PointInPolygon(a0, bClone);
                        var a2in = PointInPolygon(a2, bClone);

                        if ((a0in == true && a2in == false) || (a0in == false && a2in == true))
                        {
                            return true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (OnSegment(b1, b2, a2) || (AlmostEqual(a2.X, b1.X) && AlmostEqual(a2.Y, b1.Y)))
                    {
                        // if a point is on a segment, it could intersect or it could not. Check via the neighboring points
                        var a1in = PointInPolygon(a1, bClone);
                        var a3in = PointInPolygon(a3, bClone);

                        if ((a1in == true && a3in == false) || (a1in == false && a3in == true))
                        {
                            return true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    var p = LineIntersect(b1, b2, a1, a2);

                    if (p != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }


        // return true if point is in the polygon, false if outside, and null if exactly on a point or edge
        public static bool? PointInPolygon(Point point, Polygon polygon)
        {
            if (polygon == null || polygon.Count < 3)
            {
                return null;
            }

            var inside = false;
            var offsetx = polygon.offsetx;
            var offsety = polygon.offsety;

            var i = 0;
            var j = polygon.Count - 1;
            for (; i < polygon.Count; j = i++)
            {
                var xi = polygon[i].X + offsetx;
                var yi = polygon[i].Y + offsety;
                var xj = polygon[j].X + offsetx;
                var yj = polygon[j].Y + offsety;

                if (AlmostEqual(xi, point.X) && AlmostEqual(yi, point.Y))
                {
                    return null; // no result
                }

                if (OnSegment(new Point {X= xi, Y= yi}, new Point {X= xj, Y= yj}, point))
                {
                    return null; // exactly on the segment
                }

                if (AlmostEqual(xi, xj) && AlmostEqual(yi, yj))
                {
                    // ignore very small lines
                    continue;
                }

                var intersect = ((yi > point.Y) != (yj > point.Y)) &&
                                (point.X < (xj - xi) * (point.Y - yi) / (yj - yi) + xi);
                if (intersect) inside = !inside;
            }

            return inside;
        }


        // returns the intersection of AB and EF
        // or null if there are no intersections or other numerical error
        // if the infinite flag is set, AE and EF describe infinite lines without endpoints, they are finite line segments otherwise
        public static Point LineIntersect(
            Point A, Point B,
            Point E, Point F, bool infinite=false)
        {

            var a1 = B.Y - A.Y;
            var b1 = A.X - B.X;
            var c1 = B.X * A.Y - A.X * B.Y;
            var a2 = F.Y - E.Y;
            var b2 = E.X - F.X;
            var c2 = F.X * E.Y - E.X * F.Y;

            var denom = a1 * b2 - a2 * b1;

            var x = (b1 * c2 - b2 * c1) / denom;
            var y = (a2 * c1 - a1 * c2) / denom;

            if (!IsFinite(x) || !IsFinite(y))
            {
                return null;
            }

            // lines are colinear
            /*var crossABE = (E.Y - A.Y) * (B.X - A.X) - (E.X - A.X) * (B.Y - A.Y);
            var crossABF = (F.Y - A.Y) * (B.X - A.X) - (F.X - A.X) * (B.Y - A.Y);
            if(AlmostEqual(crossABE,0) && AlmostEqual(crossABF,0)){
                return null;
            }*/

            if (!infinite)
            {
                // coincident points do not count as intersecting
                if (Math.Abs(A.X - B.X) > _tol && ((A.X < B.X) ? x < A.X || x > B.X : x > A.X || x < B.X)) return null;
                if (Math.Abs(A.Y - B.Y) > _tol && ((A.Y < B.Y) ? y < A.Y || y > B.Y : y > A.Y || y < B.Y)) return null;

                if (Math.Abs(E.X - F.X) > _tol && ((E.X < F.X) ? x < E.X || x > F.X : x > E.X || x < F.X)) return null;
                if (Math.Abs(E.Y - F.Y) > _tol && ((E.Y < F.Y) ? y < E.Y || y > F.Y : y > E.Y || y < F.Y)) return null;
            }

            return new Point {X= x, Y= y};
        }

        // returns true if p lies on the line segment defined by AB, but not at any endpoints
        // may need work!
        public static bool OnSegment(Point A, Point B, Point p)
        {

            // vertical line
            if (AlmostEqual(A.X, B.X) && AlmostEqual(p.X, A.X))
            {
                if (!AlmostEqual(p.Y, B.Y) && !AlmostEqual(p.Y, A.Y) && p.Y < Math.Max(B.Y, A.Y) &&
                    p.Y > Math.Min(B.Y, A.Y))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            // horizontal line
            if (AlmostEqual(A.Y, B.Y) && AlmostEqual(p.Y, A.Y))
            {
                if (!AlmostEqual(p.X, B.X) && !AlmostEqual(p.X, A.X) && p.X < Math.Max(B.X, A.X) &&
                    p.X > Math.Min(B.X, A.X))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            //range check
            if ((p.X < A.X && p.X < B.X) || (p.X > A.X && p.X > B.X) || (p.Y < A.Y && p.Y < B.Y) ||
                (p.Y > A.Y && p.Y > B.Y))
            {
                return false;
            }


            // exclude End points
            if ((AlmostEqual(p.X, A.X) && AlmostEqual(p.Y, A.Y)) || (AlmostEqual(p.X, B.X) && AlmostEqual(p.Y, B.Y)))
            {
                return false;
            }

            var cross = (p.Y - A.Y) * (B.X - A.X) - (p.X - A.X) * (B.Y - A.Y);

            if (Math.Abs(cross) > _tol)
            {
                return false;
            }

            var dot = (p.X - A.X) * (B.X - A.X) + (p.Y - A.Y) * (B.Y - A.Y);



            if (dot < 0 || AlmostEqual(dot, 0))
            {
                return false;
            }

            var len2 = (B.X - A.X) * (B.X - A.X) + (B.Y - A.Y) * (B.Y - A.Y);



            if (dot > len2 || AlmostEqual(dot, len2))
            {
                return false;
            }

            return true;
        }
    }
}