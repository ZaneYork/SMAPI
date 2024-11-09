using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace StardewModdingAPI.Toolkit.Framework.Clients.ModDropExport.ResponseModels;

/// <summary>The metadata for an uploaded file for a mod from the ModDrop export API.</summary>
public class ModDropFileExport
{
    /// <summary>The file identifier.</summary>
    public uint Id { get; set; }

    /// <summary>The file's display title.</summary>
    [JsonProperty("title")]
    public string? Name { get; set; }

    /// <summary>The file's actual filename.</summary>
    public string? FileName { get; set; }

    /// <summary>The file description.</summary>
    [JsonProperty("desc")]
    public string? Description { get; set; }

    /// <summary>The file version.</summary>
    public string? Version { get; set; }

    /// <summary>Whether the file is deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Whether the file is hidden from users.</summary>
    public bool IsHidden { get; set; }

    /// <summary>Whether this is the default file for the mod.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Whether this is an archived file.</summary>
    public bool IsOld { get; set; }

    /// <summary>Whether this is a pre-release version (e.g. beta).</summary>
    public bool IsPreRelease { get; set; }

    /// <summary>Whether this is an alternative download.</summary>
    public bool IsAlternative { get; set; }

    /// <summary>When the file was uploaded, as a Unix millisecond timestamp since epoch.</summary>
    public long DateCreated { get; set; }

    /// <summary>The extra fields returned by the export API, if any.</summary>
    [JsonExtensionData]
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Used to track any new data provided by the API.")]
    public Dictionary<string, object>? OtherFields;
}
