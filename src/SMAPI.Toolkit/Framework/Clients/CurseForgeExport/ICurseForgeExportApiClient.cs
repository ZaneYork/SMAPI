using System;
using System.Threading.Tasks;
using StardewModdingAPI.Toolkit.Framework.Clients.CurseForgeExport.ResponseModels;

namespace StardewModdingAPI.Toolkit.Framework.Clients.CurseForgeExport;

/// <summary>An HTTP client for fetching the mod export from the CurseForge export API.</summary>
public interface ICurseForgeExportApiClient : IDisposable
{
    /// <summary>Fetch the cache headers for the export data on the server.</summary>
    Task<ApiCacheHeaders> FetchCacheHeadersAsync();

    /// <summary>Fetch the latest export file from the CurseForge export API.</summary>
    Task<CurseForgeFullExport> FetchExportAsync();
}
