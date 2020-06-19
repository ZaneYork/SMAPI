using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
#if HARMONY_2
using HarmonyLib;
#else
using Harmony;
#endif
using StardewModdingAPI.Framework.Patching;

namespace StardewModdingAPI.Patches
{
    /// <summary>A Harmony patch for <see cref="System.Threading.ThreadHelper"/> which detect unhandled exception and make it exit silence.</summary>
    /// <remarks>Patch methods must be static for Harmony to work correctly. See the Harmony documentation before renaming patch arguments.</remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    internal class ThreadSilenceExitPatch : IHarmonyPatch
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
        public string Name => nameof(ThreadSilenceExitPatch);


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitorForGame">Writes messages to the console and log file on behalf of the game.</param>
        public ThreadSilenceExitPatch(IMonitor monitor)
        {
            Monitor = monitor;
        }

        /// <summary>Apply the Harmony patch.</summary>
        /// <param name="harmony">The Harmony instance.</param>
#if HARMONY_2
        public void Apply(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(Type.GetType("System.Threading.ThreadHelper"), "ThreadStart_Context"),
                finalizer: new HarmonyMethod(this.GetType(), nameof(ThreadSilenceExitPatch.ThreadStart_Finalizer))
            );
        }
#else
        public void Apply(HarmonyInstance harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(Type.GetType("System.Threading.ThreadHelper"), "ThreadStart_Context"),
                prefix: new HarmonyMethod(this.GetType(), nameof(ThreadSilenceExitPatch.ThreadStart_Finalizer))
            );
        }
#endif

        /*********
        ** Private methods
        *********/
        /// <summary>The method to call instead of <see cref="System.Threading.ThreadHelper.ThreadStart_Context"/>.</summary>
        /// <param name="state">The thread context.</param>
        /// <returns>Returns whether to execute the original method.</returns>
#if HARMONY_2
        private static Exception ThreadStart_Finalizer(Exception __exception)
        {
            if (__exception != null) {
                Monitor.Log($"Thread failed:\n{__exception.InnerException ?? __exception}", LogLevel.Error);
            }
            return null;
        }
#else
        private static bool ThreadStart_Finalizer(object state)
        {
            try
            {
                object _start = state.GetType().GetField("_start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(state);
                if (_start is ThreadStart)
                {
                    ((ThreadStart)_start)();
                }
                else
                {
                    object _startArg = state.GetType().GetField("_startArg", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(state);
                    ((ParameterizedThreadStart)_start)(_startArg);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Thread failed:\n{ex.InnerException ?? ex}", LogLevel.Error);
            }
            return false;
        }
#endif
    }
}
