using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Svg;
using SvgNest;


namespace SvgNestTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var expectedJsonResult =
                "[[{\"Parent\":null,\"Children\":[{\"Parent\":null,\"Children\":null,\"Hole\":false,\"Rotation\":0.0,\"Source\":0,\"offsetx\":0.0,\"offsety\":0.0,\"Id\":0,\"Points\":[{\"Marked\":false,\"X\":0.0,\"Y\":50.5},{\"Marked\":false,\"X\":0.0,\"Y\":0.5},{\"Marked\":false,\"X\":0.5,\"Y\":0.0},{\"Marked\":false,\"X\":100.5,\"Y\":0.0},{\"Marked\":false,\"X\":101.0,\"Y\":0.5},{\"Marked\":false,\"X\":101.0,\"Y\":50.5},{\"Marked\":false,\"X\":100.5,\"Y\":51.0},{\"Marked\":false,\"X\":0.5,\"Y\":51.0}],\"X\":0.0,\"Y\":0.0,\"Height\":0.0,\"Width\":0.0,\"Count\":8},{\"Parent\":null,\"Children\":null,\"Hole\":false,\"Rotation\":0.0,\"Source\":0,\"offsetx\":0.0,\"offsety\":0.0,\"Id\":0,\"Points\":[{\"Marked\":false,\"X\":126.0,\"Y\":51.0},{\"Marked\":false,\"X\":101.0,\"Y\":51.0},{\"Marked\":false,\"X\":100.5,\"Y\":50.5},{\"Marked\":false,\"X\":100.5,\"Y\":0.5},{\"Marked\":false,\"X\":101.0,\"Y\":0.0},{\"Marked\":false,\"X\":126.0,\"Y\":0.0},{\"Marked\":false,\"X\":126.5,\"Y\":0.5},{\"Marked\":false,\"X\":126.5,\"Y\":50.5}],\"X\":0.0,\"Y\":0.0,\"Height\":0.0,\"Width\":0.0,\"Count\":8}],\"Hole\":false,\"Rotation\":0.0,\"Source\":0,\"offsetx\":0.0,\"offsety\":0.0,\"Id\":0,\"Points\":[{\"Marked\":false,\"X\":201.0,\"Y\":101.0},{\"Marked\":false,\"X\":1.0,\"Y\":101.0},{\"Marked\":false,\"X\":1.0,\"Y\":1.0},{\"Marked\":false,\"X\":201.0,\"Y\":1.0}],\"X\":0.0,\"Y\":0.0,\"Height\":0.0,\"Width\":0.0,\"Count\":4}]]";

            var random = new FakeNestRandom();
            var target = new SvgNest.SvgNest(new Commons(random), random);
            var bin = new Polygon
            {
                Points = new List<Point>
                {
                    new Point(200 + 1, 100 + 1),
                    new Point(0 + 1, 100 + 1),
                    new Point(0 + 1, 0 + 1),
                    new Point(200 + 1, 0 + 1)
                }
            };
            var data = new List<Polygon>
            {
                new Polygon
                {
                    Points = new List<Point>
                    {
                        new Point(50+250, 25+75),
                        new Point(0 + 250, 25+75),
                        new Point(0 + 250, +75),
                        new Point(50 + 250, 0+75)
                    }
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
                }
            };
            //target.parsesvg(data);
            //target.setbin(data[0]);
            target.Initialize(bin, data, (a) => { }, (a, b, c) => { });
            var result = target.Start().ToList();
            var jsonResult = JsonConvert.SerializeObject(result);
            Assert.AreEqual(jsonResult, expectedJsonResult);

        }


        [TestMethod]
        public void TestMethod2()
        {
            var expectedJsonResult =
                "[[{\"Parent\":null,\"Children\":[{\"Parent\":null,\"Children\":null,\"Hole\":false,\"Rotation\":0.0,\"Source\":0,\"offsetx\":0.0,\"offsety\":0.0,\"Id\":0,\"Points\":[{\"Marked\":false,\"X\":0.0,\"Y\":50.5},{\"Marked\":false,\"X\":0.0,\"Y\":0.5},{\"Marked\":false,\"X\":0.5,\"Y\":0.0},{\"Marked\":false,\"X\":100.5,\"Y\":0.0},{\"Marked\":false,\"X\":101.0,\"Y\":0.5},{\"Marked\":false,\"X\":101.0,\"Y\":50.5},{\"Marked\":false,\"X\":100.5,\"Y\":51.0},{\"Marked\":false,\"X\":0.5,\"Y\":51.0}],\"X\":0.0,\"Y\":0.0,\"Height\":0.0,\"Width\":0.0,\"Count\":8},{\"Parent\":null,\"Children\":null,\"Hole\":false,\"Rotation\":0.0,\"Source\":0,\"offsetx\":0.0,\"offsety\":0.0,\"Id\":0,\"Points\":[{\"Marked\":false,\"X\":126.0,\"Y\":51.0},{\"Marked\":false,\"X\":101.0,\"Y\":51.0},{\"Marked\":false,\"X\":100.5,\"Y\":50.5},{\"Marked\":false,\"X\":100.5,\"Y\":0.5},{\"Marked\":false,\"X\":101.0,\"Y\":0.0},{\"Marked\":false,\"X\":126.0,\"Y\":0.0},{\"Marked\":false,\"X\":126.5,\"Y\":0.5},{\"Marked\":false,\"X\":126.5,\"Y\":50.5}],\"X\":0.0,\"Y\":0.0,\"Height\":0.0,\"Width\":0.0,\"Count\":8}],\"Hole\":false,\"Rotation\":0.0,\"Source\":0,\"offsetx\":0.0,\"offsety\":0.0,\"Id\":0,\"Points\":[{\"Marked\":false,\"X\":201.0,\"Y\":101.0},{\"Marked\":false,\"X\":1.0,\"Y\":101.0},{\"Marked\":false,\"X\":1.0,\"Y\":1.0},{\"Marked\":false,\"X\":201.0,\"Y\":1.0}],\"X\":0.0,\"Y\":0.0,\"Height\":0.0,\"Width\":0.0,\"Count\":4}]]";
            var random = new FakeNestRandom();
            var target = new SvgNest.SvgNest(new Commons(random), random);
            var bin = new Polygon
            {
                Points = new List<Point>
                {
                    new Point(200 + 1, 100 + 1),
                    new Point(0 + 1, 100 + 1),
                    new Point(0 + 1, 0 + 1),
                    new Point(200 + 1, 0 + 1)
                }
            };
            var data = new List<Polygon>
            {
                new Polygon
                {
                    Points = new List<Point>
                    {
                        new Point(50, 25),
                        new Point(0 , 25),
                        new Point(0 , 0),
                        new Point(50 ,0 )
                    }
                },
                new Polygon
                {
                    Points = new List<Point>
                    {
                        new Point(100, 50),
                        new Point(0 , 50),
                        new Point(0, 0),
                        new Point(100 , 0)
                    }
                }
            };
            //target.parsesvg(data);
            //target.setbin(data[0]);
            target.Initialize(bin, data, (a) => { }, (a, b, c) => { });
            var result = target.Start().ToList();
            var jsonResult = JsonConvert.SerializeObject(result);
            Assert.AreEqual(jsonResult, expectedJsonResult);

        }

        /*[TestMethod]
        public void shouldParseSvg()
        {
            string svgData = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""no""?>
<!DOCTYPE svg PUBLIC ""-//W3C//DTD SVG 1.1//EN"" ""http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd"">
<svg version=""1.1"" xmlns=""http://www.w3.org/2000/svg"" xmlns:xlink=""http://www.w3.org/1999/xlink"" preserveAspectRatio=""xMidYMid meet"" viewBox=""0 0 640 640"" width=""640"" height=""640"">
	
		<path d=""M1 1L200 1L200 100L1 100L1 1Z"" fill=""none"" stroke=""#010101""></path>
<!--<polygon fill=""none"" stroke=""#010101"" stroke-miterlimit=""10"" points=""684.045,443.734 688.396,447.215 
	666.488,450.935 666.488,432.651 ""/>-->

		<path d=""M250 75L300 75L300 100L250 100L250 75Z"" fill=""none"" stroke=""#010101""></path>
		<path d=""M250 10L350 10L350 60L250 60L250 10Z"" fill=""none"" stroke=""#010101""></path>
</svg>";
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(svgData));
            var svg = SvgDocument.Open<SvgDocument>(ms) as SvgElement;
            var target = new SvgNest.SvgNest();
            var polys = target.ToPolygons(svg.Children);
            target.parsesvg(polys);
            target.setbin(polys[0]);
            var result = target.start((a) => { }, (a, b, c) => { }).ToList();

            var doc = target.ToSvg(result, svg);
            
            
            Assert.IsNotNull(result);
        }*/
    }
}
