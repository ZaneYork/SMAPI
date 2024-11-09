namespace StardewModdingAPI.Toolkit.Serialization.Models;

/// <summary>Indicates which mod can read the content pack represented by the containing manifest.</summary>
public class ManifestContentPackFor : IManifestContentPackFor
{
    /*********
    ** Accessors
    *********/
    /// <summary>The unique ID of the mod which can read this content pack.</summary>
    public string UniqueID { get; }

    /// <summary>The minimum required version (if any).</summary>
    public ISemanticVersion? MinimumVersion { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="uniqueId">The unique ID of the mod which can read this content pack.</param>
    /// <param name="minimumVersion">The minimum required version (if any).</param>
    public ManifestContentPackFor(string uniqueId, ISemanticVersion? minimumVersion)
    {
        this.UniqueID = Manifest.NormalizeWhitespace(uniqueId);
        this.MinimumVersion = minimumVersion;
    }
}
