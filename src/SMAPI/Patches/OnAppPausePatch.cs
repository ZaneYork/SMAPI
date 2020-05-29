using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HarmonyLib;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Framework.Patching;
using StardewValley;

namespace StardewModdingAPI.Patches
{
    internal class OnAppPausePatch : IHarmonyPatch
    {
        /*********
        ** Accessors
        *********/
        /// <summary>A unique name for this patch.</summary>
        public string Name => $"{nameof(OnAppPausePatch)}";

        /// <summary>Writes messages to the console and log file.</summary>
        private static IMonitor Monitor;

        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public OnAppPausePatch(Monitor monitor)
        {
            OnAppPausePatch.Monitor = monitor;
        }



        /// <summary>Apply the Harmony patch.</summary>
        /// <param name="harmony">The Harmony instance.</param>
        public void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(Game1), nameof(Game1.OnAppPause)),
                finalizer:new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(OnAppPausePatch.Game_OnAppPauseFinalizer))));
        }


        /*********
        ** Private methods
        *********/
        /// <summary>The method to call instead of <see cref="StardewValley.Game1.OnAppPause"/>.</summary>
        /// <remarks>This method must be static for Harmony to work correctly. See the Harmony documentation before renaming arguments.</remarks>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony.")]
        private static Exception Game_OnAppPauseFinalizer(Exception __exception)
        {
            if (__exception != null)
            {
                OnAppPausePatch.Monitor.Log($"Failed during OnAppPause method :\n{__exception.InnerException ?? __exception}", LogLevel.Error);
            }
            return null;
        }

    }
}
