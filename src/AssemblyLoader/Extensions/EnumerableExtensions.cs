using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorLazyLoading.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> source)
            where T : class
        {
            return source.Where(i => i != null).Cast<T>();
        }

        public static IEnumerable<T> DistinctBy<T, TOut>(
            this IEnumerable<T> source,
            Func<T, TOut> selector)
        {
            return source
                .GroupBy(m => selector(m))
                .Select(g => g.First());
        }
    }
}
