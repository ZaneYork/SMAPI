using Microsoft.Xna.Framework;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley.Objects;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="FishTankFurniture"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class FishTankFurnitureFacade : FishTankFurniture, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public static FishTankFurniture Constructor(int which, Vector2 tile, int initialRotations)
    {
        return new FishTankFurniture(which.ToString(), tile, initialRotations);
    }

    public static FishTankFurniture Constructor(int which, Vector2 tile)
    {
        return new FishTankFurniture(which.ToString(), tile);
    }


    /*********
    ** Private methods
    *********/
    private FishTankFurnitureFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
