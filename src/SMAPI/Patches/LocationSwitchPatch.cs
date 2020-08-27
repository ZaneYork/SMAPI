#if SMAPI_FOR_MOBILE
using System;
using System.Diagnostics.CodeAnalysis;
#if HARMONY_2
using HarmonyLib;
#else
using Harmony;
#endif
using StardewModdingAPI.Framework.Patching;
using StardewValley;
using StardewValley.Characters;

namespace StardewModdingAPI.Patches
{
    /// <summary>A Harmony patch for <see cref="StardewValley.Game1"/> which detect location switch and add tapToMove if possible.</summary>
    /// <remarks>Patch methods must be static for Harmony to work correctly. See the Harmony documentation before renaming patch arguments.</remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    internal class LocationSwitchPatch : IHarmonyPatch
    {
        /*********
        ** Fields
        *********/

        /*********
        ** Fields
        *********/
        /// <summary>Writes messages to the console and log file.</summary>
        private static IMonitor Monitor;

        /*********
        ** Accessors
        *********/
        /// <summary>A unique name for this patch.</summary>
        public string Name => nameof(LocationSwitchPatch);


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitorForGame">Writes messages to the console and log file on behalf of the game.</param>
        public LocationSwitchPatch(IMonitor monitor)
        {
            Monitor = monitor;
        }

        /// <summary>Apply the Harmony patch.</summary>
        /// <param name="harmony">The Harmony instance.</param>
        public void Apply(
#if HARMONY_2
            Harmony harmony
#else
            HarmonyInstance harmony
#endif
            )
        {
            harmony.Patch(
                original: AccessTools.Property(typeof(Game1), "currentLocation").SetMethod,
                prefix: new HarmonyMethod(this.GetType(), nameof(LocationSwitchPatch.Before_currentLocation_set))
            );
        }


        /*********
        ** Private methods
        *********/
        /// <summary>The method to call instead of <see cref="JunimoHarvester.ctor"/>.</summary>
        /// <param name="__instance">The instance being patched.</param>
        /// <param name="__originalMethod">The method being wrapped.</param>
        /// <returns>Returns whether to execute the original method.</returns>
        private static void Before_currentLocation_set(GameLocation value)
        {
            try
            {
                if (value != null && value.tapToMove == null)
                {
                    if (value.map != null)
                    {
                        value.tapToMove = new StardewValley.Mobile.TapToMove(value);
                    }
                    else
                    {
                        value.tapToMove = Game1.currentLocation.tapToMove;
                    }
                }
            }
            catch (Exception ex)
            {
                LocationSwitchPatch.Monitor.Log($"Failed to add tapToMove for currentLocation {value.Name}:\n{ex.InnerException ?? ex}", LogLevel.Error);
            }
        }
    }
}
#endif
