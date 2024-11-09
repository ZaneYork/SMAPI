using System;
using System.Threading.Tasks;
using StardewModdingAPI.Toolkit.Framework.Clients.ModDropExport.ResponseModels;

namespace StardewModdingAPI.Toolkit.Framework.Clients.ModDropExport;

/// <summary>An HTTP client for fetching the mod export from the ModDrop export API.</summary>
public interface IModDropExportApiClient : IDisposable
{
    /// <summary>Fetch the cache headers for the export data on the server.</summary>
    Task<ApiCacheHeaders> FetchCacheHeadersAsync();

    /// <summary>Fetch the latest export file from the ModDrop export API.</summary>
    Task<ModDropFullExport> FetchExportAsync();
}
