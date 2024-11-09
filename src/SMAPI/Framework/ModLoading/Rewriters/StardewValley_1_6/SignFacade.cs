using Microsoft.Xna.Framework;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley.Objects;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="Sign"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class SignFacade : Sign, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public static Sign Constructor(Vector2 tile, int which)
    {
        return new Sign(tile, which.ToString());
    }


    /*********
    ** Private methods
    *********/
    private SignFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
