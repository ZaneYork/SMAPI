#if SMAPI_FOR_MOBILE
using System.Reflection;
using StardewValley.Menus;

#pragma warning disable 1591 // missing documentation
namespace StardewModdingAPI.Framework.ModLoading.RewriteFacades
{
    public class TextBoxMethods
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
    }
}
#endif
