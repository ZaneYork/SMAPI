using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using StardewModdingAPI.Framework.Reflection;
using StardewModdingAPI.Internal.Patching;
using StardewValley;

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
            harmony.Patch(
                original: this.RequireMethod<string>(nameof(string.Split), new []{typeof(char), typeof(StringSplitOptions)}),
                prefix: this.GetHarmonyMethod(nameof(StringPatcher.Before_Split)),
                postfix: this.GetHarmonyMethod(nameof(StringPatcher.After_Split))
            );
            harmony.Patch(
                original: this.RequireMethod<string>(nameof(string.Split), new []{typeof(string), typeof(StringSplitOptions)}),
                prefix: this.GetHarmonyMethod(nameof(StringPatcher.Before_Split)),
                postfix: this.GetHarmonyMethod(nameof(StringPatcher.After_Split))
            );
            harmony.Patch(
                original: this.RequireMethod<string>(nameof(string.Split), new []{typeof(char), typeof(int), typeof(StringSplitOptions)}),
                prefix: this.GetHarmonyMethod(nameof(StringPatcher.Before_Split)),
                postfix: this.GetHarmonyMethod(nameof(StringPatcher.After_Split))
            );
            harmony.Patch(
                original: this.RequireMethod<string>(nameof(string.Split), new []{typeof(string), typeof(int), typeof(StringSplitOptions)}),
                prefix: this.GetHarmonyMethod(nameof(StringPatcher.Before_Split)),
                postfix: this.GetHarmonyMethod(nameof(StringPatcher.After_Split))
            );
        }


        /*********
        ** Private methods
        *********/
        /// <summary>The method to call before <see cref="string.Split"/>.</summary>
        /// <returns>Returns whether to execute the original method.</returns>
        /// <remarks>This method must be static for Harmony to work correctly. See the Harmony documentation before renaming arguments.</remarks>
        private static bool Before_Split(ref StringSplitOptions options, out bool __state)
        {
            __state = false;
            if ((0x02 & (uint)options) != 0)
            {
                options = (StringSplitOptions)(0xfffffffd & (uint)options);
                __state = true;
            }
            return true;
        }
        private static void After_Split(bool __state, ref string[] __result)
        {
            if (__state)
                for (int i = 0; i < __result.Length; i++)
                    __result[i] = __result[i].Trim();
        }

    }
}
