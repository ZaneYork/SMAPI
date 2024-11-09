using System;
using StardewModdingAPI.Toolkit.Utilities;
using StardewModdingAPI.Utilities.AssetPathUtilities;
using StardewValley;
using ToolkitPathUtilities = StardewModdingAPI.Toolkit.Utilities.PathUtilities;

namespace StardewModdingAPI.Framework.Content;

/// <summary>An asset name that can be loaded through the content pipeline.</summary>
internal class AssetName : IAssetName
{
    /*********
    ** Fields
    *********/
    /// <summary>A lowercase version of <see cref="Name"/> used for consistent hash codes and equality checks.</summary>
    private readonly string ComparableName;


    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string BaseName { get; }

    /// <inheritdoc />
    public string? LocaleCode { get; }

    /// <inheritdoc />
    public LocalizedContentManager.LanguageCode? LanguageCode { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="baseName">The base asset name without the locale code.</param>
    /// <param name="localeCode">The locale code specified in the <see cref="Name"/>, if it's a valid code recognized by the game content.</param>
    /// <param name="languageCode">The language code matching the <see cref="LocaleCode"/>, if applicable.</param>
    public AssetName(string baseName, string? localeCode, LocalizedContentManager.LanguageCode? languageCode)
    {
        // validate
        if (string.IsNullOrWhiteSpace(baseName))
            throw new ArgumentException("The asset name can't be null or empty.", nameof(baseName));
        if (string.IsNullOrWhiteSpace(localeCode))
            localeCode = null;

        // set base values
        this.BaseName = PathUtilities.NormalizeAssetName(baseName);
        this.LocaleCode = localeCode;
        this.LanguageCode = languageCode;

        // set derived values
        this.Name = localeCode != null
            ? string.Concat(this.BaseName, '.', this.LocaleCode)
            : this.BaseName;
        this.ComparableName = this.Name.ToLowerInvariant();
    }

    /// <summary>Parse a raw asset name into an instance.</summary>
    /// <param name="rawName">The raw asset name to parse.</param>
    /// <param name="parseLocale">Get the language code for a given locale, if it's valid.</param>
    /// <exception cref="ArgumentException">The <paramref name="rawName"/> is null or empty.</exception>
    public static AssetName Parse(string rawName, Func<string, LocalizedContentManager.LanguageCode?> parseLocale)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            throw new ArgumentException("The asset name can't be null or empty.", nameof(rawName));

        string baseName = rawName;
        string? localeCode = null;
        LocalizedContentManager.LanguageCode? languageCode = null;

        int lastPeriodIndex = rawName.LastIndexOf('.');
        if (lastPeriodIndex > 0 && rawName.Length > lastPeriodIndex + 1)
        {
            string possibleLocaleCode = rawName[(lastPeriodIndex + 1)..];
            LocalizedContentManager.LanguageCode? possibleLanguageCode = parseLocale(possibleLocaleCode);

            if (possibleLanguageCode != null)
            {
                baseName = rawName[..lastPeriodIndex];
                localeCode = possibleLocaleCode;
                languageCode = possibleLanguageCode;
            }
        }

        return new AssetName(baseName, localeCode, languageCode);
    }

    /// <inheritdoc />
    public bool IsEquivalentTo(string? assetName, bool useBaseName = false)
    {
        // empty asset key is never equivalent
        if (string.IsNullOrWhiteSpace(assetName))
            return false;

        AssetNamePartEnumerator curParts = new(useBaseName ? this.BaseName : this.Name);
        AssetNamePartEnumerator otherParts = new(assetName.AsSpan().Trim());

        while (true)
        {
            bool curHasMore = curParts.MoveNext();
            bool otherHasMore = otherParts.MoveNext();

            // mismatch: lengths differ
            if (otherHasMore != curHasMore)
                return false;

            // match: both reached the end without a mismatch
            if (!curHasMore)
                return true;

            // mismatch: current segment is different
            if (!curParts.Current.Equals(otherParts.Current, StringComparison.OrdinalIgnoreCase))
                return false;
        }
    }

    /// <inheritdoc />
    public bool IsEquivalentTo(IAssetName? assetName, bool useBaseName = false)
    {
        if (useBaseName)
            return this.BaseName.Equals(assetName?.BaseName, StringComparison.OrdinalIgnoreCase);

        if (assetName is AssetName impl)
            return this.ComparableName == impl.ComparableName;

        return this.Name.Equals(assetName?.Name, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public bool StartsWith(string? prefix, bool allowPartialWord = true, bool allowSubfolder = true)
    {
        // asset keys never start with null
        if (prefix is null)
            return false;

        // get initial values
        ReadOnlySpan<char> trimmedPrefix = prefix.AsSpan().Trim();
        if (trimmedPrefix.Length == 0)
            return true;
        ReadOnlySpan<char> pathSeparators = new(ToolkitPathUtilities.PossiblePathSeparators); // just to simplify calling other span APIs

        // asset keys can't have a leading slash, but AssetPathYielder will trim them
        if (pathSeparators.Contains(trimmedPrefix[0]))
            return false;

        // compare segments
        AssetNamePartEnumerator curParts = new(this.Name);
        AssetNamePartEnumerator prefixParts = new(trimmedPrefix);
        while (true)
        {
            bool curHasMore = curParts.MoveNext();
            bool prefixHasMore = prefixParts.MoveNext();

            // reached end for one side
            if (prefixHasMore != curHasMore)
            {
                // mismatch: prefix is longer
                if (prefixHasMore)
                    return false;

                // match: every segment in the prefix matched and subfolders are allowed (e.g. prefix 'Data/Events' with target 'Data/Events/Beach')
                if (allowSubfolder)
                    return true;

                // Special case: the prefix ends with a path separator, but subfolders aren't allowed. This case
                // matches if there's no further path separator in the asset name *after* the current separator.
                // For example, the prefix 'A/B/' matches 'A/B/C' but not 'A/B/C/D'.
                return pathSeparators.Contains(trimmedPrefix[^1]) && curParts.Remainder.Length == 0;
            }

            // previous segments matched exactly and both reached the end
            // match if prefix doesn't end with '/' (which should only match subfolders)
            if (!prefixHasMore)
                return !pathSeparators.Contains(trimmedPrefix[^1]);

            // compare segment
            if (curParts.Current.Length == prefixParts.Current.Length)
            {
                // mismatch: segments aren't equivalent
                if (!curParts.Current.Equals(prefixParts.Current, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            else
            {
                // mismatch: prefix has more beyond this, and this segment isn't an exact match
                if (prefixParts.Remainder.Length != 0)
                    return false;

                // mismatch: cur segment doesn't start with prefix
                if (!curParts.Current.StartsWith(prefixParts.Current, StringComparison.OrdinalIgnoreCase))
                    return false;

                // mismatch: something like "Maps/" would need an exact match
                if (pathSeparators.Contains(trimmedPrefix[^1]))
                    return false;

                // mismatch: partial word match not allowed, and the first or last letter of the suffix isn't a word separator
                if (!allowPartialWord && char.IsLetterOrDigit(prefixParts.Current[^1]) && char.IsLetterOrDigit(curParts.Current[prefixParts.Current.Length]))
                    return false;

                // possible match
                return allowSubfolder || (pathSeparators.Contains(trimmedPrefix[^1]) ? curParts.Remainder.IndexOfAny(ToolkitPathUtilities.PossiblePathSeparators) < 0 : curParts.Remainder.Length == 0);
            }
        }
    }

    /// <inheritdoc />
    public bool IsDirectlyUnderPath(string? assetFolder)
    {
        if (assetFolder is null)
            return false;

        return this.StartsWith(assetFolder + ToolkitPathUtilities.PreferredPathSeparator, allowPartialWord: false, allowSubfolder: false);
    }

    /// <inheritdoc />
    IAssetName IAssetName.GetBaseAssetName()
    {
        return this.LocaleCode == null
            ? this
            : new AssetName(this.BaseName, null, null);
    }

    /// <inheritdoc />
    public bool Equals(IAssetName? other)
    {
        return other switch
        {
            null => false,
            AssetName otherImpl => this.ComparableName == otherImpl.ComparableName,
            _ => StringComparer.OrdinalIgnoreCase.Equals(this.Name, other.Name)
        };
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return this.ComparableName.GetHashCode();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return this.Name;
    }
}
