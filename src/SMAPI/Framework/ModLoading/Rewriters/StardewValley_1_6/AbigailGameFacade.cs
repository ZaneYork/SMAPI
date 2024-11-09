using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;
using StardewValley.Minigames;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="AbigailGame"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class AbigailGameFacade : AbigailGame, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public static AbigailGame Constructor(bool playingWithAbby = false)
    {
        return new AbigailGame(
            playingWithAbby
                ? Game1.getCharacterFromName("Abigail")
                : null
        );
    }


    /*********
    ** Private methods
    *********/
    private AbigailGameFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
