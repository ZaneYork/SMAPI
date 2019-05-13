using System;
using StardewValley;
using StardewModdingAPI.Framework;
using StardewValley.Menus;

namespace SMDroid
{
    public class ModEntry : ModHooks
    {
        private SCore core;
        /// <summary>SMAPI's content manager.</summary>
        private ContentCoordinator ContentCore { get; set; }

        public static bool ContextInitialize = true;

        public static ModEntry Instance;

        public ModEntry()
        {
            this.core = SCore.Instance;
            Instance = this;
        }
        public override bool OnCommonHook_Prefix(string hookName, object __instance, ref object param1, ref object param2, ref object param3, ref object param4, ref object __result)
        {
            switch (hookName)
            {
                default:
                    return this.core.GameInstance.OnCommonHook_Prefix(hookName, __instance, ref param1, ref param2, ref param3, ref param4, ref __result);
            }
        }
        public override void OnCommonHook_Postfix(string hookName, object __instance, ref object param1, ref object param2, ref object param3, ref object param4, ref bool __state, ref object __result)
        {
            switch (hookName)
            {
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
