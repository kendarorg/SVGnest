using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SvgNest
{
    public static class ExtMethods
    {
        public static void splice<T>(this List<T> target, int where=0,int todelete=0,params T[] toAddAtWhere)
        {
            while (todelete > 0)
            {
                target.RemoveAt(where);
                todelete--;
            }

            foreach (var item in toAddAtWhere)
            {
                target.Insert(where,item);
            }
        }

        public static List<T> slice<T>(this List<T> target, int where = 0,int count=0)
        {
            var result = new List<T>();
            if (count == 0) count = target.Count;
            while (where < count)
            {
                result.Add(target[where]);
                where++;
            }

            return result;
        }

        public static T pop<T>(this List<T> target)
        {
            var last = target.LastOrDefault();
            if (last != null)
            {
                target.Remove(last);
            }

            return last;
        }
    }
}
