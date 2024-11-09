namespace StardewModdingAPI.Toolkit.Framework.Clients.CurseForgeExport.ResponseModels;

/// <summary>The metadata for a user who manages a mod from the CurseForge export API.</summary>
public class CurseForgeAuthorExport
{
    /// <summary>The author's user ID.</summary>
    public uint Id { get; set; }

    /// <summary>The author's display name.</summary>
    public string? Name { get; set; }
}
