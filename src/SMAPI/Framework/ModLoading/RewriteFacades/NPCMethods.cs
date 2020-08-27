#if SMAPI_FOR_MOBILE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StardewValley;

namespace StardewModdingAPI.Framework.ModLoading.RewriteFacades
{
    public class NPCMethods : NPC
    {
        public void reloadSprite()
        {
            base.reloadSprite(Game1.emergencyLoading);
        }
        public void checkSchedule(int timeOfDay)
        {
            base.checkSchedule(timeOfDay, Game1.emergencyLoading);
        }
    }
}
#endif
