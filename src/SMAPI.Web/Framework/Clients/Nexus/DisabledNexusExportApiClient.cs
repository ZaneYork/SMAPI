using System;
using System.Threading.Tasks;
using StardewModdingAPI.Toolkit.Framework.Clients;
using StardewModdingAPI.Toolkit.Framework.Clients.NexusExport;
using StardewModdingAPI.Toolkit.Framework.Clients.NexusExport.ResponseModels;

namespace StardewModdingAPI.Web.Framework.Clients.Nexus;

/// <summary>A client for the Nexus export API which does nothing, used for local development.</summary>
internal class DisabledNexusExportApiClient : INexusExportApiClient
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
    public async Task<NexusFullExport> FetchExportAsync()
    {
        return new NexusFullExport
        {
            Data = new(),
            CacheHeaders = await this.FetchCacheHeadersAsync()
        };
    }

    /// <inheritdoc />
    public void Dispose() { }
}
