using System;
using System.Collections.Generic;
using System.Linq;
using Android.Graphics;
using ModLoader.Common;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DllRewrite
{
    internal class MethodPatcher
    {
        private readonly Dictionary<string, FieldReference> fieldDict = new Dictionary<string, FieldReference>();
        private readonly Dictionary<string, MethodReference> methodDict = new Dictionary<string, MethodReference>();
        private AssemblyDefinition MonoGame_Framework;
        private readonly AssemblyDefinition mscorlib;
        private readonly AssemblyDefinition Mono_Android;
        private readonly DefaultAssemblyResolver resolver;
        private readonly AssemblyDefinition StardewValley;
        private readonly Dictionary<string, TypeReference> typeDict = new Dictionary<string, TypeReference>();

        public MethodPatcher()
        {
            this.resolver = new DefaultAssemblyResolver();
            this.resolver.AddSearchDirectory(Constants.AssemblyPath);
            this.mscorlib = this.resolver.Resolve(new AssemblyNameReference("mscorlib", new Version("0.0.0.0")));
            this.Mono_Android = this.resolver.Resolve(new AssemblyNameReference("Mono.Android", new Version("0.0.0.0")));
            this.MonoGame_Framework =
                this.resolver.Resolve(new AssemblyNameReference("MonoGame.Framework", new Version("0.0.0.0")));
            this.StardewValley =
                this.resolver.Resolve(new AssemblyNameReference("StardewValley", new Version("0.0.0.0")));
        }

        public void InsertModHook(string name, TypeReference[] paraTypes, string[] paraNames, TypeReference returnType)
        {
            var typeModHooksObject = this.StardewValley.MainModule.GetType("StardewValley.ModHooks");
            var hook = new MethodDefinition(name,
                MethodAttributes.Virtual | MethodAttributes.Public | MethodAttributes.NewSlot |
                MethodAttributes.HideBySig, returnType);
            for (int i = 0; i < paraTypes.Length; i++)
            {
                var define = new ParameterDefinition(paraNames[i], ParameterAttributes.None, paraTypes[i]);
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
            if (typeModHooksObject.Methods.FirstOrDefault(item => item.Name == hook.Name) == null)
                typeModHooksObject.Methods.Add(hook);
        }

        public TypeReference GetTypeReference(string fullname)
        {
            return this.GetTypeReference(fullname, this.mscorlib);
        }

        public TypeReference GetTypeReference(string fullname, AssemblyDefinition assemblyDefinition)
        {
            if (this.typeDict.ContainsKey(fullname)) return this.typeDict[fullname];
            TypeReference type = assemblyDefinition.MainModule.GetType(fullname).Resolve();
            type = this.StardewValley.MainModule.ImportReference(type);
            this.typeDict.Add(fullname, type);
            return type;
        }

        public FieldReference GetFieldReference(string name, string typename, AssemblyDefinition assemblyDefinition)
        {
            string fullname = typename + "::" + name;
            if (this.fieldDict.ContainsKey(fullname)) return this.fieldDict[fullname];
            FieldReference field = assemblyDefinition.MainModule.Types.FirstOrDefault(item => item.FullName == typename)
                .Fields.FirstOrDefault(item => item.Name == name);
            if (assemblyDefinition.FullName != this.StardewValley.FullName)
                field = this.StardewValley.MainModule.ImportReference(field);
            this.fieldDict.Add(fullname, field);
            return field;
        }

        public MethodReference GetMethodReference(string name, string typename, AssemblyDefinition assemblyDefinition, Func<MethodDefinition, bool> methodFilter = null, AssemblyDefinition refDefinition = null)
        {
            string fullname = typename + "." + name;
            if (this.fieldDict.ContainsKey(fullname)) return this.methodDict[fullname];
            MethodReference method = assemblyDefinition.MainModule.Types
                .FirstOrDefault(item => item.FullName == typename).Methods.FirstOrDefault(item =>
                {
                    if (item.Name == name)
                    {
                        if (methodFilter == null)
                            return true;
                        else
                            return methodFilter(item);
                    }
                    return false;
                });
            if (assemblyDefinition.FullName != this.StardewValley.FullName)
            {
                if(refDefinition == null)
                    method = this.StardewValley.MainModule.ImportReference(method);
                else
                    method = refDefinition.MainModule.ImportReference(method);
            }
            this.methodDict.Add(fullname, method);
            return method;
        }

        public void ApplyGamePatch()
        {
            var typeMainActivity = this.StardewValley.MainModule.GetType("StardewValley.MainActivity");
            typeMainActivity.CustomAttributes.Clear();
            // Game.hook
            var typeGame1 = this.StardewValley.MainModule.GetType("StardewValley.Game1");
            var constructor = typeGame1.Methods.FirstOrDefault(m => m.IsConstructor);
            var processor = constructor.Body.GetILProcessor();
            foreach (var instruction in constructor.Body.Instructions)
            {
                if (instruction.OpCode == OpCodes.Ldstr && (string)instruction.Operand == "Content")
                {
                    processor.InsertBefore(instruction, processor.Create(OpCodes.Call, this.GetMethodReference("get_ExternalStorageDirectory", "Android.OS.Environment", this.Mono_Android)));
                    processor.InsertBefore(instruction, processor.Create(OpCodes.Callvirt, this.GetMethodReference("get_Path", "Java.IO.File", this.Mono_Android)));
                    instruction.Operand = "SMDroid/Game/assets/Content";
                    processor.InsertAfter(instruction, processor.Create(OpCodes.Call, this.GetMethodReference("Combine", "System.IO.Path", this.mscorlib, (m)=>m.Parameters.Count == 2)));
                    break;
                }
            }

            var hooksField = typeGame1.Fields.FirstOrDefault(f => f.Name == "hooks");
            hooksField.IsFamilyOrAssembly = false;
            hooksField.IsPublic = true;

            // isRaining and isDebrisWeather
            var propertyDefinition = new PropertyDefinition("isRaining", PropertyAttributes.None,
                this.GetTypeReference("System.Boolean"));
            propertyDefinition.GetMethod = new MethodDefinition("get_isRaining",
                MethodAttributes.Public | MethodAttributes.ReuseSlot | MethodAttributes.SpecialName |
                MethodAttributes.Static | MethodAttributes.HideBySig, this.GetTypeReference("System.Boolean"));
            propertyDefinition.GetMethod.SemanticsAttributes = MethodSemanticsAttributes.Getter;
            processor = propertyDefinition.GetMethod.Body.GetILProcessor();
            var typeRainManager = this.StardewValley.MainModule.GetType("StardewValley.RainManager");
            var getMethod = typeRainManager.Methods.FirstOrDefault(m => m.Name == "get_Instance");
            processor.Emit(OpCodes.Callvirt, getMethod);
            var isRainingField = this.GetFieldReference("isRaining", "StardewValley.RainManager", this.StardewValley);
            processor.Emit(OpCodes.Ldfld, isRainingField);
            processor.Emit(OpCodes.Ret);
            typeGame1.Methods.Add(propertyDefinition.GetMethod);
            typeGame1.Properties.Add(propertyDefinition);

            propertyDefinition = new PropertyDefinition("isDebrisWeather", PropertyAttributes.None,
                this.GetTypeReference("System.Boolean"));
            propertyDefinition.GetMethod = new MethodDefinition("get_isDebrisWeather",
                MethodAttributes.Public | MethodAttributes.ReuseSlot | MethodAttributes.SpecialName |
                MethodAttributes.Static | MethodAttributes.HideBySig, this.GetTypeReference("System.Boolean"));
            propertyDefinition.GetMethod.SemanticsAttributes = MethodSemanticsAttributes.Getter;
            processor = propertyDefinition.GetMethod.Body.GetILProcessor();
            var typeWeatherDebrisManager = this.StardewValley.MainModule.GetType("StardewValley.WeatherDebrisManager");
            getMethod = typeWeatherDebrisManager.Methods.FirstOrDefault(m => m.Name == "get_Instance");
            processor.Emit(OpCodes.Callvirt, getMethod);
            var isDebrisWeatherField = this.GetFieldReference("isDebrisWeather", "StardewValley.WeatherDebrisManager",
                this.StardewValley);
            processor.Emit(OpCodes.Ldfld, isDebrisWeatherField);
            processor.Emit(OpCodes.Ret);
            typeGame1.Methods.Add(propertyDefinition.GetMethod);
            typeGame1.Properties.Add(propertyDefinition);

            //HUDMessage..ctor
            var typeHUDMessage = this.StardewValley.MainModule.GetType("StardewValley.HUDMessage");
            var hudConstructor = new MethodDefinition(".ctor",
                MethodAttributes.Public | MethodAttributes.ReuseSlot | MethodAttributes.RTSpecialName |
                MethodAttributes.SpecialName | MethodAttributes.HideBySig, this.GetTypeReference("System.Void"));
            hudConstructor.Parameters.Add(new ParameterDefinition(this.GetTypeReference("System.String")));
            hudConstructor.Parameters.Add(new ParameterDefinition(this.GetTypeReference("System.Int32")));
            processor = hudConstructor.Body.GetILProcessor();
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldarg_1);
            processor.Emit(OpCodes.Ldarg_2);
            processor.Emit(OpCodes.Ldc_I4_M1);
            var targetConstructor = typeHUDMessage.Methods.FirstOrDefault(item =>
            {
                if (item.Parameters.Count == 3 && item.Parameters[0].ParameterType.FullName == "System.String"
                                               && item.Parameters[1].ParameterType.FullName == "System.Int32" &&
                                               item.Parameters[1].ParameterType.FullName == "System.Int32")
                    return true;
                return false;
            });
            processor.Emit(OpCodes.Call, targetConstructor);
            processor.Emit(OpCodes.Ret);
            typeHUDMessage.Methods.Add(hudConstructor);


            // Back Button Fix
            var method = typeGame1.Methods.FirstOrDefault(m => m.Name == "_updateAndroidMenus");
            processor = method.Body.GetILProcessor();
            var inputField = this.GetFieldReference("input", "StardewValley.Game1", this.StardewValley);
            processor.Replace(method.Body.Instructions[0], processor.Create(OpCodes.Ldsfld, inputField));
            var typeInputState = this.StardewValley.MainModule.GetType("StardewValley.InputState");
            var GetGamePadState = typeInputState.Methods.FirstOrDefault(m => m.Name == "GetGamePadState");
            processor.Replace(method.Body.Instructions[1], processor.Create(OpCodes.Callvirt, GetGamePadState));
        }

        public void ApplyCommonMidHookEntry(TypeDefinition targetType, Func<MethodDefinition, bool> methodChecker,
            Func<Instruction, bool> jointPointChecker, int jointPointOffset, string hookname)
        {
            byte i, j;
            var targetMethod = targetType.Methods.FirstOrDefault(method => methodChecker(method));
            string qualifyName = targetType.FullName + "." + targetMethod.Name + "_mid";
            var prefixHook = this.StardewValley.MainModule.GetType("StardewValley.ModHooks").Methods
                .FirstOrDefault(m => m.Name == hookname + "_Prefix");
            var iLProcessor = targetMethod.Body.GetILProcessor();
            var field = this.GetFieldReference("hooks", "StardewValley.Game1", this.StardewValley);
            var instructions = new List<Instruction>();
            byte returnIndex = 0;
            byte parameterIndexBegin = 0;
            byte parameterIndexEnd = 0;
            byte parameterOffset = targetMethod.IsStatic ? (byte) 0 : (byte) 1;
            byte stateIndex = (byte) targetMethod.Body.Variables.Count;
            targetMethod.Body.Variables.Add(new VariableDefinition(this.GetTypeReference("System.Boolean")));
            if (targetMethod.ReturnType.FullName != "System.Void")
            {
                returnIndex = (byte) targetMethod.Body.Variables.Count;
                targetMethod.Body.Variables.Add(new VariableDefinition(this.GetTypeReference("System.Object")));
            }

            parameterIndexBegin = (byte) targetMethod.Body.Variables.Count;
            for (i = 0; i < targetMethod.Parameters.Count; i = (byte) (i + 1))
            {
                targetMethod.Body.Variables.Add(new VariableDefinition(this.GetTypeReference("System.Object")));
                parameterIndexEnd = (byte) targetMethod.Body.Variables.Count;
            }

            if (prefixHook != null)
            {
                var jointPoint = targetMethod.Body.Instructions.FirstOrDefault(ins => jointPointChecker(ins));
                for (int x = jointPointOffset; x < 0; x++) jointPoint = jointPoint.Previous;
                for (int x = 0; x < jointPointOffset; x++) jointPoint = jointPoint.Next;
                i = parameterOffset;
                for (j = parameterIndexBegin; i < targetMethod.Parameters.Count + parameterOffset; j = (byte) (j + 1))
                {
                    instructions.Add(this._createLdargsInstruction(iLProcessor, i));
                    if (targetMethod.Parameters[i - parameterOffset].ParameterType.IsValueType)
                        instructions.Add(iLProcessor.Create(OpCodes.Box,
                            targetMethod.Parameters[i - parameterOffset].ParameterType));
                    instructions.Add(this._createStlocInstruction(iLProcessor, j));
                    i = (byte) (i + 1);
                }

                instructions.Add(iLProcessor.Create(OpCodes.Ldsfld, field));
                instructions.Add(iLProcessor.Create(OpCodes.Ldstr, qualifyName));
                if (!targetMethod.IsStatic)
                {
                    instructions.Add(iLProcessor.Create(OpCodes.Ldarg_0));
                    if (targetType.IsValueType) instructions.Add(iLProcessor.Create(OpCodes.Box, targetType));
                }

                i = parameterOffset;
                for (j = parameterIndexBegin; i < targetMethod.Parameters.Count + parameterOffset; j = (byte) (j + 1))
                {
                    instructions.Add(iLProcessor.Create(OpCodes.Ldloca_S, j));
                    i = (byte) (i + 1);
                }

                while (i < prefixHook.Parameters.Count - 2)
                {
                    instructions.Add(iLProcessor.Create(OpCodes.Ldloca_S, (byte) 0));
                    i = (byte) (i + 1);
                }

                instructions.Add(iLProcessor.Create(OpCodes.Ldloca_S, returnIndex));
                instructions.Add(iLProcessor.Create(OpCodes.Callvirt, prefixHook));
                instructions.Add(this._createStlocInstruction(iLProcessor, stateIndex));
                i = parameterOffset;
                for (j = parameterIndexBegin; i < targetMethod.Parameters.Count + parameterOffset; j = (byte) (j + 1))
                {
                    instructions.Add(this._createLdlocInstruction(iLProcessor, j));
                    if (targetMethod.Parameters[i - parameterOffset].ParameterType.IsValueType)
                        instructions.Add(iLProcessor.Create(OpCodes.Unbox_Any,
                            targetMethod.Parameters[i - parameterOffset].ParameterType));
                    else
                        instructions.Add(iLProcessor.Create(OpCodes.Castclass,
                            targetMethod.Parameters[i - parameterOffset].ParameterType));
                    instructions.Add(iLProcessor.Create(OpCodes.Starg_S, i));
                    i = (byte) (i + 1);
                }

                instructions.Add(this._createLdlocInstruction(iLProcessor, stateIndex));
                instructions.Add(iLProcessor.Create(OpCodes.Brtrue, jointPoint));
                if (targetMethod.ReturnType.FullName != "System.Void")
                {
                    instructions.Add(this._createLdlocInstruction(iLProcessor, returnIndex));
                    if (targetMethod.ReturnType.IsValueType)
                        instructions.Add(iLProcessor.Create(OpCodes.Unbox_Any, targetMethod.ReturnType));
                    else
                        instructions.Add(iLProcessor.Create(OpCodes.Castclass, targetMethod.ReturnType));
                }

                instructions.Add(iLProcessor.Create(OpCodes.Ret));
                this.InsertInstructions(iLProcessor, jointPoint, instructions);
            }
        }

        public void ApplyCommonHookEntry(TypeDefinition targetType, string methodname, string hookname,
            bool patchPrefix = true, bool patchPostfix = true, Func<MethodDefinition, bool> methodFilter = null,
            string qualifyNameSuffix = "", FieldReference hooksField = null, MethodReference prefixHook = null, MethodReference postfixHook = null)
        {
            string qualifyName = $"{targetType.FullName}.{methodname}{qualifyNameSuffix}";
            var targetMethod = targetType.Methods.FirstOrDefault(method =>
                method.HasBody && method.Name == methodname && (methodFilter == null || methodFilter(method)));
            var typeModHooksObject = this.StardewValley.MainModule.GetType("StardewValley.ModHooks");
            if(prefixHook == null)
                prefixHook = typeModHooksObject.Methods.FirstOrDefault(m => m.Name == hookname + "_Prefix");
            if (postfixHook == null)
                postfixHook = typeModHooksObject.Methods.FirstOrDefault(m => m.Name == hookname + "_Postfix");
            var processor = targetMethod.Body.GetILProcessor();
            if(hooksField == null)
                hooksField = this.GetFieldReference("hooks", "StardewValley.Game1", this.StardewValley);
            Instruction jointPoint;
            List<Instruction> instructions;
            byte i, j, k;
            // state
            byte returnIndex = 0;
            byte parameterIndexBegin = 0, parameterIndexEnd = 0;
            byte parameterOffset = targetMethod.IsStatic ? (byte) 0 : (byte) 1;
            byte stateIndex = (byte) targetMethod.Body.Variables.Count;
            targetMethod.Body.Variables.Add(new VariableDefinition(this.GetTypeReference("System.Boolean")));
            if (targetMethod.ReturnType.FullName != "System.Void")
            {
                // return
                returnIndex = (byte) targetMethod.Body.Variables.Count;
                targetMethod.Body.Variables.Add(new VariableDefinition(this.GetTypeReference("System.Object")));
            }

            parameterIndexBegin = (byte) targetMethod.Body.Variables.Count;
            for (i = 0; i < targetMethod.Parameters.Count; i++)
            {
                targetMethod.Body.Variables.Add(new VariableDefinition(this.GetTypeReference("System.Object")));
                parameterIndexEnd = (byte) targetMethod.Body.Variables.Count;
            }

            if (patchPrefix && prefixHook != null)
            {
                instructions = new List<Instruction>();
                jointPoint = targetMethod.Body.Instructions[0];
                k = (byte) (targetMethod.Parameters.Count + parameterOffset > prefixHook.Parameters.Count - 2
                    ? prefixHook.Parameters.Count - 2 - parameterOffset
                    : targetMethod.Parameters.Count + parameterOffset);
                for (i = parameterOffset, j = parameterIndexBegin; i < k; i++, j++)
                {
                    instructions.Add(this._createLdargsInstruction(processor, i));
                    if (targetMethod.Parameters[i - parameterOffset].ParameterType.IsValueType)
                        instructions.Add(processor.Create(OpCodes.Box,
                            targetMethod.Parameters[i - parameterOffset].ParameterType));
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

                k = (byte) (targetMethod.Parameters.Count + parameterOffset > prefixHook.Parameters.Count - 2
                    ? prefixHook.Parameters.Count - 2 - parameterOffset
                    : targetMethod.Parameters.Count + parameterOffset);
                for (i = parameterOffset, j = parameterIndexBegin; i < k; i++, j++)
                    instructions.Add(processor.Create(OpCodes.Ldloca_S, j));
                for (; i < prefixHook.Parameters.Count - 2; i++)
                    instructions.Add(processor.Create(OpCodes.Ldloca_S, (byte) 0));
                instructions.Add(processor.Create(OpCodes.Ldloca_S, returnIndex));
                instructions.Add(processor.Create(OpCodes.Callvirt, prefixHook));
                instructions.Add(this._createStlocInstruction(processor, stateIndex));
                for (i = parameterOffset, j = parameterIndexBegin;
                    i < targetMethod.Parameters.Count + parameterOffset;
                    i++, j++)
                {
                    instructions.Add(this._createLdlocInstruction(processor, j));
                    if (targetMethod.Parameters[i - parameterOffset].ParameterType.IsValueType)
                        instructions.Add(processor.Create(OpCodes.Unbox_Any,
                            targetMethod.Parameters[i - parameterOffset].ParameterType));
                    else
                        instructions.Add(processor.Create(OpCodes.Castclass,
                            targetMethod.Parameters[i - parameterOffset].ParameterType));
                    instructions.Add(processor.Create(OpCodes.Starg_S, i));
                }

                instructions.Add(this._createLdlocInstruction(processor, stateIndex));
                instructions.Add(processor.Create(OpCodes.Brtrue, jointPoint));
                if (targetMethod.ReturnType.FullName != "System.Void")
                {
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

                k = (byte) (targetMethod.Parameters.Count + parameterOffset > postfixHook.Parameters.Count - 3
                    ? prefixHook.Parameters.Count - 3 - parameterOffset
                    : targetMethod.Parameters.Count + parameterOffset);
                for (i = parameterOffset, j = parameterIndexBegin; i < k; i++, j++)
                {
                    instructions.Add(this._createLdargsInstruction(processor, i));
                    if (targetMethod.Parameters[i - parameterOffset].ParameterType.IsValueType)
                        instructions.Add(processor.Create(OpCodes.Box,
                            targetMethod.Parameters[i - parameterOffset].ParameterType));
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

                k = (byte) (targetMethod.Parameters.Count + parameterOffset > postfixHook.Parameters.Count - 3
                    ? prefixHook.Parameters.Count - 3 - parameterOffset
                    : targetMethod.Parameters.Count + parameterOffset);
                for (i = parameterOffset, j = parameterIndexBegin; i < k; i++, j++)
                    instructions.Add(processor.Create(OpCodes.Ldloca_S, j));
                for (; i < postfixHook.Parameters.Count - 3; i++)
                    instructions.Add(processor.Create(OpCodes.Ldloca_S, (byte) 0));
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
                    var origin = targetMethod.Body.Instructions[x];
                    _patchPostfixReturn(processor, instructions, origin);
                }
            }
        }

        public void ApplyHookEntry(TypeDefinition targetType, string methodname, string hookname, bool isPrefix)
        {
            var typeModHooksObject = this.StardewValley.MainModule.GetType("StardewValley.ModHooks");
            var hook = typeModHooksObject.Methods.FirstOrDefault(m => m.Name == hookname);
            foreach (var method in targetType.Methods)
                if (!method.IsConstructor && method.HasBody && method.Name == methodname)
                {
                    var processor = method.Body.GetILProcessor();
                    Instruction jointPoint;
                    var instructions = new List<Instruction>();
                    var hooksField = this.GetFieldReference("hooks", "StardewValley.Game1", this.StardewValley);
                    if (!isPrefix)
                    {
                        jointPoint = method.Body.Instructions[method.Body.Instructions.Count - 1];
                        if (method.ReturnType.FullName != "System.Void")
                        {
                            method.Body.Variables.Add(new VariableDefinition(method.ReturnType));
                            instructions.Add(this._createStlocInstruction(processor,
                                (byte) (method.Body.Variables.Count - 1)));
                        }

                        instructions.Add(processor.Create(OpCodes.Ldsfld, hooksField));
                        if (!method.IsStatic) instructions.Add(processor.Create(OpCodes.Ldarg_0));
                        for (byte i = method.IsStatic ? (byte) 0 : (byte) 1;
                            i < method.Parameters.Count + (method.IsStatic ? (byte) 0 : (byte) 1);
                            i++) instructions.Add(this._createLdargsInstruction(processor, i));
                        if (method.ReturnType.FullName != "System.Void")
                        {
                            instructions.Add(processor.Create(OpCodes.Ldloca_S,
                                (byte) (method.Body.Variables.Count - 1)));
                            instructions.Add(processor.Create(OpCodes.Callvirt, hook));
                            instructions.Add(this._createLdlocInstruction(processor,
                                (byte) (method.Body.Variables.Count - 1)));
                        }
                        else
                            instructions.Add(processor.Create(OpCodes.Callvirt, hook));

                        for (int i = 0; i < method.Body.Instructions.Count - 1; i++)
                        {
                            var origin = method.Body.Instructions[i];
                            _patchPostfixReturn(processor, instructions, origin);
                        }
                    }
                    else
                    {
                        jointPoint = method.Body.Instructions[0];
                        if (method.ReturnType.FullName != "System.Void")
                            method.Body.Variables.Add(new VariableDefinition(method.ReturnType));
                        instructions.Add(processor.Create(OpCodes.Ldsfld, hooksField));
                        if (!method.IsStatic) instructions.Add(processor.Create(OpCodes.Ldarg_0));
                        for (byte i = method.IsStatic ? (byte) 0 : (byte) 1;
                            i < method.Parameters.Count + (method.IsStatic ? (byte) 0 : (byte) 1);
                            i++) instructions.Add(this._createLdargsInstruction(processor, i));
                        if (method.ReturnType.FullName != "System.Void")
                        {
                            instructions.Add(processor.Create(OpCodes.Ldloca_S,
                                (byte) (method.Body.Variables.Count - 1)));
                            instructions.Add(processor.Create(OpCodes.Callvirt, hook));
                            instructions.Add(processor.Create(OpCodes.Brtrue, jointPoint));
                            instructions.Add(this._createLdlocInstruction(processor,
                                (byte) (method.Body.Variables.Count - 1)));
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

        private static void _patchPostfixReturn(ILProcessor processor, List<Instruction> instructions,
            Instruction origin)
        {
            if (origin.OpCode == OpCodes.Ret)
                processor.Replace(origin, processor.Create(OpCodes.Br, instructions[0]));
            else
            {
                if (origin.OpCode == OpCodes.Br && ((Instruction) origin.Operand).OpCode == OpCodes.Ret
                    || origin.OpCode == OpCodes.Br_S && ((Instruction) origin.Operand).OpCode == OpCodes.Ret)
                {
                    origin.OpCode = OpCodes.Br;
                    origin.Operand = instructions[0];
                }
                else if (origin.OpCode == OpCodes.Brfalse && ((Instruction) origin.Operand).OpCode == OpCodes.Ret
                         || origin.OpCode == OpCodes.Brfalse_S && ((Instruction) origin.Operand).OpCode == OpCodes.Ret)
                {
                    origin.OpCode = OpCodes.Brfalse;
                    origin.Operand = instructions[0];
                }
                else if (origin.OpCode == OpCodes.Brtrue && ((Instruction) origin.Operand).OpCode == OpCodes.Ret
                         || origin.OpCode == OpCodes.Brtrue_S && ((Instruction) origin.Operand).OpCode == OpCodes.Ret)
                {
                    origin.OpCode = OpCodes.Brtrue;
                    origin.Operand = instructions[0];
                }
                else if (origin.OpCode == OpCodes.Beq && ((Instruction) origin.Operand).OpCode == OpCodes.Ret
                         || origin.OpCode == OpCodes.Beq_S && ((Instruction) origin.Operand).OpCode == OpCodes.Ret)
                {
                    origin.OpCode = OpCodes.Beq;
                    origin.Operand = instructions[0];
                }
                else if (origin.OpCode == OpCodes.Bge && ((Instruction) origin.Operand).OpCode == OpCodes.Ret
                         || origin.OpCode == OpCodes.Bge_S && ((Instruction) origin.Operand).OpCode == OpCodes.Ret)
                {
                    origin.OpCode = OpCodes.Bge;
                    origin.Operand = instructions[0];
                }
                else if (origin.OpCode == OpCodes.Bge_Un && ((Instruction) origin.Operand).OpCode == OpCodes.Ret
                         || origin.OpCode == OpCodes.Bge_Un_S && ((Instruction) origin.Operand).OpCode == OpCodes.Ret)
                {
                    origin.OpCode = OpCodes.Bge_Un;
                    origin.Operand = instructions[0];
                }
                else if (origin.OpCode == OpCodes.Bgt && ((Instruction) origin.Operand).OpCode == OpCodes.Ret
                         || origin.OpCode == OpCodes.Bgt_S && ((Instruction) origin.Operand).OpCode == OpCodes.Ret)
                {
                    origin.OpCode = OpCodes.Bgt;
                    origin.Operand = instructions[0];
                }
                else if (origin.OpCode == OpCodes.Bgt_Un && ((Instruction) origin.Operand).OpCode == OpCodes.Ret
                         || origin.OpCode == OpCodes.Bgt_Un_S && ((Instruction) origin.Operand).OpCode == OpCodes.Ret)
                {
                    origin.OpCode = OpCodes.Bgt_Un;
                    origin.Operand = instructions[0];
                }
                else if (origin.OpCode == OpCodes.Ble && ((Instruction) origin.Operand).OpCode == OpCodes.Ret
                         || origin.OpCode == OpCodes.Ble_S && ((Instruction) origin.Operand).OpCode == OpCodes.Ret)
                {
                    origin.OpCode = OpCodes.Ble;
                    origin.Operand = instructions[0];
                }
                else if (origin.OpCode == OpCodes.Ble_Un && ((Instruction) origin.Operand).OpCode == OpCodes.Ret
                         || origin.OpCode == OpCodes.Ble_Un_S && ((Instruction) origin.Operand).OpCode == OpCodes.Ret)
                {
                    origin.OpCode = OpCodes.Ble_Un;
                    origin.Operand = instructions[0];
                }
                else if (origin.OpCode == OpCodes.Blt && ((Instruction) origin.Operand).OpCode == OpCodes.Ret
                         || origin.OpCode == OpCodes.Blt_S && ((Instruction) origin.Operand).OpCode == OpCodes.Ret)
                {
                    origin.OpCode = OpCodes.Blt;
                    origin.Operand = instructions[0];
                }
                else if (origin.OpCode == OpCodes.Blt_Un && ((Instruction) origin.Operand).OpCode == OpCodes.Ret
                         || origin.OpCode == OpCodes.Blt_Un_S && ((Instruction) origin.Operand).OpCode == OpCodes.Ret)
                {
                    origin.OpCode = OpCodes.Blt_Un;
                    origin.Operand = instructions[0];
                }
                else if (origin.OpCode == OpCodes.Bne_Un && ((Instruction) origin.Operand).OpCode == OpCodes.Ret
                         || origin.OpCode == OpCodes.Bne_Un_S && ((Instruction) origin.Operand).OpCode == OpCodes.Ret)
                {
                    origin.OpCode = OpCodes.Bne_Un;
                    origin.Operand = instructions[0];
                }
                else if (origin.OpCode == OpCodes.Leave && ((Instruction) origin.Operand).OpCode == OpCodes.Ret
                         || origin.OpCode == OpCodes.Leave_S && ((Instruction) origin.Operand).OpCode == OpCodes.Ret)
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
            foreach (var instruction in instructions) processor.InsertBefore(jointPoint, instruction);
        }

        public AssemblyDefinition InsertModHooks()
        {
            this.ApplyGamePatch();

            this.InsertModHook("OnCommonHook_Prefix", new[]
                {
                    this.GetTypeReference("System.String"),
                    this.GetTypeReference("System.Object"),
                    new ByReferenceType(this.GetTypeReference("System.Object")),
                    new ByReferenceType(this.GetTypeReference("System.Object")),
                    new ByReferenceType(this.GetTypeReference("System.Object")),
                    new ByReferenceType(this.GetTypeReference("System.Object")),
                    new ByReferenceType(this.GetTypeReference("System.Object"))
                },
                new[]
                {
                    "hookName", "__instance",
                    "param1", "param2", "param3",
                    "param4", "__result"
                },
                this.GetTypeReference("System.Boolean"));
            this.InsertModHook("OnCommonStaticHook_Prefix", new[]
                {
                    this.GetTypeReference("System.String"),
                    new ByReferenceType(this.GetTypeReference("System.Object")),
                    new ByReferenceType(this.GetTypeReference("System.Object")),
                    new ByReferenceType(this.GetTypeReference("System.Object")),
                    new ByReferenceType(this.GetTypeReference("System.Object")),
                    new ByReferenceType(this.GetTypeReference("System.Object")),
                    new ByReferenceType(this.GetTypeReference("System.Object"))
                },
                new[]
                {
                    "hookName", "param1",
                    "param2", "param3", "param4",
                    "param5", "__result"
                },
                this.GetTypeReference("System.Boolean"));
            this.InsertModHook("OnCommonHook_Postfix", new[]
                {
                    this.GetTypeReference("System.String"),
                    this.GetTypeReference("System.Object"),
                    new ByReferenceType(this.GetTypeReference("System.Object")),
                    new ByReferenceType(this.GetTypeReference("System.Object")),
                    new ByReferenceType(this.GetTypeReference("System.Object")),
                    new ByReferenceType(this.GetTypeReference("System.Object")),
                    new ByReferenceType(this.GetTypeReference("System.Boolean")),
                    new ByReferenceType(this.GetTypeReference("System.Object"))
                },
                new[]
                {
                    "hookName", "__instance",
                    "param1", "param2", "param3",
                    "param4", "__state", "__result"
                },
                this.GetTypeReference("System.Void"));
            this.InsertModHook("OnCommonStaticHook_Postfix", new[]
                {
                    this.GetTypeReference("System.String"),
                    new ByReferenceType(this.GetTypeReference("System.Object")),
                    new ByReferenceType(this.GetTypeReference("System.Object")),
                    new ByReferenceType(this.GetTypeReference("System.Object")),
                    new ByReferenceType(this.GetTypeReference("System.Object")),
                    new ByReferenceType(this.GetTypeReference("System.Object")),
                    new ByReferenceType(this.GetTypeReference("System.Boolean")),
                    new ByReferenceType(this.GetTypeReference("System.Object"))
                },
                new[]
                {
                    "hookName", "param1",
                    "param2", "param3", "param4",
                    "param5", "__state", "__result"
                },
                this.GetTypeReference("System.Void"));

            this.InsertModHook("OnCommonHook10_Prefix", new[]
                {
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
                    new ByReferenceType(this.GetTypeReference("System.Object"))
                },
                new[]
                {
                    "hookName", "__instance",
                    "param1", "param2", "param3",
                    "param4", "param5", "param6",
                    "param7", "param8", "param9",
                    "__result"
                },
                this.GetTypeReference("System.Boolean"));
            this.InsertModHook("OnCommonStaticHook10_Prefix", new[]
                {
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
                    new ByReferenceType(this.GetTypeReference("System.Object"))
                },
                new[]
                {
                    "hookName", "param1",
                    "param2", "param3", "param4",
                    "param5", "param6", "param7",
                    "param8", "param9", "param10",
                    "__result"
                },
                this.GetTypeReference("System.Boolean"));
            this.InsertModHook("OnCommonHook10_Postfix", new[]
                {
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
                    new ByReferenceType(this.GetTypeReference("System.Object"))
                },
                new[]
                {
                    "hookName", "__instance",
                    "param1", "param2", "param3",
                    "param4", "param5", "param6",
                    "param7", "param8", "param9",
                    "__state", "__result"
                },
                this.GetTypeReference("System.Void"));
            this.InsertModHook("OnCommonStaticHook10_Postfix", new[]
                {
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
                    new ByReferenceType(this.GetTypeReference("System.Object"))
                },
                new[]
                {
                    "hookName", "param1",
                    "param2", "param3", "param4",
                    "param5", "param6", "param7",
                    "param8", "param9", "param10",
                    "__state", "__result"
                },
                this.GetTypeReference("System.Void"));
            // On Game1 hooks
            var typeGame1 = this.StardewValley.MainModule.GetType("StardewValley.Game1");
            this.ApplyCommonHookEntry(typeGame1, "getSourceRectForStandardTileSheet", "OnCommonStaticHook");
            this.ApplyCommonHookEntry(typeGame1, "tryToCheckAt", "OnCommonStaticHook", false);
            this.ApplyCommonHookEntry(typeGame1, "getLocationRequest", "OnCommonStaticHook", true, false);
            this.ApplyCommonHookEntry(typeGame1, "loadForNewGame", "OnCommonStaticHook");
            this.ApplyCommonHookEntry(typeGame1, "warpFarmer", "OnCommonStaticHook10", true, false,
                method => method.Parameters.Count == 6);
            this.ApplyCommonHookEntry(typeGame1, "saveWholeBackup", "OnCommonStaticHook");
            this.ApplyCommonHookEntry(typeGame1, "MakeFullBackup", "OnCommonStaticHook");
            TypeDefinition targetType = null;
            foreach (var definition21 in this.StardewValley.MainModule.GetTypes())
                if (definition21.FullName == "StardewValley.Game1/<>c")
                    targetType = definition21;
            this.ApplyCommonMidHookEntry(targetType, method => method.FullName.Contains("showEndOfNightStuff"),
                ins => ins.OpCode == OpCodes.Ldstr && (string) ins.Operand == "newRecord", -2, "OnCommonHook");

            // On Object hooks
            var typeObject = this.StardewValley.MainModule.GetType("StardewValley.Object");
            this.ApplyCommonHookEntry(typeObject, "canBePlacedHere", "OnCommonHook", true, false);
            this.ApplyCommonHookEntry(typeObject, "checkForAction", "OnCommonHook", true, false);
            this.ApplyCommonHookEntry(typeObject, "isIndexOkForBasicShippedCategory", "OnCommonStaticHook");
            this.ApplyCommonHookEntry(typeObject, "drawWhenHeld", "OnCommonHook", true, false);
            this.ApplyCommonHookEntry(typeObject, "drawInMenuWithColour", "OnCommonHook10");
            this.ApplyCommonHookEntry(typeObject, "draw", "OnCommonHook", true, false,
                method => method.Parameters.Count == 4);
            this.ApplyCommonHookEntry(typeObject, "draw", "OnCommonHook10", true, false,
                method => method.Parameters.Count == 5);
            this.ApplyCommonHookEntry(typeObject, "getDescription", "OnCommonHook", true, false);
            this.ApplyCommonHookEntry(typeObject, "maximumStackSize", "OnCommonHook", true, false);
            this.ApplyCommonHookEntry(typeObject, "addToStack", "OnCommonHook", true, false);
            this.ApplyCommonHookEntry(typeObject, "performObjectDropInAction", "OnCommonHook", true, false);
            

            // On ReadyCheckDialog hooks
            var typeReadyCheckDialog = this.StardewValley.MainModule.GetType("StardewValley.Menus.ReadyCheckDialog");
            this.ApplyCommonHookEntry(typeReadyCheckDialog, "update", "OnCommonHook");

            // On IClickableMenu hooks
            var typeIClickableMenu = this.StardewValley.MainModule.GetType("StardewValley.Menus.IClickableMenu");
            this.ApplyCommonHookEntry(typeIClickableMenu, "drawToolTip", "OnCommonStaticHook10", false);

            // On Dialogue hooks
            var typeDialogue = this.StardewValley.MainModule.GetType("StardewValley.Dialogue");
            this.ApplyCommonHookEntry(typeDialogue, ".ctor", "OnCommonHook", true, false,
                method => method.Parameters.Count == 2);

            // On CraftingRecipe hooks
            var typeCraftingRecipe = this.StardewValley.MainModule.GetType("StardewValley.CraftingRecipe");
            this.ApplyCommonHookEntry(typeCraftingRecipe, "consumeIngredients", "OnCommonHook", true, false);

            // On Building hooks
            var typeBuilding = this.StardewValley.MainModule.GetType("StardewValley.Buildings.Building");
            this.ApplyCommonHookEntry(typeBuilding, "load", "OnCommonHook");

            // On GameLocation hooks
            var typeGameLocation = this.StardewValley.MainModule.GetType("StardewValley.GameLocation");
            this.ApplyCommonHookEntry(typeGameLocation, "performTouchAction", "OnCommonHook", true, false);
            this.ApplyCommonHookEntry(typeGameLocation, "isActionableTile", "OnCommonHook", false);
            this.ApplyCommonHookEntry(typeGameLocation, "tryToAddCritters", "OnCommonHook", true, false);
            this.ApplyCommonHookEntry(typeGameLocation, "getSourceRectForObject", "OnCommonStaticHook");
            this.ApplyCommonHookEntry(typeGameLocation, "answerDialogue", "OnCommonHook", true, false);
            this.ApplyCommonHookEntry(typeGameLocation, "Equals", "OnCommonHook", true, false,
                method => method.Parameters[0].ParameterType ==
                          this.GetTypeReference("StardewValley.GameLocation", this.StardewValley));
            this.ApplyCommonHookEntry(typeGameLocation, "performAction", "OnCommonHook", true, false);

            // On Objects.TV hooks
            var typeObjectsTV = this.StardewValley.MainModule.GetType("StardewValley.Objects.TV");
            this.ApplyCommonHookEntry(typeObjectsTV, "checkForAction", "OnCommonHook");

            // On Furniture hooks
            var typeFurniture = this.StardewValley.MainModule.GetType("StardewValley.Objects.Furniture");
            this.ApplyCommonHookEntry(typeFurniture, "draw", "OnCommonHook", true, false);

            // On ColoredObject hooks
            var typeColoredObject = this.StardewValley.MainModule.GetType("StardewValley.Objects.ColoredObject");
            this.ApplyCommonHookEntry(typeColoredObject, "drawInMenu", "OnCommonHook10", false);

            // On HoeDirt hooks
            var typeHoeDirt = this.StardewValley.MainModule.GetType("StardewValley.TerrainFeatures.HoeDirt");
            this.ApplyCommonHookEntry(typeHoeDirt, "dayUpdate", "OnCommonHook", true, false);
            this.ApplyCommonHookEntry(typeHoeDirt, "canPlantThisSeedHere", "OnCommonHook");

            // On Tree hooks
            var typeTree = this.StardewValley.MainModule.GetType("StardewValley.TerrainFeatures.Tree");
            this.ApplyCommonHookEntry(typeTree, "performToolAction", "OnCommonHook", true, false);

            // On FruitTree hooks
            var typeFruitTree = this.StardewValley.MainModule.GetType("StardewValley.TerrainFeatures.FruitTree");
            this.ApplyCommonHookEntry(typeFruitTree, "performToolAction", "OnCommonHook", true, false);

            // On ResourceClump hooks
            var typeResourceClump = this.StardewValley.MainModule.GetType("StardewValley.TerrainFeatures.ResourceClump");
            this.ApplyCommonHookEntry(typeResourceClump, "performToolAction", "OnCommonHook", true, false);
            
            // On Crop hooks
            var typeCrop = this.StardewValley.MainModule.GetType("StardewValley.Crop");
            this.ApplyCommonHookEntry(typeCrop, "newDay", "OnCommonHook10", true, false);

            // On Utility hooks
            var typeUtility = this.StardewValley.MainModule.GetType("StardewValley.Utility");
            this.ApplyCommonHookEntry(typeUtility, "pickFarmEvent", "OnCommonStaticHook", false);

            // On Farmer hooks
            var typeFarmer = this.StardewValley.MainModule.GetType("StardewValley.Farmer");
            this.ApplyCommonHookEntry(typeFarmer, "doneEating", "OnCommonHook", false);
            this.ApplyCommonHookEntry(typeFarmer, "hasItemInInventory", "OnCommonHook", false);
            this.ApplyCommonHookEntry(typeFarmer, "getTallyOfObject", "OnCommonHook", false);
            
            // On MeleeWeapon hooks
            var typeMeleeWeapon = this.StardewValley.MainModule.GetType("StardewValley.Tools.MeleeWeapon");
            this.ApplyCommonHookEntry(typeMeleeWeapon, "drawDuringUse", "OnCommonStaticHook10", true, false,
                method => method.IsStatic);

            // On Multiplayer hooks
            var typeMultiplayer = this.StardewValley.MainModule.GetType("StardewValley.Multiplayer");
            this.ApplyCommonHookEntry(typeMultiplayer, "processIncomingMessage", "OnCommonHook", true, false);

            // On GameServer hooks
            var typeGameServer = this.StardewValley.MainModule.GetType("StardewValley.Network.GameServer");
            this.ApplyCommonHookEntry(typeGameServer, "sendServerIntroduction", "OnCommonHook", false);

            // On NPC hooks
            var typeNPC = this.StardewValley.MainModule.GetType("StardewValley.NPC");
            this.ApplyCommonHookEntry(typeNPC, "receiveGift", "OnCommonHook10", false);

            // On Farm hooks
            var typeFarm = this.StardewValley.MainModule.GetType("StardewValley.Farm");
            this.ApplyCommonHookEntry(typeFarm, "addCrows", "OnCommonHook", true, false);

            // On GameMenu hooks
            var typeGameMenu = this.StardewValley.MainModule.GetType("StardewValley.Menus.GameMenu");
            this.ApplyCommonHookEntry(typeGameMenu, "getTabNumberFromName", "OnCommonHook", false);

            // On FarmHouse hooks
            var typeFarmHouse = this.StardewValley.MainModule.GetType("StardewValley.Locations.FarmHouse");
            this.ApplyCommonHookEntry(typeFarmHouse, "loadSpouseRoom", "OnCommonHook", true, false);

            // On Tool hooks
            var typeTool = this.StardewValley.MainModule.GetType("StardewValley.Tool");
            this.ApplyCommonHookEntry(typeTool, "tilesAffected", "OnCommonHook", false);
            this.ApplyCommonHookEntry(typeTool, "get_Name", "OnCommonHook", true, false);
            this.ApplyCommonHookEntry(typeTool, "get_DisplayName", "OnCommonHook", true, false);
            // On Pickaxe hooks
            var typePickaxe = this.StardewValley.MainModule.GetType("StardewValley.Tools.Pickaxe");
            this.ApplyCommonHookEntry(typePickaxe, "DoFunction", "OnCommonHook10", true, false);

            return this.StardewValley;
        }

        public AssemblyDefinition InsertMonoHooks()
        {
            // On Content hooks
            var typeTitleContainer = this.MonoGame_Framework.MainModule.GetType("Microsoft.Xna.Framework.TitleContainer");
            var openStreamMethod = typeTitleContainer.Methods.FirstOrDefault(m => m.Name == "OpenStream");
            var processor = openStreamMethod.Body.GetILProcessor();
            var jointPoint = openStreamMethod.Body.Instructions[9];
            processor.InsertBefore(jointPoint, processor.Create(OpCodes.Ldarg_0));
            processor.InsertBefore(jointPoint, processor.Create(OpCodes.Ldc_I4_S, (sbyte)92));
            processor.InsertBefore(jointPoint, processor.Create(OpCodes.Ldc_I4_S, (sbyte)47));
            var replaceMethod = this.GetMethodReference("Replace", "System.String",
                this.mscorlib,
                m => m.Parameters.Count == 2 && m.Parameters[0].ParameterType.FullName == "System.Char" &&
                     m.Parameters[1].ParameterType.FullName == "System.Char", this.MonoGame_Framework);
            processor.InsertBefore(jointPoint, processor.Create(OpCodes.Callvirt, replaceMethod));
            processor.InsertBefore(jointPoint, processor.Create(OpCodes.Ldc_I4_3));
            var constructor = this.GetMethodReference(".ctor", "System.IO.FileStream",
                this.mscorlib,
                m => m.Parameters.Count == 2 && m.Parameters[0].ParameterType.FullName == "System.String" &&
                     m.Parameters[1].ParameterType.FullName == "System.IO.FileMode", this.MonoGame_Framework);
            processor.InsertBefore(jointPoint, processor.Create(OpCodes.Newobj, constructor));
            processor.InsertBefore(jointPoint, processor.Create(OpCodes.Ret));


            // On SpriteBatch hooks
            var typeSpriteBatch = this.MonoGame_Framework.MainModule.GetType("Microsoft.Xna.Framework.Graphics.SpriteBatch");
            FieldReference hooksField = this.MonoGame_Framework.MainModule.ImportReference(this.GetFieldReference("hooks", "StardewValley.Game1", this.StardewValley));
            var typeModHooksObject = this.StardewValley.MainModule.GetType("StardewValley.ModHooks");
            var prefixHook = typeModHooksObject.Methods.FirstOrDefault(m => m.Name == "OnCommonHook_Prefix");
            var prefixHook10 = typeModHooksObject.Methods.FirstOrDefault(m => m.Name == "OnCommonHook10_Prefix");
            var prefixHookRef = this.MonoGame_Framework.MainModule.ImportReference(prefixHook);
            var prefixHook10Ref = this.MonoGame_Framework.MainModule.ImportReference(prefixHook10);
            this.ApplyCommonHookEntry(typeSpriteBatch, "Draw", "OnCommonHook", true, false,
                method => method.Parameters.Count == 3 &&
                          method.Parameters[1].ParameterType.FullName == "Microsoft.Xna.Framework.Rectangle", "",
                hooksField, prefixHookRef);
            this.ApplyCommonHookEntry(typeSpriteBatch, "Draw", "OnCommonHook", true, false,
                method => method.Parameters.Count == 3 &&
                          method.Parameters[1].ParameterType.FullName == "Microsoft.Xna.Framework.Vector2", "Vector",
                hooksField, prefixHookRef);
            this.ApplyCommonHookEntry(typeSpriteBatch, "Draw", "OnCommonHook", true, false,
                method => method.Parameters.Count == 4 &&
                          method.Parameters[1].ParameterType.FullName == "Microsoft.Xna.Framework.Rectangle", "2",
                hooksField, prefixHookRef);
            this.ApplyCommonHookEntry(typeSpriteBatch, "Draw", "OnCommonHook", true, false,
                method => method.Parameters.Count == 4 &&
                          method.Parameters[1].ParameterType.FullName == "Microsoft.Xna.Framework.Vector2", "Vector2",
                hooksField, prefixHookRef);
            this.ApplyCommonHookEntry(typeSpriteBatch, "Draw", "OnCommonHook10", true, false,
                method => method.Parameters.Count == 8, "8", hooksField, prefixHook10Ref);
            this.ApplyCommonHookEntry(typeSpriteBatch, "Draw", "OnCommonHook10", true, false,
                method => method.Parameters.Count == 9 &&
                          method.Parameters[6].ParameterType.FullName == "Microsoft.Xna.Framework.Vector2", "9",
                hooksField, prefixHook10Ref);
            return this.MonoGame_Framework;
        }
    }
}
