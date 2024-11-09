using System;
using System.Diagnostics.CodeAnalysis;
using StardewModdingAPI.Toolkit.Framework.Clients;
using StardewModdingAPI.Toolkit.Framework.Clients.CurseForgeExport.ResponseModels;

namespace StardewModdingAPI.Web.Framework.Caching.CurseForgeExport;

/// <summary>Manages cached mod data from the CurseForge export API in-memory.</summary>
internal class CurseForgeExportCacheMemoryRepository : BaseExportCacheRepository, ICurseForgeExportCacheRepository
{
    /*********
    ** Fields
    *********/
    /// <summary>The cached mod data from the CurseForge export API.</summary>
    private CurseForgeFullExport? Data;


    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    [MemberNotNullWhen(true, nameof(CurseForgeExportCacheMemoryRepository.Data))]
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
    public bool TryGetMod(uint id, [NotNullWhen(true)] out CurseForgeModExport? mod)
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
    public void SetData(CurseForgeFullExport? export)
    {
        this.Data = export;
    }
}
