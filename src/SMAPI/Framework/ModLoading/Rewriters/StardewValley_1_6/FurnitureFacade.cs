using Microsoft.Xna.Framework;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;
using StardewValley.Objects;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="Furniture"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class FurnitureFacade : Furniture, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public static Furniture Constructor(int which, Vector2 tile, int initialRotations)
    {
        return new Furniture(which.ToString(), tile, initialRotations);
    }

    public static Furniture Constructor(int which, Vector2 tile)
    {
        return new Furniture(which.ToString(), tile);
    }

    public void AddLightGlow(GameLocation location)
    {
        base.AddLightGlow();
    }

    public void addLights(GameLocation environment)
    {
        base.addLights();
    }

    public static Furniture GetFurnitureInstance(int index, Vector2? position = null)
    {
        return Furniture.GetFurnitureInstance(index.ToString(), position);
    }

    public void removeLights(GameLocation environment)
    {
        base.removeLights();
    }

    public void RemoveLightGlow(GameLocation location)
    {
        base.RemoveLightGlow();
    }

    public void setFireplace(GameLocation location, bool playSound = true, bool broadcast = false)
    {
        base.setFireplace(playSound, broadcast);
    }


    /*********
    ** Private methods
    *********/
    private FurnitureFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
