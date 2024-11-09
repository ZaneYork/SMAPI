using System;

namespace StardewModdingAPI.Web.Framework.Clients;

/// <summary>Generic metadata about a file download on a mod page.</summary>
internal class GenericModDownload : IModDownload
{
    /*********
    ** Accessors
    *********/
    /// <summary>The download's display name.</summary>
    public string Name { get; }

    /// <summary>The download's description.</summary>
    public string? Description { get; }

    /// <summary>The download's file version.</summary>
    public string? Version { get; }

    /// <summary>The mod URL page from which to download this update, if different from the URL of the mod page it was fetched from.</summary>
    public string? ModPageUrl { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="name">The download's display name.</param>
    /// <param name="description">The download's description.</param>
    /// <param name="version">The download's file version.</param>
    /// <param name="modPageUrl">The mod URL page from which to download this update, if different from the URL of the mod page it was fetched from.</param>
    public GenericModDownload(string name, string? description, string? version, string? modPageUrl = null)
    {
        this.Name = name;
        this.Description = description;
        this.Version = version;
        this.ModPageUrl = modPageUrl;
    }

    /// <summary>Get whether the subkey matches this download.</summary>
    /// <param name="subkey">The update subkey to check.</param>
    public virtual bool MatchesSubkey(string subkey)
    {
        return
            this.Name.Contains(subkey, StringComparison.OrdinalIgnoreCase)
            || this.Description?.Contains(subkey, StringComparison.OrdinalIgnoreCase) == true;
    }
}
