using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;
using StardewValley.Buildings;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="ShippingBin"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class ShippingBinFacade : ShippingBin, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public void showShipment(Object item, bool playThrowSound = true)
    {
        base.showShipment(item, playThrowSound);
    }


    /*********
    ** Private methods
    *********/
    private ShippingBinFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
