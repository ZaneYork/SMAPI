using Microsoft.Xna.Framework;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley.Menus;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="DiscreteColorPicker"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class DiscreteColorPickerFacade : DiscreteColorPicker, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public new int getSelectionFromColor(Color c)
    {
#if SMAPI_FOR_MOBILE
        return base.getSelectionFromColor(c);
#else
        return DiscreteColorPicker.getSelectionFromColor(c);
#endif
    }

    public new Color getColorFromSelection(int selection)
    {
#if SMAPI_FOR_MOBILE
        return base.getColorFromSelection(selection);
#else
        return DiscreteColorPicker.getColorFromSelection(selection);
#endif
    }


    /*********
    ** Private methods
    *********/
    public DiscreteColorPickerFacade()
        : base(0, 0)
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
