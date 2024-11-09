using System;

namespace StardewModdingAPI.Web.Framework.Caching;

/// <summary>The base logic for a cache repository.</summary>
internal abstract class BaseCacheRepository : ICacheRepository
{
    /*********
    ** Public methods
    *********/
    /// <inheritdoc />
    public bool IsStale(DateTimeOffset lastUpdated, int staleMinutes)
    {
        return lastUpdated < DateTimeOffset.UtcNow.AddMinutes(-staleMinutes);
    }
}
