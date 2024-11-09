using System;
using System.Threading.Tasks;
using StardewModdingAPI.Toolkit.Framework.Clients;
using StardewModdingAPI.Toolkit.Framework.Clients.ModDropExport;
using StardewModdingAPI.Toolkit.Framework.Clients.ModDropExport.ResponseModels;

namespace StardewModdingAPI.Web.Framework.Clients.ModDrop;

/// <summary>A client for the ModDrop export API which does nothing, used for local development.</summary>
internal class DisabledModDropExportApiClient : IModDropExportApiClient
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
    public async Task<ModDropFullExport> FetchExportAsync()
    {
        return new ModDropFullExport
        {
            Mods = new(),
            CacheHeaders = await this.FetchCacheHeadersAsync()
        };
    }

    /// <inheritdoc />
    public void Dispose() { }
}
