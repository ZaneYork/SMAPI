using Microsoft.Xna.Framework;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley.Objects;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="TV"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class TvFacade : TV, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public static TV Constructor(int which, Vector2 tile)
    {
        return new TV(which.ToString(), tile);
    }


    /*********
    ** Private methods
    *********/
    private TvFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
