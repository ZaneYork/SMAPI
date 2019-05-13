using System.Collections.Generic;
using StardewValley.Menus;

namespace StardewModdingAPI
{
    /// <summary>The implementation for a Stardew Valley mod.</summary>
    public interface IMod
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Provides simplified APIs for writing mods.</summary>
        IModHelper Helper { get; }

        /// <summary>Writes messages to the console and log file.</summary>
        IMonitor Monitor { get; }

        /// <summary>The mod's manifest.</summary>
        IManifest ModManifest { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        void Entry(IModHelper helper);

        List<OptionsElement> GetConfigMenuItems();

        bool ApplyForHooks();

        bool OnCommonHook_Prefix(string hookName, object __instance, ref object param1, ref object param2, ref object param3, ref object param4, ref object __result);

        void OnCommonHook_Postfix(string hookName, object __instance, ref object param1, ref object param2, ref object param3, ref object param4, ref bool __state, ref object __result);
        bool OnCommonStaticHook_Prefix(string hookName, ref object param1, ref object param2, ref object param3, ref object param4, ref object param5, ref object __result);
        void OnCommonStaticHook_Postfix(string hookName, ref object param1, ref object param2, ref object param3, ref object param4, ref object param5, ref bool __state, ref object __result);
        void OnCommonHook10_Postfix(string hookName, object __instance, ref object param1, ref object param2, ref object param3, ref object param4, ref object param5, ref object param6, ref object param7, ref object param8, ref object param9, ref bool __state, ref object __result);
        bool OnCommonHook10_Prefix(string hookName, object __instance, ref object param1, ref object param2, ref object param3, ref object param4, ref object param5, ref object param6, ref object param7, ref object param8, ref object param9, ref object __result);
        void OnCommonStaticHook10_Postfix(string hookName, ref object param1, ref object param2, ref object param3, ref object param4, ref object param5, ref object param6, ref object param7, ref object param8, ref object param9, ref object param10, ref bool __state, ref object __result);
        bool OnCommonStaticHook10_Prefix(string hookName, ref object param1, ref object param2, ref object param3, ref object param4, ref object param5, ref object param6, ref object param7, ref object param8, ref object param9, ref object param10, ref object __result);

        /// <summary>Get an API that other mods can access. This is always called after <see cref="Entry"/>.</summary>
        object GetApi();
    }
}
