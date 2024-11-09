namespace StardewModdingAPI.Web.Framework;

/// <summary>Generic metadata about a file download on a mod page.</summary>
internal interface IModDownload
{
    /*********
    ** Accessors
    *********/
    /// <summary>The download's display name.</summary>
    string Name { get; }

    /// <summary>The download's description.</summary>
    string? Description { get; }

    /// <summary>The download's file version.</summary>
    string? Version { get; }

    /// <summary>The mod URL page from which to download this update, if different from the URL of the mod page it was fetched from.</summary>
    string? ModPageUrl { get; }


    /*********
    ** Methods
    *********/
    /// <summary>Get whether the subkey matches this download.</summary>
    /// <param name="subkey">The update subkey to check.</param>
    bool MatchesSubkey(string subkey);
}
