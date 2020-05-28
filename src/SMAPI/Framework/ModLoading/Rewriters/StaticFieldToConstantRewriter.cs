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

        /// <summary>Rewrite a CIL instruction reference if needed.</summary>
        /// <param name="module">The assembly module containing the instruction.</param>
        /// <param name="cil">The CIL processor.</param>
        /// <param name="instruction">The CIL instruction to handle.</param>
        /// <param name="replaceWith">Replaces the CIL instruction with a new one.</param>
        /// <returns>Returns whether the instruction was changed.</returns>
        public override bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction, Action<Instruction> replaceWith)
        {
            // get field reference
            FieldReference fieldRef = RewriteHelper.AsFieldReference(instruction);
            if (!RewriteHelper.IsFieldReferenceTo(fieldRef, this.Type.FullName, this.FromFieldName))
                return false;

            // rewrite to constant
            replaceWith(this.CreateConstantInstruction(cil, this.Value));
            return this.MarkRewritten();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Create a CIL constant value instruction.</summary>
        /// <param name="cil">The CIL processor.</param>
        /// <param name="value">The constant value to set.</param>
        private Instruction CreateConstantInstruction(ILProcessor cil, object value)
        {
            if (typeof(TValue) == typeof(int))
                return cil.Create(OpCodes.Ldc_I4, (int)value);
            if (typeof(TValue) == typeof(string))
                return cil.Create(OpCodes.Ldstr, (string)value);
            throw new NotSupportedException($"Rewriting to constant values of type {typeof(TValue)} isn't currently supported.");
        }
    }
}
