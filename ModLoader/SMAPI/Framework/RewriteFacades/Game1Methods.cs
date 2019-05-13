using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

#pragma warning disable 1591 // missing documentation
namespace StardewModdingAPI.Framework.RewriteFacades
{
    /// <summary>Provides <see cref="SpriteBatch"/> method signatures that can be injected into mod code for compatibility between Linux/Mac or Windows.</summary>
    /// <remarks>This is public to support SMAPI rewriting and should not be referenced directly by mods.</remarks>
    public class Game1Methods : Game1
    {
        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public static new string parseText(string text, SpriteFont whichFont, int width)
        {
            return parseText(text, whichFont, width, 1);
        }

        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public static new void warpFarmer(LocationRequest locationRequest, int tileX, int tileY, int facingDirectionAfterWarp)
        {
            warpFarmer(locationRequest, tileX, tileY, facingDirectionAfterWarp, true, false);
        }
        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public static new void warpFarmer(string locationName, int tileX, int tileY, bool flip)
        {
            warpFarmer(locationName, tileX, tileY, flip ? ((player.FacingDirection + 2) % 4) : player.FacingDirection);
        }

        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public static new void warpFarmer(string locationName, int tileX, int tileY, int facingDirectionAfterWarp)
        {
            warpFarmer(locationName, tileX, tileY, facingDirectionAfterWarp, false, true, false);
        }
    }
}
