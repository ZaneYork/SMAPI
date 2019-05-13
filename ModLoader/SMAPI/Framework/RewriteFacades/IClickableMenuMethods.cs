using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

#pragma warning disable 1591 // missing documentation
namespace StardewModdingAPI.Framework.RewriteFacades
{
    /// <summary>Provides <see cref="SpriteBatch"/> method signatures that can be injected into mod code for compatibility between Linux/Mac or Windows.</summary>
    /// <remarks>This is public to support SMAPI rewriting and should not be referenced directly by mods.</remarks>
    public class IClickableMenuMethods : IClickableMenu
    {
        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public static new void drawHoverText(SpriteBatch b, string text, SpriteFont font, int xOffset = 0, int yOffset = 0, int moneyAmountToDisplayAtBottom = -1, string boldTitleText = null, int healAmountToDisplay = -1, string[] buffIconsToDisplay = null, Item hoveredItem = null, int currencySymbol = 0, int extraItemToShowIndex = -1, int extraItemToShowAmount = -1, int overrideX = -1, int overrideY = -1, float alpha = 1, CraftingRecipe craftingIngredients = null)
        {
            drawHoverText(b, text, font, xOffset, yOffset, moneyAmountToDisplayAtBottom, boldTitleText, healAmountToDisplay, buffIconsToDisplay, hoveredItem, currencySymbol, extraItemToShowIndex, extraItemToShowAmount, overrideX, overrideY, alpha, craftingIngredients, -1, 80, -1);
        }
        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public static new void drawTextureBox(SpriteBatch b, Texture2D texture, Microsoft.Xna.Framework.Rectangle sourceRect, int x, int y, int width, int height, Color color)
        {
            drawTextureBox(b, texture, sourceRect, x, y, width, height, color, 1, true, false);
        }
        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public static new void drawTextureBox(SpriteBatch b, Texture2D texture, Microsoft.Xna.Framework.Rectangle sourceRect, int x, int y, int width, int height, Color color, float scale)
        {
            drawTextureBox(b, texture, sourceRect, x, y, width, height, color, scale, true, false);
        }
        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public static new void drawTextureBox(SpriteBatch b, Texture2D texture, Microsoft.Xna.Framework.Rectangle sourceRect, int x, int y, int width, int height, Color color, float scale, bool drawShadow)
        {
            drawTextureBox(b, texture, sourceRect, x, y, width, height, color, scale, drawShadow, false);
        }
    }
}
