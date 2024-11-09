using Microsoft.Xna.Framework;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="HoeDirt"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class HoeDirtFacade : HoeDirt, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public bool canPlantThisSeedHere(int objectIndex, int tileX, int tileY, bool isFertilizer = false)
    {
        return base.canPlantThisSeedHere(objectIndex.ToString(), isFertilizer);
    }

    public void destroyCrop(Vector2 tileLocation, bool showAnimation, GameLocation location)
    {
        base.destroyCrop(showAnimation);
    }

    public Rectangle GetFertilizerSourceRect(int fertilizer)
    {
        return base.GetFertilizerSourceRect();
    }

    public bool paddyWaterCheck(GameLocation location, Vector2 tile_location)
    {
        return base.paddyWaterCheck();
    }

    public bool plant(int index, int tileX, int tileY, Farmer who, bool isFertilizer, GameLocation location)
    {
        return base.plant(index.ToString(), who, isFertilizer);
    }

    public void updateNeighbors(GameLocation loc, Vector2 tilePos)
    {
        base.updateNeighbors();
    }


    /*********
    ** Private methods
    *********/
    private HoeDirtFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
