using System;
using System.Collections.Generic;
using StardewModdingAPI.Toolkit.Framework.UpdateData;

namespace StardewModdingAPI.Web.Framework.Metrics;

/// <summary>The metrics tracked for a specific block of time.</summary>
internal class ApiMetricsModel
{
    /*********
    ** Accessors
    *********/
    /// <summary>The total number of update-check requests received by the API (each of which may include multiple update keys).</summary>
    public int ApiRequests { get; private set; }

    /// <summary>The metrics by mod site.</summary>
    public Dictionary<ModSiteKey, MetricsModel> Sites { get; } = new();

    /// <summary>The number of update-check requests by SMAPI version.</summary>
    public Dictionary<string, long> ByApiVersion { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The number of update-check requests by game version.</summary>
    public Dictionary<string, long> ByGameVersion { get; } = new(StringComparer.OrdinalIgnoreCase);


    /*********
    ** Public methods
    *********/
    /// <summary>Track an update-check request received by the API.</summary>
    /// <param name="apiVersion">The SMAPI version installed by the player.</param>
    /// <param name="gameVersion">The game version installed by the player.</param>
    public void TrackRequest(ISemanticVersion? apiVersion, ISemanticVersion? gameVersion)
    {
        this.ApiRequests++;

        string apiVersionStr = apiVersion?.ToString() ?? "<none specified>";
        string gameVersionStr = gameVersion?.ToString() ?? "<none specified>";

        this.ByApiVersion[apiVersionStr] = this.ByApiVersion.GetValueOrDefault(apiVersionStr) + 1;
        this.ByGameVersion[gameVersionStr] = this.ByGameVersion.GetValueOrDefault(gameVersionStr) + 1;
    }

    /// <summary>Track the update-check result for a specific update key.</summary>
    /// <param name="updateKey">The update key that was requested.</param>
    /// <param name="wasCached">Whether the data was returned from the cache; else it was fetched from the remote modding site.</param>
    /// <param name="wasSuccessful">Whether the data was fetched successfully from the remote modding site.</param>
    public void TrackUpdateKey(UpdateKey updateKey, bool wasCached, bool wasSuccessful)
    {
        MetricsModel siteMetrics = this.GetSiteMetrics(updateKey.Site);
        siteMetrics.TrackUpdateKey(updateKey, wasCached, wasSuccessful);
    }

    /// <summary>Merge the values from another metrics model into this one.</summary>
    /// <param name="other">The metrics to merge into this model.</param>
    public void AggregateFrom(ApiMetricsModel other)
    {
        this.ApiRequests += other.ApiRequests;

        foreach ((ModSiteKey site, var otherSiteMetrics) in other.Sites)
        {
            var siteMetrics = this.GetSiteMetrics(site);
            siteMetrics.AggregateFrom(otherSiteMetrics);
        }
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get the metrics for a site key, adding it if needed.</summary>
    /// <param name="site">The mod site key.</param>
    private MetricsModel GetSiteMetrics(ModSiteKey site)
    {
        if (!this.Sites.TryGetValue(site, out MetricsModel? siteMetrics))
            this.Sites[site] = siteMetrics = new MetricsModel();

        return siteMetrics;
    }
}
