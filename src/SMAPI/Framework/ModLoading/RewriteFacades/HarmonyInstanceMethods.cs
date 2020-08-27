#if SMAPI_FOR_MOBILE
using System.Reflection;
using System.Reflection.Emit;
#if HARMONY_2
using HarmonyLib;
#else
using Harmony;
#endif

namespace StardewModdingAPI.Framework.ModLoading.RewriteFacades
{
    public class HarmonyInstanceMethods
    {
        public static MethodInfo Patch(
#if HARMONY_2
            Harmony instance,
#else
            HarmonyInstance instance,
#endif
            MethodBase original,
            HarmonyMethod prefix = null,
            HarmonyMethod postfix = null,
            HarmonyMethod transpiler = null,
            HarmonyMethod finalizer = null)
        {
            if (Constants.HarmonyEnabled)
#if HARMONY_2
                return instance.Patch(original, prefix, postfix, transpiler, finalizer);
#else
                return instance.Patch(original, prefix, postfix, transpiler);
#endif
            else
                return null;
        }
        public static void PatchAll(
#if HARMONY_2
            Harmony instance
#else
            HarmonyInstance instance
#endif
            )
        {
            if (Constants.HarmonyEnabled)
                instance.PatchAll();
        }
        public static void PatchAllToAssembly(
#if HARMONY_2
            Harmony instance,
#else
            HarmonyInstance instance,
#endif
            Assembly assembly)
        {
            if (Constants.HarmonyEnabled)
                instance.PatchAll(assembly);
        }
    }
}
#endif
