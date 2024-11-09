using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;
using StardewValley.Locations;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="MineShaft"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class MineShaftFacade : MineShaft, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public new int getRandomGemRichStoneForThisLevel(int level)
    {
        string itemId = base.getRandomGemRichStoneForThisLevel(level);

        return int.TryParse(itemId, out int index)
            ? index
            : Object.mineStoneBrown1Index; // old default value
    }


    /*********
    ** Private methods
    *********/
    private MineShaftFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
