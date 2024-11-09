using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Pathoschild.Http.Client;
using StardewModdingAPI.Toolkit;
using StardewModdingAPI.Toolkit.Framework.Clients.ModDropExport.ResponseModels;
using StardewModdingAPI.Toolkit.Framework.UpdateData;
using StardewModdingAPI.Web.Framework.Caching.ModDropExport;
using StardewModdingAPI.Web.Framework.Clients.ModDrop.ResponseModels;

namespace StardewModdingAPI.Web.Framework.Clients.ModDrop;

/// <summary>An HTTP client for fetching mod metadata from the ModDrop API.</summary>
internal class ModDropClient : IModDropClient
{
    /*********
    ** Fields
    *********/
    /// <summary>The underlying HTTP client.</summary>
    private readonly IClient Client;

    /// <summary>The cached mod data from the ModDrop export API to use if available.</summary>
    private readonly IModDropExportCacheRepository ExportCache;

    /// <summary>The URL for a ModDrop mod page for the user, where {0} is the mod ID.</summary>
    private readonly string ModUrlFormat;


    /*********
    ** Accessors
    *********/
    /// <summary>The unique key for the mod site.</summary>
    public ModSiteKey SiteKey => ModSiteKey.ModDrop;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="userAgent">The user agent for the API client.</param>
    /// <param name="apiUrl">The base URL for the ModDrop API.</param>
    /// <param name="modUrlFormat">The URL for a ModDrop mod page for the user, where {0} is the mod ID.</param>
    /// <param name="exportCache">The cached mod data from the ModDrop export API to use if available.</param>
    public ModDropClient(string userAgent, string apiUrl, string modUrlFormat, IModDropExportCacheRepository exportCache)
    {
        this.Client = new FluentClient(apiUrl).SetUserAgent(userAgent);
        this.ModUrlFormat = modUrlFormat;
        this.ExportCache = exportCache;
    }

    /// <summary>Get update check info about a mod.</summary>
    /// <param name="id">The mod ID.</param>
    [SuppressMessage("ReSharper", "ConditionalAccessQualifierIsNonNullableAccordingToAPIContract", Justification = "The nullability is validated in this method.")]
    public async Task<IModPage?> GetModData(string id)
    {
        IModPage page = new GenericModPage(this.SiteKey, id);

        if (!long.TryParse(id, out long parsedId))
            return page.SetError(RemoteModStatus.DoesNotExist, $"The value '{id}' isn't a valid ModDrop mod ID, must be an integer ID.");

        // To minimize time users spend waiting for the update check result, we fetch the mod
        // from these sources in order of priority:
        //
        // 1. ModDrop export API:
        //    This is a special endpoint provided by ModDrop specifically for SMAPI's update
        //    checks. It returns a cached view of every Stardew Valley mod, so we don't need to
        //    submit separate requests for each mod.
        //
        //   2. ModDrop API:
        //      Though mostly superseded by the export API, this is the fallback if the export
        //      isn't available for some reason.
        return
            (
                this.ExportCache.IsLoaded
                    ? this.GetModFromExportData(parsedId)
                    : await this.GetModFromApiAsync(parsedId)
            )
            ?? this.InitModPage(parsedId).SetError(RemoteModStatus.DoesNotExist, "Found no ModDrop mod with this ID.");
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
        this.Client.Dispose();
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Initialize an empty mod page model.</summary>
    /// <param name="id">The ModDrop mod ID.</param>
    private IModPage InitModPage(long id)
    {
        return new GenericModPage(this.SiteKey, id.ToString());
    }

    /// <summary>Get metadata about a mod by searching the ModDrop export API data.</summary>
    /// <param name="id">The ModDrop mod ID.</param>
    /// <returns>Returns the mod info if found, else <c>null</c>.</returns>
    private IModPage? GetModFromExportData(long id)
    {
        // skip if no data available
        if (!this.ExportCache.IsLoaded || !this.ExportCache.TryGetMod(id, out ModDropModExport? data))
            return null;

        // get downloads
        var downloads = new List<IModDownload>();
        foreach (ModDropFileExport file in data.Files)
        {
            if (file.IsOld || file.IsDeleted || file.IsHidden || !this.TryParseVersionFromFileName(file.Name, file.Version, out ISemanticVersion? version))
                continue;

            downloads.Add(
                new GenericModDownload(file.Name ?? file.Id.ToString(), file.Description, version.ToString())
            );
        }

        // yield info
        return this
            .InitModPage(id)
            .SetInfo(name: data.Title ?? id.ToString(), version: null, url: data.PageUrl ?? this.GetDefaultModPageUrl(id), downloads: downloads.ToArray());
    }

    /// <summary>Get metadata about a mod from the ModDrop API.</summary>
    /// <param name="id">The ModDrop mod ID.</param>
    /// <returns>Returns the mod info if found, else <c>null</c>.</returns>
    private async Task<IModPage?> GetModFromApiAsync(long id)
    {
        // get raw data
        ModListModel response = await this.Client
            .PostAsync("")
            .WithBody(new
            {
                ModIDs = new[] { id },
                Files = true,
                Mods = true
            })
            .As<ModListModel>();

        if (!response.Mods.TryGetValue(id, out ModModel? mod) || mod?.Mod is null)
            return this.InitModPage(id).SetError(RemoteModStatus.DoesNotExist, "Found no ModDrop page with this ID.");
        if (mod.Mod.ErrorCode is not null)
            return this.InitModPage(id).SetError(RemoteModStatus.InvalidData, $"ModDrop returned error code {mod.Mod.ErrorCode} for mod ID '{id}'.");

        // get files
        var downloads = new List<IModDownload>();
        foreach (FileDataModel file in mod.Files)
        {
            if (file.IsOld || file.IsDeleted || file.IsHidden || !this.TryParseVersionFromFileName(file.Name, file.Version, out ISemanticVersion? version))
                continue;

            downloads.Add(
                new GenericModDownload(file.Name, file.Description, version.ToString())
            );
        }

        // return info
        return this
            .InitModPage(id)
            .SetInfo(name: mod.Mod.Title, version: null, url: this.GetDefaultModPageUrl(id), downloads: downloads);
    }

    /// <summary>Extract the version number from a ModDrop file's info, if possible.</summary>
    /// <param name="fileName">The file name.</param>
    /// <param name="fileVersion">The file version provided by ModDrop.</param>
    /// <param name="version">The parsed version number, if valid.</param>
    /// <returns>Returns whether a version number was successfully parsed.</returns>
    private bool TryParseVersionFromFileName(string? fileName, string? fileVersion, [NotNullWhen(true)] out ISemanticVersion? version)
    {
        // ModDrop drops the version prerelease tag if it's not in their whitelist of allowed suffixes. For
        // example, "1.0.0-alpha" is fine but "1.0.0-sdvalpha" will have version field "1.0.0".
        //
        // If the version is non-prerelease but the file's display name contains a prerelease version, parse it
        // out of the name instead.
        if (fileName != null && fileName.Contains(fileVersion + "-") && SemanticVersion.TryParse(fileVersion, out ISemanticVersion? parsedVersion) && !parsedVersion.IsPrerelease())
        {
            string[] parts = fileName.Split(' ');

            if (parts.Length > 1) // can't safely parse name without spaces (e.g. "mod-1.0.0-release" may not be version 1.0.0-release)
            {
                foreach (string part in parts)
                {
                    if (part.StartsWith(fileVersion + "-") && SemanticVersion.TryParse(part, out parsedVersion))
                    {
                        version = parsedVersion;
                        return true;
                    }
                }
            }
        }

        // else the provided version
        return SemanticVersion.TryParse(fileVersion, out version);
    }

    /// <summary>Get the mod page URL for a ModDrop mod.</summary>
    /// <param name="id">The ModDrop mod ID.</param>
    private string GetDefaultModPageUrl(long id)
    {
        return string.Format(this.ModUrlFormat, id);
    }
}
