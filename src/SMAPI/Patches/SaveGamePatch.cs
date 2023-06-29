#if SMAPI_FOR_MOBILE
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml.Serialization;
using HarmonyLib;
// using Microsoft.AppCenter.Crashes;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Internal.Patching;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI.Patches
{
    internal class SaveGamePatch : BasePatcher
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
        public override void Apply(Harmony harmony, IMonitor monitor)
        {
            // harmony.Patch(
            //     original: AccessTools.Method(typeof(SaveGame), "HandleLoadError"),
            //     prefix: new HarmonyMethod(this.GetType(), nameof(SaveGamePatch.Prefix))
            // );
            // harmony.Patch(
            //     original: AccessTools.Method(typeof(SaveGameMenu), "update"),
            //     finalizer: new HarmonyMethod(this.GetType(), nameof(SaveGamePatch.SaveGameMenu_UpdateFinalizer))
            // );
            harmony.Patch(
                original: AccessTools.Method(typeof(XmlSerializer).Assembly.GetType("System.Xml.Serialization.XmlSerializationReaderInterpreter"), "GetValueFromXmlString"),
                prefix: new HarmonyMethod(this.GetType(), nameof(SaveGamePatch.XmlSerializationReaderInterpreter_PrefixGetValueFromXmlString))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(XmlSerializer).Assembly.GetType("System.Xml.Serialization.XmlSerializationReaderInterpreter"), "AddListValue"),
                prefix: new HarmonyMethod(this.GetType(), nameof(SaveGamePatch.XmlSerializationReaderInterpreter_PrefixAddListValue))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(XmlSerializer).Assembly.GetType("System.Xml.Serialization.XmlSerializationWriterInterpreter"), "GetEnumXmlValue"),
                prefix: new HarmonyMethod(this.GetType(), nameof(SaveGamePatch.XmlSerializationWriterInterpreter_PrefixGetEnumXmlValue))
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
                SaveGame.Load(fileName, false, true);
            else
            {
                if (SaveGame.partialOldBackUpExists(fileName) == null)
                    failed = true;
                else
                {
                    IEnumerator<int> enumerator1 = SaveGame.getLoadEnumerator(fileName, false, true, true);
                    while (enumerator1 != null)
                        if (!enumerator1.MoveNext())
                            enumerator1 = null;

                    IEnumerator<int> enumerator2 = SaveGame.Save();
                    while (enumerator2 != null)
                        try
                        {
                            if (!enumerator2.MoveNext())
                                enumerator2 = null;
                        }
                        catch (Exception ex)
                        {
                            // ErrorAttachmentLog[] errorAttachmentLogArray = Array.Empty<ErrorAttachmentLog>();
                            // Crashes.TrackError(ex, null, errorAttachmentLogArray);
                            failed = true;
                        }
                }
            }

            if (failed)
                SAlertDialogUtil.AlertMessage(
                    SaveGamePatch.Translator.Get("warn.save-broken"),
                    positive: SaveGamePatch.Translator.Get("btn.swap"),
                    negative: SaveGamePatch.Translator.Get("btn.back"),
                    callback: action =>
                    {
                        if (action == SAlertDialogUtil.ActionType.POSITIVE)
                        {
                            if (!SaveGame.swapForOldSave()) Game1.ExitToTitle();
                        }
                        else
                            Game1.ExitToTitle();
                    }
                );

            return false;
        }

        /// <summary>The method to call instead of <see cref="StardewValley.Menus.SaveGameMenu.update"/>.</summary>
        /// <remarks>This method must be static for Harmony to work correctly. See the Harmony documentation before renaming arguments.</remarks>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony.")]
        private static Exception SaveGameMenu_UpdateFinalizer(SaveGameMenu __instance, Exception __exception)
        {
            if (__exception != null)
            {
                SaveGamePatch.Monitor.Log($"Failed during SaveGameMenu.update method :\n{__exception.InnerException ?? __exception}", LogLevel.Error);
                __instance.complete();
                Game1.addHUDMessage(new HUDMessage("An error occurs during save the game.Check the error log for details.", HUDMessage.error_type));
            }

            return null;
        }

        private static bool XmlSerializationReaderInterpreter_PrefixGetValueFromXmlString(string value, ref object __result)
        {
            if (value?.Length > 0) return true;
            __result = null;
            return false;
        }

        private static bool XmlSerializationReaderInterpreter_PrefixAddListValue(object listType, ref object list, int index, object value, bool canCreateInstance)
        {
            Type type = (Type)AccessTools.Property(listType.GetType(), "Type").GetValue(listType);
            if (type.IsArray) return true;

            if (list == null)
            {
                if (!canCreateInstance) throw new InvalidOperationException(string.Format("Could not serialize {0}. Default constructors are required for collections and enumerators.", type.FullName));

                list = Activator.CreateInstance(type, true);
            }

            Type listItemType = (Type)AccessTools.Property(listType.GetType(), "ListItemType").GetValue(listType);
            if (listItemType.IsEnum && value != null)
                type.GetMethod("Add", new[] { typeof(string) }).Invoke(list, new[] { value.ToString() });
            else
                type.GetMethod("Add", new[] { listItemType }).Invoke(list, new[] { value });

            return false;
        }

        private static bool XmlSerializationWriterInterpreter_PrefixGetEnumXmlValue(XmlTypeMapping typeMap, object ob, ref string __result)
        {
            if (ob == null)
            {
                __result = null;
                return false;
            }

            object objectMap = AccessTools.Property(typeMap.GetType(), "ObjectMap").GetValue(typeMap);
            if (ob is string enumString)
            {
                string[] enumNames = (string[])AccessTools.Property(objectMap.GetType(), "EnumNames").GetValue(objectMap);
                string[] xmlNames = (string[])AccessTools.Property(objectMap.GetType(), "XmlNames").GetValue(objectMap);
                for (int i = 0; i < enumNames.Length; i++)
                    if (enumString == enumNames[i])
                    {
                        __result = xmlNames[i];
                        return false;
                    }

                __result = null;
            }
            else
                __result = (string)AccessTools.Method(objectMap.GetType(), "GetXmlName").Invoke(objectMap, new[] { typeMap.TypeFullName, ob });

            return false;
        }
    }
}
#endif
