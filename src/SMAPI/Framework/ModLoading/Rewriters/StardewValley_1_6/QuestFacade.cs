using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley.Quests;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="Quest"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class QuestFacade : Quest, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public static Quest getQuestFromId(int id)
    {
        return Quest.getQuestFromId(id.ToString());
    }


    /*********
    ** Private methods
    *********/
    private QuestFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
