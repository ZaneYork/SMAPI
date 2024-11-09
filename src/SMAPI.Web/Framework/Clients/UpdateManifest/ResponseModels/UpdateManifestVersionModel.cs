namespace StardewModdingAPI.Web.Framework.Clients.UpdateManifest.ResponseModels;

/// <summary>Data model for a Version in an update manifest.</summary>
internal class UpdateManifestVersionModel
{
    /*********
    ** Accessors
    *********/
    /// <summary>The mod's semantic version.</summary>
    public string? Version { get; }

    /// <summary>The mod page URL from which to download updates, if different from <see cref="UpdateManifestModModel.ModPageUrl"/>.</summary>
    public string? ModPageUrl { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="version">The mod's semantic version.</param>
    /// <param name="modPageUrl">The mod page URL from which to download updates, if different from <see cref="UpdateManifestModModel.ModPageUrl"/>.</param>
    public UpdateManifestVersionModel(string version, string? modPageUrl)
    {
        this.Version = version;
        this.ModPageUrl = modPageUrl;
    }
}
