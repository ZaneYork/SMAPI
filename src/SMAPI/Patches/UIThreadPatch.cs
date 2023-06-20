#if SMAPI_FOR_MOBILE
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using HarmonyLib;
using StardewModdingAPI.Internal.Patching;

namespace StardewModdingAPI.Patches
{
    /// <summary>A Harmony patch for <see cref="Microsoft.Xna.Framework.Threading"/> .</summary>
    /// <remarks>Patch methods must be static for Harmony to work correctly. See the Harmony documentation before renaming patch arguments.</remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    internal class UIThreadPatch : BasePatcher
    {
        /*********
        ** Fields
        *********/

        /*********
        ** Fields
        *********/
        /// <summary>Writes messages to the console and log file.</summary>
        private static IMonitor Monitor;

        /*********
        ** Accessors
        *********/
        /// <summary>A unique name for this patch.</summary>
        public string Name => nameof(UIThreadPatch);


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitorForGame">Writes messages to the console and log file on behalf of the game.</param>
        public UIThreadPatch(IMonitor monitor)
        {
            Monitor = monitor;
        }

        /// <summary>Apply the Harmony patch.</summary>
        /// <param name="harmony">The Harmony instance.</param>
        public override void Apply(Harmony harmony, IMonitor monitor)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(Microsoft.Xna.Framework.Point).Assembly.GetType("Microsoft.Xna.Framework.Threading"), "EnsureUIThread"),
                prefix: this.GetHarmonyMethod(nameof(UIThreadPatch.UIThreadPatch_Prefix))
            );
        }

        /*********
        ** Private methods
        *********/
        /// <summary>The method to call instead of <see cref="Microsoft.Xna.Framework.Threading.EnsureUIThread"/>.</summary>
        /// <returns>Returns whether to execute the original method.</returns>
        private static bool UIThreadPatch_Prefix()
        {
            return false;
        }
    }
}
#endif
