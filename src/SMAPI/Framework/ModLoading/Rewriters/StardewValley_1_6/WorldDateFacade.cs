using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="WorldDate"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class WorldDateFacade : WorldDate, IRewriteFacade
{
    /*********
    ** Accessors
    *********/
    public new string Season
    {
        get => base.SeasonKey;
        set => base.SeasonKey = value;
    }


    /*********
    ** Private methods
    *********/
    private WorldDateFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
