using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley.Objects;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="Clothing"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class ClothingFacade : Clothing, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public static Clothing Constructor(int item_index)
    {
        return new Clothing(item_index.ToString());
    }


    /*********
    ** Private methods
    *********/
    private ClothingFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
