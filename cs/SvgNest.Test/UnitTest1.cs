using System.Collections.Generic;
using System.Linq;
using Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace SvgNestTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var target = new SvgNest.SvgNest();
            var data = new List<Polygon>
            {
                new Polygon
                {
                    Points = new List<Point>
                        {
                            new Point(200+1, 100+1),
                            new Point(0+1, 100+1),
                            new Point(0+1, 0+1),
                            new Point(200+1, 0+1)}
                },
                new Polygon
                {
                    Points = new List<Point>
                    {
                        new Point(100+250, 50+10),
                        new Point(0 + 250, 50+10),
                        new Point(0 + 250, 0+10),
                        new Point(100 + 250, 0+10)
                    }
                },
                new Polygon
                {
                    Points = new List<Point>
                    {
                        new Point(50+250, 25+75),
                        new Point(0 + 250, 25+75),
                        new Point(0 + 250, +75),
                        new Point(50 + 250, 0+75)
                    }
                }
            };
            target.parsesvg(data);
            target.setbin(data[0]);
            var result = target.start((a) => { }, (a, b, c) => { }).ToList();
            Assert.IsNotNull(result);

        }
    }
}
