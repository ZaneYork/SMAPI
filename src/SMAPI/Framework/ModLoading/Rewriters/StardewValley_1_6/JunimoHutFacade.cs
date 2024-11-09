using Netcode;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6.Internal;
using StardewValley.Buildings;
using StardewValley.Objects;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="JunimoHut"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class JunimoHutFacade : JunimoHut, IRewriteFacade
{
    /*********
    ** Accessors
    *********/
    public NetRef<Chest> output => NetRefWrapperCache<Chest>.GetCachedWrapperFor(base.GetBuildingChest("Output"));


    /*********
    ** Private methods
    *********/
    private JunimoHutFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
