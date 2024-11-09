using System;
using System.Collections.Generic;
using StardewModdingAPI.Toolkit.Framework.UpdateData;

namespace StardewModdingAPI.Web.Framework.Metrics;

/// <summary>An aggregate summary of tracked metrics.</summary>
/// <param name="Uptime">The total time since the server began tracking metrics.</param>
/// <param name="SuccessCacheMinutes">The number of minutes for which a successful data fetch from a remote mod site is cached.</param>
/// <param name="ErrorCacheMinutes">The number of minutes for which a failed data fetch from a remote mod site is cached.</param>
/// <param name="TotalApiRequests">The total number of update-check requests received by the API (each of which may include multiple update keys).</param>
/// <param name="UniqueModsChecked">The number of unique mod IDs requested.</param>
/// <param name="TotalCacheHits">The number of times an update key returned data from the cache.</param>
/// <param name="TotalSuccessCacheMisses">The number of times an update key successfully fetched data from a remote mod site.</param>
/// <param name="TotalErrorCacheMisses">The number of times an update key could not fetch data from a remote mod site (e.g. mod page didn't exist or mod site returned an API error).</param>
/// <param name="ByApiVersion">The number of update-check request by SMAPI version.</param>
/// <param name="ByGameVersion">The number of update-check request by game version.</param>
/// <param name="BySite">The metrics grouped by site.</param>
/// <param name="ByDate">The metrics grouped by UTC date.</param>
internal record MetricsSummary(
    TimeSpan Uptime,
    int SuccessCacheMinutes,
    int ErrorCacheMinutes,
    int TotalApiRequests,
    int UniqueModsChecked,
    int TotalCacheHits,
    int TotalSuccessCacheMisses,
    int TotalErrorCacheMisses,
    IDictionary<string, long> ByApiVersion,
    IDictionary<string, long> ByGameVersion,
    IDictionary<ModSiteKey, MetricsModel> BySite,
    IDictionary<string, ApiMetricsModel> ByDate
);
