using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley.Locations;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="BoatTunnel"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class BoatTunnelFacade : BoatTunnel, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public int GetTicketPrice()
    {
        return base.TicketPrice;
    }


    /*********
    ** Private methods
    *********/
    private BoatTunnelFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
