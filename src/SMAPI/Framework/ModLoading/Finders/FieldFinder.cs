using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Finders
{
    /// <summary>Finds incompatible CIL instructions that reference a given field.</summary>
    internal class FieldFinder : BaseInstructionHandler
    {
        /*********
        ** Fields
        *********/
        /// <summary>The full type name for which to find references.</summary>
        private readonly string FullTypeName;

        /// <summary>The field name for which to find references.</summary>
        private readonly string FieldName;

        /// <summary>The result to return for matching instructions.</summary>
        private readonly InstructionHandleResult Result;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="fullTypeName">The full type name for which to find references.</param>
        /// <param name="fieldName">The field name for which to find references.</param>
        /// <param name="result">The result to return for matching instructions.</param>
        public FieldFinder(string fullTypeName, string fieldName, InstructionHandleResult result)
            : base(defaultPhrase: $"{fullTypeName}.{fieldName} field")
        {
            this.FullTypeName = fullTypeName;
            this.FieldName = fieldName;
            this.Result = result;
        }

        /// <summary>Rewrite a CIL instruction reference if needed.</summary>
        /// <param name="module">The assembly module containing the instruction.</param>
        /// <param name="cil">The CIL processor.</param>
        /// <param name="instruction">The CIL instruction to handle.</param>
        /// <param name="replaceWith">Replaces the CIL instruction with a new one.</param>
        /// <returns>Returns whether the instruction was changed.</returns>
        public override bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction, Action<Instruction> replaceWith)
        {
            if (!this.Flags.Contains(this.Result) && RewriteHelper.IsFieldReferenceTo(instruction, this.FullTypeName, this.FieldName))
                this.MarkFlag(this.Result);

            return false;
        }
    }
}
