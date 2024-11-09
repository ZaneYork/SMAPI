using System;
using ToolkitPathUtilities = StardewModdingAPI.Toolkit.Utilities.PathUtilities;

namespace StardewModdingAPI.Utilities.AssetPathUtilities;

/// <summary>Handles enumerating the normalized segments in an asset name.</summary>
internal ref struct AssetNamePartEnumerator
{
    /*********
    ** Fields
    *********/
    /// <summary>The backing field for <see cref="Remainder"/>.</summary>
    private ReadOnlySpan<char> RemainderImpl;


    /*********
    ** Properties
    *********/
    /// <summary>The remainder of the asset name being enumerated, ignoring segments which have already been yielded.</summary>
    public ReadOnlySpan<char> Remainder => this.RemainderImpl;

    /// <summary>Get the current segment.</summary>
    public ReadOnlySpan<char> Current { get; private set; } = default;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="assetName">The asset name to enumerate.</param>
    public AssetNamePartEnumerator(ReadOnlySpan<char> assetName)
    {
        this.RemainderImpl = AssetNamePartEnumerator.TrimLeadingPathSeparators(assetName);
    }

    /// <summary>Move the enumerator to the next segment.</summary>
    /// <returns>Returns true if a new value was found (accessible via <see cref="Current"/>).</returns>
    public bool MoveNext()
    {
        if (this.RemainderImpl.Length == 0)
            return false;

        int index = this.RemainderImpl.IndexOfAny(ToolkitPathUtilities.PossiblePathSeparators);

        // no more separator characters found, I'm done.
        if (index < 0)
        {
            this.Current = this.RemainderImpl;
            this.RemainderImpl = ReadOnlySpan<char>.Empty;
            return true;
        }

        // Yield the next separate character bit
        this.Current = this.RemainderImpl[..index];
        this.RemainderImpl = AssetNamePartEnumerator.TrimLeadingPathSeparators(this.RemainderImpl[(index + 1)..]);
        return true;
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Trim path separators at the start of the given path or segment.</summary>
    /// <param name="span">The path or segment to trim.</param>
    private static ReadOnlySpan<char> TrimLeadingPathSeparators(ReadOnlySpan<char> span)
    {
        return span.TrimStart(new ReadOnlySpan<char>(ToolkitPathUtilities.PossiblePathSeparators));
    }
}
