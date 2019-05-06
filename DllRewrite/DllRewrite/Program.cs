using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DllRewrite
{
    class Program
    {
        static void Main(string[] args)
        {
            MethodPatcher mp = new MethodPatcher();
            AssemblyDefinition StardewValley = mp.InsertModHooks();
            StardewValley.Write("./StardewValley.dll");
            //AssemblyDefinition MonoFramework = mp.InsertMonoHooks();
            //MonoFramework.Write("./MonoGame.Framework.dll");
        }
    }
}
