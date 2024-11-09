using System;
using System.Collections.Generic;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="FishingRod"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class FishingRodFacade : FishingRod, IRewriteFacade
{
    /*********
    ** Accessors
    *********/
    public bool caughtDoubleFish
    {
        get => base.numberOfFishCaught > 1;
        set => base.numberOfFishCaught = value ? Math.Max(2, base.numberOfFishCaught) : 1;
    }


    /*********
    ** Public methods
    *********/
    public int getBaitAttachmentIndex()
    {
        return int.TryParse(base.GetBait()?.ItemId, out int index)
            ? index
            : -1;
    }

    public int getBobberAttachmentIndex()
    {
        List<SObject?>? tackle = base.GetTackle();

        return tackle?.Count > 0 && int.TryParse(tackle[0]?.ItemId, out int index)
            ? index
            : -1;
    }

    public void pullFishFromWater(int whichFish, int fishSize, int fishQuality, int fishDifficulty, bool treasureCaught, bool wasPerfect, bool fromFishPond, bool caughtDouble = false, string itemCategory = "Object")
    {
        base.pullFishFromWater(whichFish.ToString(), fishSize, fishQuality, fishDifficulty, treasureCaught, wasPerfect, fromFishPond, null, false, caughtDouble ? 2 : 1);
    }


    /*********
    ** Private methods
    *********/
    private FishingRodFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
