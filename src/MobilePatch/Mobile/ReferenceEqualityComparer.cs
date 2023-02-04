using System.Runtime.CompilerServices;


#nullable enable
namespace System.Collections.Generic
{
  /// <summary>An <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> that uses reference equality (<see cref="M:System.Object.ReferenceEquals(System.Object,System.Object)" />) instead of value equality (<see cref="M:System.Object.Equals(System.Object)" />) when comparing two object instances.</summary>
  public sealed class ReferenceEqualityComparer : IEqualityComparer<object?>, IEqualityComparer
  {
    private ReferenceEqualityComparer()
    {
    }

    /// <summary>Gets the singleton <see cref="T:System.Collections.Generic.ReferenceEqualityComparer" /> instance.</summary>
    public static ReferenceEqualityComparer Instance { get; } = new ReferenceEqualityComparer();

    /// <summary>Determines whether two object references refer to the same object instance.</summary>
    /// <param name="x">The first object to compare.</param>
    /// <param name="y">The second object to compare.</param>
    /// <returns>
    /// <see langword="true" /> if both <paramref name="x" /> and <paramref name="y" /> refer to the same object instance or if both are <see langword="null" />; otherwise, <see langword="false" />.</returns>
    public bool Equals(object? x, object? y) => x == y;

    /// <summary>Returns a hash code for the specified object. The returned hash code is based on the object identity, not on the contents of the object.</summary>
    /// <param name="obj">The object for which to retrieve the hash code.</param>
    /// <returns>A hash code for the identity of <paramref name="obj" />.</returns>
    public int GetHashCode(object? obj) => RuntimeHelpers.GetHashCode(obj);
  }
}
