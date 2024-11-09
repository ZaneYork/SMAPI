using System.Collections.Generic;
using Newtonsoft.Json;

namespace StardewModdingAPI.Toolkit.Framework.Clients.NexusExport.ResponseModels;

/// <summary>The metadata for all Stardew Valley from the Nexus Mods export API.</summary>
public class NexusFullExport
{
    /// <summary>The mod data indexed by public mod ID.</summary>
    public Dictionary<uint, NexusModExport> Data { get; set; } = new();

    /// <summary>The HTTP cache headers set by a remote server.</summary>
    [JsonIgnore]
    public ApiCacheHeaders CacheHeaders = null!; // set in API client
}
