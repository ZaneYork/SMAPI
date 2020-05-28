using System;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters
{
    /// <summary>Rewrites references to one field with another.</summary>
    internal class FieldReplaceRewriter : BaseInstructionHandler
    {
        /*********
        ** Fields
        *********/
        /// <summary>The type containing the field to which references should be rewritten.</summary>
        private readonly Type Type;

        /// <summary>The field name to which references should be rewritten.</summary>
        private readonly string FromFieldName;

        /// <summary>The new field to reference.</summary>
        private readonly FieldInfo ToField;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="type">The type whose field to rewrite.</param>
        /// <param name="fromFieldName">The field name to rewrite.</param>
        /// <param name="toFieldName">The new field name to reference.</param>
        public FieldReplaceRewriter(Type type, string fromFieldName, string toFieldName)
            : base(defaultPhrase: $"{type.FullName}.{fromFieldName} field")
        {
            this.Type = type;
            this.FromFieldName = fromFieldName;
            this.ToField = type.GetField(toFieldName);
            if (this.ToField == null)
                throw new InvalidOperationException($"The {type.FullName} class doesn't have a {toFieldName} field.");
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

            // replace with new field
            FieldReference newRef = module.ImportReference(this.ToField);
            replaceWith(cil.Create(instruction.OpCode, newRef));
            return this.MarkRewritten();
        }
    }
}
