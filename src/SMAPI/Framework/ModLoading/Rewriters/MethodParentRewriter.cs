using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters
{
    /// <summary>Rewrites method references from one parent type to another if the signatures match.</summary>
    internal class MethodParentRewriter : BaseInstructionHandler
    {
        /*********
        ** Fields
        *********/
        /// <summary>The full name of the type whose methods to remap.</summary>
        private readonly string FromType;

        /// <summary>The type with methods to map to.</summary>
        private readonly Type ToType;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="fromType">The type whose methods to remap.</param>
        /// <param name="toType">The type with methods to map to.</param>
        /// <param name="nounPhrase">A brief noun phrase indicating what the instruction finder matches (or <c>null</c> to generate one).</param>
        public MethodParentRewriter(string fromType, Type toType, string nounPhrase = null)
            : base(nounPhrase ?? $"{fromType.Split('.').Last()} methods")
        {
            this.FromType = fromType;
            this.ToType = toType;
        }

        /// <summary>Construct an instance.</summary>
        /// <param name="fromType">The type whose methods to remap.</param>
        /// <param name="toType">The type with methods to map to.</param>
        /// <param name="nounPhrase">A brief noun phrase indicating what the instruction finder matches (or <c>null</c> to generate one).</param>
        public MethodParentRewriter(Type fromType, Type toType, string nounPhrase = null)
            : this(fromType.FullName, toType, nounPhrase) { }

        /// <summary>Rewrite a CIL instruction reference if needed.</summary>
        /// <param name="module">The assembly module containing the instruction.</param>
        /// <param name="cil">The CIL processor.</param>
        /// <param name="instruction">The CIL instruction to handle.</param>
        /// <param name="replaceWith">Replaces the CIL instruction with a new one.</param>
        /// <returns>Returns whether the instruction was changed.</returns>
        public override bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction, Action<Instruction> replaceWith)
        {
            // get method ref
            MethodReference methodRef = RewriteHelper.AsMethodReference(instruction);
            if (!this.IsMatch(methodRef))
                return false;

            // rewrite
            methodRef.DeclaringType = module.ImportReference(this.ToType);
            return this.MarkRewritten();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get whether a CIL instruction matches.</summary>
        /// <param name="methodRef">The method reference.</param>
        private bool IsMatch(MethodReference methodRef)
        {
            return
                methodRef != null
                && methodRef.DeclaringType.FullName == this.FromType
                && RewriteHelper.HasMatchingSignature(this.ToType, methodRef);
        }
    }
}
