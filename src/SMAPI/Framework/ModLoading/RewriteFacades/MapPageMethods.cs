#if SMAPI_FOR_MOBILE
using StardewValley.Menus;

namespace StardewModdingAPI.Framework.ModLoading.RewriteFacades
{
    public class MapPageMethods : MapPage
    {
        public MapPageMethods(int x, int y, int width, int height)
            : base(x, y, width, height, 1f, 1f)
        {
        }

    }
}
#endif
