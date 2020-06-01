using System;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Finders;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters
{
    internal class TypeFieldToAnotherTypeFieldRewriter : FieldFinder
    {
        /*********
        ** Fields
        *********/
        /// <summary>The type whose field to which references should be rewritten to.</summary>
        private readonly Type ToType;

        /// <summary>The property name.</summary>
        private readonly string InstancePropertyName;

        private readonly string TargetFieldName;

        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="type">The type whose field to which references should be rewritten.</param>
        /// <param name="toType"></param>
        /// <param name="fieldName">The field name to rewrite.</param>
        /// <param name="targetFieldName"></param>
        /// <param name="instancePropertyName">The property name (if different).</param>
        public TypeFieldToAnotherTypeFieldRewriter(Type type, Type toType, string fieldName, string targetFieldName = null, string instancePropertyName = null)
            : base(type.FullName, fieldName, InstructionHandleResult.None)
        {
            this.ToType = toType;
            this.InstancePropertyName = instancePropertyName;
            if (targetFieldName == null)
            {
                this.TargetFieldName = fieldName;
            }
            else
            {
                this.TargetFieldName = targetFieldName;
            }
        }

        /// <summary>Perform the predefined logic for an instruction if applicable.</summary>
        /// <param name="module">The assembly module containing the instruction.</param>
        /// <param name="cil">The CIL processor.</param>
        /// <param name="instruction">The instruction to handle.</param>
        /// <param name="replaceWith">Replaces the CIL instruction with a new one.</param>
        /// <returns>Returns whether the instruction was changed.</returns>
        public override bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction, Action<Instruction> replaceWith)
        {
            if (!this.IsMatch(instruction))
                return false;

            FieldInfo targetFieldInfo = this.ToType.GetField(this.TargetFieldName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            PropertyInfo targetPropertyInfo = null;
            if (targetFieldInfo == null)
            {
                targetPropertyInfo = this.ToType.GetProperty(this.TargetFieldName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }
            if (this.InstancePropertyName != null)
            {
                MethodReference instanceMethod = module.ImportReference(this.ToType.GetMethod($"get_{this.InstancePropertyName}"));
                if (targetFieldInfo != null)
                {
                    FieldReference targetField = module.ImportReference(targetFieldInfo);
                    if (instruction.OpCode == OpCodes.Ldfld)
                    {
                        cil.Replace(instruction.Previous, cil.Create(OpCodes.Call, instanceMethod));
                        replaceWith.Invoke(cil.Create(instruction.OpCode, targetField));
                    }
                    else if (instruction.OpCode == OpCodes.Stfld)
                    {
                        cil.Replace(instruction.Previous.Previous, cil.Create(OpCodes.Call, instanceMethod));
                        replaceWith.Invoke(cil.Create(instruction.OpCode, targetField));
                    }
                    else if(instruction.OpCode == OpCodes.Ldsfld)
                    {
                        cil.InsertAfter(instruction, cil.Create(instruction.OpCode, targetField));
                        replaceWith.Invoke(cil.Create(OpCodes.Call, instanceMethod));
                    }
                    else if (instruction.OpCode == OpCodes.Stsfld)
                    {
                        cil.InsertBefore(instruction.Previous, cil.Create(OpCodes.Call, instanceMethod));
                        replaceWith.Invoke(cil.Create(instruction.OpCode, targetField));
                    }
                }
                else if(targetPropertyInfo != null)
                {
                    if (instruction.OpCode == OpCodes.Ldfld)
                    {
                        cil.Replace(instruction.Previous, cil.Create(OpCodes.Call, instanceMethod));
                        replaceWith.Invoke(cil.Create(OpCodes.Call, module.ImportReference(targetPropertyInfo.GetGetMethod())));
                    }
                    else if (instruction.OpCode == OpCodes.Stfld)
                    {
                        cil.Replace(instruction.Previous.Previous, cil.Create(OpCodes.Call, instanceMethod));
                        replaceWith.Invoke(cil.Create(OpCodes.Call, module.ImportReference(targetPropertyInfo.GetSetMethod())));
                    }
                    else if(instruction.OpCode == OpCodes.Ldsfld)
                    {
                        cil.InsertAfter(instruction, cil.Create(OpCodes.Call, module.ImportReference(targetPropertyInfo.GetGetMethod())));
                        replaceWith.Invoke(cil.Create(OpCodes.Call, instanceMethod));
                    }
                    else if (instruction.OpCode == OpCodes.Stsfld)
                    {
                        cil.InsertBefore(instruction.Previous, cil.Create(OpCodes.Call, instanceMethod));
                        replaceWith.Invoke(cil.Create(OpCodes.Call, module.ImportReference(targetPropertyInfo.GetSetMethod())));
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (targetFieldInfo != null)
                {
                    FieldReference targetField = module.ImportReference(targetFieldInfo);
                    replaceWith.Invoke(cil.Create(instruction.OpCode, targetField));
                }
                else if (targetPropertyInfo != null)
                {
                    if (instruction.OpCode == OpCodes.Ldfld || instruction.OpCode == OpCodes.Ldsfld)
                    {
                        replaceWith.Invoke(cil.Create(OpCodes.Call, module.ImportReference(targetPropertyInfo.GetGetMethod())));
                    }
                    else
                    {
                        replaceWith.Invoke(cil.Create(OpCodes.Call, module.ImportReference(targetPropertyInfo.GetSetMethod())));
                    }
                }
                else
                {
                    return false;
                }
            }
            return this.MarkRewritten();
        }
    }
}
