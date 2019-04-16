using System;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewModdingAPI.Framework;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;
using System.IO;

namespace SMDroid
{
    public class ModEntry : ModHooks
    {

        private SCore core;
        /// <summary>Whether the next content manager requested by the game will be for <see cref="Game1.content"/>.</summary>
        private bool NextContentManagerIsMain;
        /// <summary>SMAPI's content manager.</summary>
        private ContentCoordinator ContentCore { get; set; }


        public ModEntry()
        {
            this.core = new SCore(Path.Combine(Android.OS.Environment.ExternalStorageDirectory.Path, "SMDroid/Mods"), false);
        }
        public override bool OnGame1_CreateContentManager_Prefix(Game1 _, IServiceProvider serviceProvider, string rootDirectory, ref LocalizedContentManager __result)
        {
            // Game1._temporaryContent initialising from SGame constructor
            // NOTE: this method is called before the SGame constructor runs. Don't depend on anything being initialised at this point.
            if (this.ContentCore == null)
            {
                this.ContentCore = new ContentCoordinator(serviceProvider, rootDirectory, Thread.CurrentThread.CurrentUICulture, SGame.ConstructorHack.Monitor, SGame.ConstructorHack.Reflection, SGame.ConstructorHack.JsonHelper, SGame.OnLoadingFirstAsset ?? SGame.ConstructorHack?.OnLoadingFirstAsset);
                this.NextContentManagerIsMain = true;
                this.core.RunInteractively(this.ContentCore);
                __result = this.ContentCore.CreateGameContentManager("Game1._temporaryContent");
            }
            // Game1.content initialising from LoadContent
            if (this.NextContentManagerIsMain)
            {
                this.NextContentManagerIsMain = false;
                __result = this.ContentCore.MainContentManager;
            }

            // any other content manager
            __result = this.ContentCore.CreateGameContentManager("(generated)");
            return false;
        }
        public override bool OnGame1_Update_Prefix(Game1 _, GameTime time)
        {
            return this.core.GameInstance.Update(time);
        }
        public override void OnGame1_Update_Postfix(Game1 _, GameTime time)
        {
            this.core.GameInstance.Update_Postfix(time);
        }
        public override bool OnGame1_Draw_Prefix(Game1 _, GameTime time)
        {
            return this.core.GameInstance.Draw(time);
        }
        public override void OnGame1_NewDayAfterFade(Action action)
        {
            this.core.GameInstance.OnNewDayAfterFade();
            base.OnGame1_NewDayAfterFade(action);
        }
        public override bool OnObject_canBePlacedHere_Prefix(StardewValley.Object __instance, GameLocation location, Vector2 tile, ref bool __result)
        {
            return this.core.GameInstance.OnObjectCanBePlacedHere(__instance, location, tile, ref __result);
        }
        public override void OnObject_isIndexOkForBasicShippedCategory_Postfix(int index, ref bool __result)
        {
            this.core.GameInstance.OnObjectIsIndexOkForBasicShippedCategory(index, ref __result);
        }
        public override bool OnObject_checkForAction_Prefix(StardewValley.Object __instance, Farmer value, bool justCheckingForActivity, ref bool __result)
        {
            return this.core.GameInstance.OnObjectCheckForAction(__instance);
        }
    }
}
