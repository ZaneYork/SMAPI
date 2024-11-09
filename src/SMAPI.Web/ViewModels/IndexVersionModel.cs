namespace StardewModdingAPI.Web.ViewModels;

/// <summary>The fields for a SMAPI version.</summary>
public class IndexVersionModel
{
    /*********
    ** Accessors
    *********/
    /// <summary>The release version.</summary>
    public string Version { get; }

    /// <summary>The Markdown description for the release.</summary>
    public string Description { get; }

    /// <summary>The URL to the download page.</summary>
    public string WebUrl { get; }

    /// <summary>The direct download URL for the main version.</summary>
    public string DownloadUrl { get; }

    /// <summary>The direct download URL for the for-developers version. Not applicable for prerelease versions.</summary>
    public string? DevDownloadUrl { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="version">The release number.</param>
    /// <param name="description">The Markdown description for the release.</param>
    /// <param name="webUrl">The URL to the download page.</param>
    /// <param name="downloadUrl">The direct download URL for the main version.</param>
    /// <param name="devDownloadUrl">The direct download URL for the for-developers version. Not applicable for prerelease versions.</param>
    internal IndexVersionModel(string version, string description, string webUrl, string downloadUrl, string? devDownloadUrl)
    {
        this.Version = version;
        this.Description = description;
        this.WebUrl = webUrl;
        this.DownloadUrl = downloadUrl;
        this.DevDownloadUrl = devDownloadUrl;
    }
}
