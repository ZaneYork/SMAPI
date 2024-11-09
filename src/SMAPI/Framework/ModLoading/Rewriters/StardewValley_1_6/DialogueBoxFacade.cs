using System.Collections.Generic;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="DialogueBox"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class DialogueBoxFacade : DialogueBox, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public static DialogueBox Constructor(string dialogue, List<Response> responses, int width = 1200)
    {
        return new DialogueBox(dialogue, responses.ToArray(), width);
    }


    /*********
    ** Private methods
    *********/
    public DialogueBoxFacade()
        : base(null as string)
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
