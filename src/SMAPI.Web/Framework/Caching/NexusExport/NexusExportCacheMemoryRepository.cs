using System;
using System.Diagnostics.CodeAnalysis;
using StardewModdingAPI.Toolkit.Framework.Clients;
using StardewModdingAPI.Toolkit.Framework.Clients.NexusExport.ResponseModels;

namespace StardewModdingAPI.Web.Framework.Caching.NexusExport;

/// <summary>Manages cached mod data from the Nexus export API in-memory.</summary>
internal class NexusExportCacheMemoryRepository : BaseExportCacheRepository, INexusExportCacheRepository
{
    /*********
    ** Fields
    *********/
    /// <summary>The cached mod data from the Nexus export API.</summary>
    private NexusFullExport? Data;


    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    [MemberNotNullWhen(true, nameof(NexusExportCacheMemoryRepository.Data))]
    public override bool IsLoaded => this.Data?.Data.Count > 0;

    /// <inheritdoc />
    public override ApiCacheHeaders? CacheHeaders => this.Data?.CacheHeaders;


    /*********
    ** Public methods
    *********/
    /// <inheritdoc />
    public override void Clear()
    {
        this.SetData(null);
    }

    /// <inheritdoc />
    public override void SetCacheHeaders(ApiCacheHeaders headers)
    {
        if (!this.IsLoaded)
            throw new InvalidOperationException("Can't set the cache headers before any data is loaded.");

        this.Data.CacheHeaders = headers;
    }

    /// <inheritdoc />
    public bool TryGetMod(uint id, [NotNullWhen(true)] out NexusModExport? mod)
    {
        var data = this.Data?.Data;

        if (data is null || !data.TryGetValue(id, out mod))
        {
            mod = null;
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public void SetData(NexusFullExport? export)
    {
        this.Data = export;
    }
}
