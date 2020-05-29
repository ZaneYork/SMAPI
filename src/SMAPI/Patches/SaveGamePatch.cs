using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using HarmonyLib;
using Microsoft.AppCenter.Crashes;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Framework.Patching;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI.Patches
{
    internal class SaveGamePatch : IHarmonyPatch
    {
        /*********
        ** Accessors
        *********/
        /// <summary>A unique name for this patch.</summary>
        public string Name => $"{nameof(SaveGamePatch)}";

        /// <summary>Writes messages to the console and log file.</summary>
        private static IMonitor Monitor;

        /// <summary>An Instance of <see cref="Translator"/>.</summary>
        private static Translator Translator;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitor">Monitor</param>
        public SaveGamePatch(Translator translator, Monitor monitor)
        {
            SaveGamePatch.Monitor = monitor;
            SaveGamePatch.Translator = translator;
        }


        /// <summary>Apply the Harmony patch.</summary>
        /// <param name="harmony">The Harmony instance.</param>
        public void Apply(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(SaveGame), "HandleLoadError"),
                prefix: new HarmonyMethod(this.GetType(), nameof(SaveGamePatch.Prefix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(SaveGameMenu), "update"),
                finalizer: new HarmonyMethod(this.GetType(), nameof(SaveGamePatch.SaveGameMenu_UpdateFinalizer))
            );
        }


        /*********
        ** Private methods
        *********/
        /// <summary>The method to call instead of <see cref="SaveGame.HandleLoadError"/>.</summary>
        /// <remarks>This method must be static for Harmony to work correctly. See the Harmony documentation before renaming arguments.</remarks>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony.")]
        private static bool Prefix(string fileName, bool loadEmergencySave, bool loadBackupSave, bool partialBackup)
        {
            bool failed = false;
            if (partialBackup)
            {
                Game1.emergencyLoading = false;
                Game1.gameMode = Game1.titleScreenGameMode;
            }

            if (loadEmergencySave)
            {
                if (!File.Exists(SaveGame.emergencySaveIndexPath))
                    return false;
                File.Delete(SaveGame.emergencySaveIndexPath);
                if (fileName == "")
                    return false;
                Game1.emergencyLoading = false;
                SaveGame.Load(fileName, false, true);
            }
            else if (loadBackupSave)
            {
                if (!File.Exists(SaveGame.backupSaveIndexPath))
                    return false;
                File.Delete(SaveGame.backupSaveIndexPath);
                Game1.emergencyLoading = false;
                Game1.gameMode = Game1.titleScreenGameMode;
                failed = true;
            }
            else if (SaveGame.newerBackUpExists(fileName) != null)
                SaveGame.Load(fileName, false, true);
            else if (SaveGame.oldBackUpExists(fileName) != null)
            {
                SaveGame.Load(fileName, false, true);
            }
            else
            {
                if (SaveGame.partialOldBackUpExists(fileName) == null)
                {
                    failed = true;
                }
                else
                {
                    IEnumerator<int> enumerator1 = SaveGame.getLoadEnumerator(fileName, false, true, true);
                    while (enumerator1 != null)
                    {
                        if (!enumerator1.MoveNext())
                            enumerator1 = null;
                    }

                    IEnumerator<int> enumerator2 = SaveGame.Save();
                    while (enumerator2 != null)
                    {
                        try
                        {
                            if (!enumerator2.MoveNext())
                                enumerator2 = null;
                        }
                        catch (Exception ex)
                        {
                            ErrorAttachmentLog[] errorAttachmentLogArray = Array.Empty<ErrorAttachmentLog>();
                            Crashes.TrackError(ex, null, errorAttachmentLogArray);
                            failed = true;
                        }
                    }
                }
            }

            if (failed)
            {
                SAlertDialogUtil.AlertMessage(
                    SaveGamePatch.Translator.Get("warn.save-broken"),
                    positive: SaveGamePatch.Translator.Get("btn.swap"),
                    negative: SaveGamePatch.Translator.Get("btn.back"),
                    callback: action =>
                    {
                        if (action == SAlertDialogUtil.ActionType.POSITIVE)
                        {
                            if (!SaveGame.swapForOldSave())
                            {
                                Game1.ExitToTitle();
                            }
                        }
                        else
                        {
                            Game1.ExitToTitle();
                        }
                    }
                );
            }

            return false;
        }
        /// <summary>The method to call instead of <see cref="StardewValley.Menus.SaveGameMenu.update"/>.</summary>
        /// <remarks>This method must be static for Harmony to work correctly. See the Harmony documentation before renaming arguments.</remarks>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony.")]
        private static Exception SaveGameMenu_UpdateFinalizer(SaveGameMenu __instance, Exception __exception)
        {
            if(__exception != null) {
                SaveGamePatch.Monitor.Log($"Failed during SaveGameMenu.update method :\n{__exception.InnerException ?? __exception}", LogLevel.Error);
                __instance.complete();
                Game1.addHUDMessage(new HUDMessage("An error occurs during save the game.Check the error log for details.", HUDMessage.error_type));
            }
            return null;
        }
    }
}
