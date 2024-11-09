using System.Diagnostics.CodeAnalysis;
using StardewModdingAPI.Toolkit.Framework.Clients.ModDropExport.ResponseModels;

namespace StardewModdingAPI.Web.Framework.Caching.ModDropExport;

/// <summary>Manages cached mod data from the ModDrop export API.</summary>
internal interface IModDropExportCacheRepository : IExportCacheRepository
{
    /*********
    ** Methods
    *********/
    /// <summary>Get the cached data for a mod, if it exists in the export.</summary>
    /// <param name="id">The ModDrop mod ID.</param>
    /// <param name="mod">The fetched metadata.</param>
    bool TryGetMod(long id, [NotNullWhen(true)] out ModDropModExport? mod);

    /// <summary>Set the cached data to use.</summary>
    /// <param name="export">The export received from the ModDrop Mods API, or <c>null</c> to remove it.</param>
    void SetData(ModDropFullExport? export);
}
