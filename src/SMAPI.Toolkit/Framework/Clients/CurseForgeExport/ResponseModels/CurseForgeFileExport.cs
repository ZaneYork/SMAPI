using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace StardewModdingAPI.Toolkit.Framework.Clients.CurseForgeExport.ResponseModels;

/// <summary>The metadata for an uploaded file for a mod from the CurseForge export API.</summary>
public class CurseForgeFileExport
{
    /// <summary>The file identifier.</summary>
    public long Id { get; set; }

    /// <summary>The file's display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>The file internal name.</summary>
    public string? FileName { get; set; }

    /// <summary>The game version for which it was uploaded.</summary>
    public string? GameVersion { get; set; }

    /// <summary>The file release type.</summary>
    public int ReleaseType { get; set; }

    /// <summary>The group the file is listed under, or <c>null</c> if the file predates file groups.</summary>
    public CurseForgeFileGroupType? FileGroupType { get; set; }

    /// <summary>The file version type (e.g. release or beta).</summary>
    public int VersionTypeId { get; set; }

    /// <summary>When the file was uploaded.</summary>
    public DateTimeOffset FileDate { get; set; }

    /// <summary>The extra fields returned by the export API, if any.</summary>
    [JsonExtensionData]
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Used to track any new data provided by the API.")]
    public Dictionary<string, object>? OtherFields;
}
