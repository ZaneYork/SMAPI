using System.Diagnostics.CodeAnalysis;
using StardewModdingAPI.Toolkit.Framework.Clients;

namespace StardewModdingAPI.Web.Framework.Caching;

/// <summary>Encapsulates logic for accessing data in a cached mod export from a remote API.</summary>
internal interface IExportCacheRepository : ICacheRepository
{
    /*********
    ** Accessors
    *********/
    /// <summary>Whether the export data is currently available.</summary>
    [MemberNotNullWhen(true, nameof(IExportCacheRepository.CacheHeaders))]
    public bool IsLoaded { get; }

    /// <summary>The date and version of the cached export data, if it's loaded.</summary>
    public ApiCacheHeaders? CacheHeaders { get; }


    /*********
    ** Methods
    *********/
    /// <summary>Get whether the cached data is stale.</summary>
    /// <param name="staleMinutes">The age in minutes before data is considered stale.</param>
    bool IsStale(int staleMinutes);

    /// <summary>Clear all data in the cache.</summary>
    void Clear();

    /// <summary>Set the date and version of the cached export data.</summary>
    /// <param name="headers">The headers to set.</param>
    void SetCacheHeaders(ApiCacheHeaders headers);
}
