﻿using System;
using System.Reflection;
using System.Linq.Expressions;
using MonoMod.Utils;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
#if !NETSTANDARD
using System.Security.Permissions;
using System.Security;
using System.Diagnostics.SymbolStore;
#endif

namespace MonoMod.Utils {
    public sealed partial class DynamicMethodDefinition {

        public MethodInfo GenerateViaCecil(TypeDefinition typeDef) {
            MethodDefinition def = Definition;

            bool moduleIsPrivate = false;
            ModuleDefinition module = typeDef?.Module;
            HashSet<string> accessChecksIgnored = null;
            if (typeDef == null) {
                moduleIsPrivate = true;
                accessChecksIgnored = new HashSet<string>();

                string name = _GetDumpName("Cecil");
                module = ModuleDefinition.CreateModule(name, new ModuleParameters() {
                    Kind = ModuleKind.Dll,
                    AssemblyResolver = new AssemblyCecilDefinitionResolver(_ModuleGen, new DefaultAssemblyResolver()),
#if !CECIL0_9
                    ReflectionImporterProvider = new ReflectionCecilImporterProvider(null)
#endif
                });

#if !NETSTANDARD1_X
                module.Assembly.CustomAttributes.Add(new CustomAttribute(module.ImportReference(c_UnverifiableCodeAttribute)));
#endif

                if (Debug) {
                    CustomAttribute caDebug = new CustomAttribute(module.ImportReference(c_DebuggableAttribute));
                    caDebug.ConstructorArguments.Add(new CustomAttributeArgument(
                        module.ImportReference(typeof(DebuggableAttribute.DebuggingModes)),
                        DebuggableAttribute.DebuggingModes.DisableOptimizations | DebuggableAttribute.DebuggingModes.Default
                    ));
                    module.Assembly.CustomAttributes.Add(caDebug);
                }

                typeDef = new TypeDefinition(
                    "",
                    $"DMD<{Method?.GetFindableID(simple: true)?.Replace('.', '_')}>?{GetHashCode()}",
                    Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Abstract | Mono.Cecil.TypeAttributes.Sealed | Mono.Cecil.TypeAttributes.Class
                ) {
                    BaseType = module.TypeSystem.Object
                };

                module.Types.Add(typeDef);
            }

            try {

#pragma warning disable IDE0039 // Use local function
                Relinker relinker = (mtp, ctx) => {
                    return module.ImportReference(mtp);
                };
#pragma warning restore IDE0039 // Use local function

                MethodDefinition clone = new MethodDefinition("_" + def.Name.Replace('.', '_'), def.Attributes, module.TypeSystem.Void) {
                    MethodReturnType = def.MethodReturnType,
                    Attributes = Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.HideBySig | Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static,
                    ImplAttributes = Mono.Cecil.MethodImplAttributes.IL | Mono.Cecil.MethodImplAttributes.Managed,
                    DeclaringType = typeDef,
                    NoInlining = true
                };

                foreach (ParameterDefinition param in def.Parameters)
                    clone.Parameters.Add(param.Clone().Relink(relinker, clone));

                clone.ReturnType = def.ReturnType.Relink(relinker, clone);

                typeDef.Methods.Add(clone);

                clone.HasThis = def.HasThis;
                Mono.Cecil.Cil.MethodBody body = clone.Body = def.Body.Clone(clone);

                foreach (VariableDefinition var in clone.Body.Variables)
                    var.VariableType = var.VariableType.Relink(relinker, clone);

                foreach (ExceptionHandler handler in clone.Body.ExceptionHandlers)
                    if (handler.CatchType != null)
                        handler.CatchType = handler.CatchType.Relink(relinker, clone);

                for (int instri = 0; instri < body.Instructions.Count; instri++) {
                    Instruction instr = body.Instructions[instri];
                    object operand = instr.Operand;

                    // Import references.
                    if (operand is ParameterDefinition param) {
                        operand = clone.Parameters[param.Index];

                    } else if (operand is IMetadataTokenProvider mtp) {
                        operand = mtp.Relink(relinker, clone);

                    }

                    if (operand is DynamicMethodReference dmref) {
                        // TODO: Fix up DynamicMethod inline refs.
                    }

                    if (accessChecksIgnored != null && operand is MemberReference mref) {
                        // TODO: Only do the following for inaccessible members.
                        IMetadataScope asmRef = (mref as TypeReference)?.Scope ?? mref.DeclaringType.Scope;
                        if (!accessChecksIgnored.Contains(asmRef.Name)) {
                            CustomAttribute caAccess = new CustomAttribute(module.ImportReference(c_IgnoresAccessChecksToAttribute));
                            caAccess.ConstructorArguments.Add(new CustomAttributeArgument(
                                module.ImportReference(typeof(DebuggableAttribute.DebuggingModes)),
                                asmRef.Name
                            ));
                            module.Assembly.CustomAttributes.Add(caAccess);
                            accessChecksIgnored.Add(asmRef.Name);
                        }
                    }

                    instr.Operand = operand;
                }

                clone.HasThis = false;

                if (def.HasThis) {
                    TypeReference type = def.DeclaringType;
                    if (type.IsValueType)
                        type = new ByReferenceType(type);
                    clone.Parameters.Insert(0, new ParameterDefinition("<>_this", Mono.Cecil.ParameterAttributes.None, type.Relink(relinker, clone)));
                }

                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MONOMOD_DMD_DUMP"))) {
                    string dir = Path.GetFullPath(Environment.GetEnvironmentVariable("MONOMOD_DMD_DUMP"));
                    string name = module.Name + ".dll";
                    string path = Path.Combine(dir, name);
                    dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    if (File.Exists(path))
                        File.Delete(path);
                    using (Stream fileStream = File.OpenWrite(path))
                        module.Write(fileStream);
                }

                Assembly asm = ReflectionHelper.Load(module);

                _DynModuleCache[module.Assembly.Name.FullName] = module.Assembly;
                _DynModuleReflCache[asm.GetModules()[0]] = module;
                _DynModuleIsPrivate = false;
                moduleIsPrivate = false;

                return _Postbuild(asm.GetType(typeDef.FullName.Replace("+", "\\+"), false, false).GetMethod(clone.Name));

            } finally {
#if !CECIL0_9
                if (moduleIsPrivate)
                    module.Dispose();
#endif
            }
        }

    }
}
