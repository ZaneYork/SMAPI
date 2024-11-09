using System.Collections.Generic;
using Newtonsoft.Json;

namespace StardewModdingAPI.Toolkit.Framework.Clients.CurseForgeExport.ResponseModels;

/// <summary>The metadata for all Stardew Valley from the CurseForge export API.</summary>
public class CurseForgeFullExport
{
    /// <summary>The mod data indexed by public mod ID.</summary>
    public Dictionary<uint, CurseForgeModExport> Mods { get; set; } = new();

    /// <summary>The HTTP cache headers set by a remote server.</summary>
    [JsonIgnore]
    public ApiCacheHeaders CacheHeaders = null!; // set in API client
}
