using System.Diagnostics.CodeAnalysis;
using StardewModdingAPI.Toolkit.Framework.Clients.CurseForgeExport.ResponseModels;

namespace StardewModdingAPI.Web.Framework.Caching.CurseForgeExport;

/// <summary>Manages cached mod data from the CurseForge export API.</summary>
internal interface ICurseForgeExportCacheRepository : IExportCacheRepository
{
    /*********
    ** Methods
    *********/
    /// <summary>Get the cached data for a mod, if it exists in the export.</summary>
    /// <param name="id">The CurseForge mod ID.</param>
    /// <param name="mod">The fetched metadata.</param>
    bool TryGetMod(uint id, [NotNullWhen(true)] out CurseForgeModExport? mod);

    /// <summary>Set the cached data to use.</summary>
    /// <param name="export">The export received from the CurseForge Mods API, or <c>null</c> to remove it.</param>
    void SetData(CurseForgeFullExport? export);
}
