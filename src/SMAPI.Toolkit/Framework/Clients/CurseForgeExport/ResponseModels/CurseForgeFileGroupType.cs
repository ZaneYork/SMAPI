namespace StardewModdingAPI.Toolkit.Framework.Clients.CurseForgeExport.ResponseModels;

/// <summary>The group a file is listed under.</summary>
public enum CurseForgeFileGroupType
{
    /// <summary>Unknown or invalid group. This is usually a file which predates file group types.</summary>
    None = 0,

    /// <summary>A primary download for the mod (e.g. the version most players should install).</summary>
    Main = 1,

    /// <summary>An optional secondary download.</summary>
    Optional = 2,

    /// <summary>An old version of the mod that was originally in the <see cref="Main"/> category.</summary>
    OldMain = 3,

    /// <summary>An old version of the mod that was originally in the <see cref="Optional"/> category.</summary>
    OldOptional = 4
}
