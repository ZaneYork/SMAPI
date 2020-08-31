#if SMAPI_FOR_MOBILE
using System.Reflection;
using StardewValley.Menus;

namespace StardewModdingAPI.Framework.ModLoading.RewriteFacades
{
    public class GameMenuMethods : GameMenu
    {
        public GameMenuMethods(bool playOpeningSound = true) : base()
        {
        }

        public GameMenuMethods(int startingTab, int extra = -1, bool playOpeningSound = true) : base(startingTab, extra)
        {
        }
        public void changeTab(int whichTab, bool playSound = true)
        {
            base.changeTab(whichTab);
        }
    }
}
#endif
