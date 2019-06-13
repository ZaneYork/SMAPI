using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

#pragma warning disable 1591 // missing documentation
namespace StardewModdingAPI.Framework.RewriteFacades
{
    public class ItemGrabMenuMethods : ItemGrabMenu
    {
        public ItemGrabMenuMethods(IList<Item> inventory, bool reverseGrab, bool showReceivingMenu,
            InventoryMenu.highlightThisItem highlightFunction, behaviorOnItemSelect behaviorOnItemSelectFunction,
            string message, behaviorOnItemSelect behaviorOnItemGrab = null, bool snapToBottom = false,
            bool canBeExitedWithKey = false, bool playRightClickSound = true, bool allowRightClick = true,
            bool showOrganizeButton = false, int source = 0, Item sourceItem = null, int whichSpecialButton = -1,
            object context = null) : base(inventory, reverseGrab, showReceivingMenu, highlightFunction, behaviorOnItemSelectFunction, message,
                behaviorOnItemGrab, snapToBottom, canBeExitedWithKey, playRightClickSound, allowRightClick, showOrganizeButton, source, sourceItem,
                whichSpecialButton)
        {

        }

    }
}
