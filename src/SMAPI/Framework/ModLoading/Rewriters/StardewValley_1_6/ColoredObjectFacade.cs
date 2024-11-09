using Microsoft.Xna.Framework;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley.Objects;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="ColoredObject"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class ColoredObjectFacade : ColoredObject, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public static ColoredObject Constructor(int parentSheetIndex, int stack, Color color)
    {
        return new ColoredObject(parentSheetIndex.ToString(), stack, color);
    }


    /*********
    ** Private methods
    *********/
    private ColoredObjectFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
