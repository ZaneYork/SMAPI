using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;

namespace StardewModdingAPI.Framework.ModLoading.RewriteFacades;

public class OptionsElementMethods : OptionsElement
{
    public OptionsElementMethods(string label) : base(label)
    {
    }

    public virtual void draw(SpriteBatch b, int slotX, int slotY, IClickableMenu context = null)
    {
        base.draw(b, slotX, slotY);
    }
}
