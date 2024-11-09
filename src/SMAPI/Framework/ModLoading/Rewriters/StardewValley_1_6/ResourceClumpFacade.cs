using Netcode;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley.TerrainFeatures;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="ResourceClump"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class ResourceClumpFacade : ResourceClump, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public NetVector2 tile => base.netTile;


    /*********
    ** Private methods
    *********/
    private ResourceClumpFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
