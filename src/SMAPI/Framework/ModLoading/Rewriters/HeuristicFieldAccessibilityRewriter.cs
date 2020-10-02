using System;
using System.Collections.Generic;
using System.Reflection;
#if HARMONY_2
using HarmonyLib;
#else
using Harmony;
#endif
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters
{
    /// <summary>Automatically fix references to fields that have been replaced by a property or const field.</summary>
    internal class HeuristicFieldAccessibilityRewriter : BaseInstructionHandler
    {
        /*********
        ** Fields
        *********/
        /// <summary>The assembly names to which to rewrite broken references.</summary>
        private readonly HashSet<string> RewriteReferencesToAssemblies;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="rewriteReferencesToAssemblies">The assembly names to which to rewrite broken references.</param>
        public HeuristicFieldAccessibilityRewriter(string[] rewriteReferencesToAssemblies)
            : base(defaultPhrase: "field visibility changed to private") // ignored since we specify phrases
        {
            this.RewriteReferencesToAssemblies = new HashSet<string>(rewriteReferencesToAssemblies);
        }

        /// <inheritdoc />
        public override bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction)
        {
            // get field ref
            FieldReference fieldRef = RewriteHelper.AsFieldReference(instruction);
            if (fieldRef == null || !this.ShouldValidate(fieldRef.DeclaringType))
                return false;

            // skip if not broken
            FieldDefinition fieldDefinition = fieldRef.Resolve();
            if (fieldDefinition == null || fieldDefinition.IsPublic || fieldDefinition.IsFamily)
                return false;

            // rewrite if possible
            bool isRead = instruction.OpCode == OpCodes.Ldsfld || instruction.OpCode == OpCodes.Ldfld;
            bool isStatic = instruction.OpCode == OpCodes.Ldsfld || instruction.OpCode == OpCodes.Stsfld;
            return
                this.TryRewriteWithReflection(cil, module, instruction, fieldRef, isRead, isStatic);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Whether references to the given type should be validated.</summary>
        /// <param name="type">The type reference.</param>
        private bool ShouldValidate(TypeReference type)
        {
            return type != null && this.RewriteReferencesToAssemblies.Contains(type.Scope.Name);
        }

        /// <summary>Try rewriting the direct field reference with reflection access.</summary>
        /// <param name="cil">The CIL processor.</param>
        /// <param name="module">The assembly module containing the instruction.</param>
        /// <param name="instruction">The CIL instruction to rewrite.</param>
        /// <param name="fieldRef">The field reference.</param>
        /// <param name="isRead">Whether the field is being read; else it's being written to.</param>
        /// <param name="isStatic">Whether the field is static field; else it's instance field.</param>
        /// <param name="declaringType">The type on which the field was defined.</param>
        private bool TryRewriteWithReflection(ILProcessor cil, ModuleDefinition module, Instruction instruction, FieldReference fieldRef, bool isRead, bool isStatic)
        {
            MethodReference getTypeFromHandleRef = module.ImportReference(typeof(Type).GetMethod("GetTypeFromHandle", new Type[] {typeof(RuntimeTypeHandle)}));
            MethodReference getFieldRef = module.ImportReference(typeof(Type).GetMethod("GetField", new Type[] {typeof(string), typeof(BindingFlags)}));
            VariableDefinition varInstance = null;
            if(isRead)
            {
                if (!isStatic)
                {
                    varInstance = new VariableDefinition(fieldRef.DeclaringType);
                    cil.Body.Variables.Add(varInstance);
                }
                // inverse order insert
                // load instance (origin logic, if not static)
                // stloc.s 0
                // ldtoken	MonoGame.Framework.Patcher.Test
                // call	System.Type System.Type::GetTypeFromHandle(System.RuntimeTypeHandle)
                // ldstr	text
                // ldc.i4.s	60
                // callvirt	System.Reflection.FieldInfo System.Type::GetField(System.String,System.Reflection.BindingFlags)
                // ldloc.0
                // callvirt	System.Object System.Reflection.FieldInfo::GetValue(System.Object)
                // castclass System.String
                if (fieldRef.FieldType.IsValueType)
                    cil.InsertAfter(instruction, cil.Create(OpCodes.Unbox_Any, fieldRef.FieldType));
                else
                    cil.InsertAfter(instruction, cil.Create(OpCodes.Castclass, fieldRef.FieldType));
                MethodReference getValueMethodRef = module.ImportReference(AccessTools.Method(typeof(FieldInfo), nameof(FieldInfo.GetValue)));
                cil.InsertAfter(instruction, cil.Create(OpCodes.Callvirt, getValueMethodRef));
                if (!isStatic)
                    cil.InsertAfter(instruction, cil.Create(OpCodes.Ldloc_S, varInstance));
                else
                    cil.InsertAfter(instruction, cil.Create(OpCodes.Ldnull));
                cil.InsertAfter(instruction, cil.Create(OpCodes.Callvirt, getFieldRef));
                cil.InsertAfter(instruction, cil.Create(OpCodes.Ldc_I4_S, (sbyte)60));
                cil.InsertAfter(instruction, cil.Create(OpCodes.Ldstr, fieldRef.Name));
                cil.InsertAfter(instruction, cil.Create(OpCodes.Call, getTypeFromHandleRef));
                if (!isStatic)
                {
                    // prohibit replace remove entry point (may cause jump logic broken)
                    cil.InsertAfter(instruction, cil.Create(OpCodes.Nop));
                    instruction.OpCode = OpCodes.Stloc_S;
                    instruction.Operand = varInstance;
                    instruction = instruction.Next;
                }
            }
            else
            {
                VariableDefinition varValue = null;
                if (!isStatic)
                {
                    varInstance = new VariableDefinition(fieldRef.DeclaringType);
                    cil.Body.Variables.Add(varInstance);
                }
                varValue = new VariableDefinition(fieldRef.FieldType);
                cil.Body.Variables.Add(varValue);
                // inverse order insert
                // load instance (origin logic, if not static)
                // load value (origin logic)
                // stloc.s 0
                // ldtoken	Patcher.TestClass
                // call	System.Type System.Type::GetTypeFromHandle(System.RuntimeTypeHandle)
                // ldstr	text
                // ldc.i4.s	60
                // callvirt	System.Reflection.FieldInfo System.Type::GetField(System.String,System.Reflection.BindingFlags)
                // ldloc.0
                // ldloc.1
                // callvirt	System.Object System.Reflection.FieldInfo::SetValue(System.Object, System.Object)
                MethodReference setValueMethodRef = module.ImportReference(AccessTools.Method(typeof(FieldInfo), nameof(FieldInfo.SetValue), new Type[] {typeof(object), typeof(object)}));
                cil.InsertAfter(instruction, cil.Create(OpCodes.Callvirt, setValueMethodRef));
                cil.InsertAfter(instruction, cil.Create(OpCodes.Ldloc_S, varValue));
                if(isStatic)
                    cil.InsertAfter(instruction, cil.Create(OpCodes.Ldnull));
                else
                    cil.InsertAfter(instruction, cil.Create(OpCodes.Ldloc_S, varInstance));
                cil.InsertAfter(instruction, cil.Create(OpCodes.Callvirt, getFieldRef));
                cil.InsertAfter(instruction, cil.Create(OpCodes.Ldc_I4_S, (sbyte)60));
                cil.InsertAfter(instruction, cil.Create(OpCodes.Ldstr, fieldRef.Name));
                cil.InsertAfter(instruction, cil.Create(OpCodes.Call, getTypeFromHandleRef));

                // prohibit replace remove entry point (may cause jump logic broken)
                instruction.OpCode = OpCodes.Stloc_S;
                instruction.Operand = varValue;
                cil.InsertAfter(instruction, cil.Create(OpCodes.Nop));
                if (!isStatic)
                {
                    cil.InsertAfter(instruction, cil.Create(OpCodes.Stloc_S, varInstance));
                    instruction = instruction.Next;
                }
                instruction = instruction.Next;
            }
            // rewrite field ref to ldtoken
            instruction.OpCode = OpCodes.Ldtoken;
            instruction.Operand = fieldRef.DeclaringType;

            this.Phrases.Add($"{fieldRef.DeclaringType.Name}.{fieldRef.Name} (field ref => reflection ref)");
// #if SMAPI_FOR_MOBILE
//             this.Phrases.Add($"{cil.Body.Method.FullName} => {cil.Body.Instructions.Select(ins => ins.ToString()).Join(null, ";")}");
// #endif
            cil.Body.MaxStackSize += 5;
            return this.MarkRewritten();
        }
    }
}
