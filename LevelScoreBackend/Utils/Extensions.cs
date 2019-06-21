using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LevelScoreBackend.Utils
{
    public static class Extensions
    {
        private static Dictionary<ReaderWriterLockSlim, string> tags = new Dictionary<ReaderWriterLockSlim, string>();

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

        public static string GetCurrentHeldLocks(this ReaderWriterLockSlim rwl)
        {
            var strs = new List<string>();
            if (rwl.IsReadLockHeld)
            {
                strs.Add("ReadLock");
            }
            if (rwl.IsUpgradeableReadLockHeld)
            {
                strs.Add("Upgradeable ReadLock");
            }
            if (rwl.IsWriteLockHeld)
            {
                strs.Add("WriteLock");
            }
            if (strs.Count == 0)
            {
                return "none";
            }
            return strs.Aggregate((a, b) => a + ", " + b);
        }
        public static void AddTag(this ReaderWriterLockSlim rwl, string tag)
        {
            tags.Add(rwl, tag);
        }
        public static string GetTag(this ReaderWriterLockSlim rwl)
        {
            if (tags.TryGetValue(rwl, out var val))
            {
                return val;
            }
            return "";
        }
    }
}
