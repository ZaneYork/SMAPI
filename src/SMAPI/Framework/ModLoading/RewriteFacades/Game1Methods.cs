#if SMAPI_FOR_MOBILE
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI.Framework.ModLoading.RewriteFacades
{
    public class Game1Methods : Game1
    {
        public static RainDrop[] RainDropsProp => (typeof(RainManager).GetField("_rainDropList", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(RainManager.Instance) as List<RainDrop>).ToArray();

        public static bool IsRainingProp
        {
            get => RainManager.Instance.isRaining;
            set => RainManager.Instance.isRaining = value;
        }

        public static bool IsSnowingProp
        {
            get => WeatherDebrisManager.Instance.isSnowing;
            set => WeatherDebrisManager.Instance.isSnowing = value;
        }

        public static bool IsDebrisWeatherProp
        {
            get => WeatherDebrisManager.Instance.isDebrisWeather;
            set => WeatherDebrisManager.Instance.isDebrisWeather = value;
        }


        public static new IList<IClickableMenu> onScreenMenus => Game1.onScreenMenus;

        public static void updateDebrisWeatherForMovement(List<WeatherDebris> debris)
        {
            WeatherDebrisManager.Instance.UpdateDebrisWeatherForMovement();
        }


        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public static new void warpFarmer(string locationName, int tileX, int tileY, bool flip)
        {
            warpFarmer(locationName, tileX, tileY, flip ? ((player.FacingDirection + 2) % 4) : player.FacingDirection);
        }

        public static void removeSquareDebrisFromTile(int tileX, int tileY)
        {
            Game1.currentLocation.debris.debrisNetCollection.Filter(debris => {
                if ((debris.debrisType == 2) && (((int)(debris.Chunks[0].position.X / 64f)) == tileX))
                {
                    return (debris.chunkFinalYLevel / 0x40) != tileY;
                }
                return true;
            });
        }


        public static void randomizeDebrisWeatherPositions(List<WeatherDebris> debris)
        {
            if (debris != null)
            {
                using (List<WeatherDebris>.Enumerator enumerator = debris.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        enumerator.Current.position = Utility.getRandomPositionOnScreen();
                    }
                }
            }
        }

        public static new void createItemDebris(Item item, Vector2 origin, int direction, GameLocation location = null, int groundLevel = -1)
        {
            Game1.createItemDebris(item, origin, direction, location, groundLevel);
        }
    }
}
#endif
