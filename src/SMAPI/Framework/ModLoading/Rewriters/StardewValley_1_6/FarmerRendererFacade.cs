using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="FarmerRenderer"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class FarmerRendererFacade : FarmerRenderer, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public void recolorShoes(int which)
    {
        base.recolorShoes(which.ToString());
    }


    /*********
    ** Private methods
    *********/
    private FarmerRendererFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
