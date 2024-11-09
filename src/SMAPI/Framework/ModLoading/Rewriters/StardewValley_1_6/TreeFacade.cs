using Microsoft.Xna.Framework;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="Tree"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class TreeFacade : Tree, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public static Tree Constructor(int which)
    {
        return new Tree(which.ToString());
    }

    public static Tree Constructor(int which, int growthStage)
    {
        return new Tree(which.ToString(), growthStage);
    }

    public bool fertilize(GameLocation location)
    {
        return base.fertilize();
    }

    public bool instantDestroy(Vector2 tileLocation, GameLocation location)
    {
        return base.instantDestroy(tileLocation);
    }


    /*********
    ** Private methods
    *********/
    private TreeFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
