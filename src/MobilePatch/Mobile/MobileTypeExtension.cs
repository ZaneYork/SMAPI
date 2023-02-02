namespace System;

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

public static class MobileTypeExtension
{

    /// <summary>Determines whether the current type can be assigned to a variable of the specified <paramref name="targetType" />.</summary>
    /// <param name="targetType">The type to compare with the current type.</param>
    /// <returns>
    ///         <see langword="true" /> if any of the following conditions is true:
    ///
    /// -   The current instance and <paramref name="targetType" /> represent the same type.
    ///
    /// -   The current type is derived either directly or indirectly from <paramref name="targetType" />. The current type is derived directly from <paramref name="targetType" /> if it inherits from <paramref name="targetType" />; the current type is derived indirectly from <paramref name="targetType" /> if it inherits from a succession of one or more classes that inherit from <paramref name="targetType" />.
    ///
    /// -   <paramref name="targetType" /> is an interface that the current type implements.
    ///
    /// -   The current type is a generic type parameter, and <paramref name="targetType" /> represents one of the constraints of the current type.
    ///
    /// -   The current type represents a value type, and <paramref name="targetType" /> represents <c>Nullable&lt;c&gt;</c> (<c>Nullable(Of c)</c> in Visual Basic).
    ///
    ///  <see langword="false" /> if none of these conditions are true, or if <paramref name="targetType" /> or <see langword="this" /> is <see langword="null" />.</returns>
    public static bool IsAssignableTo(this Type type, [NotNullWhen(true)] Type? targetType) => (object) targetType != null && targetType.IsAssignableFrom(type);

}
