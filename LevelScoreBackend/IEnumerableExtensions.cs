using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LevelScoreBackend
{
    public static class IEnumerableExtensions
    {
        public static int Max<T>(this IEnumerable<T> source, Func<T,int> selector, int defaultValue)
        {
            if (source == null) return defaultValue;
            if (source.Count() == 0) return defaultValue;

            if (typeof(T) == typeof(int) && selector == null)
            {
                selector = i => (int)(object)i;
            }

            if (selector == null) return defaultValue;

            return source.Max(selector);
        }
    }
}
