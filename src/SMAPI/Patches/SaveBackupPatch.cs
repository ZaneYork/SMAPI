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
using StardewModdingAPI.Framework.Events;
using StardewModdingAPI.Framework.Patching;
using StardewValley;

namespace StardewModdingAPI.Patches
{
    internal class SaveBackupPatch : IHarmonyPatch
    {
        /*********
        ** Accessors
        *********/
        /// <summary>A unique name for this patch.</summary>
        public string Name => $"{nameof(SaveBackupPatch)}";

        /// <summary>An Instance of <see cref="EventManager"/>.</summary>
        private static EventManager Events;

        /// <summary>Writes messages to the console and log file.</summary>
        private static IMonitor Monitor;

        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="eventManager">SMAPI's EventManager Instance</param>

        public SaveBackupPatch(EventManager eventManager, Monitor monitor)
        {
            SaveBackupPatch.Events = eventManager;
            SaveBackupPatch.Monitor = monitor;
        }



        /// <summary>Apply the Harmony patch.</summary>
        /// <param name="harmony">The Harmony instance.</param>
#if HARMONY_2
        public void Apply(Harmony harmony)
        {
            MethodInfo makeFullBackup = AccessTools.Method(typeof(Game1), nameof(Game1.MakeFullBackup));
            MethodInfo saveWholeBackup = AccessTools.Method(typeof(Game1), nameof(Game1.saveWholeBackup));

            MethodInfo prefix = AccessTools.Method(this.GetType(), nameof(SaveBackupPatch.GameSave_Prefix));
            MethodInfo finalizer = AccessTools.Method(this.GetType(), nameof(SaveBackupPatch.GameSave_Finalizer));

            harmony.Patch(makeFullBackup, new HarmonyMethod(prefix), finalizer: new HarmonyMethod(finalizer));
            harmony.Patch(saveWholeBackup, new HarmonyMethod(prefix), finalizer: new HarmonyMethod(finalizer));
        }
#else
        public void Apply(HarmonyInstance harmony)
        {
            MethodInfo makeFullBackup = AccessTools.Method(typeof(Game1), nameof(Game1.MakeFullBackup));
            MethodInfo saveWholeBackup = AccessTools.Method(typeof(Game1), nameof(Game1.saveWholeBackup));

            MethodInfo prefix = AccessTools.Method(this.GetType(), nameof(SaveBackupPatch.GameSave_Prefix));

            harmony.Patch(makeFullBackup, new HarmonyMethod(prefix));
            harmony.Patch(saveWholeBackup, new HarmonyMethod(prefix));
        }
#endif
        /*********
        ** Private methods
        *********/
        /// <summary>The method to call instead of <see cref="StardewValley.Object.getDescription"/>.</summary>
        /// <remarks>This method must be static for Harmony to work correctly. See the Harmony documentation before renaming arguments.</remarks>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony.")]
#if HARMONY_2
        private static bool GameSave_Prefix()
        {
            SaveBackupPatch.Events.Saving.RaiseEmpty();
            return true;
        }

        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony.")]
        private static Exception GameSave_Finalizer(Exception __exception)
        {
            if (__exception != null)
            {
                SaveBackupPatch.Monitor.Log($"Failed to save the game :\n{__exception.InnerException ?? __exception}", LogLevel.Error);
                Game1.addHUDMessage(new HUDMessage("An error occurs during save the game.Check the error log for details.", HUDMessage.error_type));
            }
            SaveBackupPatch.Events.Saved.RaiseEmpty();
            return null;
        }
#else
        private static bool GameSave_Prefix(MethodInfo __originalMethod)
        {
            const string key = nameof(SaveBackupPatch.GameSave_Prefix);
            if (!PatchHelper.StartIntercept(key))
                return true;
            SaveBackupPatch.Events.Saving.RaiseEmpty();
            try
            {
                __originalMethod.Invoke(null, new object[] { });
            }
            catch (Exception ex)
            {
                SaveBackupPatch.Monitor.Log($"Failed to save the game :\n{ex.InnerException ?? ex}", LogLevel.Error);
                Game1.addHUDMessage(new HUDMessage("An error occurs during save the game.Check the error log for details.", HUDMessage.error_type));
            }
            finally
            {
                PatchHelper.StopIntercept(key);
            }
            SaveBackupPatch.Events.Saved.RaiseEmpty();
            return false;
        }
#endif
    }
}
#endif
