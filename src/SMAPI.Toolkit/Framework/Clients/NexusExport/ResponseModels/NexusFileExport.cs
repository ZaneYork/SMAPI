using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace StardewModdingAPI.Toolkit.Framework.Clients.NexusExport.ResponseModels;

/// <summary>The metadata for an uploaded file for a mod from the Nexus Mods export API.</summary>
public class NexusFileExport
{
    /// <summary>The unique internal file identifier.</summary>
    public long Uid { get; set; }

    /// <summary>The file's display name.</summary>
    public string? Name { get; set; }

    /// <summary>The file's display description.</summary>
    public string? Description { get; set; }

    /// <summary>The file name that will be downloaded.</summary>
    [JsonProperty("uri")]
    public string? FileName { get; set; }

    /// <summary>The file's semantic version.</summary>
    public string? Version { get; set; }

    /// <summary>The file category ID.</summary>
    [JsonProperty("category_id")]
    public uint CategoryId { get; set; }

    /// <summary>Whether this is the main Vortex file.</summary>
    public bool Primary { get; set; }

    /// <summary>The file's size in bytes.</summary>
    [JsonProperty("size_in_byes")]
    public long? SizeInBytes { get; set; }

    /// <summary>When the file was uploaded.</summary>
    [JsonProperty("uploaded_at")]
    public long UploadedAt { get; set; }

    /// <summary>The extra fields returned by the export API, if any.</summary>
    [JsonExtensionData]
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Used to track any new data provided by the API.")]
    public Dictionary<string, object>? OtherFields;
}
