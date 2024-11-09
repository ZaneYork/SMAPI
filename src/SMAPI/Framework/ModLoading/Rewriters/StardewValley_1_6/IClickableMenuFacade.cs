using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="IClickableMenu"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class IClickableMenuFacade : IClickableMenu, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public static void drawHoverText(SpriteBatch b, string text, SpriteFont font, int xOffset = 0, int yOffset = 0, int moneyAmountToDisplayAtBottom = -1, string? boldTitleText = null, int healAmountToDisplay = -1, string[]? buffIconsToDisplay = null, Item? hoveredItem = null, int currencySymbol = 0, int extraItemToShowIndex = -1, int extraItemToShowAmount = -1, int overrideX = -1, int overrideY = -1, float alpha = 1f, CraftingRecipe? craftingIngredients = null, IList<Item>? additional_craft_materials = null)
    {
        IClickableMenu.drawHoverText(
            b: b,
            text: text,
            font: font,
            xOffset: xOffset,
            yOffset: yOffset,
            moneyAmountToDisplayAtBottom: moneyAmountToDisplayAtBottom,
            boldTitleText: boldTitleText,
            healAmountToDisplay: healAmountToDisplay,
            buffIconsToDisplay: buffIconsToDisplay,
            hoveredItem: hoveredItem,
            currencySymbol: currencySymbol,
            extraItemToShowAmount: extraItemToShowAmount,
            extraItemToShowIndex: extraItemToShowIndex != -1 ? extraItemToShowAmount.ToString() : null,
            overrideX: overrideX,
            overrideY: overrideY,
            alpha: alpha,
            craftingIngredients: craftingIngredients,
            additional_craft_materials: additional_craft_materials
        );
    }

    public static void drawHoverText(SpriteBatch b, StringBuilder text, SpriteFont font, int xOffset = 0, int yOffset = 0, int moneyAmountToDisplayAtBottom = -1, string? boldTitleText = null, int healAmountToDisplay = -1, string[]? buffIconsToDisplay = null, Item? hoveredItem = null, int currencySymbol = 0, int extraItemToShowIndex = -1, int extraItemToShowAmount = -1, int overrideX = -1, int overrideY = -1, float alpha = 1f, CraftingRecipe? craftingIngredients = null, IList<Item>? additional_craft_materials = null)
    {
        IClickableMenu.drawHoverText(
            b: b,
            text: text,
            font: font,
            xOffset: xOffset,
            yOffset: yOffset,
            moneyAmountToDisplayAtBottom: moneyAmountToDisplayAtBottom,
            boldTitleText: boldTitleText,
            healAmountToDisplay: healAmountToDisplay,
            buffIconsToDisplay: buffIconsToDisplay,
            hoveredItem: hoveredItem,
            currencySymbol: currencySymbol,
            extraItemToShowAmount: extraItemToShowAmount,
            extraItemToShowIndex: extraItemToShowIndex != -1 ? extraItemToShowAmount.ToString() : null,
            overrideX: overrideX,
            overrideY: overrideY,
            alpha: alpha,
            craftingIngredients: craftingIngredients,
            additional_craft_materials: additional_craft_materials
        );
    }

    public static void drawToolTip(SpriteBatch b, string hoverText, string hoverTitle, Item hoveredItem, bool heldItem = false, int healAmountToDisplay = -1, int currencySymbol = 0, int extraItemToShowIndex = -1, int extraItemToShowAmount = -1, CraftingRecipe? craftingIngredients = null, int moneyAmountToShowAtBottom = -1)
    {
        IClickableMenu.drawToolTip(
            b: b,
            hoverText: hoverText,
            hoverTitle: hoverTitle,
            hoveredItem: hoveredItem,
            heldItem: heldItem,
            healAmountToDisplay: healAmountToDisplay,
            currencySymbol: currencySymbol,
            extraItemToShowIndex: extraItemToShowIndex != -1 ? extraItemToShowAmount.ToString() : null,
            extraItemToShowAmount: extraItemToShowAmount,
            craftingIngredients: craftingIngredients,
            moneyAmountToShowAtBottom: moneyAmountToShowAtBottom
        );
    }


    /*********
    ** Private methods
    *********/
    private IClickableMenuFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
