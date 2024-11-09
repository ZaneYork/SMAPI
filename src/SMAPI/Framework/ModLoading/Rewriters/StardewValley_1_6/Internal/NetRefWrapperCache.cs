using System;
using System.Runtime.CompilerServices;
using Netcode;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6.Internal;

/// <summary>A cache of <see cref="NetRef{T}"/> instances for specific values.</summary>
internal static class NetRefWrapperCache<T>
    where T : class, INetObject<INetSerializable>
{
    /*********
    ** Fields
    *********/
    /// <summary>A cached lookup of wrappers.</summary>
    private static readonly ConditionalWeakTable<T, NetRef<T>> CachedWrappers = new();


    /*********
    ** Public methods
    *********/
    /// <summary>Get a wrapper for a given value.</summary>
    /// <param name="value">The value to wrap.</param>
    public static NetRef<T> GetCachedWrapperFor(T value)
    {
        if (value is null)
            throw new InvalidOperationException($"{nameof(NetRefWrapperCache<T>)} doesn't support wrapping null values.");

        if (!CachedWrappers.TryGetValue(value, out NetRef<T>? wrapper))
        {
            wrapper = new NetRef<T>(value);
            CachedWrappers.AddOrUpdate(value, wrapper);
        }

        return wrapper;
    }
}
