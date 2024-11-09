using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;
using StardewValley.Objects;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="Cask"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class CaskFacade : Cask, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public bool IsValidCaskLocation(GameLocation location)
    {
        return base.IsValidCaskLocation();
    }


    /*********
    ** Private methods
    *********/
    private CaskFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
