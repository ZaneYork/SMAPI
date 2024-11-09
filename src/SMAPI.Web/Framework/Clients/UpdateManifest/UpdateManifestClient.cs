using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Pathoschild.Http.Client;
using StardewModdingAPI.Toolkit;
using StardewModdingAPI.Toolkit.Framework.UpdateData;
using StardewModdingAPI.Web.Framework.Clients.UpdateManifest.ResponseModels;

namespace StardewModdingAPI.Web.Framework.Clients.UpdateManifest;

/// <summary>An API client for fetching update metadata from an arbitrary JSON URL.</summary>
internal class UpdateManifestClient : IUpdateManifestClient
{
    /*********
    ** Fields
    *********/
    /// <summary>The underlying HTTP client.</summary>
    private readonly IClient Client;


    /*********
    ** Accessors
    *********/
    /// <summary>The unique key for the mod site.</summary>
    public ModSiteKey SiteKey => ModSiteKey.UpdateManifest;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="userAgent">The user agent for the API client.</param>
    public UpdateManifestClient(string userAgent)
    {
        this.Client = new FluentClient()
            .SetUserAgent(userAgent);

        this.Client.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/plain"));
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
        this.Client.Dispose();
    }

    /// <inheritdoc/>
    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract", Justification = "This is the method which ensures the annotations are correct.")]
    public async Task<IModPage?> GetModData(string id)
    {
        // get raw update manifest
        UpdateManifestModel? manifest;
        try
        {
            manifest = await this.Client.GetAsync(id).As<UpdateManifestModel?>();
            if (manifest is null)
                return this.GetFormatError(id, "manifest can't be empty");
        }
        catch (ApiException ex) when (ex.Status == HttpStatusCode.NotFound)
        {
            return new GenericModPage(this.SiteKey, id).SetError(RemoteModStatus.DoesNotExist, $"No update manifest found at {id}");
        }
        catch (Exception ex)
        {
            return this.GetFormatError(id, ex.Message);
        }

        // validate
        if (!SemanticVersion.TryParse(manifest.Format, out _))
            return this.GetFormatError(id, $"invalid format version '{manifest.Format}'");
        foreach (UpdateManifestModModel mod in manifest.Mods.Values)
        {
            if (mod is null)
                return this.GetFormatError(id, "a mod record can't be null");
            if (string.IsNullOrWhiteSpace(mod.ModPageUrl))
                return this.GetFormatError(id, $"all mods must have a {nameof(mod.ModPageUrl)} value");
            foreach (UpdateManifestVersionModel? version in mod.Versions)
            {
                if (version is null)
                    return this.GetFormatError(id, "a version record can't be null");
                if (string.IsNullOrWhiteSpace(version.Version))
                    return this.GetFormatError(id, $"all version records must have a {nameof(version.Version)} field");
                if (!SemanticVersion.TryParse(version.Version, out _))
                    return this.GetFormatError(id, $"invalid mod version '{version.Version}'");
            }
        }

        // build model
        return new UpdateManifestModPage(id, manifest);
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get a mod page instance with an error indicating the update manifest is invalid.</summary>
    /// <param name="url">The full URL to the update manifest.</param>
    /// <param name="reason">A human-readable reason phrase indicating why it's invalid.</param>
    private IModPage GetFormatError(string url, string reason)
    {
        return new GenericModPage(this.SiteKey, url).SetError(RemoteModStatus.InvalidData, $"The update manifest at {url} is invalid ({reason})");
    }
}
