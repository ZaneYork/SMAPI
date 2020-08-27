using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Finders;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters
{
    internal class TypeFieldToAnotherTypePropertyRewriter : FieldFinder
    {
        /*********
        ** Fields
        *********/
        /// <summary>The type whose field to which references should be rewritten to.</summary>
        private readonly Type ToType;

        /// <summary>The property name.</summary>
        private readonly string PropertyName;

        /// <summary>The property name.</summary>
        private readonly string InstancePropertyName;

        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="type">The type whose field to which references should be rewritten.</param>
        /// <param name="fieldName">The field name to rewrite.</param>
        /// <param name="propertyName">The property name (if different).</param>
        public TypeFieldToAnotherTypePropertyRewriter(Type type, Type toType, string fieldName, string propertyName, string instancePropertyName = null)
            : base(type.FullName, fieldName, InstructionHandleResult.None)
        {
            this.ToType = toType;
            this.PropertyName = propertyName;
            this.InstancePropertyName = instancePropertyName;
        }

        /// <summary>Perform the predefined logic for an instruction if applicable.</summary>
        /// <param name="module">The assembly module containing the instruction.</param>
        /// <param name="cil">The CIL processor.</param>
        /// <param name="instruction">The instruction to handle.</param>
        /// <returns>Returns whether the instruction was changed.</returns>
        public override bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction)
        {
            if (!this.IsMatch(instruction))
                return false;

            if (instruction.OpCode == OpCodes.Ldfld || instruction.OpCode == OpCodes.Ldsfld)
            {
                if (this.InstancePropertyName == null)
                {
                    MethodReference methodRef = module.ImportReference(this.ToType.GetProperty(this.PropertyName).GetGetMethod());
                    instruction.OpCode = OpCodes.Call;
                    instruction.Operand = methodRef;
                }
                else
                {
                    MethodReference instanceMethodRef = module.ImportReference(this.ToType.GetProperty(this.InstancePropertyName).GetGetMethod());
                    MethodReference methodRef = module.ImportReference(this.ToType.GetProperty(this.PropertyName).GetGetMethod());
                    cil.InsertAfter(instruction, cil.Create(OpCodes.Call, methodRef));
                    instruction.OpCode = OpCodes.Call;
                    instruction.Operand = instanceMethodRef;
                }
            }
            else if (instruction.OpCode == OpCodes.Stfld || instruction.OpCode == OpCodes.Stsfld)
            {
                if (this.InstancePropertyName == null)
                {
                    MethodReference methodRef = module.ImportReference(this.ToType.GetProperty(this.PropertyName).GetSetMethod());
                    instruction.OpCode = OpCodes.Call;
                    instruction.Operand = methodRef;
                }
                else
                {
                    MethodReference instanceMethodRef = module.ImportReference(this.ToType.GetProperty(this.InstancePropertyName).GetGetMethod());
                    MethodReference methodRef = module.ImportReference(this.ToType.GetProperty(this.PropertyName).GetSetMethod());
                    cil.InsertAfter(instruction, cil.Create(OpCodes.Call, methodRef));
                    instruction.OpCode = OpCodes.Call;
                    instruction.Operand = instanceMethodRef;
                }
            }
            return this.MarkRewritten();
        }
    }
}
