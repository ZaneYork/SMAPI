using System;
#if HARMONY_2
using HarmonyLib;
#else
#if SMAPI_FOR_MOBILE
using MonoMod.RuntimeDetour;
#endif
using Harmony;
#endif

namespace StardewModdingAPI.Framework.Patching
{
    /// <summary>Encapsulates applying Harmony patches to the game.</summary>
    internal class GamePatcher
    {
        /*********
        ** Fields
        *********/
        /// <summary>Encapsulates monitoring and logging.</summary>
        private readonly IMonitor Monitor;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        public GamePatcher(IMonitor monitor)
        {
            this.Monitor = monitor;
        }

        /// <summary>Apply all loaded patches to the game.</summary>
        /// <param name="patches">The patches to apply.</param>
        public void Apply(params IHarmonyPatch[] patches)
        {
#if HARMONY_2
            Harmony harmony = new Harmony("SMAPI");
#else
#if SMAPI_FOR_MOBILE
            if (!HarmonyDetourBridge.Initialized && Constants.HarmonyEnabled)
            {
                try {
                    HarmonyDetourBridge.Init();
                }
                catch { Constants.HarmonyEnabled = false; }
            }
#endif
            HarmonyInstance harmony = HarmonyInstance.Create("SMAPI");
#endif
            foreach (IHarmonyPatch patch in patches)
            {
#if SMAPI_FOR_MOBILE
                try
                {
                    if(Constants.HarmonyEnabled)
                        patch.Apply(harmony);
                }
                catch (Exception ex)
                {
                    Constants.HarmonyEnabled = false;
                    this.Monitor.Log($"Couldn't apply runtime patch '{patch.Name}' to the game. Some SMAPI features may not work correctly. See log file for details.", LogLevel.Error);
                    this.Monitor.Log(ex.GetLogSummary(), LogLevel.Trace);
                }
#else
                try
                {
                    patch.Apply(harmony);
                }
                catch (Exception ex)
                {
                    this.Monitor.Log($"Couldn't apply runtime patch '{patch.Name}' to the game. Some SMAPI features may not work correctly. See log file for details.", LogLevel.Error);
                    this.Monitor.Log(ex.GetLogSummary(), LogLevel.Trace);
                }
#endif
            }
        }
    }
}
