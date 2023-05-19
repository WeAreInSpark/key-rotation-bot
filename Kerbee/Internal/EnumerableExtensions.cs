using System;
using System.Collections.Generic;
using System.Linq;

using CommunityToolkit.Diagnostics;

namespace Kerbee.Internal;

public static class EnumerableExtensions
{
    public static bool None<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
    {
        if (source == null)
        {
            ThrowHelper.ThrowArgumentNullException(nameof(source));
        }

        if (predicate == null)
        {
            ThrowHelper.ThrowArgumentNullException(nameof(predicate));
        }

        return !source.Any(predicate);
    }
}
