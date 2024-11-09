using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;
using StardewValley.Objects;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="Ring"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class RingFacade : Ring, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public static Ring Constructor(int which)
    {
        return new Ring(which.ToString());
    }

    public virtual bool GetsEffectOfRing(int ring_index)
    {
        return base.GetsEffectOfRing(ring_index.ToString());
    }

    public virtual void onEquip(Farmer who, GameLocation location)
    {
        base.onEquip(who);
    }

    public virtual void onUnequip(Farmer who, GameLocation location)
    {
        base.onUnequip(who);
    }


    /*********
    ** Private methods
    *********/
    private RingFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
