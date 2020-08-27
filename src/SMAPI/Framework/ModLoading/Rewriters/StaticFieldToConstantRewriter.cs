using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters
{
    /// <summary>Rewrites static field references into constant values.</summary>
    /// <typeparam name="TValue">The constant value type.</typeparam>
    internal class StaticFieldToConstantRewriter<TValue> : BaseInstructionHandler
    {
        /*********
        ** Fields
        *********/
        /// <summary>The type containing the field to which references should be rewritten.</summary>
        private readonly Type Type;

        /// <summary>The field name to which references should be rewritten.</summary>
        private readonly string FromFieldName;

        /// <summary>The constant value to replace with.</summary>
        private readonly TValue Value;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="type">The type whose field to which references should be rewritten.</param>
        /// <param name="fieldName">The field name to rewrite.</param>
        /// <param name="value">The constant value to replace with.</param>
        public StaticFieldToConstantRewriter(Type type, string fieldName, TValue value)
            : base(defaultPhrase: $"{type.FullName}.{fieldName} field")
        {
            this.Type = type;
            this.FromFieldName = fieldName;
            this.Value = value;
        }

        /// <inheritdoc />
        public override bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction)
        {
            // get field reference
            FieldReference fieldRef = RewriteHelper.AsFieldReference(instruction);
            if (!RewriteHelper.IsFieldReferenceTo(fieldRef, this.Type.FullName, this.FromFieldName))
                return false;

            // rewrite to constant
            if (typeof(TValue) == typeof(int))
            {
                instruction.OpCode = OpCodes.Ldc_I4;
                instruction.Operand = this.Value;
            }
            else if (typeof(TValue) == typeof(string))
            {
                instruction.OpCode = OpCodes.Ldstr;
                instruction.Operand = this.Value;
            }
            else
                throw new NotSupportedException($"Rewriting to constant values of type {typeof(TValue)} isn't currently supported.");

            return this.MarkRewritten();
        }
    }
}
