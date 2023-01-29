namespace System;
using Internal.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

public static class MobileMemoryExtension
{

    /// <summary>Indicates whether a specified value is found in a read-only span. Values are compared using IEquatable{T}.Equals(T).</summary>
    /// <param name="span">The span to search.</param>
    /// <param name="value">The value to search for.</param>
    /// <typeparam name="T">The type of the span.</typeparam>
    /// <returns>
    /// <see langword="true" /> if found, <see langword="false" /> otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>
    {
        return span.IndexOf(value) >= 0;
    }
}
