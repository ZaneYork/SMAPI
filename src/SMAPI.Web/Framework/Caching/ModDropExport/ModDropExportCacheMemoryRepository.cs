using System;
using System.Diagnostics.CodeAnalysis;
using StardewModdingAPI.Toolkit.Framework.Clients;
using StardewModdingAPI.Toolkit.Framework.Clients.ModDropExport.ResponseModels;

namespace StardewModdingAPI.Web.Framework.Caching.ModDropExport;

/// <summary>Manages cached mod data from the ModDrop export API in-memory.</summary>
internal class ModDropExportCacheMemoryRepository : BaseExportCacheRepository, IModDropExportCacheRepository
{
    /*********
    ** Fields
    *********/
    /// <summary>The cached mod data from the ModDrop export API.</summary>
    private ModDropFullExport? Data;


    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    [MemberNotNullWhen(true, nameof(ModDropExportCacheMemoryRepository.Data))]
    public override bool IsLoaded => this.Data?.Mods.Count > 0;

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
    public bool TryGetMod(long id, [NotNullWhen(true)] out ModDropModExport? mod)
    {
        var data = this.Data?.Mods;

        if (data is null || !data.TryGetValue(id, out mod))
        {
            mod = null;
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public void SetData(ModDropFullExport? export)
    {
        this.Data = export;
    }
}
