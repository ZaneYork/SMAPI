using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewModdingAPI.Framework.ModLoading;
using StardewModdingAPI.Framework.ModLoading.Finders;
using StardewModdingAPI.Framework.ModLoading.RewriteFacades;
using StardewModdingAPI.Framework.ModLoading.Rewriters;
using StardewValley;
#if SMAPI_FOR_MOBILE
using HarmonyLib;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
#endif
using StardewValley.Locations;

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
        private readonly ISet<string> ValidateReferencesToAssemblies = new HashSet<string> { "StardewModdingAPI", "Stardew Valley", "StardewValley", "Netcode" };

#if SMAPI_FOR_MOBILE
        private readonly IMonitor Monitor;

        public InstructionMetadata(IMonitor monitor)
        {
            this.Monitor = monitor;
        }
#endif

        /*********
        ** Public methods
        *********/
        /// <summary>Get rewriters which detect or fix incompatible CIL instructions in mod assemblies.</summary>
        /// <param name="paranoidMode">Whether to detect paranoid mode issues.</param>
        /// <param name="platformChanged">Whether the assembly was rewritten for crossplatform compatibility.</param>
        /// <param name="rewriteMods">Whether to get handlers which rewrite mods for compatibility.</param>
        public IEnumerable<IInstructionHandler> GetHandlers(bool paranoidMode, bool platformChanged, bool rewriteMods)
        {
            /****
            ** rewrite CIL to fix incompatible code
            ****/
            // rewrite for crossplatform compatibility
            if (rewriteMods)
            {
                // rewrite for Stardew Valley 1.5
                yield return new FieldReplaceRewriter()
                    .AddField(typeof(DecoratableLocation), "furniture", typeof(GameLocation), nameof(GameLocation.furniture))
                    .AddField(typeof(Farm), "resourceClumps", typeof(GameLocation), nameof(GameLocation.resourceClumps))
                    .AddField(typeof(MineShaft), "resourceClumps", typeof(GameLocation), nameof(GameLocation.resourceClumps));

#if SMAPI_FOR_MOBILE
                // module rewrite for .Net 5 runtime assemblies
                yield return new ModuleReferenceRewriter("System.Collections", "System.Collections", typeof(System.Collections.CollectionBase).Assembly);
                yield return new ModuleReferenceRewriter("System.Runtime", "System.Runtime", typeof(System.Collections.CollectionBase).Assembly);

                yield return new TypeFieldToAnotherTypePropertyRewriter(typeof(Game1), typeof(Game1Methods), "onScreenMenus", "onScreenMenus");

                // Menu fix
                yield return new TypeFieldToAnotherTypePropertyRewriter(typeof(MenuWithInventory), typeof(MenuWithInventoryMethods), "trashCan", nameof(MenuWithInventoryMethods.TrashCanProp));
                yield return new TypeFieldToAnotherTypePropertyRewriter(typeof(ItemGrabMenu), typeof(ItemGrabMenuMethods), "fillStacksButton", nameof(ItemGrabMenuMethods.FillStacksButtonProp));
                yield return new TypeFieldToAnotherTypePropertyRewriter(typeof(AnimalQueryMenu), typeof(AnimalQueryMenuMethods), "allowReproductionButton", nameof(AnimalQueryMenuMethods.AllowReproductionButtonProp));
                yield return new TypeFieldToAnotherTypePropertyRewriter(typeof(AnimalQueryMenu), typeof(AnimalQueryMenuMethods), "sellButton", nameof(AnimalQueryMenuMethods.SellButtonProp));
                yield return new TypeFieldToAnotherTypePropertyRewriter(typeof(AnimalQueryMenu), typeof(AnimalQueryMenuMethods), "moveHomeButton", nameof(AnimalQueryMenuMethods.MoveHomeButtonProp));
                // TextBox fix
                yield return new TypePropertyToAnotherTypeMethodRewriter(typeof(TextBox), typeof(TextBoxMethods), "Selected", null, "SelectedSetter");

                // Rewrite Missing Type
                yield return new TypeReferenceRewriter("StardewValley.Menus.CraftingPage", typeof(CraftingPageMobile));
                yield return new TypeReferenceRewriter("StardewValley.Menus.InventoryMenu/BorderSide", typeof(InventoryMenuMethods.BorderSide));

                //Method Rewrites
                yield return new MethodParentRewriter(typeof(Game1), typeof(Game1Methods));
                yield return new MethodParentRewriter(typeof(IClickableMenu), typeof(IClickableMenuMethods));
                yield return new MethodParentRewriter(typeof(SpriteText), typeof(SpriteTextMethods));
                yield return new MethodParentRewriter(typeof(Utility), typeof(UtilityMethods));

                //Constructor Rewrites
                yield return new MethodParentRewriter(typeof(MapPage), typeof(MapPageMethods));
                yield return new MethodParentRewriter(typeof(ItemGrabMenu), typeof(ItemGrabMenuMethods));
                yield return new MethodParentRewriter(typeof(InventoryMenu), typeof(InventoryMenuMethods));
                yield return new MethodParentRewriter(typeof(MenuWithInventory), typeof(MenuWithInventoryMethods));
                yield return new MethodParentRewriter(typeof(GameMenu), typeof(GameMenuMethods));
                yield return new MethodParentRewriter(typeof(CraftingPageMobile), typeof(CraftingPageMobileMethods));

#endif

                // heuristic rewrites
                yield return new HeuristicFieldRewriter(this.ValidateReferencesToAssemblies);
                yield return new HeuristicMethodRewriter(this.ValidateReferencesToAssemblies);
#if SMAPI_FOR_MOBILE
                yield return new HeuristicFieldAccessibilityRewriter(this.ValidateReferencesToAssemblies);
#endif

                // rewrite for Stardew Valley 1.5.5
                if (platformChanged)
                    yield return new MethodParentRewriter(typeof(SpriteBatch), typeof(SpriteBatchFacade));
                yield return new ArchitectureAssemblyRewriter();

                // detect Harmony & rewrite for SMAPI 3.12 (Harmony 1.x => 2.0 update)
                yield return new HarmonyRewriter();

#if SMAPI_DEPRECATED
                // detect issues for SMAPI 4.0.0
                yield return new LegacyAssemblyFinder();
#endif

#if SMAPI_FOR_MOBILE
                // MonoMod fix
                if (!Constants.HarmonyEnabled)
                {
                    yield return new MethodToAnotherStaticMethodRewriter(typeof(Harmony), (method) => method.Name == "Patch", typeof(HarmonyInstanceMethods), "Patch");
                    yield return new MethodToAnotherStaticMethodRewriter(typeof(Harmony), (method) => method.Name == "PatchAll" && method.Parameters.Count == 0, typeof(HarmonyInstanceMethods), "PatchAll");
                    yield return new MethodToAnotherStaticMethodRewriter(typeof(Harmony), (method) => method.Name == "PatchAll" && method.Parameters.Count == 1, typeof(HarmonyInstanceMethods), "PatchAllToAssembly");
                }

                if(Constants.RewriteMissing)
                    yield return new ReferenceToMissingMemberRewriter(this.ValidateReferencesToAssemblies);
#endif
            }
            else
                yield return new HarmonyRewriter(shouldRewrite: false);

            /****
            ** detect mod issues
            ****/
            // detect broken code
            yield return new ReferenceToMissingMemberFinder(this.ValidateReferencesToAssemblies);
            yield return new ReferenceToMemberWithUnexpectedTypeFinder(this.ValidateReferencesToAssemblies);

            /****
            ** detect code which may impact game stability
            ****/
            yield return new FieldFinder(typeof(SaveGame).FullName!, new[] { nameof(SaveGame.serializer), nameof(SaveGame.farmerSerializer), nameof(SaveGame.locationSerializer) }, InstructionHandleResult.DetectedSaveSerializer);
            yield return new EventFinder(typeof(ISpecializedEvents).FullName!, new[] { nameof(ISpecializedEvents.UnvalidatedUpdateTicked), nameof(ISpecializedEvents.UnvalidatedUpdateTicking) }, InstructionHandleResult.DetectedUnvalidatedUpdateTick);

            /****
            ** detect paranoid issues
            ****/
            if (paranoidMode)
            {
                // filesystem access
                yield return new TypeFinder(typeof(System.Console).FullName!, InstructionHandleResult.DetectedConsoleAccess);
                yield return new TypeFinder(
                    new[]
                    {
                        typeof(System.IO.File).FullName!,
                        typeof(System.IO.FileStream).FullName!,
                        typeof(System.IO.FileInfo).FullName!,
                        typeof(System.IO.Directory).FullName!,
                        typeof(System.IO.DirectoryInfo).FullName!,
                        typeof(System.IO.DriveInfo).FullName!,
                        typeof(System.IO.FileSystemWatcher).FullName!
                    },
                    InstructionHandleResult.DetectedFilesystemAccess
                );

                // shell access
                yield return new TypeFinder(typeof(System.Diagnostics.Process).FullName!, InstructionHandleResult.DetectedShellAccess);
            }
        }
    }
}
