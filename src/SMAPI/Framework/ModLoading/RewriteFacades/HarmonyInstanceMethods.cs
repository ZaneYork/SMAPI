using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace StardewModdingAPI.Framework.ModLoading.RewriteFacades
{
    public class HarmonyInstanceMethods
    {
        public static MethodInfo Patch(
            Harmony instance,
            MethodBase original,
            HarmonyMethod prefix = null,
            HarmonyMethod postfix = null,
            HarmonyMethod transpiler = null,
            HarmonyMethod finalizer = null)
        {
            if (Constants.MonoModInit)
                return instance.Patch(original, prefix, postfix, transpiler, finalizer);
            else
                return null;
        }
        public static void PatchAll(Harmony instance)
        {
            if (Constants.MonoModInit)
                instance.PatchAll();
        }
        public static void PatchAllToAssembly(Harmony instance, Assembly assembly)
        {
            if (Constants.MonoModInit)
                instance.PatchAll(assembly);
        }
    }
}
