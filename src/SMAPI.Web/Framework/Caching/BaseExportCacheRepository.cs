using System.Diagnostics.CodeAnalysis;
using StardewModdingAPI.Toolkit.Framework.Clients;

namespace StardewModdingAPI.Web.Framework.Caching;

/// <summary>The base logic for an export cache repository.</summary>
internal abstract class BaseExportCacheRepository : BaseCacheRepository, IExportCacheRepository
{
    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    [MemberNotNullWhen(true, nameof(BaseExportCacheRepository.CacheHeaders))]
    public abstract bool IsLoaded { get; }

    /// <inheritdoc />
    public abstract ApiCacheHeaders? CacheHeaders { get; }


    /*********
    ** Public methods
    *********/
    /// <inheritdoc />
    public bool IsStale(int staleMinutes)
    {
        return
            this.IsLoaded
            && this.IsStale(this.CacheHeaders.LastModified, staleMinutes);
    }

    /// <inheritdoc />
    public abstract void Clear();

    /// <inheritdoc />
    public abstract void SetCacheHeaders(ApiCacheHeaders headers);
}
