namespace StardewModdingAPI.Web.Framework.Clients.UpdateManifest;

/// <summary>Metadata about a mod download in an update manifest file.</summary>
internal class UpdateManifestModDownload : GenericModDownload
{
    /*********
    ** Fields
    *********/
    /// <summary>The update subkey for this mod download.</summary>
    private readonly string Subkey;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="fieldName">The field name for this mod download in the manifest.</param>
    /// <param name="name">The mod name for this download.</param>
    /// <param name="version">The download's version.</param>
    /// <param name="url">The download's URL.</param>
    public UpdateManifestModDownload(string fieldName, string name, string? version, string? url)
        : base(name, null, version, url)
    {
        this.Subkey = '@' + fieldName;
    }

    /// <summary>Get whether the subkey matches this download.</summary>
    /// <param name="subkey">The update subkey to check.</param>
    public override bool MatchesSubkey(string subkey)
    {
        return subkey == this.Subkey;
    }
}
