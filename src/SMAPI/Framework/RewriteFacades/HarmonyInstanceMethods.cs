using System.Reflection;
using System.Reflection.Emit;
using Harmony;

namespace StardewModdingAPI.Framework.RewriteFacades
{
    public class HarmonyInstanceMethods
    {
        public static DynamicMethod Patch(HarmonyInstance instance, MethodBase original, HarmonyMethod prefix = null, HarmonyMethod postfix = null, HarmonyMethod transpiler = null)
        {
            if (Constants.MonoModInit)
                return instance.Patch(original, prefix, postfix, transpiler);
            else
                return null;
        }
        public static void PatchAll(HarmonyInstance instance)
        {
            if (Constants.MonoModInit)
                instance.PatchAll();
        }
        public static void PatchAllToAssembly(HarmonyInstance instance, Assembly assembly)
        {
            if (Constants.MonoModInit)
                instance.PatchAll(assembly);
        }
    }
}
