using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley.Tools;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="Slingshot"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class SlingshotFacade : Slingshot, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public static Slingshot Constructor(int which = 32)
    {
        return new Slingshot(which.ToString());
    }


    /*********
    ** Private methods
    *********/
    private SlingshotFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
