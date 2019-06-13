using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;

#pragma warning disable 1591 // missing documentation
namespace StardewModdingAPI.Framework.RewriteFacades
{
    public class TextBoxMethods : TextBox
    {
        public TextBoxMethods(Texture2D textboxTexture, Texture2D caretTexture, SpriteFont font, Color textColor)
            : base(textboxTexture, caretTexture, font, textColor, true, false)
        {

        }

    }
}
