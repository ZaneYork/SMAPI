using System;
using System.Threading.Tasks;
using StardewModdingAPI.Toolkit.Framework.Clients;
using StardewModdingAPI.Toolkit.Framework.Clients.CurseForgeExport;
using StardewModdingAPI.Toolkit.Framework.Clients.CurseForgeExport.ResponseModels;

namespace StardewModdingAPI.Web.Framework.Clients.CurseForge;

/// <summary>A client for the CurseForge export API which does nothing, used for local development.</summary>
internal class DisabledCurseForgeExportApiClient : ICurseForgeExportApiClient
{
    /*********
    ** Public methods
    *********/
    /// <inheritdoc />
    public Task<ApiCacheHeaders> FetchCacheHeadersAsync()
    {
        return Task.FromResult(
            new ApiCacheHeaders(DateTimeOffset.MinValue, "immutable")
        );
    }

    /// <inheritdoc />
    public async Task<CurseForgeFullExport> FetchExportAsync()
    {
        return new CurseForgeFullExport
        {
            Mods = new(),
            CacheHeaders = await this.FetchCacheHeadersAsync()
        };
    }

    /// <inheritdoc />
    public void Dispose() { }
}
