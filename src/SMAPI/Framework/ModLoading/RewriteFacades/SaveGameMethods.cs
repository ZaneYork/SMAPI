#if SMAPI_FOR_MOBILE
using System.Collections.Generic;
using StardewValley;

namespace StardewModdingAPI.Framework.ModLoading.RewriteFacades
{
    public class SaveGameMethods : SaveGame
    {
        public static IEnumerator<int> Save()
        {
            return SaveGame.Save(false, false);
        }
    }
}
#endif
