using Netcode;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley.Network;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="NetWorldState"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class NetWorldStateFacade : NetWorldState, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public new NetIntDelta MiniShippingBinsObtained => base.miniShippingBinsObtained;
    public new NetIntDelta GoldenWalnutsFound => base.goldenWalnutsFound;
    public new NetIntDelta GoldenWalnuts => base.goldenWalnuts;
    public new NetBool GoldenCoconutCracked => base.goldenCoconutCracked;
    public new NetBool ParrotPlatformsUnlocked => base.parrotPlatformsUnlocked;
    public new NetIntDelta LostBooksFound => base.lostBooksFound;


    /*********
    ** Private methods
    *********/
    private NetWorldStateFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
