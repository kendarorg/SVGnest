using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Geometry
{
    public class Point
    {
        public bool Marked { get; set; }
        public Point()
        {
            
        }
        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }
        public double X { get; set; }
        public double Y { get; set; }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "({0},{1})", X, Y);
        }
    }

    public class Rect
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Height { get; set; }
        public double Width { get; set; }

        public Polygon ToPolygon()
        {
            return new Polygon
            {
                Points = new List<Point>{new Point {X = X, Y = Y},
                new Point {X = X, Y = Y + Height},
                new Point {X = X + Width, Y = Y + Height},
                new Point {X = X + Width, Y = Y}
                    }
            };
        }
    }


    public class Vector
    {
        public double X { get; set; }
        public double Y { get; set; }
        public Point Start { get; set; }
        public Point End { get; set; }
    }

    public class QuadraticBezier
    {
        public Point P1 { get; set; }
        public Point P2 { get; set; }
        public Point C1 { get; set; }
    }

    public class CubicBezier : QuadraticBezier
    {
        public Point C2 { get; set; }
    }

    public class Arc
    {
        public Point Center { get; set; }
        public Point P1 { get; set; }
        public Point P2 { get; set; }
        public double Rx { get; set; }
        public double Ry { get; set; }
        public double Theta { get; set; }
        public double Extent { get; set; }
        public double Angle { get; set; }
        public bool LargeArc { get; set; }
        public bool Sweep { get; set; }
    }

    public class Polygon
    {
        public Polygon Parent;
        public List<Polygon> Children;
        public bool Hole;
        public double Rotation;
        public int Source;
        public double offsetx;
        public double offsety;


        public Polygon()
        {
            //Children = new List<Polygon>();
            Points = new List<Point>();
        }
        public int Id { get; set; }
        public List<Point> Points { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        /*public double X { get; set; }
        public double Y { get; set; }*/
        public double Height { get; set; }
        public double Width { get; set; }

        public void Reverse()
        {
            Points.Reverse();
        }
        public override string ToString()
        {
            return "{" + string.Join(",", Points.Select(p => p.ToString()).ToArray()) + "}";
        }

        public int Count
        {
            get { return Points.Count(); }
        }

        public void Add(Point p)
        {
            Points.Add(p);
        }

        public void Insert(int position, Point p)
        {
            Points.Insert(position, p);
        }

        public void RemoveAt(int position)
        {
            Points.RemoveAt(position);
        }

        public List<Point> AsList()
        {
            return new List<Point>(Points);
        }

        public Polygon Clone(bool deep=true,Func<Point, Point> action = null)
        {
            
            if (action == null)
            {
                action = (p) => new Point { X = p.X, Y = p.Y };
            }
            var result = new Polygon
            {
                Points = Points.Select(p => action(p)).ToList(),
                Children = deep?(Children!=null?Children.Select(a=>a.Clone(deep)).ToList():null):null,
                X = X,
                Y = Y,
                Id=Id,
                Source = Source,
                Hole = Hole,
                Rotation = Rotation,
                Height = Height,
                Width = Width
            };
            if (result.Children != null)
            {
                foreach (var ch in result.Children)
                {
                    ch.Parent = result;
                }
            }

            return result;
        }

        public Point this[int i] // square bracket operator with int argument
        {
            get => Points[i];
            set => Points[i] = value;
        }
    }

    public static class ArrayUtils
    {
        public static List<T> Splice<T>(this List<T> target, int index, int howMany, params T[] insert)
        {
            var result = new List<T>();
            for (var i = (index + howMany - 1); i >= index; i--)
            {
                result.Add(target[i]);
                target.RemoveAt(i);
            }

            foreach (var item in insert)
            {
                target.Add(item);
            }

            return result;
        }
    }
}