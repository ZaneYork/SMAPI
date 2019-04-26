using System;
using System.Collections.Generic;
using System.Linq;
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
        public void InsertModHook(string name, TypeReference[] paraTypes, string[] paraNames, TypeReference returnType)
        {
            TypeDefinition typeModHooksObject = this.StardewValley.MainModule.GetType("StardewValley.ModHooks");
            var hook = new MethodDefinition(name, MethodAttributes.Virtual | MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.HideBySig, returnType);
            for(int i = 0; i< paraTypes.Length; i++)
            {
                ParameterDefinition define = new ParameterDefinition(paraNames[i], ParameterAttributes.None, paraTypes[i]);
                hook.Parameters.Add(define);
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

            // isRaining and isDebrisWeather
            PropertyDefinition propertyDefinition = new PropertyDefinition("isRaining", PropertyAttributes.None, this.GetTypeReference("System.Boolean"));
            propertyDefinition.GetMethod = new MethodDefinition("get_isRaining", MethodAttributes.Public | MethodAttributes.ReuseSlot | MethodAttributes.SpecialName | MethodAttributes.Static | MethodAttributes.HideBySig, this.GetTypeReference("System.Boolean"));
            propertyDefinition.GetMethod.SemanticsAttributes = MethodSemanticsAttributes.Getter;
            processor = propertyDefinition.GetMethod.Body.GetILProcessor();
            TypeDefinition typeRainManager = this.StardewValley.MainModule.GetType("StardewValley.RainManager");
            MethodDefinition getMethod = typeRainManager.Methods.FirstOrDefault(m => m.Name == "get_Instance");
            processor.Emit(OpCodes.Callvirt, getMethod);
            FieldReference isRainingField = this.GetFieldReference("isRaining", "StardewValley.RainManager", this.StardewValley);
            processor.Emit(OpCodes.Ldfld, isRainingField);
            processor.Emit(OpCodes.Ret);
            typeGame1.Methods.Add(propertyDefinition.GetMethod);
            typeGame1.Properties.Add(propertyDefinition);

            propertyDefinition = new PropertyDefinition("isDebrisWeather", PropertyAttributes.None, this.GetTypeReference("System.Boolean"));
            propertyDefinition.GetMethod = new MethodDefinition("get_isDebrisWeather", MethodAttributes.Public | MethodAttributes.ReuseSlot | MethodAttributes.SpecialName | MethodAttributes.Static | MethodAttributes.HideBySig, this.GetTypeReference("System.Boolean"));
            propertyDefinition.GetMethod.SemanticsAttributes = MethodSemanticsAttributes.Getter;
            processor = propertyDefinition.GetMethod.Body.GetILProcessor();
            TypeDefinition typeWeatherDebrisManager = this.StardewValley.MainModule.GetType("StardewValley.WeatherDebrisManager");
            getMethod = typeWeatherDebrisManager.Methods.FirstOrDefault(m => m.Name == "get_Instance");
            processor.Emit(OpCodes.Callvirt, getMethod);
            FieldReference isDebrisWeatherField = this.GetFieldReference("isDebrisWeather", "StardewValley.WeatherDebrisManager", this.StardewValley);
            processor.Emit(OpCodes.Ldfld, isDebrisWeatherField);
            processor.Emit(OpCodes.Ret);
            typeGame1.Methods.Add(propertyDefinition.GetMethod);
            typeGame1.Properties.Add(propertyDefinition);

            //HUDMessage..ctor
            TypeDefinition typeHUDMessage = this.StardewValley.MainModule.GetType("StardewValley.HUDMessage");
            MethodDefinition hudConstructor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.ReuseSlot | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName | MethodAttributes.HideBySig, this.GetTypeReference("System.Void"));
            hudConstructor.Parameters.Add(new ParameterDefinition(this.GetTypeReference("System.String")));
            hudConstructor.Parameters.Add(new ParameterDefinition(this.GetTypeReference("System.Int32")));
            processor = hudConstructor.Body.GetILProcessor();
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldarg_1);
            processor.Emit(OpCodes.Ldarg_2);
            processor.Emit(OpCodes.Ldc_I4_M1);
            MethodDefinition targetConstructor = typeHUDMessage.Methods.FirstOrDefault(item => {
                if(item.Parameters.Count == 3 && item.Parameters[0].ParameterType.FullName == "System.String"
                    && item.Parameters[1].ParameterType.FullName == "System.Int32" && item.Parameters[1].ParameterType.FullName == "System.Int32")
                {
                    return true;
                }
                return false;
            });
            processor.Emit(OpCodes.Call, targetConstructor);
            processor.Emit(OpCodes.Ret);
            typeHUDMessage.Methods.Add(hudConstructor);


            // Back Button Fix
            MethodDefinition method = typeGame1.Methods.FirstOrDefault(m => m.Name == "_updateAndroidMenus");
            processor = method.Body.GetILProcessor();
            FieldReference inputField = this.GetFieldReference("input", "StardewValley.Game1", this.StardewValley);
            processor.Replace(method.Body.Instructions[0], processor.Create(OpCodes.Ldsfld, inputField));
            TypeDefinition typeInputState = this.StardewValley.MainModule.GetType("StardewValley.InputState");
            var GetGamePadState = typeInputState.Methods.FirstOrDefault(m => m.Name == "GetGamePadState");
            processor.Replace(method.Body.Instructions[1], processor.Create(OpCodes.Callvirt, GetGamePadState));
        }
        public void ApplyCommonMidHookEntry(TypeDefinition targetType, Func<MethodDefinition, bool> methodChecker, Func<Instruction, bool> jointPointChecker, int jointPointOffset, string hookname)
        {
            byte i, j;
            MethodDefinition targetMethod = targetType.Methods.FirstOrDefault(method => methodChecker(method));
            string qualifyName = targetType.FullName + "." + targetMethod.Name + "_mid";
            MethodDefinition prefixHook = this.StardewValley.MainModule.GetType("StardewValley.ModHooks").Methods.FirstOrDefault(m => m.Name == (hookname + "_Prefix"));
            ILProcessor iLProcessor = targetMethod.Body.GetILProcessor();
            FieldReference field = this.GetFieldReference("hooks", "StardewValley.Game1", this.StardewValley);
            List<Instruction> instructions = new List<Instruction>();
            byte returnIndex = 0;
            byte parameterIndexBegin = 0;
            byte parameterIndexEnd = 0;
            byte parameterOffset = targetMethod.IsStatic ? ((byte)0) : ((byte)1);
            byte stateIndex = (byte)targetMethod.Body.Variables.Count;
            targetMethod.Body.Variables.Add(new VariableDefinition(this.GetTypeReference("System.Boolean")));
            if (targetMethod.ReturnType.FullName != "System.Void")
            {
                returnIndex = (byte)targetMethod.Body.Variables.Count;
                targetMethod.Body.Variables.Add(new VariableDefinition(this.GetTypeReference("System.Object")));
            }
            parameterIndexBegin = (byte)targetMethod.Body.Variables.Count;
            for (i = 0; i < targetMethod.Parameters.Count; i = (byte)(i + 1))
            {
                targetMethod.Body.Variables.Add(new VariableDefinition(this.GetTypeReference("System.Object")));
                parameterIndexEnd = (byte)targetMethod.Body.Variables.Count;
            }
            if (prefixHook != null)
            {
                Instruction jointPoint = targetMethod.Body.Instructions.FirstOrDefault(ins => jointPointChecker(ins));
                for (int x = jointPointOffset; x < 0; x++)
                {
                    jointPoint = jointPoint.Previous;
                }
                for (int x = 0; x < jointPointOffset; x++)
                {
                    jointPoint = jointPoint.Next;
                }
                i = parameterOffset;
                for (j = parameterIndexBegin; i < (targetMethod.Parameters.Count + parameterOffset); j = (byte)(j + 1))
                {
                    instructions.Add(this._createLdargsInstruction(iLProcessor, i));
                    if (targetMethod.Parameters[i - parameterOffset].ParameterType.IsValueType)
                    {
                        instructions.Add(iLProcessor.Create(OpCodes.Box, targetMethod.Parameters[i - parameterOffset].ParameterType));
                    }
                    instructions.Add(this._createStlocInstruction(iLProcessor, j));
                    i = (byte)(i + 1);
                }
                instructions.Add(iLProcessor.Create(OpCodes.Ldsfld, field));
                instructions.Add(iLProcessor.Create(OpCodes.Ldstr, qualifyName));
                if (!targetMethod.IsStatic)
                {
                    instructions.Add(iLProcessor.Create(OpCodes.Ldarg_0));
                    if (targetType.IsValueType)
                    {
                        instructions.Add(iLProcessor.Create(OpCodes.Box, targetType));
                    }
                }
                i = parameterOffset;
                for (j = parameterIndexBegin; i < (targetMethod.Parameters.Count + parameterOffset); j = (byte)(j + 1))
                {
                    instructions.Add(iLProcessor.Create(OpCodes.Ldloca_S, j));
                    i = (byte)(i + 1);
                }
                while (i < (prefixHook.Parameters.Count - 2))
                {
                    instructions.Add(iLProcessor.Create(OpCodes.Ldloca_S, (byte)0));
                    i = (byte)(i + 1);
                }
                instructions.Add(iLProcessor.Create(OpCodes.Ldloca_S, returnIndex));
                instructions.Add(iLProcessor.Create(OpCodes.Callvirt, prefixHook));
                instructions.Add(this._createStlocInstruction(iLProcessor, stateIndex));
                i = parameterOffset;
                for (j = parameterIndexBegin; i < (targetMethod.Parameters.Count + parameterOffset); j = (byte)(j + 1))
                {
                    instructions.Add(this._createLdlocInstruction(iLProcessor, j));
                    if (targetMethod.Parameters[i - parameterOffset].ParameterType.IsValueType)
                    {
                        instructions.Add(iLProcessor.Create(OpCodes.Unbox_Any, targetMethod.Parameters[i - parameterOffset].ParameterType));
                    }
                    else
                    {
                        instructions.Add(iLProcessor.Create(OpCodes.Castclass, targetMethod.Parameters[i - parameterOffset].ParameterType));
                    }
                    instructions.Add(iLProcessor.Create(OpCodes.Starg_S, i));
                    i = (byte)(i + 1);
                }
                instructions.Add(this._createLdlocInstruction(iLProcessor, stateIndex));
                instructions.Add(iLProcessor.Create(OpCodes.Brtrue, jointPoint));
                if (targetMethod.ReturnType.FullName != "System.Void")
                {
                    instructions.Add(this._createLdlocInstruction(iLProcessor, returnIndex));
                    if (targetMethod.ReturnType.IsValueType)
                    {
                        instructions.Add(iLProcessor.Create(OpCodes.Unbox_Any, targetMethod.ReturnType));
                    }
                    else
                    {
                        instructions.Add(iLProcessor.Create(OpCodes.Castclass, targetMethod.ReturnType));
                    }
                }
                instructions.Add(iLProcessor.Create(OpCodes.Ret));
                this.InsertInstructions(iLProcessor, jointPoint, instructions);
            }
        }
        
        public void ApplyCommonHookEntry(TypeDefinition targetType, string methodname, string hookname, bool patchPrefix = true, bool patchPostfix = true, Func<MethodDefinition, bool> methodFilter = null, string qualifyNameSuffix = "")
        {
            string qualifyName = $"{targetType.FullName}.{methodname}{qualifyNameSuffix}";
            TypeDefinition typeModHooksObject = this.StardewValley.MainModule.GetType("StardewValley.ModHooks");
            var targetMethod = targetType.Methods.FirstOrDefault(method => (method.HasBody && method.Name == methodname && (methodFilter == null || methodFilter(method))));
            var prefixHook = typeModHooksObject.Methods.FirstOrDefault(m => m.Name == hookname + "_Prefix");
            var postfixHook = typeModHooksObject.Methods.FirstOrDefault(m => m.Name == hookname + "_Postfix");
            var processor = targetMethod.Body.GetILProcessor();
            FieldReference hooksField = this.GetFieldReference("hooks", "StardewValley.Game1", this.StardewValley);
            Instruction jointPoint;
            List<Instruction> instructions;
            byte i, j, k;
            // state
            byte returnIndex = 0;
            byte parameterIndexBegin = 0, parameterIndexEnd = 0;
            byte parameterOffset = (targetMethod.IsStatic ? (byte)0 : (byte)1);
            byte stateIndex = (byte)targetMethod.Body.Variables.Count;
            targetMethod.Body.Variables.Add(new VariableDefinition(this.GetTypeReference("System.Boolean")));
            if (targetMethod.ReturnType.FullName != "System.Void")
            {
                // return
                returnIndex = (byte)targetMethod.Body.Variables.Count;
                targetMethod.Body.Variables.Add(new VariableDefinition(this.GetTypeReference("System.Object")));
            }
            parameterIndexBegin = (byte)targetMethod.Body.Variables.Count;
            for (i = 0; i < targetMethod.Parameters.Count; i++)
            {
                targetMethod.Body.Variables.Add(new VariableDefinition(this.GetTypeReference("System.Object")));
                parameterIndexEnd = (byte)targetMethod.Body.Variables.Count;
            }
            if (patchPrefix && prefixHook != null)
            {
                instructions = new List<Instruction>();
                jointPoint = targetMethod.Body.Instructions[0];
                k = (byte)(targetMethod.Parameters.Count + parameterOffset > prefixHook.Parameters.Count - 2 ? prefixHook.Parameters.Count - 2 - parameterOffset : targetMethod.Parameters.Count + parameterOffset);
                for (i = parameterOffset, j = parameterIndexBegin; i < k; i++, j++)
                {
                    instructions.Add(this._createLdargsInstruction(processor, i));
                    if(targetMethod.Parameters[i - parameterOffset].ParameterType.IsValueType)
                    {
                        instructions.Add(processor.Create(OpCodes.Box, targetMethod.Parameters[i - parameterOffset].ParameterType));
                    }
                    instructions.Add(this._createStlocInstruction(processor, j));
                }
                instructions.Add(processor.Create(OpCodes.Ldsfld, hooksField));
                instructions.Add(processor.Create(OpCodes.Ldstr, qualifyName));
                if (!targetMethod.IsStatic)
                {
                    instructions.Add(processor.Create(OpCodes.Ldarg_0));
                    if (targetType.IsValueType)
                        instructions.Add(processor.Create(OpCodes.Box, targetType));
                }

                k = (byte)(targetMethod.Parameters.Count + parameterOffset > prefixHook.Parameters.Count - 2 ? prefixHook.Parameters.Count - 2 - parameterOffset : targetMethod.Parameters.Count + parameterOffset);
                for (i = parameterOffset, j = parameterIndexBegin; i < k; i++, j++)
                {
                    instructions.Add(processor.Create(OpCodes.Ldloca_S, j));
                }
                for (; i < prefixHook.Parameters.Count - 2; i++)
                {
                    instructions.Add(processor.Create(OpCodes.Ldloca_S, (byte)0));
                }
                instructions.Add(processor.Create(OpCodes.Ldloca_S, returnIndex));
                instructions.Add(processor.Create(OpCodes.Callvirt, prefixHook));
                instructions.Add(this._createStlocInstruction(processor, stateIndex));
                for (i = parameterOffset, j = parameterIndexBegin; i < targetMethod.Parameters.Count + parameterOffset; i++, j++)
                {
                    instructions.Add(this._createLdlocInstruction(processor, j));
                    if (targetMethod.Parameters[i - parameterOffset].ParameterType.IsValueType)
                    {
                        instructions.Add(processor.Create(OpCodes.Unbox_Any, targetMethod.Parameters[i - parameterOffset].ParameterType));
                    }
                    else
                    {
                        instructions.Add(processor.Create(OpCodes.Castclass, targetMethod.Parameters[i - parameterOffset].ParameterType));
                    }
                    instructions.Add(processor.Create(OpCodes.Starg_S, i));
                }
                instructions.Add(this._createLdlocInstruction(processor, stateIndex));
                instructions.Add(processor.Create(OpCodes.Brtrue, jointPoint));
                if (targetMethod.ReturnType.FullName != "System.Void") { 
                    instructions.Add(this._createLdlocInstruction(processor, returnIndex));
                    if (targetMethod.ReturnType.IsValueType)
                        instructions.Add(processor.Create(OpCodes.Unbox_Any, targetMethod.ReturnType));
                    else
                        instructions.Add(processor.Create(OpCodes.Castclass, targetMethod.ReturnType));
                }
                instructions.Add(processor.Create(OpCodes.Ret));
                this.InsertInstructions(processor, jointPoint, instructions);
            }
            if (patchPostfix && postfixHook != null)
            {
                instructions = new List<Instruction>();
                jointPoint = targetMethod.Body.Instructions[targetMethod.Body.Instructions.Count - 1];
                if (targetMethod.ReturnType.FullName != "System.Void")
                {
                    instructions.Add(processor.Create(OpCodes.Box, targetMethod.ReturnType));
                    instructions.Add(this._createStlocInstruction(processor, returnIndex));
                }
                k = (byte)(targetMethod.Parameters.Count + parameterOffset > postfixHook.Parameters.Count - 3 ? prefixHook.Parameters.Count - 3 - parameterOffset : targetMethod.Parameters.Count + parameterOffset);
                for (i = parameterOffset, j = parameterIndexBegin; i < k; i++, j++)
                {
                    instructions.Add(this._createLdargsInstruction(processor, i));
                    if (targetMethod.Parameters[i - parameterOffset].ParameterType.IsValueType)
                    {
                        instructions.Add(processor.Create(OpCodes.Box, targetMethod.Parameters[i - parameterOffset].ParameterType));
                    }
                    instructions.Add(this._createStlocInstruction(processor, j));
                }
                instructions.Add(processor.Create(OpCodes.Ldsfld, hooksField));
                instructions.Add(processor.Create(OpCodes.Ldstr, qualifyName));
                if (!targetMethod.IsStatic)
                {
                    instructions.Add(processor.Create(OpCodes.Ldarg_0));
                    if (targetType.IsValueType)
                        instructions.Add(processor.Create(OpCodes.Box, targetType));
                }
                k = (byte)(targetMethod.Parameters.Count + parameterOffset > postfixHook.Parameters.Count - 3 ? prefixHook.Parameters.Count - 3 - parameterOffset : targetMethod.Parameters.Count + parameterOffset);
                for (i = parameterOffset, j = parameterIndexBegin; i < k; i++, j++)
                {
                    instructions.Add(processor.Create(OpCodes.Ldloca_S, j));
                }
                for (; i < postfixHook.Parameters.Count - 3; i++)
                {
                    instructions.Add(processor.Create(OpCodes.Ldloca_S, (byte)0));
                }
                if (targetMethod.ReturnType.FullName != "System.Void")
                {
                    instructions.Add(processor.Create(OpCodes.Ldloca_S, stateIndex));
                    instructions.Add(processor.Create(OpCodes.Ldloca_S, returnIndex));
                    instructions.Add(processor.Create(OpCodes.Callvirt, postfixHook));
                    instructions.Add(this._createLdlocInstruction(processor, returnIndex));
                    if (targetMethod.ReturnType.IsValueType)
                        instructions.Add(processor.Create(OpCodes.Unbox_Any, targetMethod.ReturnType));
                    else
                        instructions.Add(processor.Create(OpCodes.Castclass, targetMethod.ReturnType));
                }
                else
                {
                    instructions.Add(processor.Create(OpCodes.Ldloca_S, stateIndex));
                    instructions.Add(processor.Create(OpCodes.Ldloca_S, returnIndex));
                    instructions.Add(processor.Create(OpCodes.Callvirt, postfixHook));
                }
                this.InsertInstructions(processor, jointPoint, instructions);
                for (int x = 0; x < targetMethod.Body.Instructions.Count - 1; x++)
                {
                    Instruction origin = targetMethod.Body.Instructions[x];
                    _patchPostfixReturn(processor, instructions, origin);
                }
            }
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
                            _patchPostfixReturn(processor, instructions, origin);
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

        private static void _patchPostfixReturn(ILProcessor processor, List<Instruction> instructions, Instruction origin)
        {
            if (origin.OpCode == OpCodes.Ret)
            {
                processor.Replace(origin, processor.Create(OpCodes.Br, instructions[0]));
            }
            else
            {
                if ((origin.OpCode == OpCodes.Br && ((Instruction)origin.Operand).OpCode == OpCodes.Ret)
                    || (origin.OpCode == OpCodes.Br_S && ((Instruction)origin.Operand).OpCode == OpCodes.Ret))
                {
                    origin.OpCode = OpCodes.Br;
                    origin.Operand = instructions[0];
                }
                else if ((origin.OpCode == OpCodes.Brfalse && ((Instruction)origin.Operand).OpCode == OpCodes.Ret)
                    || (origin.OpCode == OpCodes.Brfalse_S && ((Instruction)origin.Operand).OpCode == OpCodes.Ret))
                {
                    origin.OpCode = OpCodes.Brfalse;
                    origin.Operand = instructions[0];
                }
                else if ((origin.OpCode == OpCodes.Brtrue && ((Instruction)origin.Operand).OpCode == OpCodes.Ret)
                    || (origin.OpCode == OpCodes.Brtrue_S && ((Instruction)origin.Operand).OpCode == OpCodes.Ret))
                {
                    origin.OpCode = OpCodes.Brtrue;
                    origin.Operand = instructions[0];
                }
                else if ((origin.OpCode == OpCodes.Beq && ((Instruction)origin.Operand).OpCode == OpCodes.Ret)
                    || (origin.OpCode == OpCodes.Beq_S && ((Instruction)origin.Operand).OpCode == OpCodes.Ret))
                {
                    origin.OpCode = OpCodes.Beq;
                    origin.Operand = instructions[0];
                }
                else if ((origin.OpCode == OpCodes.Bge && ((Instruction)origin.Operand).OpCode == OpCodes.Ret)
                    || (origin.OpCode == OpCodes.Bge_S && ((Instruction)origin.Operand).OpCode == OpCodes.Ret))
                {
                    origin.OpCode = OpCodes.Bge;
                    origin.Operand = instructions[0];
                }
                else if ((origin.OpCode == OpCodes.Bge_Un && ((Instruction)origin.Operand).OpCode == OpCodes.Ret)
                    || (origin.OpCode == OpCodes.Bge_Un_S && ((Instruction)origin.Operand).OpCode == OpCodes.Ret))
                {
                    origin.OpCode = OpCodes.Bge_Un;
                    origin.Operand = instructions[0];
                }
                else if ((origin.OpCode == OpCodes.Bgt && ((Instruction)origin.Operand).OpCode == OpCodes.Ret)
                    || (origin.OpCode == OpCodes.Bgt_S && ((Instruction)origin.Operand).OpCode == OpCodes.Ret))
                {
                    origin.OpCode = OpCodes.Bgt;
                    origin.Operand = instructions[0];
                }
                else if ((origin.OpCode == OpCodes.Bgt_Un && ((Instruction)origin.Operand).OpCode == OpCodes.Ret)
                    || (origin.OpCode == OpCodes.Bgt_Un_S && ((Instruction)origin.Operand).OpCode == OpCodes.Ret))
                {
                    origin.OpCode = OpCodes.Bgt_Un;
                    origin.Operand = instructions[0];
                }
                else if ((origin.OpCode == OpCodes.Ble && ((Instruction)origin.Operand).OpCode == OpCodes.Ret)
                    || (origin.OpCode == OpCodes.Ble_S && ((Instruction)origin.Operand).OpCode == OpCodes.Ret))
                {
                    origin.OpCode = OpCodes.Ble;
                    origin.Operand = instructions[0];
                }
                else if ((origin.OpCode == OpCodes.Ble_Un && ((Instruction)origin.Operand).OpCode == OpCodes.Ret)
                    || (origin.OpCode == OpCodes.Ble_Un_S && ((Instruction)origin.Operand).OpCode == OpCodes.Ret))
                {
                    origin.OpCode = OpCodes.Ble_Un;
                    origin.Operand = instructions[0];
                }
                else if ((origin.OpCode == OpCodes.Blt && ((Instruction)origin.Operand).OpCode == OpCodes.Ret)
                    || (origin.OpCode == OpCodes.Blt_S && ((Instruction)origin.Operand).OpCode == OpCodes.Ret))
                {
                    origin.OpCode = OpCodes.Blt;
                    origin.Operand = instructions[0];
                }
                else if ((origin.OpCode == OpCodes.Blt_Un && ((Instruction)origin.Operand).OpCode == OpCodes.Ret)
                    || (origin.OpCode == OpCodes.Blt_Un_S && ((Instruction)origin.Operand).OpCode == OpCodes.Ret))
                {
                    origin.OpCode = OpCodes.Blt_Un;
                    origin.Operand = instructions[0];
                }
                else if ((origin.OpCode == OpCodes.Bne_Un && ((Instruction)origin.Operand).OpCode == OpCodes.Ret)
                    || (origin.OpCode == OpCodes.Bne_Un_S && ((Instruction)origin.Operand).OpCode == OpCodes.Ret))
                {
                    origin.OpCode = OpCodes.Bne_Un;
                    origin.Operand = instructions[0];
                }
                else if ((origin.OpCode == OpCodes.Leave && ((Instruction)origin.Operand).OpCode == OpCodes.Ret)
                    || (origin.OpCode == OpCodes.Leave_S && ((Instruction)origin.Operand).OpCode == OpCodes.Ret))
                {
                    origin.OpCode = OpCodes.Leave;
                    origin.Operand = instructions[0];
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

            this.InsertModHook("OnCommonHook_Prefix", new[] {
                this.GetTypeReference("System.String"),
                this.GetTypeReference("System.Object"),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object"))},
                new[] {
                    "hookName", "__instance",
                    "param1", "param2", "param3",
                    "param4", "__result"},
                this.GetTypeReference("System.Boolean"));
            this.InsertModHook("OnCommonStaticHook_Prefix", new[] {
                this.GetTypeReference("System.String"),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object"))},
                new[] {
                    "hookName", "param1",
                    "param2", "param3", "param4",
                    "param5", "__result"},
                this.GetTypeReference("System.Boolean"));
            this.InsertModHook("OnCommonHook_Postfix", new[] {
                this.GetTypeReference("System.String"),
                this.GetTypeReference("System.Object"),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Boolean")),
                new ByReferenceType(this.GetTypeReference("System.Object"))},
                new[] {
                    "hookName", "__instance",
                    "param1", "param2", "param3",
                    "param4", "__state", "__result"},
                this.GetTypeReference("System.Void"));
            this.InsertModHook("OnCommonStaticHook_Postfix", new[] {
                this.GetTypeReference("System.String"),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Boolean")),
                new ByReferenceType(this.GetTypeReference("System.Object"))},
                new[] {
                    "hookName", "param1",
                    "param2", "param3", "param4",
                    "param5", "__state", "__result"},
                this.GetTypeReference("System.Void"));

            this.InsertModHook("OnCommonHook10_Prefix", new[] {
                this.GetTypeReference("System.String"),
                this.GetTypeReference("System.Object"),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object"))},
                new[] {
                    "hookName", "__instance",
                    "param1", "param2", "param3",
                    "param4", "param5", "param6",
                    "param7", "param8", "param9",
                    "__result"},
                this.GetTypeReference("System.Boolean"));
            this.InsertModHook("OnCommonStaticHook10_Prefix", new[] {
                this.GetTypeReference("System.String"),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object"))},
                new[] {
                    "hookName", "param1",
                    "param2", "param3", "param4",
                    "param5", "param6", "param7",
                    "param8", "param9", "param10",
                    "__result"},
                this.GetTypeReference("System.Boolean"));
            this.InsertModHook("OnCommonHook10_Postfix", new[] {
                this.GetTypeReference("System.String"),
                this.GetTypeReference("System.Object"),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Boolean")),
                new ByReferenceType(this.GetTypeReference("System.Object"))},
                new[] {
                    "hookName", "__instance",
                    "param1", "param2", "param3",
                    "param4", "param5", "param6",
                    "param7", "param8", "param9",
                    "__state", "__result"},
                this.GetTypeReference("System.Void"));
            this.InsertModHook("OnCommonStaticHook10_Postfix", new[] {
                this.GetTypeReference("System.String"),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Object")),
                new ByReferenceType(this.GetTypeReference("System.Boolean")),
                new ByReferenceType(this.GetTypeReference("System.Object"))},
                new[] {
                    "hookName", "param1",
                    "param2", "param3", "param4",
                    "param5", "param6", "param7",
                    "param8", "param9", "param10",
                    "__state", "__result"},
                this.GetTypeReference("System.Void"));
            // On Game1 hooks
            TypeDefinition typeGame1 = this.StardewValley.MainModule.GetType("StardewValley.Game1");
            this.ApplyCommonHookEntry(typeGame1, "Update", "OnCommonHook");
            this.ApplyCommonHookEntry(typeGame1, "_draw", "OnCommonHook");
            this.ApplyCommonHookEntry(typeGame1, "getSourceRectForStandardTileSheet", "OnCommonStaticHook");
            this.ApplyCommonHookEntry(typeGame1, "tryToCheckAt", "OnCommonStaticHook", false);
            this.ApplyCommonHookEntry(typeGame1, "getLocationRequest", "OnCommonStaticHook", true, false);
            this.ApplyCommonHookEntry(typeGame1, "getLocationRequest", "OnCommonStaticHook", true, false);
            this.ApplyCommonHookEntry(typeGame1, "loadForNewGame", "OnCommonStaticHook");
            this.ApplyCommonHookEntry(typeGame1, "warpFarmer", "OnCommonStaticHook10", true, false, method => method.Parameters.Count == 6);
            this.ApplyCommonHookEntry(typeGame1, ".ctor", "OnCommonHook", false);
            this.ApplyCommonHookEntry(typeGame1, "LoadContent", "OnCommonHook", false);
            TypeDefinition targetType = null;
            foreach (TypeDefinition definition21 in this.StardewValley.MainModule.GetTypes())
            {
                if (definition21.FullName == "StardewValley.Game1/<>c")
                {
                    targetType = definition21;
                }
            }
            this.ApplyCommonMidHookEntry(targetType, method => method.FullName.Contains("showEndOfNightStuff"), ins => (ins.OpCode == OpCodes.Ldstr) && (((string)ins.Operand) == "newRecord"), -2, "OnCommonHook");

            // On Object hooks
            TypeDefinition typeObject = this.StardewValley.MainModule.GetType("StardewValley.Object");
            this.ApplyCommonHookEntry(typeObject, "canBePlacedHere", "OnCommonHook", true, false);
            this.ApplyCommonHookEntry(typeObject, "checkForAction", "OnCommonHook", true, false);
            this.ApplyCommonHookEntry(typeObject, "isIndexOkForBasicShippedCategory", "OnCommonStaticHook");
            this.ApplyCommonHookEntry(typeObject, "drawWhenHeld", "OnCommonHook", true, false);
            this.ApplyCommonHookEntry(typeObject, "drawInMenuWithColour", "OnCommonHook10");
            this.ApplyCommonHookEntry(typeObject, "draw", "OnCommonHook", true, false, method => method.Parameters.Count == 4);
            this.ApplyCommonHookEntry(typeObject, "draw", "OnCommonHook10", true, false, method => method.Parameters.Count == 5);
            this.ApplyCommonHookEntry(typeObject, "getDescription", "OnCommonHook", true, false);

            // On ReadyCheckDialog hooks
            TypeDefinition typeReadyCheckDialog = this.StardewValley.MainModule.GetType("StardewValley.Menus.ReadyCheckDialog");
            this.ApplyCommonHookEntry(typeReadyCheckDialog, "update", "OnCommonHook");

            // On IClickableMenu hooks
            TypeDefinition typeIClickableMenu = this.StardewValley.MainModule.GetType("StardewValley.Menus.IClickableMenu");
            this.ApplyCommonHookEntry(typeIClickableMenu, "drawToolTip", "OnCommonStaticHook10", false);

            // On Dialogue hooks
            TypeDefinition typeDialogue = this.StardewValley.MainModule.GetType("StardewValley.Dialogue");
            this.ApplyCommonHookEntry(typeDialogue, ".ctor", "OnCommonHook", true, false, method => method.Parameters.Count == 2);

            // On Building hooks
            TypeDefinition typeBuilding = this.StardewValley.MainModule.GetType("StardewValley.Buildings.Building");
            this.ApplyCommonHookEntry(typeBuilding, "load", "OnCommonHook");

            // On GameLocation hooks
            TypeDefinition typeGameLocation = this.StardewValley.MainModule.GetType("StardewValley.GameLocation");
            this.ApplyCommonHookEntry(typeGameLocation, "performTouchAction", "OnCommonHook", true, false);
            this.ApplyCommonHookEntry(typeGameLocation, "isActionableTile", "OnCommonHook", false);
            this.ApplyCommonHookEntry(typeGameLocation, "tryToAddCritters", "OnCommonHook", true, false);
            this.ApplyCommonHookEntry(typeGameLocation, "getSourceRectForObject", "OnCommonStaticHook");
            this.ApplyCommonHookEntry(typeGameLocation, "answerDialogue", "OnCommonHook", true, false);
            this.ApplyCommonHookEntry(typeGameLocation, "Equals", "OnCommonHook", true, false, method => method.Parameters[0].ParameterType == this.GetTypeReference("StardewValley.GameLocation", this.StardewValley));
            this.ApplyCommonHookEntry(typeGameLocation, "performAction", "OnCommonHook", true, false);

            // On Objects.TV hooks
            TypeDefinition typeObjectsTV = this.StardewValley.MainModule.GetType("StardewValley.Objects.TV");
            this.ApplyCommonHookEntry(typeObjectsTV, "checkForAction", "OnCommonHook");

            // On Furniture hooks
            TypeDefinition typeFurniture = this.StardewValley.MainModule.GetType("StardewValley.Objects.Furniture");
            this.ApplyCommonHookEntry(typeFurniture, "draw", "OnCommonHook", true, false);

            // On ColoredObject hooks
            TypeDefinition typeColoredObject = this.StardewValley.MainModule.GetType("StardewValley.Objects.ColoredObject");
            this.ApplyCommonHookEntry(typeColoredObject, "drawInMenu", "OnCommonHook10", false);

            // On HoeDirt hooks
            TypeDefinition typeHoeDirt = this.StardewValley.MainModule.GetType("StardewValley.TerrainFeatures.HoeDirt");
            this.ApplyCommonHookEntry(typeHoeDirt, "dayUpdate", "OnCommonHook", true, false);

            // On Utility hooks
            TypeDefinition typeUtility = this.StardewValley.MainModule.GetType("StardewValley.Utility");
            this.ApplyCommonHookEntry(typeUtility, "pickFarmEvent", "OnCommonStaticHook", false);

            // On Farmer hooks
            TypeDefinition typeFarmer = this.StardewValley.MainModule.GetType("StardewValley.Farmer");
            this.ApplyCommonHookEntry(typeFarmer, "doneEating", "OnCommonHook", false);

            // On MeleeWeapon hooks
            TypeDefinition typeMeleeWeapon = this.StardewValley.MainModule.GetType("StardewValley.Tools.MeleeWeapon");
            this.ApplyCommonHookEntry(typeMeleeWeapon, "drawDuringUse", "OnCommonStaticHook10", true, false, method => method.IsStatic);

            // On Multiplayer hooks
            TypeDefinition typeMultiplayer = this.StardewValley.MainModule.GetType("StardewValley.Multiplayer");
            this.ApplyCommonHookEntry(typeMultiplayer, "processIncomingMessage", "OnCommonHook", true, false);

            // On GameServer hooks
            TypeDefinition typeGameServer = this.StardewValley.MainModule.GetType("StardewValley.Network.GameServer");
            this.ApplyCommonHookEntry(typeGameServer, "sendServerIntroduction", "OnCommonHook", false);

            // On NPC hooks
            TypeDefinition typeNPC = this.StardewValley.MainModule.GetType("StardewValley.NPC");
            this.ApplyCommonHookEntry(typeNPC, "receiveGift", "OnCommonHook10", false);

            // On GameMenu hooks
            TypeDefinition definition19 = this.StardewValley.MainModule.GetType("StardewValley.Menus.GameMenu");
            this.ApplyCommonHookEntry(definition19, "getTabNumberFromName", "OnCommonHook", false);

            // On FarmHouse hooks
            TypeDefinition definition20 = this.StardewValley.MainModule.GetType("StardewValley.Locations.FarmHouse");
            this.ApplyCommonHookEntry(definition20, "loadSpouseRoom", "OnCommonHook", true, false);

            this.InsertModHook("OnGame1_CreateContentManager_Prefix", new[] {
                this.GetTypeReference("StardewValley.Game1", this.StardewValley),
                this.GetTypeReference("System.IServiceProvider"),
                this.GetTypeReference("System.String"),
                new ByReferenceType(this.GetTypeReference("StardewValley.LocalizedContentManager", this.StardewValley)) },
                new[] { "game1", "serviceProvider", "rootDirectory", "__result"},
                this.GetTypeReference("System.Boolean"));
            this.ApplyHookEntry(typeGame1, "CreateContentManager", "OnGame1_CreateContentManager_Prefix", true);

            return this.StardewValley;
        }
    }
}
