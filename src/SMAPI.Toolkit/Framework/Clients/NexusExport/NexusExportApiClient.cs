using System.Net.Http;
using System.Threading.Tasks;
using Pathoschild.Http.Client;
using StardewModdingAPI.Toolkit.Framework.Clients.NexusExport.ResponseModels;

namespace StardewModdingAPI.Toolkit.Framework.Clients.NexusExport;

/// <inheritdoc cref="INexusExportApiClient" />
public class NexusExportApiClient : INexusExportApiClient
{
    /*********
    ** Fields
    *********/
    /// <summary>The underlying HTTP client.</summary>
    private readonly IClient Client;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="userAgent">The user agent for the Nexus export API.</param>
    /// <param name="baseUrl">The base URL for the Nexus export API.</param>
    public NexusExportApiClient(string userAgent, string baseUrl)
    {
        this.Client = new FluentClient(baseUrl).SetUserAgent(userAgent);
    }

    /// <inheritdoc />
    public async Task<ApiCacheHeaders> FetchCacheHeadersAsync()
    {
        IResponse response = await this.Client.SendAsync(HttpMethod.Head, "");
        return ApiCacheHeaders.FromResponse(response);
    }

    /// <inheritdoc />
    public async Task<NexusFullExport> FetchExportAsync()
    {
        IResponse response = await this.Client.GetAsync("");

        NexusFullExport export = await response.As<NexusFullExport>();
        export.CacheHeaders = ApiCacheHeaders.FromResponse(response);

        return export;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this.Client.Dispose();
    }
}
