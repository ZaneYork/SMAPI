using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.BellsAndWhistles;

#pragma warning disable 1591 // missing documentation
namespace StardewModdingAPI.Framework.RewriteFacades
{
    public class SpriteTextMethods : SpriteText
    {
        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public new static void drawStringHorizontallyCenteredAt(SpriteBatch b, string s, int x, int y, int characterPosition = 0xf423f, int width = -1, int height = 0xf423f, float alpha = 1f, float layerDepth = 0.088f, bool junimoText = false, int color = -1, int maxWidth = 0x1869f)
        {
            drawString(b, s, x - (getWidthOfString(s) / 2), y, characterPosition, width, height, alpha, layerDepth, junimoText, -1, "", color);
        }

        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public new static int getWidthOfString(string s, int widthConstraint = 999999)
        {
            return getWidthOfString(s);
        }

        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public new static void drawStringWithScrollBackground(SpriteBatch b, string s, int x, int y,
            string placeHolderWidthText = "", float alpha = 1f, int color = -1)
        {
            drawStringWithScrollBackground(b, s, x, y, placeHolderWidthText, alpha, color, 0.088f);
        }
    }
}
