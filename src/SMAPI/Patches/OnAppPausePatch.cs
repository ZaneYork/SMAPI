#if SMAPI_FOR_MOBILE
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
#if HARMONY_2
using HarmonyLib;
#else
using Harmony;
#endif
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
#if HARMONY_2
        public void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(Game1), nameof(Game1.OnAppPause)),
                finalizer:new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(OnAppPausePatch.Game_OnAppPauseFinalizer))));
        }
#else
        public void Apply(HarmonyInstance harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(Game1), nameof(Game1.OnAppPause)),
                new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(OnAppPausePatch.Game_OnAppPausePrefix))));
        }
#endif

        /*********
        ** Private methods
        *********/
        /// <summary>The method to call instead of <see cref="StardewValley.Game1.OnAppPause"/>.</summary>
        /// <remarks>This method must be static for Harmony to work correctly. See the Harmony documentation before renaming arguments.</remarks>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony.")]
#if HARMONY_2
        private static Exception Game_OnAppPauseFinalizer(Exception __exception)
        {
            if (__exception != null)
            {
                OnAppPausePatch.Monitor.Log($"Failed during OnAppPause method :\n{__exception.InnerException ?? __exception}", LogLevel.Error);
            }
            return null;
        }
#else
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
#endif
}
#endif
