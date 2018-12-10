using System;
using System.Collections.Generic;
using Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GeometryTest
{
    [TestClass]
    public class GeometryUtilTest
    {
        [TestMethod]
        public void AlmostEqualTrue()
        {
            
            Assert.IsTrue(GeometryUtil.AlmostEqual(3, 4, 2));
        }

        [TestMethod]
        public void AlmostEqualFalse()
        {
            
            Assert.IsFalse(GeometryUtil.AlmostEqual(3, 4, 1));
        }

        [TestMethod]
        public void DegreesToRadians()
        {
            
            var res = GeometryUtil.DegreesToRadians(180);
            Assert.IsTrue(GeometryUtil.AlmostEqual(res, 3.14, 0.01));
        }

        [TestMethod]
        public void RadiansToDegrees()
        {
            
            var res = GeometryUtil.RadiansToDegrees(Math.PI);
            Assert.IsTrue(GeometryUtil.AlmostEqual(res, 180, 0.1));
        }

        [TestMethod]
        public void PolygonAreaConvexClockwise()
        {
            
            var poly = new Polygon();
            poly.Add(new Point { X = 0, Y = 0 });
            poly.Add(new Point { X = 2, Y = 0 });
            poly.Add(new Point { X = 2, Y = 2 });
            poly.Add(new Point { X = 1, Y = 2 });
            poly.Add(new Point { X = 1, Y = 1 });
            poly.Add(new Point { X = 0, Y = 1 });
            var res = GeometryUtil.PolygonArea(poly);
            Assert.IsTrue(GeometryUtil.AlmostEqual(res, -3, 0.1));
        }

        [TestMethod]
        public void PolygonAreaConvexCounterClockwise()
        {
            
            var poly = new Polygon();
            poly.Add(new Point { X = 0, Y = 1 });
            poly.Add(new Point { X = 1, Y = 1 });
            poly.Add(new Point { X = 1, Y = 2 });
            poly.Add(new Point { X = 2, Y = 2 });
            poly.Add(new Point { X = 2, Y = 0 });
            poly.Add(new Point { X = 0, Y = 0 });
            var res = GeometryUtil.PolygonArea(poly);
            Assert.IsTrue(GeometryUtil.AlmostEqual(res, 3, 0.1));
        }


        [TestMethod]
        public void GetPolygonBounds()
        {
            
            var poly = new Polygon();
            poly.X = 10;
            poly.Y = 5;
            poly.Add(new Point { X = 0, Y = 1 });
            poly.Add(new Point { X = 1, Y = 1 });
            poly.Add(new Point { X = 1, Y = 2 });
            poly.Add(new Point { X = 2, Y = 2 });
            poly.Add(new Point { X = 2, Y = 0 });
            poly.Add(new Point { X = 0, Y = 0 });
            var res = GeometryUtil.GetPolygonBounds(poly);
            Assert.IsTrue(GeometryUtil.AlmostEqual(res.Width, 2, 0.1));
            Assert.IsTrue(GeometryUtil.AlmostEqual(res.Height, 2, 0.1));
            Assert.IsTrue(GeometryUtil.AlmostEqual(res.X, 10, 0.1));
            Assert.IsTrue(GeometryUtil.AlmostEqual(res.Y, 5, 0.1));
        }

        [TestMethod]
        public void IntersectDisjointOffset()
        {
            
            var a = new Polygon();
            a.Add(new Point { X = 0, Y = 2 });
            a.Add(new Point { X = 2, Y = 2 });
            a.Add(new Point { X = 2, Y = 0 });
            a.Add(new Point { X = 0, Y = 0 });

            var b = new Polygon();
            b.X = 3;
            b.Add(new Point { X = 0, Y = 2 });
            b.Add(new Point { X = 2, Y = 2 });
            b.Add(new Point { X = 2, Y = 0 });
            b.Add(new Point { X = 0, Y = 1 });

            Assert.IsFalse(GeometryUtil.Intersect(a, b));
        }

        [TestMethod]
        public void IntersectDisjoint()
        {
            
            var a = new Polygon();
            a.Add(new Point { X = 0, Y = 2 });
            a.Add(new Point { X = 2, Y = 2 });
            a.Add(new Point { X = 2, Y = 0 });
            a.Add(new Point { X = 0, Y = 0 });

            var b = new Polygon();
            b.Add(new Point { X = 0 + 3, Y = 2 });
            b.Add(new Point { X = 2 + 3, Y = 2 });
            b.Add(new Point { X = 2 + 3, Y = 0 });
            b.Add(new Point { X = 0 + 3, Y = 1 });

            Assert.IsFalse(GeometryUtil.Intersect(a, b));
        }

        [TestMethod]
        public void IntersectJointOffset()
        {
            
            var a = new Polygon();
            a.Add(new Point { X = 0, Y = 2 });
            a.Add(new Point { X = 2, Y = 2 });
            a.Add(new Point { X = 2, Y = 0 });
            a.Add(new Point { X = 0, Y = 0 });

            var b = new Polygon();
            b.X = 1;
            b.Add(new Point { X = 0, Y = 2 });
            b.Add(new Point { X = 2, Y = 2 });
            b.Add(new Point { X = 2, Y = 0 });
            b.Add(new Point { X = 0, Y = 1 });

            Assert.IsFalse(GeometryUtil.Intersect(a, b));
        }


        [TestMethod]
        public void IntersectJoint()
        {
            
            var a = new Polygon();
            a.Add(new Point { X = 0, Y = 2 });
            a.Add(new Point { X = 2, Y = 2 });
            a.Add(new Point { X = 2, Y = 0 });
            a.Add(new Point { X = 0, Y = 0 });

            var b = new Polygon();
            b.Add(new Point { X = 0 + 1, Y = 2 });
            b.Add(new Point { X = 2 + 1, Y = 2 });
            b.Add(new Point { X = 2 + 1, Y = 0 });
            b.Add(new Point { X = 0 + 1, Y = 1 });

            Assert.IsTrue(GeometryUtil.Intersect(a, b));
        }

        [TestMethod]
        public void OnSegment()
        {
            
            var a = new Point { X = 0, Y = 0 };
            var b = new Point { X = 2, Y = 2 };
            var c = new Point { X = 1, Y = 1 };
            Assert.IsTrue(GeometryUtil.OnSegment(a, b, c));
        }

        [TestMethod]
        public void NotOnSegment()
        {
            
            var a = new Point { X = 0, Y = 0 };
            var b = new Point { X = 2, Y = 2 };
            var c = new Point { X = 2, Y = 1 };
            Assert.IsFalse(GeometryUtil.OnSegment(a, b, c));
        }

        [TestMethod]
        public void LineIntersecting()
        {
            
            var a = new Point { X = 0, Y = 0 };
            var b = new Point { X = 2, Y = 2 };
            var c = new Point { X = 0, Y = 2 };
            var d = new Point { X = 2, Y = 0 };
            var res = GeometryUtil.LineIntersect(a, b, c, d);
            Assert.IsNotNull(res);
            Assert.AreEqual(1, res.X);
            Assert.AreEqual(1, res.Y);
        }


        [TestMethod]
        public void LineNotIntersecting()
        {
            
            var a = new Point { X = 0, Y = 0 };
            var b = new Point { X = 0, Y = 2 };
            var c = new Point { X = 2, Y = 0 };
            var d = new Point { X = 2, Y = 2 };
            var res = GeometryUtil.LineIntersect(a, b, c, d);
            Assert.IsNull(res);
        }

        [TestMethod]
        public void LineIntersectingInfinte()
        {
            
            var a = new Point { X = 0, Y = 0 };
            var b = new Point { X = 2, Y = 1 };
            var c = new Point { X = 0, Y = 3 };
            var d = new Point { X = 2, Y = 2 };
            var res = GeometryUtil.LineIntersect(a, b, c, d, true);
            Assert.IsNotNull(res);
            Assert.AreEqual(3, res.X);
            Assert.AreEqual(1.5, res.Y);
        }

        [TestMethod]
        public void PointInPolygonOutside()
        {
            
            var poly = new Polygon();
            poly.Add(new Point { X = 0, Y = 1 });
            poly.Add(new Point { X = 1, Y = 1 });
            poly.Add(new Point { X = 1, Y = 2 });
            poly.Add(new Point { X = 2, Y = 2 });
            poly.Add(new Point { X = 2, Y = 0 });
            poly.Add(new Point { X = 0, Y = 0 });
            var res = GeometryUtil.PointInPolygon(new Point { X = 0.5, Y = 1.5 }, poly);
            Assert.IsNotNull(res);
            Assert.AreEqual(false, res.Value);
        }


        [TestMethod]
        public void PointInPolygonInside()
        {
            
            var poly = new Polygon();
            poly.Add(new Point { X = 0, Y = 1 });
            poly.Add(new Point { X = 1, Y = 1 });
            poly.Add(new Point { X = 1, Y = 2 });
            poly.Add(new Point { X = 2, Y = 2 });
            poly.Add(new Point { X = 2, Y = 0 });
            poly.Add(new Point { X = 0, Y = 0 });
            var res = GeometryUtil.PointInPolygon(new Point { X = 0.5, Y = 0.5 }, poly);
            Assert.IsNotNull(res);
            Assert.AreEqual(true, res.Value);
        }



        [TestMethod]
        public void PolygonHull()
        {
            
            var a = new Polygon();
            a.Add(new Point { X = 0, Y =0 });
            a.Add(new Point { X = 2, Y = 0 });
            a.Add(new Point { X = 2, Y = 2 });
            a.Add(new Point { X = 0, Y = 2 });

            var b = new Polygon();
            b.Add(new Point { X = 0+2, Y = 0 });
            b.Add(new Point { X = 2 + 2, Y = 0 });
            b.Add(new Point { X = 2 + 2, Y = 2 });
            b.Add(new Point { X = 0 + 2, Y = 2 });

            var res = GeometryUtil.PolygonHull(a, b);
            Assert.AreEqual("{(2,2),(0,2),(0,0),(2,0),(4,0),(4,2)}",res.ToString());
        }

        [TestMethod]
        public void SegmentDistance()
        {
            var res = GeometryUtil.SegmentDistance(

                new Point { X = 0, Y = 0 }, new Point { X = 1, Y = 0 },
                new Point { X = 0, Y = 1 }, new Point { X = 1, Y = 1 },
                new Vector { X = 0, Y = -1 }
            );
            Assert.AreEqual(res, 1);
        }

        [TestMethod]
        public void PolygonSlideDistance()
        {
        	var A = new Polygon();
            A.Points = new List<Point>
            {
                new Point {X = 0, Y = 0}, new Point {X = 1, Y = 0}, new Point {X = 1, Y = 1}, new Point {X = 0, Y = 1}
            };
        	var B = new Polygon();
            B.Points = new List<Point>
            {
                new Point {X = 0, Y = 2}, new Point {X = 1, Y = 2}, new Point {X = 1, Y = 3}, new Point {X = 0, Y = 3}
            };
            var res = GeometryUtil.PolygonSlideDistance(
				A,B,
                new Vector { X = 0, Y = -1 },
                true
            );
            Assert.AreEqual(res, 1);
        }
        
        
        [TestMethod]
        public void NoFitPolygon1()
        {
        	var inside = true;
			var searchEdges=true;
        	var A = new Polygon();
            A.Points = new List<Point>
            {
                new Point {X = 0, Y = 0}, new Point {X = 1, Y = 0}, new Point {X = 1, Y = 1}, new Point {X = 0, Y = 1}
            };
        	var B = new Polygon();
            B.Points = new List<Point>
            {
                new Point {X = 0, Y = 2}, new Point {X = 1, Y = 2}, new Point {X = 1, Y = 3}, new Point {X = 0, Y = 3}
            };
            
            // given a static polygon A and a movable polygon B, compute a no fit polygon by orbiting B about A
			// if the inside flag is set, B is orbited inside of A rather than outside
			// if the searchEdges flag is set, all edges of A are explored for NFPs - multiple 
            var res = GeometryUtil.NoFitPolygon(
				A,B,
                inside,
                searchEdges
            );
            Assert.AreEqual(res.Count, 0);
        }


        [TestMethod]
        public void NoFitPolygon2()
        {
            var inside = true;
            var searchEdges = true;
            var A = new Polygon();
            A.Points = new List<Point>
            {
                new Point {X = 0, Y = 0}, new Point {X = 0.5, Y = 0}, new Point {X = 0.5, Y = 0.5}, new Point {X = 0, Y = 0.5}
            };
            var B = new Polygon();
            B.Points = new List<Point>
            {
                new Point {X = 0, Y = 2}, new Point {X = 1, Y = 2}, new Point {X = 1, Y = 3}, new Point {X = 0, Y = 3}
            };

            // given a static polygon A and a movable polygon B, compute a no fit polygon by orbiting B about A
            // if the inside flag is set, B is orbited inside of A rather than outside
            // if the searchEdges flag is set, all edges of A are explored for NFPs - multiple 
            var res = GeometryUtil.NoFitPolygon(
                A, B,
                inside,
                searchEdges
            );
            Assert.AreEqual(res.Count, 0);
        }

        [TestMethod]
        public void NoFitPolygon3()
        {
            var inside = true;
            var searchEdges = true;
            var A = new Polygon();
            A.Points = new List<Point>
            {
                new Point {X = 0, Y = 0}, new Point {X = 0.5, Y = 0}, new Point {X = 0.5, Y = 0.5}, new Point {X = 0, Y = 0.5}
            };
            var B = new Polygon();
            B.Points = new List<Point>
            {
                new Point {X = 0, Y = 2}, new Point {X = 1, Y = 2}, new Point {X = 1, Y = 3}, new Point {X = 0, Y = 3}
            };

            // given a static polygon A and a movable polygon B, compute a no fit polygon by orbiting B about A
            // if the inside flag is set, B is orbited inside of A rather than outside
            // if the searchEdges flag is set, all edges of A are explored for NFPs - multiple 
            var res = GeometryUtil.NoFitPolygon(
                B,A,
                inside,
                searchEdges
            );
            Assert.AreEqual(res.Count,1);
            Assert.AreEqual(res[0].Count, 4);
        }

        [TestMethod]
        public void SearchStartPoint1()
        {
            var inside = true;
            //var searchEdges = true;
            var A = new Polygon();
            A.Points = new List<Point>
            {
                new Point {X = 0, Y = 0}, new Point {X = 0.5, Y = 0}, new Point {X = 0.5, Y = 0.5}, new Point {X = 0, Y = 0.5}
            };
            var B = new Polygon();
            B.Points = new List<Point>
            {
                new Point {X = 0, Y = 2}, new Point {X = 1, Y = 2}, new Point {X = 1, Y = 3}, new Point {X = 0, Y = 3}
            };

            var res = GeometryUtil.SearchStartPoint(
                A,B,
                inside,
                null
            );
            Assert.IsNull(res);
        }
        
        [TestMethod]
        public void SearchStartPoint2()
        {
            var inside = true;
            //var searchEdges = true;
            var A = new Polygon();
            A.Points = new List<Point>
            {
                new Point {X = 0, Y = 0}, new Point {X = 0.5, Y = 0}, new Point {X = 0.5, Y = 0.5}, new Point {X = 0, Y = 0.5}
            };
            var B = new Polygon();
            B.Points = new List<Point>
            {
                new Point {X = 0, Y = 2}, new Point {X = 1, Y = 2}, new Point {X = 1, Y = 3}, new Point {X = 0, Y = 3}
            };

            var res = GeometryUtil.SearchStartPoint(
                B,A,
                inside,
                null
            );
            Assert.IsNotNull(res);
            Assert.AreEqual(0,res.X);
            Assert.AreEqual(2, res.Y);
        }
    }
}
