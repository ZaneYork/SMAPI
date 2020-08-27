#if SMAPI_FOR_MOBILE
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI.Framework.ModLoading.RewriteFacades
{
    public class IClickableMenuMethods : IClickableMenu
    {
        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public static new void drawHoverText(SpriteBatch b, string text, SpriteFont font, int xOffset = 0, int yOffset = 0, int moneyAmounttoDisplayAtBottom = -1, string boldTitleText = null, int healAmountToDisplay = -1, string[] buffIconsToDsiplay = null, Item hoveredItem = null, int currencySymbol = 0, int extraItemToShowIndex = -1, int extraItemToShowAmount = -1, int overideX = -1, int overrideY = -1, float alpha = 1, CraftingRecipe craftingIngrediants = null, IList<Item> additional_craft_materials = null)
        {
            drawHoverText(b, text, font, xOffset, yOffset, moneyAmounttoDisplayAtBottom, boldTitleText, healAmountToDisplay, buffIconsToDsiplay, hoveredItem, currencySymbol, extraItemToShowIndex, extraItemToShowAmount, overideX, overrideY, alpha, craftingIngrediants, -1, 80, -1, additional_craft_materials);
        }

        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public new void drawHorizontalPartition(SpriteBatch b, int yPosition, bool small = false, int red = -1, int green = -1, int blue = -1)
        {
            base.drawMobileHorizontalPartition(b, 0, yPosition, 64, small);
        }

        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public new void drawVerticalUpperIntersectingPartition(SpriteBatch b, int xPosition, int partitionHeight, int red = -1, int green = -1, int blue = -1)
        {
            base.drawVerticalUpperIntersectingPartition(b, xPosition, partitionHeight);
        }

        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public new void drawVerticalIntersectingPartition(SpriteBatch b, int xPosition, int yPosition, int red = -1, int green = -1, int blue = -1)
        {
            base.drawVerticalIntersectingPartition(b, xPosition, yPosition);
        }

        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public new void drawVerticalPartition(SpriteBatch b, int xPosition, bool small = false, int red = -1, int green = -1, int blue = -1)
        {
            base.drawVerticalPartition(b, xPosition, small);
        }

    }
}
#endif
