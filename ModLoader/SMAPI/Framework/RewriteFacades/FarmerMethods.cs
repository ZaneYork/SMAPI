using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

#pragma warning disable 1591 // missing documentation
namespace StardewModdingAPI.Framework.RewriteFacades
{
    /// <summary>Provides <see cref="SpriteBatch"/> method signatures that can be injected into mod code for compatibility between Linux/Mac or Windows.</summary>
    /// <remarks>This is public to support SMAPI rewriting and should not be referenced directly by mods.</remarks>
    public class FarmerMethods : Farmer
    {
        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public new bool couldInventoryAcceptThisItem(Item item)
        {
            return base.couldInventoryAcceptThisItem(item, true);
        }
    }
}
