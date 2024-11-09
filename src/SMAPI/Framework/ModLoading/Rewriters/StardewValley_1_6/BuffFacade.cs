using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;
using StardewValley.Buffs;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="Buff"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class BuffFacade : Buff, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public static Buff Constructor(string description, int millisecondsDuration, string source, int index)
    {
        return new Buff(index.ToString(), source, description: description, duration: millisecondsDuration, iconSheetIndex: index);
    }

    public static Buff Constructor(int which)
    {
        return new Buff(which.ToString());
    }

    public static Buff Constructor(int farming, int fishing, int mining, int digging, int luck, int foraging, int crafting, int maxStamina, int magneticRadius, int speed, int defense, int attack, int minutesDuration, string source, string displaySource)
    {
        return new Buff(
            null,
            source,
            displaySource,
            duration: minutesDuration / Game1.realMilliSecondsPerGameMinute,
            effects: new BuffEffects { FarmingLevel = { farming }, FishingLevel = { fishing }, MiningLevel = { mining }, LuckLevel = { luck }, ForagingLevel = { foraging }, MaxStamina = { maxStamina }, MagneticRadius = { magneticRadius }, Speed = { speed }, Defense = { defense }, Attack = { attack } }
        );
    }

    public void addBuff()
    {
        Game1.player.buffs.Apply(this);
    }

    public void removeBuff()
    {
        Game1.player.buffs.Remove(base.id);
    }


    /*********
    ** Private methods
    *********/
    private BuffFacade()
        : base(null)
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
