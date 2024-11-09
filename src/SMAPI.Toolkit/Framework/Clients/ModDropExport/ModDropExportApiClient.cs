using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pathoschild.Http.Client;
using StardewModdingAPI.Toolkit.Framework.Clients.ModDropExport.ResponseModels;

namespace StardewModdingAPI.Toolkit.Framework.Clients.ModDropExport;

/// <inheritdoc cref="IModDropExportApiClient" />
public class ModDropExportApiClient : IModDropExportApiClient
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
    /// <param name="userAgent">The user agent for the ModDrop export API.</param>
    /// <param name="baseUrl">The base URL for the ModDrop export API.</param>
    public ModDropExportApiClient(string userAgent, string baseUrl)
    {
        this.Client = new FluentClient(baseUrl).SetUserAgent(userAgent);
    }

    /// <inheritdoc />
    public async Task<ApiCacheHeaders> FetchCacheHeadersAsync()
    {
        IResponse response = await this.Client.SendAsync(HttpMethod.Head, "");
        return this.ReadCacheHeaders(response);
    }

    /// <inheritdoc />
    public async Task<ModDropFullExport> FetchExportAsync()
    {
        // fetch response
        IResponse response = await this.Client.GetAsync("");

        // read compressed stream
        // ModDrop uses pre-compressed data, it doesn't set the HTTP compression headers
        IEnumerable<JObject?> rawMods;
        using (Stream responseStream = await response.Message.Content.ReadAsStreamAsync())
        using (DeflateStream decompressorStream = new(responseStream, CompressionMode.Decompress))
        using (StreamReader streamReader = new(decompressorStream))
        using (JsonTextReader jsonReader = new(streamReader))
        {
            JObject rootData = await JObject.LoadAsync(jsonReader);
            JObject data = rootData.Property("data")?.Value.Value<JObject>() ?? throw new InvalidOperationException("Can't parse ModDrop response: required element 'data' not found");
            rawMods = data.Property("mods")?.Value.Values<JObject>().ToArray() ?? throw new InvalidOperationException("Can't parse ModDrop response: required element 'data' > 'mods' not found");
        }

        // parse mods
        Dictionary<long, ModDropModExport> mods = new();
        foreach (JObject? rawMod in rawMods)
        {
            if (rawMod is null)
                continue;

            ModDropModExport mod = rawMod.Property("mod")?.Value.ToObject<ModDropModExport>() ?? throw new InvalidOperationException("Can't parse ModDrop response: required element 'mod' not found");
            mod.Files = rawMod.Property("files")?.Value.ToObject<ModDropFileExport[]>()?.ToArray() ?? throw new InvalidOperationException("Can't parse ModDrop response: required element 'mod' not found");

            mods[mod.Id] = mod;
        }


        // build response
        return new()
        {
            CacheHeaders = this.ReadCacheHeaders(response),
            Mods = mods
        };
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this.Client.Dispose();
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Read the HTTP cache headers from a response.</summary>
    /// <param name="response">The API response from ModDrop.</param>
    private ApiCacheHeaders ReadCacheHeaders(IResponse response)
    {
        return ApiCacheHeaders.FromResponse(response, allowNullEntityTag: true); // ModDrop doesn't set the ETag header
    }
}
