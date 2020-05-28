using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters
{
    /// <summary>Rewrites field references into property references.</summary>
    internal class FieldToPropertyRewriter : BaseInstructionHandler
    {
        /*********
        ** Fields
        *********/
        /// <summary>The type containing the field to which references should be rewritten.</summary>
        private readonly Type Type;

        /// <summary>The field name to which references should be rewritten.</summary>
        private readonly string FromFieldName;

        /// <summary>The new property name.</summary>
        private readonly string ToPropertyName;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="type">The type whose field to which references should be rewritten.</param>
        /// <param name="fieldName">The field name to rewrite.</param>
        /// <param name="propertyName">The property name (if different).</param>
        public FieldToPropertyRewriter(Type type, string fieldName, string propertyName)
            : base(defaultPhrase: $"{type.FullName}.{fieldName} field")
        {
            this.Type = type;
            this.FromFieldName = fieldName;
            this.ToPropertyName = propertyName;
        }

        /// <summary>Construct an instance.</summary>
        /// <param name="type">The type whose field to which references should be rewritten.</param>
        /// <param name="fieldName">The field name to rewrite.</param>
        public FieldToPropertyRewriter(Type type, string fieldName)
            : this(type, fieldName, fieldName) { }

        /// <summary>Rewrite a CIL instruction reference if needed.</summary>
        /// <param name="module">The assembly module containing the instruction.</param>
        /// <param name="cil">The CIL processor.</param>
        /// <param name="instruction">The CIL instruction to handle.</param>
        /// <param name="replaceWith">Replaces the CIL instruction with a new one.</param>
        /// <returns>Returns whether the instruction was changed.</returns>
        public override bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction, Action<Instruction> replaceWith)
        {
            // get field ref
            FieldReference fieldRef = RewriteHelper.AsFieldReference(instruction);
            if (!RewriteHelper.IsFieldReferenceTo(fieldRef, this.Type.FullName, this.FromFieldName))
                return false;

            // replace with property
            string methodPrefix = instruction.OpCode == OpCodes.Ldsfld || instruction.OpCode == OpCodes.Ldfld ? "get" : "set";
            MethodReference propertyRef = module.ImportReference(this.Type.GetMethod($"{methodPrefix}_{this.ToPropertyName}"));
            replaceWith(cil.Create(OpCodes.Call, propertyRef));
            return this.MarkRewritten();
        }
    }
}
