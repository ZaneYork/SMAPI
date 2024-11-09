using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="ProfileMenu"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class ProfileMenuFacade : ProfileMenu, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public Character? GetCharacter()
    {
        return base.Current?.Character;
    }


    /*********
    ** Private methods
    *********/
    private ProfileMenuFacade()
        : base(null, null)
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
