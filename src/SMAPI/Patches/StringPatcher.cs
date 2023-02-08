using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HarmonyLib;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Framework.Reflection;
using StardewModdingAPI.Internal.Patching;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Minigames;

namespace StardewModdingAPI.Patches
{
    /// <summary>Harmony patches for <see cref="Game1"/> which notify SMAPI for save load stages.</summary>
    /// <remarks>Patch methods must be static for Harmony to work correctly. See the Harmony documentation before renaming patch arguments.</remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    internal class StringPatcher : BasePatcher
    {
        /*********
        ** Fields
        *********/
        /// <summary>Simplifies access to private code.</summary>
        private static Reflector Reflection = null!; // initialized in constructor


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="reflection">Simplifies access to private code.</param>
        /// <param name="onStageChanged">A callback to invoke when the load stage changes.</param>
        public StringPatcher(Reflector reflection)
        {
            StringPatcher.Reflection = reflection;
        }

        /// <inheritdoc />
        public override void Apply(Harmony harmony, IMonitor monitor)
        {
            // detect CreatedInitialLocations and SaveAddedLocations
            harmony.Patch(
                original: this.RequireMethod<string>(nameof(string.Split), new []{typeof(char), typeof(StringSplitOptions)}),
                prefix: this.GetHarmonyMethod(nameof(StringPatcher.Before_Split))
            );
            harmony.Patch(
                original: this.RequireMethod<string>(nameof(string.Split), new []{typeof(string), typeof(StringSplitOptions)}),
                prefix: this.GetHarmonyMethod(nameof(StringPatcher.Before_Split))
            );
            harmony.Patch(
                original: this.RequireMethod<string>(nameof(string.Split), new []{typeof(char), typeof(int), typeof(StringSplitOptions)}),
                prefix: this.GetHarmonyMethod(nameof(StringPatcher.Before_Split))
            );
            harmony.Patch(
                original: this.RequireMethod<string>(nameof(string.Split), new []{typeof(string), typeof(int), typeof(StringSplitOptions)}),
                prefix: this.GetHarmonyMethod(nameof(StringPatcher.Before_Split))
            );
        }


        /*********
        ** Private methods
        *********/
        /// <summary>The method to call before <see cref="string.Split"/>.</summary>
        /// <returns>Returns whether to execute the original method.</returns>
        /// <remarks>This method must be static for Harmony to work correctly. See the Harmony documentation before renaming arguments.</remarks>
        private static bool Before_Split(string __instance, ref StringSplitOptions options)
        {
            if (3 == (uint)options)
            {
                options = StringSplitOptions.RemoveEmptyEntries;
            }
            else if(2 == (uint)options)
            {
                options = StringSplitOptions.None;
            }
            return true;
        }

    }
}
