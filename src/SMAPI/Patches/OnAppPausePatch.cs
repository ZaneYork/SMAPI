using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Harmony;
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
        public void Apply(HarmonyInstance harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(Game1), nameof(Game1.OnAppPause)),
                new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(OnAppPausePatch.Game_OnAppPausePrefix))));
        }


        /*********
        ** Private methods
        *********/
        /// <summary>The method to call instead of <see cref="StardewValley.Game1.OnAppPause"/>.</summary>
        /// <remarks>This method must be static for Harmony to work correctly. See the Harmony documentation before renaming arguments.</remarks>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony.")]
        private static bool Game_OnAppPausePrefix(Game1 __instance, MethodInfo __originalMethod)
        {
            const string key = nameof(OnAppPausePatch.Game_OnAppPausePrefix);
            if (!PatchHelper.StartIntercept(key))
                return true;
            try
            {
                __originalMethod.Invoke(__instance, new object[] { });
            }
            catch (Exception ex)
            {
                OnAppPausePatch.Monitor.Log($"Failed during OnAppPause method :\n{ex.InnerException ?? ex}", LogLevel.Error);
            }
            finally
            {
                PatchHelper.StopIntercept(key);
            }
            return false;
        }

    }
}
