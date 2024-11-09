using System;

namespace StardewModdingAPI;

/// <summary>Basic metadata for a content asset.</summary>
public interface IAssetInfo
{
    /*********
    ** Accessors
    *********/
    /// <summary>The content's locale code, if the content is localized.</summary>
    string? Locale { get; }

    /// <summary>The asset name being read.</summary>
    public IAssetName Name { get; }

    /// <summary>The <see cref="Name"/> with any locale codes stripped.</summary>
    /// <remarks>For example, if <see cref="Name"/> contains a locale like <c>Data/Bundles.fr-FR</c>, this will be the name without locale like <c>Data/Bundles</c>. If the name has no locale, this field is equivalent.</remarks>
    public IAssetName NameWithoutLocale { get; }

    /// <summary>The content data type.</summary>
    Type DataType { get; }
}
