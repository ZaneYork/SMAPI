using System;
using System.Collections.Generic;
using StardewModdingAPI.Toolkit.Framework.UpdateData;
using StardewModdingAPI.Web.Framework.ConfigModels;

namespace StardewModdingAPI.Web.Framework.Metrics;

/// <summary>Manages in-memory update check metrics since the server was last deployed or restarted.</summary>
internal static class MetricsManager
{
    /*********
    ** Fields
    *********/
    /// <summary>The date/time format used to generate hourly metrics keys.</summary>
    private const string HourlyKeyFormat = "yyyy-MM-dd HH:*";

    /// <summary>The length of the date-only prefix in <see cref="HourlyKeyFormat"/>.</summary>
    private const int DateOnlyKeyLength = 10;

    /// <summary>The tracked metrics.</summary>
    private static readonly IDictionary<string, ApiMetricsModel> Metrics = new Dictionary<string, ApiMetricsModel>();

    /// <summary>When the server began tracking metrics.</summary>
    private static readonly DateTimeOffset MetricsTrackedSince = DateTimeOffset.UtcNow;


    /*********
    ** Public methods
    *********/
    /// <summary>Get the metrics model for the current hour.</summary>
    public static ApiMetricsModel GetMetricsForNow()
    {
        string key = $"{DateTimeOffset.UtcNow.ToString(HourlyKeyFormat)}";

        if (!MetricsManager.Metrics.TryGetValue(key, out ApiMetricsModel? metrics))
            MetricsManager.Metrics[key] = metrics = new ApiMetricsModel();

        return metrics;
    }

    /// <summary>Get a summary of the metrics collected since the server started.</summary>
    /// <param name="config">The update-check settings.</param>
    public static MetricsSummary GetSummary(ModUpdateCheckConfig config)
    {
        // get aggregate stats
        int totalRequests = 0;
        var totals = new MetricsModel();
        var bySite = new Dictionary<ModSiteKey, MetricsModel>();
        var byDate = new Dictionary<string, ApiMetricsModel>();
        var byApiVersion = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        var byGameVersion = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach ((string hourlyKey, ApiMetricsModel hourly) in MetricsManager.Metrics)
        {
            // totals
            totalRequests += hourly.ApiRequests;
            foreach (MetricsModel site in hourly.Sites.Values)
                totals.AggregateFrom(site);

            // by site
            foreach ((ModSiteKey site, MetricsModel fromMetrics) in hourly.Sites)
            {
                if (!bySite.TryGetValue(site, out MetricsModel? metrics))
                    bySite[site] = metrics = new MetricsModel();

                metrics.AggregateFrom(fromMetrics);
            }

            // by date
            string dailyKey = hourlyKey[..MetricsManager.DateOnlyKeyLength];
            if (!byDate.TryGetValue(dailyKey, out ApiMetricsModel? daily))
                byDate[dailyKey] = daily = new ApiMetricsModel();

            // by version
            foreach ((string apiVersion, long count) in hourly.ByApiVersion)
                byApiVersion[apiVersion] = byApiVersion.GetValueOrDefault(apiVersion) + count;
            foreach ((string gameVersion, long count) in hourly.ByGameVersion)
                byGameVersion[gameVersion] = byGameVersion.GetValueOrDefault(gameVersion) + count;

            daily.AggregateFrom(hourly);
        }

        return new MetricsSummary(
            Uptime: DateTimeOffset.UtcNow - MetricsTrackedSince,
            SuccessCacheMinutes: config.SuccessCacheMinutes,
            ErrorCacheMinutes: config.ErrorCacheMinutes,
            TotalApiRequests: totalRequests,
            UniqueModsChecked: totals.UniqueModsChecked,
            TotalCacheHits: totals.CacheHits,
            TotalSuccessCacheMisses: totals.SuccessCacheMisses,
            TotalErrorCacheMisses: totals.ErrorCacheMisses,
            ByApiVersion: byApiVersion,
            ByGameVersion: byGameVersion,
            BySite: bySite,
            ByDate: byDate
        );
    }
}
