using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;

#pragma warning disable 1591 // missing documentation
namespace StardewModdingAPI.Framework.ModLoading.RewriteFacades
{
    public class TextBoxMethods : TextBox
    {
        public static void SelectedSetter(TextBox textBox, bool value)
        {
            if(!textBox.Selected && value)
            {
                typeof(TextBox).GetMethod("ShowAndroidKeyboard", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(textBox, new object[] { });
                textBox.Selected = value;
            }
            else
                textBox.Selected = value;
        }

        public TextBoxMethods(Texture2D textboxTexture, Texture2D caretTexture, SpriteFont font, Color textColor)
            : base(textboxTexture, caretTexture, font, textColor, true, false)
        {
        }
    }
}
