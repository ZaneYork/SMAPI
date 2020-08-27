using System.Collections.Generic;
#if HARMONY_2
using HarmonyLib;
#else
using Harmony;
#endif
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewModdingAPI.Framework.ModLoading;
using StardewModdingAPI.Framework.ModLoading.Finders;
using StardewModdingAPI.Framework.ModLoading.RewriteFacades;
using StardewModdingAPI.Framework.ModLoading.Rewriters;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace StardewModdingAPI.Metadata
{
    /// <summary>Provides CIL instruction handlers which rewrite mods for compatibility and throw exceptions for incompatible code.</summary>
    internal class InstructionMetadata
    {
        /*********
        ** Fields
        *********/
        /// <summary>The assembly names to which to heuristically detect broken references.</summary>
        /// <remarks>The current implementation only works correctly with assemblies that should always be present.</remarks>
        private readonly string[] ValidateReferencesToAssemblies = { "StardewModdingAPI", "Stardew Valley", "StardewValley", "Netcode" };

        private readonly IMonitor Monitor;

        public InstructionMetadata(IMonitor monitor)
        {
            this.Monitor = monitor;
        }


        /*********
        ** Public methods
        *********/
        /// <summary>Get rewriters which detect or fix incompatible CIL instructions in mod assemblies.</summary>
        /// <param name="paranoidMode">Whether to detect paranoid mode issues.</param>
        /// <param name="platformChanged">Whether the assembly was rewritten for crossplatform compatibility.</param>
        public IEnumerable<IInstructionHandler> GetHandlers(bool paranoidMode, bool platformChanged)
        {
            /****
            ** rewrite CIL to fix incompatible code
            ****/
            // rewrite for crossplatform compatibility
            if (platformChanged)
                yield return new MethodParentRewriter(typeof(SpriteBatch), typeof(SpriteBatchFacade));

#if SMAPI_FOR_MOBILE
            // Redirect reference
            yield return new TypeFieldToAnotherTypePropertyRewriter(typeof(Game1), typeof(Game1Methods), "isRaining", "IsRainingProp");
            yield return new TypeFieldToAnotherTypePropertyRewriter(typeof(Game1), typeof(Game1Methods), "isSnowing", "IsSnowingProp");
            yield return new TypeFieldToAnotherTypePropertyRewriter(typeof(Game1), typeof(Game1Methods), "isDebrisWeather", "IsDebrisWeatherProp");
            yield return new TypeFieldToAnotherTypePropertyRewriter(typeof(Game1), typeof(Game1Methods), "rainDrops", "RainDropsProp");
            yield return new TypeFieldToAnotherTypeFieldRewriter(typeof(GameLocation), typeof(DebrisManager), "debris", this.Monitor, "debrisNetCollection");
            // yield return new TypeFieldToAnotherTypePropertyRewriter(typeof(GameLocation), typeof(DebrisManager), "debris", "debrisNetCollection", "Instance");
            yield return new TypeFieldToAnotherTypePropertyRewriter(typeof(Game1), typeof(WeatherDebrisManager), "debrisWeather","weatherDebrisList", "Instance");
            yield return new TypeFieldToAnotherTypePropertyRewriter(typeof(Game1), typeof(Game1Methods), "onScreenMenus", "onScreenMenus");

            yield return new PropertyToFieldRewriter(typeof(Game1), "toolSpriteSheet", "toolSpriteSheet");

            // Accessibility fix
            yield return new TypeFieldToAnotherTypePropertyRewriter(typeof(GameMenu), typeof(GameMenuMethods), "hoverText", "HoverTextProp");
            yield return new TypeFieldToAnotherTypePropertyRewriter(typeof(ShopMenu), typeof(ShopMenuMethods), "heldItem", "HeldItemProp");
            yield return new TypeFieldToAnotherTypePropertyRewriter(typeof(ShopMenu), typeof(ShopMenuMethods), "hoveredItem", "HoveredItemProp");
            yield return new TypeFieldToAnotherTypePropertyRewriter(typeof(ShopMenu), typeof(ShopMenuMethods), "hoverPrice", "HoverPriceProp");
            yield return new TypeFieldToAnotherTypePropertyRewriter(typeof(ShopMenu), typeof(ShopMenuMethods), "hoverText", "HoverTextProp");
            yield return new TypeFieldToAnotherTypePropertyRewriter(typeof(ShopMenu), typeof(ShopMenuMethods), "categoriesToSellHere", "CategoriesToSellHereProp");
            yield return new TypeFieldToAnotherTypePropertyRewriter(typeof(ShopMenu), typeof(ShopMenuMethods), "itemPriceAndStock", "ItemPriceAndStockProp");
            yield return new TypeFieldToAnotherTypePropertyRewriter(typeof(ShopMenu), typeof(ShopMenuMethods), "forSale", "ForSaleProp");
            yield return new TypeFieldToAnotherTypePropertyRewriter(typeof(MenuWithInventory), typeof(MenuWithInventoryMethods), "trashCan", "TrashCanProp");
            yield return new TypeFieldToAnotherTypePropertyRewriter(typeof(ItemGrabMenu), typeof(ItemGrabMenuMethods), "fillStacksButton", "FillStacksButtonProp");
            yield return new TypeFieldToAnotherTypePropertyRewriter(typeof(AnimalQueryMenu), typeof(AnimalQueryMenuMethods), "allowReproductionButton", "AllowReproductionButtonProp");
            yield return new TypeFieldToAnotherTypePropertyRewriter(typeof(AnimalQueryMenu), typeof(AnimalQueryMenuMethods), "sellButton", "SellButtonProp");
            yield return new TypeFieldToAnotherTypePropertyRewriter(typeof(AnimalQueryMenu), typeof(AnimalQueryMenuMethods), "moveHomeButton", "MoveHomeButtonProp");
            // TextBox fix
            yield return new TypePropertyToAnotherTypeMethodRewriter(typeof(TextBox), typeof(TextBoxMethods), "Selected", null, "SelectedSetter");

            // Rewrite Missing Type
            yield return new TypeReferenceRewriter("StardewValley.Menus.CraftingPage", typeof(CraftingPageMobile));
            yield return new TypeReferenceRewriter("StardewValley.Menus.InventoryMenu/BorderSide", typeof(InventoryMenuMethods.BorderSide));

            //Method Rewrites
            yield return new MethodParentRewriter(typeof(Game1), typeof(Game1Methods));
            yield return new MethodParentRewriter(typeof(IClickableMenu), typeof(IClickableMenuMethods));
            yield return new MethodParentRewriter(typeof(SpriteText), typeof(SpriteTextMethods));
            yield return new MethodParentRewriter(typeof(NPC), typeof(NPCMethods));
            yield return new MethodParentRewriter(typeof(Utility), typeof(UtilityMethods));
            yield return new MethodParentRewriter(typeof(DayTimeMoneyBox), typeof(DayTimeMoneyBoxMethods));
            yield return new MethodParentRewriter(typeof(SaveGame), typeof(SaveGameMethods));

            //Constructor Rewrites
            yield return new MethodParentRewriter(typeof(ItemGrabMenu), typeof(ItemGrabMenuMethods));
            yield return new MethodParentRewriter(typeof(WeatherDebris), typeof(WeatherDebrisMethods));
            yield return new MethodParentRewriter(typeof(Debris), typeof(DebrisMethods));
            yield return new MethodParentRewriter(typeof(InventoryMenu), typeof(InventoryMenuMethods));
            yield return new MethodParentRewriter(typeof(MenuWithInventory), typeof(MenuWithInventoryMethods));
            yield return new MethodParentRewriter(typeof(GameMenu), typeof(GameMenuMethods));
            yield return new MethodParentRewriter(typeof(CraftingPageMobile), typeof(CraftingPageMobileMethods));

            //Field Rewriters
            yield return new FieldReplaceRewriter(typeof(ItemGrabMenu), "context", "specialObject");

#endif

            // heuristic rewrites
            yield return new HeuristicFieldRewriter(this.ValidateReferencesToAssemblies);
            yield return new HeuristicMethodRewriter(this.ValidateReferencesToAssemblies);

#if HARMONY_2
            // rewrite for SMAPI 3.6 (Harmony 1.x => 2.0 update)
            yield return new Harmony1AssemblyRewriter();
#endif

#if SMAPI_FOR_MOBILE
            // MonoMod fix
            if (!Constants.HarmonyEnabled)
            {
#if HARMONY_2
                yield return new MethodToAnotherStaticMethodRewriter(typeof(Harmony), (method) => method.Name == "Patch", typeof(HarmonyInstanceMethods), "Patch");
                yield return new MethodToAnotherStaticMethodRewriter(typeof(Harmony), (method) => method.Name == "PatchAll" && method.Parameters.Count == 0, typeof(HarmonyInstanceMethods), "PatchAll");
                yield return new MethodToAnotherStaticMethodRewriter(typeof(Harmony), (method) => method.Name == "PatchAll" && method.Parameters.Count == 1, typeof(HarmonyInstanceMethods), "PatchAllToAssembly");
#else
                yield return new MethodToAnotherStaticMethodRewriter(typeof(HarmonyInstance), (method) => method.Name == "Patch", typeof(HarmonyInstanceMethods), "Patch");
                yield return new MethodToAnotherStaticMethodRewriter(typeof(HarmonyInstance), (method) => method.Name == "PatchAll" && method.Parameters.Count == 0, typeof(HarmonyInstanceMethods), "PatchAll");
                yield return new MethodToAnotherStaticMethodRewriter(typeof(HarmonyInstance), (method) => method.Name == "PatchAll" && method.Parameters.Count == 1, typeof(HarmonyInstanceMethods), "PatchAllToAssembly");
#endif
            }
#endif

            /****
            ** detect mod issues
            ****/
            // detect broken code
            yield return new ReferenceToMissingMemberFinder(this.ValidateReferencesToAssemblies);
            yield return new ReferenceToMemberWithUnexpectedTypeFinder(this.ValidateReferencesToAssemblies);

            /****
            ** detect code which may impact game stability
            ****/
#if HARMONY_2
            yield return new TypeFinder(typeof(HarmonyLib.Harmony).FullName, InstructionHandleResult.DetectedGamePatch);
#else
            yield return new TypeFinder(typeof(Harmony.HarmonyInstance).FullName, InstructionHandleResult.DetectedGamePatch);
#endif
            yield return new TypeFinder("System.Runtime.CompilerServices.CallSite", InstructionHandleResult.DetectedDynamic);
            yield return new FieldFinder(typeof(SaveGame).FullName, nameof(SaveGame.serializer), InstructionHandleResult.DetectedSaveSerializer);
            yield return new FieldFinder(typeof(SaveGame).FullName, nameof(SaveGame.farmerSerializer), InstructionHandleResult.DetectedSaveSerializer);
            yield return new FieldFinder(typeof(SaveGame).FullName, nameof(SaveGame.locationSerializer), InstructionHandleResult.DetectedSaveSerializer);
            yield return new EventFinder(typeof(ISpecializedEvents).FullName, nameof(ISpecializedEvents.UnvalidatedUpdateTicked), InstructionHandleResult.DetectedUnvalidatedUpdateTick);
            yield return new EventFinder(typeof(ISpecializedEvents).FullName, nameof(ISpecializedEvents.UnvalidatedUpdateTicking), InstructionHandleResult.DetectedUnvalidatedUpdateTick);

            /****
            ** detect paranoid issues
            ****/
            if (paranoidMode)
            {
                // filesystem access
                yield return new TypeFinder(typeof(System.Console).FullName, InstructionHandleResult.DetectedConsoleAccess);
                yield return new TypeFinder(typeof(System.IO.File).FullName, InstructionHandleResult.DetectedFilesystemAccess);
                yield return new TypeFinder(typeof(System.IO.FileStream).FullName, InstructionHandleResult.DetectedFilesystemAccess);
                yield return new TypeFinder(typeof(System.IO.FileInfo).FullName, InstructionHandleResult.DetectedFilesystemAccess);
                yield return new TypeFinder(typeof(System.IO.Directory).FullName, InstructionHandleResult.DetectedFilesystemAccess);
                yield return new TypeFinder(typeof(System.IO.DirectoryInfo).FullName, InstructionHandleResult.DetectedFilesystemAccess);
                yield return new TypeFinder(typeof(System.IO.DriveInfo).FullName, InstructionHandleResult.DetectedFilesystemAccess);
                yield return new TypeFinder(typeof(System.IO.FileSystemWatcher).FullName, InstructionHandleResult.DetectedFilesystemAccess);

                // shell access
                yield return new TypeFinder(typeof(System.Diagnostics.Process).FullName, InstructionHandleResult.DetectedShellAccess);
            }
        }
    }
}
