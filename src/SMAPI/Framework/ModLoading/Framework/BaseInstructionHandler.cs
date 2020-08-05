using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace StardewModdingAPI.Framework.ModLoading.Framework
{
    /// <summary>The base implementation for a CIL instruction handler or rewriter.</summary>
    internal abstract class BaseInstructionHandler : IInstructionHandler
    {
        /*********
        ** Accessors
        *********/
        /// <summary>A brief noun phrase indicating what the handler matches, used if <see cref="Phrases"/> is empty.</summary>
        public string DefaultPhrase { get; }

        /// <summary>The rewrite flags raised for the current module.</summary>
        public ISet<InstructionHandleResult> Flags { get; } = new HashSet<InstructionHandleResult>();

        /// <summary>The brief noun phrases indicating what the handler matched for the current module.</summary>
        public ISet<string> Phrases { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


        /*********
        ** Public methods
        *********/
        /// <summary>Rewrite a type reference if needed.</summary>
        /// <param name="module">The assembly module containing the instruction.</param>
        /// <param name="type">The type definition to handle.</param>
        /// <param name="replaceWith">Replaces the type reference with a new one.</param>
        /// <returns>Returns whether the type was changed.</returns>
        public virtual bool Handle(ModuleDefinition module, TypeReference type, Action<TypeReference> replaceWith)
        {
            return false;
        }

        /// <summary>Rewrite a CIL instruction reference if needed.</summary>
        /// <param name="module">The assembly module containing the instruction.</param>
        /// <param name="cil">The CIL processor.</param>
        /// <param name="instruction">The CIL instruction to handle.</param>
        /// <param name="replaceWith">Replaces the CIL instruction with a new one.</param>
        /// <returns>Returns whether the instruction was changed.</returns>
        public virtual bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction, Action<Instruction> replaceWith)
        {
            return false;
        }


        /*********
        ** Protected methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="defaultPhrase">A brief noun phrase indicating what the handler matches.</param>
        protected BaseInstructionHandler(string defaultPhrase)
        {
            this.DefaultPhrase = defaultPhrase;
        }

        /// <summary>Raise a result flag.</summary>
        /// <param name="flag">The result flag to set.</param>
        /// <param name="resultMessage">The result message to add.</param>
        /// <returns>Returns true for convenience.</returns>
        protected bool MarkFlag(InstructionHandleResult flag, string resultMessage = null)
        {
            this.Flags.Add(flag);
            if (resultMessage != null)
                this.Phrases.Add(resultMessage);
            return true;
        }

        /// <summary>Raise a generic flag indicating that the code was rewritten.</summary>
        public bool MarkRewritten()
        {
            return this.MarkFlag(InstructionHandleResult.Rewritten);
        }
    }
}
