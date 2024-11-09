using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley.GameData.FishPonds;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.6.8's <see cref="FishPondReward"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class FishPondRewardFacade : FishPondReward, IRewriteFacade
{
    /*********
    ** Accessors
    *********/
    public new string ItemId
    {
        get => base.ItemId;
        set => base.ItemId = value;
    }

    public new int MinQuantity
    {
        get => base.MinStack;
        set => base.MinStack = value;
    }

    public new int MaxQuantity
    {
        get => base.MaxStack;
        set => base.MaxStack = value;
    }


    /*********
    ** Private methods
    *********/
    private FishPondRewardFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
