using Netcode;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley.TerrainFeatures;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="LargeTerrainFeature"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class LargeTerrainFeatureFacade : LargeTerrainFeature, IRewriteFacade
{
    /*********
    ** Accessors
    *********/
    public NetVector2 tilePosition => base.netTilePosition;


    /*********
    ** Private methods
    *********/
    private LargeTerrainFeatureFacade()
        : base(false)
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
