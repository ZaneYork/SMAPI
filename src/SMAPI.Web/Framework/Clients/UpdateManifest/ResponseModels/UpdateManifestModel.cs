using System.Collections.Generic;

namespace StardewModdingAPI.Web.Framework.Clients.UpdateManifest.ResponseModels;

/// <summary>The data model for an update manifest file.</summary>
internal class UpdateManifestModel
{
    /*********
    ** Accessors
    *********/
    /// <summary>The manifest format version. This is equivalent to the SMAPI version, and is used to parse older manifests correctly if later versions of SMAPI change the expected format.</summary>
    public string Format { get; }

    /// <summary>The mod info in this update manifest.</summary>
    public IDictionary<string, UpdateManifestModModel> Mods { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="format">The manifest format version.</param>
    /// <param name="mods">The mod info in this update manifest.</param>
    public UpdateManifestModel(string format, IDictionary<string, UpdateManifestModModel>? mods)
    {
        this.Format = format;
        this.Mods = mods ?? new Dictionary<string, UpdateManifestModModel>();
    }
}
