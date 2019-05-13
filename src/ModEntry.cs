using System;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewModdingAPI.Framework;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using StardewModdingAPI;
using StardewValley.Menus;
using StardewValley.Buildings;
using StardewValley.Objects;
using StardewModdingAPI.Patches;

namespace SMDroid
{
    public class ModEntry : ModHooks
    {
        private SCore core;
        /// <summary>Whether the next content manager requested by the game will be for <see cref="Game1.content"/>.</summary>
        private bool NextContentManagerIsMain;
        /// <summary>SMAPI's content manager.</summary>
        private ContentCoordinator ContentCore { get; set; }

        public static bool ContextInitialize = true;

        public static ModEntry Instance;

        public static bool IsHalt = false;

        public ModEntry()
        {
            Instance = this;
            new GameConsole();
            this.core = new SCore(Path.Combine(Android.OS.Environment.ExternalStorageDirectory.Path, "SMDroid/Mods"), false);
        }
        public override bool OnGame1_CreateContentManager_Prefix(Game1 game1, IServiceProvider serviceProvider, string rootDirectory, ref LocalizedContentManager __result)
        {
            // Game1._temporaryContent initialising from SGame constructor
            // NOTE: this method is called before the SGame constructor runs. Don't depend on anything being initialised at this point.
            if (this.ContentCore == null)
            {
                //if (Constants.PackageName == "com.zane.smdroid")
                //{
                //    rootDirectory = Constants.AssetsPath;
                //}
                this.ContentCore = new ContentCoordinator(serviceProvider, rootDirectory, Thread.CurrentThread.CurrentUICulture, SGame.ConstructorHack.Monitor, SGame.ConstructorHack.Reflection, SGame.ConstructorHack.JsonHelper, SGame.OnLoadingFirstAsset ?? SGame.ConstructorHack?.OnLoadingFirstAsset);
                this.NextContentManagerIsMain = true;
                __result = this.ContentCore.CreateGameContentManager("Game1._temporaryContent");
                ContextInitialize = true;
                this.core.RunInteractively(this.ContentCore, __result);
                return false;
            }
            // Game1.content initialising from LoadContent
            if (this.NextContentManagerIsMain)
            {
                this.NextContentManagerIsMain = false;
                __result = this.ContentCore.MainContentManager;
                GameConsole.Instance.InitContent(__result);
                ContextInitialize = false;
                return false;
            }

            // any other content manager
            __result = this.ContentCore.CreateGameContentManager("(generated)");
            return false;
        }
        public override bool OnCommonHook_Prefix(string hookName, object __instance, ref object param1, ref object param2, ref object param3, ref object param4, ref object __result)
        {
            switch (hookName)
            {
                case "StardewValley.Game1.Update":
                    return this.core.GameInstance.Update(param1 as GameTime);
                case "StardewValley.Game1._draw":
                    return this.core.GameInstance.Draw(param1 as GameTime, param2 as RenderTarget2D);
                case "StardewValley.Object.getDescription":
                    if (!ObjectErrorPatch.Object_GetDescription_Prefix(__instance as StardewValley.Object, ref __result))
                    {
                        return false;
                    }
                    return true;
                default:
                    if ((hookName == "StardewValley.Dialogue..ctor") && !DialogueErrorPatch.Prefix(__instance as Dialogue, (string)param1, param2 as NPC))
                    {
                        return false;
                    }
                    return this.core.GameInstance.OnCommonHook_Prefix(hookName, __instance, ref param1, ref param2, ref param3, ref param4, ref __result);
            }
        }
        public override void OnCommonHook_Postfix(string hookName, object __instance, ref object param1, ref object param2, ref object param3, ref object param4, ref bool __state, ref object __result)
        {
            switch (hookName)
            {
                case "StardewValley.Game1.Update":
                    this.core.GameInstance.Update_Postfix(param1 as GameTime);
                    return;
                default:
                    this.core.GameInstance.OnCommonHook_Postfix(hookName, __instance, ref param1, ref param2, ref param3, ref param4, ref __state, ref __result);
                    return;
            }
        }
        public override bool OnCommonStaticHook_Prefix(string hookName, ref object param1, ref object param2, ref object param3, ref object param4, ref object param5, ref object __result)
        {
            return this.core.GameInstance.OnCommonStaticHook_Prefix(hookName, ref param1, ref param2, ref param3, ref param4, ref param5, ref __result);
        }
        public override void OnCommonStaticHook_Postfix(string hookName, ref object param1, ref object param2, ref object param3, ref object param4, ref object param5, ref bool __state, ref object __result)
        {
            this.core.GameInstance.OnCommonStaticHook_Postfix(hookName, ref param1, ref param2, ref param3, ref param4, ref param5, ref __state, ref __result);
        }
        public override void OnCommonHook10_Postfix(string hookName, object __instance, ref object param1, ref object param2, ref object param3, ref object param4, ref object param5, ref object param6, ref object param7, ref object param8, ref object param9, ref bool __state, ref object __result)
        {
            this.core.GameInstance.OnCommonHook10_Postfix(hookName, __instance, ref param1, ref param2, ref param3, ref param4, ref param5, ref param6, ref param7, ref param8, ref param9, ref __state, ref __result);
        }
        public override bool OnCommonHook10_Prefix(string hookName, object __instance, ref object param1, ref object param2, ref object param3, ref object param4, ref object param5, ref object param6, ref object param7, ref object param8, ref object param9, ref object __result)
        {
            if ((hookName == "StardewValley.Menus.IClickableMenu.drawToolTip") && !ObjectErrorPatch.IClickableMenu_DrawTooltip_Prefix(__instance as IClickableMenu, param4 as Item))
            {
                return false;
            }
            return this.core.GameInstance.OnCommonHook10_Prefix(hookName, __instance, ref param1, ref param2, ref param3, ref param4, ref param5, ref param6, ref param7, ref param8, ref param9, ref __result);
        }
        public override void OnCommonStaticHook10_Postfix(string hookName, ref object param1, ref object param2, ref object param3, ref object param4, ref object param5, ref object param6, ref object param7, ref object param8, ref object param9, ref object param10, ref bool __state, ref object __result)
        {
            this.core.GameInstance.OnCommonStaticHook10_Postfix(hookName, ref param1, ref param2, ref param3, ref param4, ref param5, ref param6, ref param7, ref param8, ref param9, ref param10, ref __state, ref __result);
        }

        public override bool OnCommonStaticHook10_Prefix(string hookName, ref object param1, ref object param2, ref object param3, ref object param4, ref object param5, ref object param6, ref object param7, ref object param8, ref object param9, ref object param10, ref object __result)
        {
            return this.core.GameInstance.OnCommonStaticHook10_Prefix(hookName, ref param1, ref param2, ref param3, ref param4, ref param5, ref param6, ref param7, ref param8, ref param9, ref param10, ref __result);
        }


        public override void OnGame1_NewDayAfterFade(Action action)
        {
            this.core.GameInstance.OnNewDayAfterFade();
            base.OnGame1_NewDayAfterFade(action);
        }
    }
}
