using System.Diagnostics.CodeAnalysis;
using StardewModdingAPI.Toolkit.Framework.Clients.NexusExport.ResponseModels;

namespace StardewModdingAPI.Web.Framework.Caching.NexusExport;

/// <summary>Manages cached mod data from the Nexus export API.</summary>
internal interface INexusExportCacheRepository : IExportCacheRepository
{
    /*********
    ** Methods
    *********/
    /// <summary>Get the cached data for a mod, if it exists in the export.</summary>
    /// <param name="id">The Nexus mod ID.</param>
    /// <param name="mod">The fetched metadata.</param>
    bool TryGetMod(uint id, [NotNullWhen(true)] out NexusModExport? mod);

    /// <summary>Set the cached data to use.</summary>
    /// <param name="export">The export received from the Nexus Mods API, or <c>null</c> to remove it.</param>
    void SetData(NexusFullExport? export);
}
