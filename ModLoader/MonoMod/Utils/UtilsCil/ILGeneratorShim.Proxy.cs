﻿using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace MonoMod.Utils.Cil {
    public partial class ILGeneratorShim {

        public System.Reflection.Emit.ILGenerator GetProxy() {
            return (System.Reflection.Emit.ILGenerator) ILGeneratorBuilder
                .GenerateProxy()
                .MakeGenericType(GetType())
                .GetConstructors()[0]
                .Invoke(new object[] { this });
        }

        public static Type GetProxyType<TShim>() where TShim : ILGeneratorShim => GetProxyType(typeof(TShim));
        public static Type GetProxyType(Type tShim) => ProxyType.MakeGenericType(tShim);
        public static Type ProxyType => ILGeneratorBuilder.GenerateProxy();

        static class ILGeneratorBuilder {

            public const string Namespace = "MonoMod.Utils.Cil";
            public const string Name = "ILGeneratorProxy";
            public const string FullName = Namespace + "." + Name;
            static Type ProxyType;

            public static Type GenerateProxy() {
                if (ProxyType != null)
                    return ProxyType;
                Assembly asm;

                Type t_ILGenerator = typeof(System.Reflection.Emit.ILGenerator);
                Type t_ILGeneratorProxyTarget = typeof(ILGeneratorShim);

#if !CECIL0_9
                using (
#endif
                ModuleDefinition module = ModuleDefinition.CreateModule(
                    FullName,
                    new ModuleParameters() {
                        Kind = ModuleKind.Dll,
#if !CECIL0_9
                        ReflectionImporterProvider = MMReflectionImporter.Provider
#endif
                    }
                )
#if CECIL0_9
                ;
#else
                )
#endif
                {

                    TypeDefinition type = new TypeDefinition(
                        Namespace,
                        Name,
                        TypeAttributes.Public
                    ) {
                        BaseType = module.ImportReference(t_ILGenerator)
                    };
                    module.Types.Add(type);

                    TypeReference tr_ILGeneratorProxyTarget = module.ImportReference(t_ILGeneratorProxyTarget);

                    GenericParameter g_TTarget = new GenericParameter("TTarget", type);
                    g_TTarget.Constraints.Add(tr_ILGeneratorProxyTarget);
                    type.GenericParameters.Add(g_TTarget);

                    FieldDefinition fd_Target = new FieldDefinition(
                        "Target",
                        FieldAttributes.Public,
                        g_TTarget
                    );
                    type.Fields.Add(fd_Target);

                    ILProcessor il;

                    MethodDefinition ctor = new MethodDefinition(".ctor",
                        MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                        module.TypeSystem.Void
                    );
                    ctor.Parameters.Add(new ParameterDefinition(g_TTarget));
                    type.Methods.Add(ctor);

                    il = ctor.Body.GetILProcessor();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Stfld, fd_Target);
                    il.Emit(OpCodes.Ret);

                    foreach (MethodInfo orig in t_ILGenerator.GetMethods(BindingFlags.Public | BindingFlags.Instance)) {
                        MethodInfo target = t_ILGeneratorProxyTarget.GetMethod(orig.Name, orig.GetParameters().Select(p => p.ParameterType).ToArray());
                        if (target == null)
                            continue;

                        MethodDefinition proxy = new MethodDefinition(
                            orig.Name,
                            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig,
                            module.ImportReference(orig.ReturnType)
                        ) {
                            HasThis = true
                        };
                        foreach (ParameterInfo param in orig.GetParameters())
                            proxy.Parameters.Add(new ParameterDefinition(module.ImportReference(param.ParameterType)));
                        type.Methods.Add(proxy);

                        il = proxy.Body.GetILProcessor();
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldfld, fd_Target);
                        foreach (ParameterDefinition param in proxy.Parameters)
                            il.Emit(OpCodes.Ldarg, param);
                        il.Emit(target.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, target);
                        il.Emit(OpCodes.Ret);
                    }

                    asm = ReflectionHelper.Load(module);
                }

                return ProxyType = asm.GetType(FullName);
            }

        }

    }
}
