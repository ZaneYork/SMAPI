using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;
using StardewValley.Objects;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="Boots"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class BootsFacade : Boots, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public static Boots Constructor(int which)
    {
        return new Boots(which.ToString());
    }

    public virtual void onEquip()
    {
        base.onEquip(Game1.player);
    }

    public virtual void onUnequip()
    {
        base.onUnequip(Game1.player);
    }


    /*********
    ** Private methods
    *********/
    private BootsFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
