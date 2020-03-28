using System.Collections.Generic;
using StardewValley;

namespace StardewModdingAPI.Framework.RewriteFacades
{
    public class SaveGameMethods : SaveGame
    {
        public static IEnumerator<int> Save()
        {
            return SaveGame.Save(false, false);
        }
    }
}
