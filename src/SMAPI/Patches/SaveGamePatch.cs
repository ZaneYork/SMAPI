#if SMAPI_FOR_MOBILE
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml;
using System.Xml.Serialization;
using HarmonyLib;
using Java.Util;
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

        private static PropertyInfo XmlTypeMappingObjectMapProperty = AccessTools.Property(typeof(XmlTypeMapping), "ObjectMap");
        private static PropertyInfo EnumMapEnumNamesProperty = AccessTools.Property(typeof(XmlSerializer).Assembly.GetType("System.Xml.Serialization.EnumMap"), "EnumNames");
        private static PropertyInfo EnumMapXmlNamesProperty = AccessTools.Property(typeof(XmlSerializer).Assembly.GetType("System.Xml.Serialization.EnumMap"), "XmlNames");
        private static MethodInfo EnumMapGetXmlNameMethod = AccessTools.Method(typeof(XmlSerializer).Assembly.GetType("System.Xml.Serialization.EnumMap"), "GetXmlName");

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
                transpiler: new HarmonyMethod(this.GetType(), nameof(SaveGamePatch.XmlSerializationReaderInterpreter_TranspileGetValueFromXmlString))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(XmlSerializer).Assembly.GetType("System.Xml.Serialization.XmlSerializationReaderInterpreter"), "AddListValue"),
                prefix: new HarmonyMethod(this.GetType(), nameof(SaveGamePatch.XmlSerializationReaderInterpreter_PrefixAddListValue))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(XmlSerializer).Assembly.GetType("System.Xml.Serialization.XmlSerializationWriterInterpreter"), "GetEnumXmlValue"),
                prefix: new HarmonyMethod(this.GetType(), nameof(SaveGamePatch.XmlSerializationWriterInterpreter_PrefixGetEnumXmlValue))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(XmlSerializer).Assembly.GetType("System.Xml.Serialization.XmlReflectionImporter"), "GetReflectionMembers"),
                postfix: new HarmonyMethod(this.GetType(), nameof(SaveGamePatch.XmlReflectionImporter_PostfixGetReflectionMembers))
            );

            harmony.Patch(
                original: AccessTools.PropertyGetter(typeof(XmlSerializer).Assembly.GetType("System.Xml.Serialization.TypeData"), "ListItemType"),
                prefix: new HarmonyMethod(this.GetType(), nameof(SaveGamePatch.TypeData_PrefixListItemType))
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

        private static IEnumerable<CodeInstruction> XmlSerializationReaderInterpreter_TranspileGetValueFromXmlString(ILGenerator gen, MethodBase original, IEnumerable<CodeInstruction> insns)
        {
            List<CodeInstruction> newInsns = new();
            foreach (var insn in insns)
            {
                if (insn.opcode == OpCodes.Bne_Un_S)
                {
                    if (newInsns[newInsns.Count - 1].opcode == OpCodes.Ldc_I4_2)
                    {
                        var lastIns = newInsns[newInsns.Count - 2];
                        if (lastIns.opcode == OpCodes.Callvirt && lastIns.operand is MethodInfo minfo && minfo.DeclaringType.FullName == "System.Xml.Serialization.TypeData" && minfo.Name == "get_SchemaType")
                        {
                            newInsns.Add(insn);
                            Label continueLabel = gen.DefineLabel();
                            Label retLabel = gen.DefineLabel();
                            newInsns.Add(new CodeInstruction(OpCodes.Ldarg_0));
                            newInsns.Add(new CodeInstruction(OpCodes.Brfalse_S, retLabel));
                            newInsns.Add(new CodeInstruction(OpCodes.Ldarg_0));
                            newInsns.Add(new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(string), nameof(string.Length))));
                            newInsns.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
                            newInsns.Add(new CodeInstruction(OpCodes.Cgt));
                            newInsns.Add(new CodeInstruction(OpCodes.Brfalse_S, retLabel));
                            newInsns.Add(new CodeInstruction(OpCodes.Br_S, continueLabel));

                            newInsns.Add(new CodeInstruction(OpCodes.Ldnull).WithLabels(retLabel));
                            newInsns.Add(new CodeInstruction(OpCodes.Ret));

                            CodeInstruction label = new CodeInstruction(OpCodes.Nop).WithLabels(continueLabel);
                            newInsns.Add(label);
                            continue;
                        }
                    }
                }
                newInsns.Add(insn);
            }
            return newInsns;
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
            try
            {
                if (listItemType.IsEnum && value != null)
                    type.GetMethod("Add", new[] { typeof(string) }).Invoke(list, new[] { value.ToString() });
                else if(!listItemType.IsEnum)
                    type.GetMethod("Add", new[] { listItemType }).Invoke(list, new[] { value });
            }
            catch (Exception e)
            {
                SaveGamePatch.Monitor.Log($"AddListValue error: {e}");
            }

            return false;
        }

        private static bool XmlSerializationWriterInterpreter_PrefixGetEnumXmlValue(XmlTypeMapping typeMap, object ob, ref string __result)
        {
            if (ob == null)
            {
                __result = null;
                return false;
            }

            object objectMap = SaveGamePatch.XmlTypeMappingObjectMapProperty.GetValue(typeMap);
            if (ob is string enumString)
            {
                string[] enumNames = (string[])SaveGamePatch.EnumMapEnumNamesProperty.GetValue(objectMap);
                string[] xmlNames = (string[])SaveGamePatch.EnumMapXmlNamesProperty.GetValue(objectMap);
                for (int i = 0; i < enumNames.Length; i++)
                    if (enumString == enumNames[i])
                    {
                        __result = xmlNames[i];
                        return false;
                    }

                __result = null;
            }
            else
                __result = (string)SaveGamePatch.EnumMapGetXmlNameMethod.Invoke(objectMap, new[] { typeMap.TypeFullName, ob });

            return false;
        }

        private static void XmlReflectionImporter_PostfixGetReflectionMembers(Type type, ref List<XmlReflectionMember> __result)
        {
            if (type.FullName.StartsWith("StardewValley."))
                foreach (XmlReflectionMember member in __result)
                    if (member.MemberType.FullName.StartsWith("Netcode.NetEvent"))
                        member.XmlAttributes.XmlIgnore = true;
        }

        private static bool TypeData_PrefixListItemType(object __instance, ref Type __result)
        {
            string name = (string)AccessTools.Property(__instance.GetType(), "CSharpFullName").GetValue(__instance);
            bool isRewrite = name == "StardewValley.Network.NetIntDictionary<System.Int32,Netcode.NetInt>" ||
                             name == "StardewValley.Network.NetStringDictionary<System.String,Netcode.NetString>";

            if (!isRewrite) return true;

            Type runtimeType = (Type)AccessTools.Field(__instance.GetType(), "type").GetValue(__instance);
            if (runtimeType == null) throw new InvalidOperationException("Property ListItemType is not supported for custom types");
            FieldInfo listItemTypeField = AccessTools.Field(__instance.GetType(), "listItemType");
            Type listItemType = (Type)listItemTypeField.GetValue(__instance);
            if (listItemType != null)
            {
                __result = listItemType;
                return false;
            }

            Type type = null;
            if (runtimeType.IsArray)
            {
                listItemType = runtimeType.GetElementType();
                listItemTypeField.SetValue(__instance, listItemType);
            }
            else if (typeof(ICollection<object>).IsAssignableFrom(runtimeType))
            {
                if (typeof(IDictionary<object, object>).IsAssignableFrom(runtimeType)) throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "The type {0} is not supported because it implements IDictionary.", runtimeType.FullName));
                PropertyInfo indexerProperty = (PropertyInfo)AccessTools.Method(__instance.GetType(), "GetIndexerProperty", new[] { typeof(Type) }).Invoke(null, new[] { runtimeType });
                if (indexerProperty == null) throw new InvalidOperationException("You must implement a default accessor on " + runtimeType.FullName + " because it inherits from ICollection");
                listItemType = indexerProperty.PropertyType;
                listItemTypeField.SetValue(__instance, listItemType);

                if (runtimeType.GetMethod("Add", new[] { listItemType }) == null)
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                        "To be XML serializable, types which inherit from {0} must have an implementation of Add({1}) at all levels of their inheritance hierarchy. {2} does not implement Add({1}).",
                        "ICollection", listItemType.FullName, type.FullName));
            }
            else
            {
                MethodInfo methodInfo = runtimeType.GetMethod("GetEnumerator", Type.EmptyTypes);
                if (methodInfo == null) methodInfo = runtimeType.GetMethod("System.Collections.IEnumerable.GetEnumerator", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                PropertyInfo property = methodInfo.ReturnType.GetProperty("Current");
                if (property == null)
                    listItemType = typeof(object);
                else
                    listItemType = property.PropertyType;
                listItemTypeField.SetValue(__instance, listItemType);
                if (runtimeType.GetMethod("Add", new[] { listItemType }) == null)
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                        "To be XML serializable, types which inherit from {0} must have an implementation of Add({1}) at all levels of their inheritance hierarchy. {2} does not implement Add({1}).",
                        "IEnumerable", listItemType.FullName, type.FullName));
            }

            __result = listItemType;
            return false;
        }
    }
}
#endif
