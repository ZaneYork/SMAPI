using Microsoft.Xna.Framework;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;
using StardewValley.BellsAndWhistles;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="Butterfly"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class ButterflyFacade : Butterfly, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public static Butterfly Constructor(Vector2 position, bool islandButterfly = false)
    {
        return new Butterfly(Game1.currentLocation, position, islandButterfly);
    }


    /*********
    ** Private methods
    *********/
    private ButterflyFacade()
        : base(null, Vector2.Zero)
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
