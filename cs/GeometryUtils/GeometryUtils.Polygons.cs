using System;
using System.Collections.Generic;
using System.Linq;

namespace Geometry
{
    public partial class GeometryUtil
    {

		public static Polygon OffsetPolygon(Polygon polygon,double x=0,double y=0,bool deep=false){
			var result = polygon.Clone(false);
			foreach(var pt in result.Points){
				pt.X+=x;
				pt.Y+=y;
			}
			if(deep){
				foreach(var child in polygon.Children){
					result.Children.Add(OffsetPolygon(child,x,y,deep));
				}
			}

		    return result;
		}
        // given two polygons that touch at at least one point, but do not intersect. Return the outer perimeter of both polygons as a single continuous polygon
        // A and B must have the same winding direction
        public static Polygon PolygonHull(Polygon A, Polygon B)
        {
            if (null == A || A.Count < 3 || null == B || B.Count < 3)
            {
                return null;
            }

            //var A = a.Clone();
            //var B = b.Clone();
            int i, j;

            var Aoffsetx = A.offsetx;
            var Aoffsety = A.offsety;
            var Boffsetx = B.offsetx;
            var Boffsety = B.offsety;

            // Start at an extreme point that is guaranteed to be on the final polygon
            var miny = A[0].Y;
            var startPolygon = A;
            var startIndex = 0;

            for (i = 0; i < A.Count; i++)
            {
                if (A[i].Y + Aoffsety < miny)
                {
                    miny = A[i].Y + Aoffsety;
                    startPolygon = A;
                    startIndex = i;
                }
            }

            for (i = 0; i < B.Count; i++)
            {
                if (B[i].Y + Boffsety < miny)
                {
                    miny = B[i].Y + Boffsety;
                    startPolygon = B;
                    startIndex = i;
                }
            }

            // for simplicity we'll define polygon A as the starting polygon
            if (startPolygon == B)
            {
                B = A;
                A = startPolygon;
                Aoffsetx = A.offsetx;
                Aoffsety = A.offsety;
                Boffsetx = B.offsetx;
                Boffsety = B.offsety;
            }


            var c = new Polygon();
            var current = startIndex;
            int? intercept1 = null;
            int? intercept2 = null;

            // scan forward from the starting point
            for (i = 0; i < A.Count + 1; i++)
            {
                current = (current == A.Count) ? 0 : current;
                var next = (current == A.Count - 1) ? 0 : current + 1;
                var touching = false;
                for (j = 0; j < B.Count; j++)
                {
                    var nextj = (j == B.Count - 1) ? 0 : j + 1;
                    if (AlmostEqual(A[current].X + Aoffsetx, B[j].X + Boffsetx) &&
                        AlmostEqual(A[current].Y + Aoffsety, B[j].Y + Boffsety))
                    {
                        c.Add(new Point { X = A[current].X + Aoffsetx, Y = A[current].Y + Aoffsety });
                        intercept1 = j;
                        touching = true;
                        break;
                    }
                    else if (OnSegment(
                        new Point { X = A[current].X + Aoffsetx, Y = A[current].Y + Aoffsety },
                        new Point { X = A[next].X + Aoffsetx, Y = A[next].Y + Aoffsety },
                        new Point { X = B[j].X + Boffsetx, Y = B[j].Y + Boffsety }))
                    {
                        c.Add(new Point { X = A[current].X + Aoffsetx, Y = A[current].Y + Aoffsety });
                        c.Add(new Point { X = B[j].X + Boffsetx, Y = B[j].Y + Boffsety });
                        intercept1 = j;
                        touching = true;
                        break;
                    }
                    else if (OnSegment(
                        new Point { X = B[j].X + Boffsetx, Y = B[j].Y + Boffsety },
                        new Point { X = B[nextj].X + Boffsetx, Y = B[nextj].Y + Boffsety },
                        new Point { X = A[current].X + Aoffsetx, Y = A[current].Y + Aoffsety }))
                    {
                        c.Add(new Point { X = A[current].X + Aoffsetx, Y = A[current].Y + Aoffsety });
                        c.Add(new Point { X = B[nextj].X + Boffsetx, Y = B[nextj].Y + Boffsety });
                        intercept1 = nextj;
                        touching = true;
                        break;
                    }
                }

                if (touching)
                {
                    break;
                }

                c.Add(new Point { X = A[current].X + Aoffsetx, Y = A[current].Y + Aoffsety });

                current++;
            }

            // scan backward from the starting point
            current = startIndex - 1;
            for (i = 0; i < A.Count + 1; i++)
            {
                current = (current < 0) ? A.Count - 1 : current;
                var next = (current == 0) ? A.Count - 1 : current - 1;
                var touching = false;
                for (j = 0; j < B.Count; j++)
                {
                    var nextj = (j == B.Count - 1) ? 0 : j + 1;
                    if (AlmostEqual(A[current].X + Aoffsetx, B[j].X + Boffsetx) &&
                        AlmostEqual(A[current].Y, B[j].Y + Boffsety))
                    {
                        c.Insert(0, new Point { X = A[current].X + Aoffsetx, Y = A[current].Y + Aoffsety });
                        intercept2 = j;
                        touching = true;
                        break;
                    }
                    else if (OnSegment(
                        new Point { X = A[current].X + Aoffsetx, Y = A[current].Y + Aoffsety },
                        new Point { X = A[next].X + Aoffsetx, Y = A[next].Y + Aoffsety },
                        new Point { X = B[j].X + Boffsetx, Y = B[j].Y + Boffsety }))
                    {
                        c.Insert(0, new Point { X = A[current].X + Aoffsetx, Y = A[current].Y + Aoffsety });
                        c.Insert(0, new Point { X = B[j].X + Boffsetx, Y = B[j].Y + Boffsety });
                        intercept2 = j;
                        touching = true;
                        break;
                    }
                    else if (OnSegment(
                        new Point { X = B[j].X + Boffsetx, Y = B[j].Y + Boffsety },
                        new Point { X = B[nextj].X + Boffsetx, Y = B[nextj].Y + Boffsety },
                        new Point { X = A[current].X + Aoffsetx, Y = A[current].Y + Aoffsety }))
                    {
                        c.Insert(0, new Point { X = A[current].X + Aoffsetx, Y = A[current].Y + Aoffsety });
                        intercept2 = j;
                        touching = true;
                        break;
                    }
                }

                if (touching)
                {
                    break;
                }

                c.Insert(0, new Point { X = A[current].X + Aoffsetx, Y = A[current].Y + Aoffsety });

                current--;
            }



            if (intercept1 == null || intercept2 == null)
            {
                // polygons not touching?
                return null;
            }

            // the relevant points on B now lie between intercept1 and intercept2
            current = intercept1.Value + 1;
            for (i = 0; i < B.Count; i++)
            {
                current = (current == B.Count) ? 0 : current;
                c.Add(new Point { X = B[current].X + Boffsetx, Y = B[current].Y + Boffsety });

                if (current == intercept2)
                {
                    break;
                }

                current++;
            }

            // dedupe
            for (i = 0; i < c.Count; i++)
            {
                var next = (i == c.Count - 1) ? 0 : i + 1;
                if (AlmostEqual(c[i].X, c[next].X) && AlmostEqual(c[i].Y, c[next].Y))
                {
                    c.Points.Splice(i, 1);
                    i--;
                }
            }

            return c;
        }


        // placement algos as outlined in [1] http://www.cs.stir.ac.uk/~goc/papers/EffectiveHueristic2DAOR2013.pdf

        // returns a continuous polyline representing the normal-most edge of the given polygon
        // eg. a normal vector of [-1, 0] will return the left-most edge of the polygon
        // this is essentially algo 8 in [1], generalized for any vector direction
        public static List<Point> PolygonEdge(Polygon polygon, Vector normal)
        {
            if (null == polygon || polygon.Count < 3)
            {
                return null;
            }

            normal = NormalizeVector(normal);

            var direction = new Point
            {
                X = -normal.Y,
                Y = normal.X
            };

            // find the max and min points, they will be the endpoints of our edge
            var min = double.MaxValue;
            var max = double.MinValue;

            var dotproduct = new List<double>();

            for (var a = 0; a < polygon.Count; a++)
            {
                var dot = polygon[a].X * direction.X + polygon[a].Y * direction.Y;
                dotproduct.Add(dot);
                if (dot < min)
                {
                    min = dot;
                }

                if (dot > max)
                {
                    max = dot;
                }
            }

            // there may be multiple vertices with min/max values. In which case we choose the one that is normal-most (eg. left most)
            var indexmin = 0;
            var indexmax = 0;

            var normalmin = double.MaxValue;
            var normalmax = double.MinValue;

            for (var b = 0; b < polygon.Count; b++)
            {
                if (AlmostEqual(dotproduct[b], min))
                {
                    var dot = polygon[b].X * normal.X + polygon[b].Y * normal.Y;
                    if (dot > normalmin)
                    {
                        normalmin = dot;
                        indexmin = b;
                    }
                }
                else if (AlmostEqual(dotproduct[b], max))
                {
                    var dot = polygon[b].X * normal.X + polygon[b].Y * normal.Y;
                    if (dot > normalmax)
                    {
                        normalmax = dot;
                        indexmax = b;
                    }
                }
            }

            // now we have two edges bound by min and max points, figure out which edge faces our direction vector

            var indexleft = indexmin - 1;
            var indexright = indexmin + 1;

            if (indexleft < 0)
            {
                indexleft = polygon.Count - 1;
            }

            if (indexright >= polygon.Count)
            {
                indexright = 0;
            }

            var minvertex = polygon[indexmin];
            var left = polygon[indexleft];
            var right = polygon[indexright];

            var leftvector = new Vector
            {
                X = left.X - minvertex.X,
                Y = left.Y - minvertex.Y
            };

            var rightvector = new Vector
            {
                X = right.X - minvertex.X,
                Y = right.Y - minvertex.Y
            };

            var dotleft = leftvector.X * direction.X + leftvector.Y * direction.Y;
            var dotright = rightvector.X * direction.X + rightvector.Y * direction.Y;

            // -1 = left, 1 = right
            var scandirection = -1;

            if (AlmostEqual(dotleft, 0))
            {
                scandirection = 1;
            }
            else if (AlmostEqual(dotright, 0))
            {
                scandirection = -1;
            }
            else
            {
                double normaldotleft = 0;
                double normaldotright = 0;

                if (AlmostEqual(dotleft, dotright))
                {
                    // the points line up exactly along the normal vector
                    normaldotleft = leftvector.X * normal.X + leftvector.Y * normal.Y;
                    normaldotright = rightvector.X * normal.X + rightvector.Y * normal.Y;
                }
                else if (dotleft < dotright)
                {
                    // normalize right vertex so normal projection can be directly compared
                    normaldotleft = leftvector.X * normal.X + leftvector.Y * normal.Y;
                    normaldotright = (rightvector.X * normal.X + rightvector.Y * normal.Y) * (dotleft / dotright);
                }
                else
                {
                    // normalize left vertex so normal projection can be directly compared
                    normaldotleft = leftvector.X * normal.X + leftvector.Y * normal.Y * (dotright / dotleft);
                    normaldotright = rightvector.X * normal.X + rightvector.Y * normal.Y;
                }

                if (normaldotleft > normaldotright)
                {
                    scandirection = -1;
                }
                else
                {
                    // technically they could be equal, (ie. the segments bound by left and right points are incident)
                    // in which case we'll have to climb up the chain until lines are no longer incident
                    // for now we'll just not handle it and assume people aren't giving us garbage input..
                    scandirection = 1;
                }
            }

            // connect all points between indexmin and indexmax along the scan direction
            var edge = new List<Point>();
            var count = 0;
            var i = indexmin;
            while (count < polygon.Count)
            {
                if (i >= polygon.Count)
                {
                    i = 0;
                }
                else if (i < 0)
                {
                    i = polygon.Count - 1;
                }

                edge.Add(polygon[i]);

                if (i == indexmax)
                {
                    break;
                }

                i += scandirection;
                count++;
            }

            return edge;
        }



        // returns the area of the polygon, assuming no self-intersections
        // a negative area indicates counter-clockwise winding direction
        public static double PolygonArea(Polygon polygon)
        {
            double area = 0;
            int i = 0;
            int j = 0;
            for (i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                area += (polygon[j].X + polygon[i].X) *
                        (polygon[j].Y - polygon[i].Y);
            }

            return 0.5 * area;
        }


        // returns the rectangular bounding box of the given polygon
        public static Rect GetPolygonBounds(Polygon polygon)
        {
            if (polygon == null || polygon.Count < 3)
            {
                return null;
            }

            var xmin = polygon[0].X;
            var xmax = polygon[0].X;
            var ymin = polygon[0].Y;
            var ymax = polygon[0].Y;

            for (var i = 1; i < polygon.Count; i++)
            {
                if (polygon[i].X > xmax)
                {
                    xmax = polygon[i].X;
                }
                else if (polygon[i].X < xmin)
                {
                    xmin = polygon[i].X;
                }

                if (polygon[i].Y > ymax)
                {
                    ymax = polygon[i].Y;
                }
                else if (polygon[i].Y < ymin)
                {
                    ymin = polygon[i].Y;
                }
            }

            return new Rect
            {
                X = xmin + polygon.X,
                Y = ymin + polygon.Y,
                Width = xmax - xmin,
                Height = ymax - ymin,
            };
        }


        // returns an interior NFP for the special case where A is a rectangle
        public static List<Polygon> NoFitPolygonRectangle(Polygon A, Polygon B)
        {
            var minAx = A[0].X;
            var minAy = A[0].Y;
            var maxAx = A[0].X;
            var maxAy = A[0].Y;

            for (var i = 1; i < A.Count; i++)
            {
                if (A[i].X < minAx)
                {
                    minAx = A[i].X;
                }

                if (A[i].Y < minAy)
                {
                    minAy = A[i].Y;
                }

                if (A[i].X > maxAx)
                {
                    maxAx = A[i].X;
                }

                if (A[i].Y > maxAy)
                {
                    maxAy = A[i].Y;
                }
            }

            var minBx = B[0].X;
            var minBy = B[0].Y;
            var maxBx = B[0].X;
            var maxBy = B[0].Y;
            for (var i = 1; i < B.Count; i++)
            {
                if (B[i].X < minBx)
                {
                    minBx = B[i].X;
                }

                if (B[i].Y < minBy)
                {
                    minBy = B[i].Y;
                }

                if (B[i].X > maxBx)
                {
                    maxBx = B[i].X;
                }

                if (B[i].Y > maxBy)
                {
                    maxBy = B[i].Y;
                }
            }

            if (maxBx - minBx > maxAx - minAx)
            {
                return null;
            }

            if (maxBy - minBy > maxAy - minAy)
            {
                return null;
            }

            return new List<Polygon>
            {
                new Polygon
                {
                    Points=new List<Point>{
                        new Point {X= minAx - minBx + B[0].X, Y= minAy - minBy + B[0].Y},
                        new Point {X= maxAx - maxBx + B[0].X, Y= minAy - minBy + B[0].Y},
                        new Point {X= maxAx - maxBx + B[0].X, Y= maxAy - maxBy + B[0].Y},
                        new Point {X= minAx - minBx + B[0].X, Y= maxAy - maxBy + B[0].Y}
                    }
                }
            };
        }

        class Touching
        {
            public int Type { get; set; }
            public int A { get; set; }
            public int B { get; set; }
        }

       
        // given a static polygon A and a movable polygon B, compute a no fit polygon by orbiting B about A
        // if the inside flag is set, B is orbited inside of A rather than outside
        // if the searchEdges flag is set, all edges of A are explored for NFPs - multiple 
        public static List<Polygon> NoFitPolygon(Polygon A, Polygon B, bool inside, bool searchEdges)
        {
            if (null == A || A.Count < 3 || null == B || B.Count < 3)
            {
                return null;
            }

            A.offsetx = 0;
            A.offsety = 0;

            int i, j;

            var minA = A[0].Y;
            var minAindex = 0;

            var maxB = B[0].Y;
            var maxBindex = 0;

            for (i = 1; i < A.Count; i++)
            {
                (A[i]).Marked = false;
                if (A[i].Y < minA)
                {
                    minA = A[i].Y;
                    minAindex = i;
                }
            }

            for (i = 1; i < B.Count; i++)
            {
                (B[i]).Marked = false;
                if (B[i].Y > maxB)
                {
                    maxB = B[i].Y;
                    maxBindex = i;
                }
            }

            Point startpoint;
            if (!inside)
            {
                // shift B such that the bottom-most point of B is at the top-most point of A. This guarantees an initial placement with no intersections
                startpoint = new Point
                {
                    X = A[minAindex].X - B[maxBindex].X,
                    Y = A[minAindex].Y - B[maxBindex].Y
                };
            }
            else
            {
                // no reliable heuristic for inside
                startpoint = SearchStartPoint(A, B, true);
            }

            var NFPlist = new List<Polygon>();

            while (startpoint != null)
            {

                B.offsetx = startpoint.X;
                B.offsety = startpoint.Y;

                // maintain a list of touching points/edges
                var touching = new List<Touching>();

                Vector prevvector = null; // keep track of previous vector
                var NFP = new Polygon
                {
                    Points = new List<Point>{
                        new Point
                        {
                            X= B[0].X + B.offsetx,
                            Y= B[0].Y + B.offsety
                        }

                    }
                };

                var referencex = B[0].X + B.offsetx;
                var referencey = B[0].Y + B.offsety;
                var startx = referencex;
                var starty = referencey;
                var counter = 0;

                while (counter < 10 * (A.Count + B.Count))
                {
                    // sanity check, prevent infinite loop
                    touching = new List<Touching>();
                    // find touching vertices/edges
                    for (i = 0; i < A.Count; i++)
                    {
                        var nexti = (i == A.Count - 1) ? 0 : i + 1;
                        for (j = 0; j < B.Count; j++)
                        {
                            var nextj = (j == B.Count - 1) ? 0 : j + 1;
                            if (AlmostEqual(A[i].X, B[j].X + B.offsetx) && AlmostEqual(A[i].Y, B[j].Y + B.offsety))
                            {
                                touching.Add(new Touching { Type = 0, A = i, B = j });
                            }
                            else if (OnSegment(A[i], A[nexti], new Point{X = B[j].X + B.offsetx,Y = B[j].Y + B.offsety}))
                            {
                                touching.Add(new Touching { Type = 1, A = nexti, B = j });
                            }
                            else if (OnSegment(new Point{X =B[j].X + B.offsetx,Y =B[j].Y + B.offsety}, new Point{X =B[nextj].X + B.offsetx,Y =B[nextj].Y + B.offsety}, A[i]))
                            {
                                touching.Add(new Touching { Type = 2, A = i, B = nextj });
                            }
                        }
                    }

                    // generate translation vectors from touching vertices/edges
                    var vectors = new List<Vector>();
                    for (i = 0; i < touching.Count(); i++)
                    {
                        var vertexA = A[touching[i].A];
                        (vertexA).Marked = true;

                        // adjacent A vertices
                        var prevAindex = touching[i].A - 1;
                        var nextAindex = touching[i].A + 1;

                        prevAindex = (prevAindex < 0) ? A.Count - 1 : prevAindex; // loop
                        nextAindex = (nextAindex >= A.Count) ? 0 : nextAindex; // loop

                        var prevA = A[prevAindex];
                        var nextA = A[nextAindex];

                        // adjacent B vertices
                        var vertexB = B[touching[i].B];

                        var prevBindex = touching[i].B - 1;
                        var nextBindex = touching[i].B + 1;

                        prevBindex = (prevBindex < 0) ? B.Count - 1 : prevBindex; // loop
                        nextBindex = (nextBindex >= B.Count) ? 0 : nextBindex; // loop

                        var prevB = B[prevBindex];
                        var nextB = B[nextBindex];

                        if (touching[i].Type == 0)
                        {

                            var vA1 = new Vector
                            {
                                X = prevA.X - vertexA.X,
                                Y = prevA.Y - vertexA.Y,
                                Start = vertexA,
                                End = prevA
                            };

                            var vA2 = new Vector
                            {
                                X = nextA.X - vertexA.X,
                                Y = nextA.Y - vertexA.Y,
                                Start = vertexA,
                                End = nextA
                            };

                            // B vectors need to be inverted
                            var vB1 = new Vector
                            {
                                X = vertexB.X - prevB.X,
                                Y = vertexB.Y - prevB.Y,
                                Start = prevB,
                                End = vertexB
                            };

                            var vB2 = new Vector
                            {
                                X = vertexB.X - nextB.X,
                                Y = vertexB.Y - nextB.Y,
                                Start = nextB,
                                End = vertexB
                            };

                            vectors.Add(vA1);
                            vectors.Add(vA2);
                            vectors.Add(vB1);
                            vectors.Add(vB2);
                        }
                        else if (touching[i].Type == 1)
                        {
                            vectors.Add(new Vector
                            {
                                X = vertexA.X - (vertexB.X + B.offsetx),
                                Y = vertexA.Y - (vertexB.Y + B.offsety),
                                Start = prevA,
                                End = vertexA
                            });

                            vectors.Add(new Vector
                            {
                                X = prevA.X - (vertexB.X + B.offsetx),
                                Y = prevA.Y - (vertexB.Y + B.offsety),
                                Start = vertexA,
                                End = prevA
                            });
                        }
                        else if (touching[i].Type == 2)
                        {
                            vectors.Add(new Vector
                            {
                                X = vertexA.X - (vertexB.X + B.offsetx),
                                Y = vertexA.Y - (vertexB.Y + B.offsety),
                                Start = prevB,
                                End = vertexB
                            });

                            vectors.Add(new Vector
                            {
                                X = vertexA.X - (prevB.X + B.offsetx),
                                Y = vertexA.Y - (prevB.Y + B.offsety),
                                Start = vertexB,
                                End = prevB
                            });
                        }
                    }

                    // todo: there should be a faster way to reject vectors that will cause immediate intersection. For now just check them all

                    Vector translate = null;
                    double? maxd = 0;
                    for (i = 0; i < vectors.Count(); i++){
                        if (vectors[i].X == 0 && vectors[i].Y == 0){
                            continue;
                        }

                        // if this vector points us back to where we came from, ignore it.
                        // ie cross product = 0, dot product < 0
                        if (prevvector != null && vectors[i].Y * prevvector.Y + vectors[i].X * prevvector.X < 0)
                        {

                            // compare magnitude with unit vectors
                            var vectorlength = Math.Sqrt(vectors[i].X * vectors[i].X + vectors[i].Y * vectors[i].Y);
                            var unitv = new Vector { X = vectors[i].X / vectorlength, Y = vectors[i].Y / vectorlength };

                            var prevlength = Math.Sqrt(prevvector.X * prevvector.X + prevvector.Y * prevvector.Y);
                            var prevunit = new Vector { X = prevvector.X / prevlength, Y = prevvector.Y / prevlength };

                            // we need to scale down to unit vectors to normalize vector Count(). Could also just do a tan here
                            if (Math.Abs(unitv.Y * prevunit.X - unitv.X * prevunit.Y) < 0.0001)
                            {
                                continue;
                            }
                        }

                        var d = PolygonSlideDistance(A, B, vectors[i], true);
                        var vecd2 = vectors[i].X * vectors[i].X + vectors[i].Y * vectors[i].Y;

                        if (d == null || d * d > vecd2)
                        {
                            var vecd = Math.Sqrt(vectors[i].X * vectors[i].X + vectors[i].Y * vectors[i].Y);
                            d = vecd;
                        }

                        if (d != null && d > maxd)
                        {
                            maxd = d;
                            translate = vectors[i];
                        }
                    }


                    if (translate == null || AlmostEqual(maxd.Value, 0))
                    {
                        // didn't close the loop, something went wrong here
                        NFP = null;
                        break;
                    }

                    (translate.Start).Marked = true;
                    (translate.End).Marked = true;

                    prevvector = translate;

                    // trim
                    var vlength2 = translate.X * translate.X + translate.Y * translate.Y;
                    if (maxd * maxd < vlength2 && !AlmostEqual(maxd.Value * maxd.Value, vlength2))
                    {
                        var scale = Math.Sqrt((maxd.Value * maxd.Value) / vlength2);
                        translate.X *= scale;
                        translate.Y *= scale;
                    }

                    referencex += translate.X;
                    referencey += translate.Y;

                    if (AlmostEqual(referencex, startx) && AlmostEqual(referencey, starty))
                    {
                        // we've made a full loop
                        break;
                    }

                    // if A and B Start on a touching horizontal line, the End point may not be the Start point
                    var looped = false;
                    if (NFP.Count > 0)
                    {
                        for (i = 0; i < NFP.Count - 1; i++)
                        {
                            if (AlmostEqual(referencex, NFP[i].X) && AlmostEqual(referencey, NFP[i].Y))
                            {
                                looped = true;
                            }
                        }
                    }

                    if (looped)
                    {
                        // we've made a full loop
                        break;
                    }

                    NFP.Add(new Point
                    {
                        X = referencex,
                        Y = referencey
                    });

                    B.offsetx += translate.X;
                    B.offsety += translate.Y;

                    counter++;
                }

                if (NFP != null && NFP.Count > 0)
                {
                    NFPlist.Add(NFP);
                }

                if (!searchEdges)
                {
                    // only get outer NFP or first inner NFP
                    break;
                }

                startpoint = SearchStartPoint(A, B, inside, NFPlist);

            }

            return NFPlist;
        }


        public static Polygon RotatePolygon(Polygon polygon, double angle,bool deep=false)
        {
            var rotated = new Polygon();
            angle = angle * Math.PI / 180;
            for (var i = 0; i < polygon.Count; i++)
            {
                var x = polygon[i].X;
                var y = polygon[i].Y;
                var x1 = x * Math.Cos(angle) - y * Math.Sin(angle);
                var y1 = x * Math.Sin(angle) + y * Math.Cos(angle);

                rotated.Add(new Point { X = x1, Y = y1 });
            }

            // reset bounding box
            var bounds = GetPolygonBounds(rotated);
            rotated.X = bounds.X;
            rotated.Y = bounds.Y;
            rotated.Width = bounds.Width;
            rotated.Height = bounds.Height;
            if(deep){
	            foreach(var child in polygon.Children){
	            	rotated.Children.Add(RotatePolygon(child,angle,deep));
	            }
	        }

            return rotated;
        }

        // searches for an arrangement of A and B such that they do not overlap
        // if an NFP is given, only search for startpoints that have not already been traversed in the given NFP
        public static Point SearchStartPoint(
            Polygon a, Polygon b, bool inside, List<Polygon> NFP = null)
        {
            // clone arrays
            var A = new Polygon {Points = a.Points};
            var B = new Polygon {Points = b.Points};

            // close the loop for polygons
            if (A[0] != A[A.Count - 1])
            {
                A.Add(A[0]);
            }

            if (B[0] != B[B.Count - 1])
            {
                B.Add(B[0]);
            }

            for (var i = 0; i < A.Count - 1; i++)
            {
                if (!(A[i]).Marked)
                {
                    (A[i]).Marked = true;
                    for (var j = 0; j < B.Count; j++)
                    {
                        B.offsetx = A[i].X - B[j].X;
                        B.offsety = A[i].Y - B[j].Y;

                        bool? Binside = null;
                        for (var k = 0; k < B.Count; k++)
                        {
                            var inpoly = PointInPolygon(new Point { X = B[k].X + B.offsetx, Y = B[k].Y + B.offsety },A);
                            if (inpoly != null)
                            {
                                Binside = inpoly;
                                break;
                            }
                        }

                        if (Binside == null)
                        {
                            // A and B are the same
                            return null;
                        }

                        var startPoint = new Point { X = B.offsetx, Y = B.offsety };
                        if (((Binside.Value && inside) || (!Binside.Value && !inside)) && !Intersect(A, B) &&
                            !InNfp(startPoint, NFP))
                        {
                            return startPoint;
                        }

                        // slide B along vector
                        var vx = A[i + 1].X - A[i].X;
                        var vy = A[i + 1].Y - A[i].Y;

                        var d1 = PolygonProjectionDistance(A, B, new Vector { X = vx, Y = vy });
                        var d2 = PolygonProjectionDistance(B, A, new Vector { X = -vx, Y = -vy });

                        double? d = null;

                        // todo: clean this up
                        if (d1 == null && d2 == null)
                        {
                            // nothin
                        }
                        else if (d1 == null)
                        {
                            d = d2;
                        }
                        else if (d2 == null)
                        {
                            d = d1;
                        }
                        else
                        {
                            d = Math.Min(d1.Value, d2.Value);
                        }

                        // only slide until no longer negative
                        // todo: clean this up
                        if (d != null && !AlmostEqual(d.Value, 0) && d > 0)
                        {

                        }
                        else
                        {
                            continue;
                        }

                        var vd2 = vx * vx + vy * vy;

                        if (d * d < vd2 && !AlmostEqual(d.Value * d.Value, vd2))
                        {
                            var vd = Math.Sqrt(vx * vx + vy * vy);
                            vx *= d.Value / vd;
                            vy *= d.Value / vd;
                        }

                        B.offsetx += vx;
                        B.offsety += vy;

                        for (var k = 0; k < B.Count; k++)
                        {
                            var inpoly = PointInPolygon(new Point { X = B[k].X + B.offsetx, Y = B[k].Y + B.offsety },
                                A);
                            if (inpoly != null)
                            {
                                Binside = inpoly;
                                break;
                            }
                        }

                        startPoint = new Point { X = B.offsetx, Y = B.offsety };
                        if (((Binside != null && Binside.Value && inside) || (Binside == null && !inside)) && !Intersect(A, B) &&
                            !InNfp(startPoint, NFP))
                        {
                            return startPoint;
                        }
                    }
                }
            }



            return null;
        }

        public static bool IsRectangle(Polygon poly, double? tolerance = null)
        {
            var bb = GetPolygonBounds(poly);
            tolerance = tolerance ?? _tol;

            for (var i = 0; i < poly.Count; i++)
            {
                if (!AlmostEqual(poly[i].X, bb.X) && !AlmostEqual(poly[i].X, bb.X + bb.Width))
                {
                    return false;
                }

                if (!AlmostEqual(poly[i].Y, bb.Y) && !AlmostEqual(poly[i].Y, bb.Y + bb.Height))
                {
                    return false;
                }
            }

            return true;
        }
    }
}