using System.Collections.Generic;
using Newtonsoft.Json;

namespace StardewModdingAPI.Toolkit.Framework.Clients.ModDropExport.ResponseModels;

/// <summary>The metadata for all Stardew Valley from the ModDrop export API.</summary>
public class ModDropFullExport
{
    /// <summary>The mod data indexed by public mod ID.</summary>
    public Dictionary<long, ModDropModExport> Mods { get; set; } = new();

    /// <summary>The HTTP cache headers set by a remote server.</summary>
    [JsonIgnore]
    public ApiCacheHeaders CacheHeaders = null!; // set in API client
}
