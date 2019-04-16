using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DllRewrite
{
    class MethodPatcher
    {
        DefaultAssemblyResolver resolver;
        AssemblyDefinition StardewModdingAPI;
        AssemblyDefinition StardewValley;
        AssemblyDefinition MonoGame_Framework;
        AssemblyDefinition mscorlib;
        Dictionary<string, MethodReference> methodDict = new Dictionary<string, MethodReference>();
        Dictionary<string, FieldReference> fieldDict = new Dictionary<string, FieldReference>();
        Dictionary<string, TypeReference> typeDict = new Dictionary<string, TypeReference>();
        public MethodPatcher()
        {
            this.resolver = new DefaultAssemblyResolver();
            this.resolver.AddSearchDirectory("./assemblies");
            this.mscorlib = this.resolver.Resolve(new AssemblyNameReference("mscorlib", new Version("0.0.0.0")));
            this.MonoGame_Framework = this.resolver.Resolve(new AssemblyNameReference("MonoGame.Framework", new Version("0.0.0.0")));
            this.StardewValley = this.resolver.Resolve(new AssemblyNameReference("StardewValley", new Version("1.3.0.0")));
            this.StardewModdingAPI = this.resolver.Resolve(new AssemblyNameReference("StardewModdingAPI", new Version("0.0.0.0")));
        }
        public void InsertModHook(string name, TypeReference[] paraTypes, TypeReference returnType)
        {
            TypeDefinition typeModHooksObject = this.StardewValley.MainModule.GetType("StardewValley.ModHooks");
            var hook = new MethodDefinition(name, MethodAttributes.Virtual | MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.HideBySig, returnType);
            foreach (TypeReference typeReference in paraTypes)
            {
                hook.Parameters.Add(new ParameterDefinition(typeReference));
            }
            switch (returnType.FullName)
            {
                case "System.Void":
                    break;
                case "System.Boolean":
                case "System.Int32":
                    hook.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
                    break;
                default:
                    hook.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
                    break;
            }
            hook.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            if(typeModHooksObject.Methods.FirstOrDefault(item=>item.Name == hook.Name) == null)
            {
                typeModHooksObject.Methods.Add(hook);
            }

        }
        public TypeReference GetTypeReference(string fullname)
        {
            return this.GetTypeReference(fullname, this.mscorlib);
        }
        public TypeReference GetTypeReference(string fullname, AssemblyDefinition assemblyDefinition)
        {
            if (this.typeDict.ContainsKey(fullname))
            {
                return this.typeDict[fullname];
            }
            TypeReference type = assemblyDefinition.MainModule.GetType(fullname).Resolve();
            type = this.StardewValley.MainModule.ImportReference(type);
            this.typeDict.Add(fullname, type);
            return type;
        }
        public FieldReference GetFieldReference(string name, string typename, AssemblyDefinition assemblyDefinition)
        {
            string fullname = typename + "::" + name;
            if (this.fieldDict.ContainsKey(fullname))
            {
                return this.fieldDict[fullname];
            }
            FieldReference field = assemblyDefinition.MainModule.Types.FirstOrDefault(item => item.FullName == typename).Fields.FirstOrDefault(item => item.Name == name);
            if(assemblyDefinition.FullName != this.StardewValley.FullName)
            {
                field = this.StardewValley.MainModule.ImportReference(field);
            }
            this.fieldDict.Add(fullname, field);
            return field;
        }
        public MethodReference GetMethodReference(string name, string typename, AssemblyDefinition assemblyDefinition)
        {
            string fullname = typename + "." + name;
            if (this.fieldDict.ContainsKey(fullname))
            {
                return this.methodDict[fullname];
            }
            MethodReference method = assemblyDefinition.MainModule.Types.FirstOrDefault(item => item.FullName == typename).Methods.FirstOrDefault(item => item.Name == name);
            if (assemblyDefinition.FullName != this.StardewValley.FullName)
            {
                method = this.StardewValley.MainModule.ImportReference(method);
            }
            this.methodDict.Add(fullname, method);
            return method;
        }
        public void ApplyGamePatch()
        {
            // Game.hook
            TypeDefinition typeGame1 = this.StardewValley.MainModule.GetType("StardewValley.Game1");
            MethodDefinition constructor = typeGame1.Methods.FirstOrDefault(m => m.IsConstructor);
            ILProcessor processor = constructor.Body.GetILProcessor();
            List<Instruction> instructions = new List<Instruction>();
            Instruction jointPoint = constructor.Body.Instructions[0];
            instructions.Add(processor.Create(OpCodes.Newobj, this.GetMethodReference(".ctor", "SMDroid.ModEntry", this.StardewModdingAPI)));
            instructions.Add(processor.Create(OpCodes.Stsfld, this.GetFieldReference("hooks", "StardewValley.Game1", this.StardewValley)));
            this.InsertInstructions(processor, jointPoint, instructions);

            // Back Button Fix
            MethodDefinition method = typeGame1.Methods.FirstOrDefault(m => m.Name == "_updateAndroidMenus");
            processor = method.Body.GetILProcessor();
            FieldReference inputField = this.GetFieldReference("input", "StardewValley.Game1", this.StardewValley);
            processor.Replace(method.Body.Instructions[0], processor.Create(OpCodes.Ldsfld, inputField));
            TypeDefinition typeInputState = this.StardewValley.MainModule.GetType("StardewValley.InputState");
            var GetGamePadState = typeInputState.Methods.FirstOrDefault(m => m.Name == "GetGamePadState");
            processor.Replace(method.Body.Instructions[1], processor.Create(OpCodes.Callvirt, GetGamePadState));
        }
        public void ApplyHookEntry(TypeDefinition targetType, string methodname, string hookname, bool isPrefix)
        {
            TypeDefinition typeModHooksObject = this.StardewValley.MainModule.GetType("StardewValley.ModHooks");
            var hook = typeModHooksObject.Methods.FirstOrDefault(m => m.Name == hookname);
            foreach (MethodDefinition method in targetType.Methods)
            {
                if (!method.IsConstructor && method.HasBody && method.Name == methodname)
                {
                    var processor = method.Body.GetILProcessor();
                    Instruction jointPoint;
                    List<Instruction> instructions = new List<Instruction>();
                    FieldReference hooksField = this.GetFieldReference("hooks", "StardewValley.Game1", this.StardewValley);
                    if (!isPrefix)
                    {
                        jointPoint = method.Body.Instructions[method.Body.Instructions.Count - 1];
                        if (method.ReturnType.FullName != "System.Void")
                        {
                            method.Body.Variables.Add(new VariableDefinition(method.ReturnType));
                            instructions.Add(this._createStlocInstruction(processor, (byte)(method.Body.Variables.Count - 1)));
                        }
                        instructions.Add(processor.Create(OpCodes.Ldsfld, hooksField));
                        if (!method.IsStatic)
                        {
                            instructions.Add(processor.Create(OpCodes.Ldarg_0));
                        }
                        for(byte i = (method.IsStatic ? (byte)0 : (byte)1); i < method.Parameters.Count + (method.IsStatic ? (byte)0 : (byte)1); i++)
                        {
                            instructions.Add(this._createLdargsInstruction(processor, i));
                        }
                        if (method.ReturnType.FullName != "System.Void")
                        {
                            instructions.Add(processor.Create(OpCodes.Ldloca_S, (byte)(method.Body.Variables.Count - 1)));
                            instructions.Add(processor.Create(OpCodes.Callvirt, hook));
                            instructions.Add(this._createLdlocInstruction(processor, (byte)(method.Body.Variables.Count - 1)));
                        }
                        else
                        {
                            instructions.Add(processor.Create(OpCodes.Callvirt, hook));
                        }
                        for(int i = 0; i < method.Body.Instructions.Count - 1; i++)
                        {
                            Instruction origin = method.Body.Instructions[i];
                            if (origin.OpCode == OpCodes.Ret)
                            {
                                processor.Replace(origin, processor.Create(OpCodes.Br, instructions[0]));
                            }
                        }
                    }
                    else
                    {
                        jointPoint = method.Body.Instructions[0];
                        if (method.ReturnType.FullName != "System.Void")
                        {
                            method.Body.Variables.Add(new VariableDefinition(method.ReturnType));
                        }
                        instructions.Add(processor.Create(OpCodes.Ldsfld, hooksField));
                        if (!method.IsStatic)
                        {
                            instructions.Add(processor.Create(OpCodes.Ldarg_0));
                        }
                        for (byte i = (method.IsStatic ? (byte)0 : (byte)1); i < method.Parameters.Count + (method.IsStatic ? (byte)0 : (byte)1); i++)
                        {
                            instructions.Add(this._createLdargsInstruction(processor, i));
                        }
                        if (method.ReturnType.FullName != "System.Void")
                        {
                            instructions.Add(processor.Create(OpCodes.Ldloca_S, (byte)(method.Body.Variables.Count - 1)));
                            instructions.Add(processor.Create(OpCodes.Callvirt, hook));
                            instructions.Add(processor.Create(OpCodes.Brtrue, jointPoint));
                            instructions.Add(this._createLdlocInstruction(processor, (byte)(method.Body.Variables.Count - 1)));
                            instructions.Add(processor.Create(OpCodes.Ret));
                        }
                        else
                        {
                            instructions.Add(processor.Create(OpCodes.Callvirt, hook));
                            instructions.Add(processor.Create(OpCodes.Brtrue, jointPoint));
                            instructions.Add(processor.Create(OpCodes.Ret));
                        }
                    }
                    this.InsertInstructions(processor, jointPoint, instructions);
                }
            }

        }
        private Instruction _createStlocInstruction(ILProcessor processor, byte index)
        {
            switch (index)
            {
                case 0:
                    return processor.Create(OpCodes.Stloc_0);
                case 1:
                    return processor.Create(OpCodes.Stloc_1);
                case 2:
                    return processor.Create(OpCodes.Stloc_2);
                case 3:
                    return processor.Create(OpCodes.Stloc_3);
                default:
                    return processor.Create(OpCodes.Stloc_S, index);
            }
        }
        private Instruction _createLdlocInstruction(ILProcessor processor, byte index)
        {
            switch (index)
            {
                case 0:
                    return processor.Create(OpCodes.Ldloc_0);
                case 1:
                    return processor.Create(OpCodes.Ldloc_1);
                case 2:
                    return processor.Create(OpCodes.Ldloc_2);
                case 3:
                    return processor.Create(OpCodes.Ldloc_3);
                default:
                    return processor.Create(OpCodes.Ldloc_S, index);
            }
        }
        private Instruction _createLdargsInstruction(ILProcessor processor, byte index)
        {
            switch (index)
            {
                case 0:
                    return processor.Create(OpCodes.Ldarg_0);
                case 1:
                    return processor.Create(OpCodes.Ldarg_1);
                case 2:
                    return processor.Create(OpCodes.Ldarg_2);
                case 3:
                    return processor.Create(OpCodes.Ldarg_3);
                default:
                    return processor.Create(OpCodes.Ldarg_S, index);
            }
        }
        private void InsertInstructions(ILProcessor processor, Instruction jointPoint, List<Instruction> instructions)
        {
            foreach(Instruction instruction in instructions)
            {
                processor.InsertBefore(jointPoint, instruction);
            }
        }
        public AssemblyDefinition InsertModHooks()
        {
            this.ApplyGamePatch();
            TypeDefinition typeGame1 = this.StardewValley.MainModule.GetType("StardewValley.Game1");
            this.InsertModHook("OnGame1_Update_Prefix", new TypeReference[] {
                this.GetTypeReference("StardewValley.Game1", this.StardewValley),
                this.GetTypeReference("Microsoft.Xna.Framework.GameTime", this.MonoGame_Framework)},
                this.GetTypeReference("System.Boolean"));
            this.ApplyHookEntry(typeGame1, "Update", "OnGame1_Update_Prefix", true);
            this.InsertModHook("OnGame1_Update_Postfix", new TypeReference[] {
                this.GetTypeReference("StardewValley.Game1", this.StardewValley),
                this.GetTypeReference("Microsoft.Xna.Framework.GameTime", this.MonoGame_Framework)},
                this.GetTypeReference("System.Void"));
            this.ApplyHookEntry(typeGame1, "Update", "OnGame1_Update_Postfix", false);

            this.InsertModHook("OnGame1_CreateContentManager_Prefix", new TypeReference[] {
                this.GetTypeReference("StardewValley.Game1", this.StardewValley),
                this.GetTypeReference("System.IServiceProvider"),
                this.GetTypeReference("System.String"),
                new ByReferenceType(this.GetTypeReference("StardewValley.LocalizedContentManager", this.StardewValley)) },
                this.GetTypeReference("System.Boolean"));
            this.ApplyHookEntry(typeGame1, "CreateContentManager", "OnGame1_CreateContentManager_Prefix", true);

            this.InsertModHook("OnGame1_Draw_Prefix", new TypeReference[] {
                this.GetTypeReference("StardewValley.Game1", this.StardewValley),
                this.GetTypeReference("Microsoft.Xna.Framework.GameTime", this.MonoGame_Framework)},
                this.GetTypeReference("System.Boolean"));
            this.ApplyHookEntry(typeGame1, "Draw", "OnGame1_Draw_Prefix", true);


            this.InsertModHook("OnObject_canBePlacedHere_Prefix", new TypeReference[] {
                this.GetTypeReference("StardewValley.Object", this.StardewValley),
                this.GetTypeReference("StardewValley.GameLocation", this.StardewValley),
                this.GetTypeReference("Microsoft.Xna.Framework.Vector2", this.MonoGame_Framework),
                new ByReferenceType(this.GetTypeReference("System.Boolean")) },
                this.GetTypeReference("System.Boolean"));
            TypeDefinition typeObject = this.StardewValley.MainModule.GetType("StardewValley.Object");
            this.ApplyHookEntry(typeObject, "canBePlacedHere", "OnObject_canBePlacedHere_Prefix", true);

            this.InsertModHook("OnObject_checkForAction_Prefix", new TypeReference[] {
                this.GetTypeReference("StardewValley.Object", this.StardewValley),
                this.GetTypeReference("StardewValley.Farmer", this.StardewValley),
                this.GetTypeReference("System.Boolean"),
                new ByReferenceType(this.GetTypeReference("System.Boolean"))},
                this.GetTypeReference("System.Boolean"));
            this.ApplyHookEntry(typeObject, "checkForAction", "OnObject_checkForAction_Prefix", true);

            this.InsertModHook("OnObject_isIndexOkForBasicShippedCategory_Postfix", new TypeReference[] {
                this.GetTypeReference("System.Int32"),
                new ByReferenceType(this.GetTypeReference("System.Boolean")) },
                this.GetTypeReference("System.Void"));
            this.ApplyHookEntry(typeObject, "isIndexOkForBasicShippedCategory", "OnObject_isIndexOkForBasicShippedCategory_Postfix", false);

            return this.StardewValley;
        }
    }
}
