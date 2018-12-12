using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClipperLib;
using Geometry;
using Newtonsoft.Json;

namespace SvgNest
{
    public class Commons
    {
        
        public List<T> shuffle<T>(List<T> array)
        {
            var currentIndex = array.Count;
            T temporaryValue = default(T);
            int randomIndex;

            // While there remain elements to shuffle...
            while (0 != currentIndex)
            {

                // Pick a remaining element...
                randomIndex = (int)Math.Floor(NestRandom.NextDouble() * currentIndex);
                currentIndex -= 1;

                // And swap it with the current element.
                temporaryValue = array[currentIndex];
                array[currentIndex] = array[randomIndex];
                array[randomIndex] = temporaryValue;
            }

            return array;
        }
        public static bool IsSimpleType(Type type)
        {
            return
                type.IsPrimitive ||
                new Type[] {
            typeof(Enum),
            typeof(String),
            typeof(Decimal),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(TimeSpan),
            typeof(Guid)
                }.Contains(type) ||
                Convert.GetTypeCode(type) != TypeCode.Object ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && IsSimpleType(type.GetGenericArguments()[0]))
                ;
        }

        public void log(params object[] data)
        {
            var result = string.Empty;
            for (int i = 0; i < data.Length; i++)
            {
                object item = data[i];
                if (null == item)
                {
                    result += "null";
                }

                else if (!IsSimpleType(item.GetType()))
                {
                    result +=  JsonConvert.SerializeObject(item, Newtonsoft.Json.Formatting.None,
                        new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        });
                }
                else
                {
                    result += item;
                }

                if (i < data.Length - 1)
                {
                    result += ",\r\n";
                }
            }
            Console.WriteLine(result);
        }

        public Polygon rotatePolygon(Polygon polygon, double degrees)
        {
            var rotated = GeometryUtil.RotatePolygon(polygon, degrees);
            /*[];
                    angle = degrees * Math.PI / 180;
                    for(var i=0; i<polygon.length; i++){
                        var x = polygon[i].x;
                        var y = polygon[i].y;
                        var x1 = x*Math.cos(angle)-y*Math.sin(angle);
                        var y1 = x*Math.sin(angle)+y*Math.cos(angle);

                        rotated.push({x:x1, y:y1});
                    }*/

            if (polygon.Children!=null && polygon.Children.Count > 0)
            {
                rotated.Children = new List<Polygon>();
                for (var j = 0; j < polygon.Children.Count; j++)
                {
                    rotated.Children.Add(this.rotatePolygon(polygon.Children[j], degrees));
                }
            }

            return rotated;
        }

        public List<IntPoint> toClipperCoordinates(Polygon polygon)
        {
            var clone = new List<IntPoint>();
            for (var i = 0; i < polygon.Count; i++)
            {
                clone.Add(new IntPoint
                {
                    X= (int)polygon[i].X,
                    Y= (int)polygon[i].Y
                });
            }

            return clone;
        }

        public Polygon toNestCoordinates(List<IntPoint> polygon, double scale)
        {
            var clone = new Polygon();
            for (var i = 0; i < polygon.Count; i++)
            {
                clone.Add(new Point{
                    X= polygon[i].X / scale,
                    Y= polygon[i].Y / scale
                });
            }

            return clone;
        }
    }
}
