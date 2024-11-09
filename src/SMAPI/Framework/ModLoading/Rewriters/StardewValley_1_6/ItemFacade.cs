using Microsoft.Xna.Framework;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="Item"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public abstract class ItemFacade : Item, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public virtual bool canBePlacedHere(GameLocation l, Vector2 tile)
    {
        return base.canBePlacedHere(l, tile);
    }


    /*********
    ** Private methods
    *********/
    private ItemFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
