#if SMAPI_FOR_MOBILE
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.AppCenter.Crashes;
#if HARMONY_2
using HarmonyLib;
#else
using Harmony;
#endif
using StardewModdingAPI.Framework;
using StardewModdingAPI.Framework.Patching;
using StardewValley;

namespace StardewModdingAPI.Patches
{
    internal class MusicBankPatch : IHarmonyPatch
    {
        /*********
        ** Accessors
        *********/
        /// <summary>A unique name for this patch.</summary>
        public string Name => $"{nameof(MusicBankPatch)}";

        /// <summary>Writes messages to the console and log file.</summary>
        private static IMonitor Monitor;

        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public MusicBankPatch(Monitor monitor)
        {
            MusicBankPatch.Monitor = monitor;
        }

        /// <summary>Apply the Harmony patch.</summary>
        /// <param name="harmony">The Harmony instance.</param>
        public void Apply(HarmonyInstance harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(Game1), "FetchMusicXWBPath"),
                new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(MusicBankPatch.Game_FetchMusicXWBPathPrefix))));
        }

        /*********
        ** Private methods
        *********/
        /// <summary>The method to call instead of <see cref="StardewValley.Game1.FetchMusicXWBPath"/>.</summary>
        /// <remarks>This method must be static for Harmony to work correctly. See the Harmony documentation before renaming arguments.</remarks>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony.")]
        private static bool Game_FetchMusicXWBPathPrefix(ref string __result)
        {
            if (Android.OS.Build.VERSION.SdkInt > Android.OS.BuildVersionCodes.Q)
            {
                string path = Path.Combine((string) (Java.Lang.Object) Android.OS.Environment.ExternalStorageDirectory ?? string.Empty, "Android", "obb", SMainActivity.instance.PackageName);
                string str = string.Empty;
                try
                {
                    if (Directory.Exists(path))
                    {
                        foreach (string file in Directory.GetFiles(path))
                        {
                            if (file.Contains("com.chucklefish.stardewvalley.obb"))
                                str = file;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Microsoft.AppCenter.Crashes.Crashes.TrackError(ex, (IDictionary<string, string>) null, Array.Empty<ErrorAttachmentLog>());
                    return true;
                }
                __result = str;
                return false;
            }
            return true;
        }
    }
}
#endif
