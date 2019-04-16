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
            TypeDefinition typeModHooksObject = StardewValley.MainModule.GetType("StardewValley.ModHooks");
            
            TypeDefinition typeObject = StardewValley.MainModule.GetType("StardewValley.Object");
            //foreach (MethodDefinition method in typeObject.Methods) {
            //    if(!method.IsConstructor && method.HasBody)
            //    {
            //        var processor = method.Body.GetILProcessor();
            //        var hook = typeModHooksObject.Methods.FirstOrDefault(m => m.Name == "OnObject_xxx");
            //        var newInstruction = processor.Create(OpCodes.Callvirt, hook);
            //        var firstInstruction = method.Body.Instructions[0];
            //        processor.InsertBefore(firstInstruction, newInstruction);
            //    }
            //}
            StardewValley.Write("./StardewValley.dll");
        }
    }
}
