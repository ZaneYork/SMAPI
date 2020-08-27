using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters
{
    /// <summary>Rewrites method references from one parent type to another if the signatures match.</summary>
    internal class MethodToAnotherStaticMethodRewriter : BaseInstructionHandler
    {
        /*********
        ** Fields
        *********/
        /// <summary>The type whose methods to remap.</summary>
        private readonly Type FromType;

        /// <summary>The type whose methods to remap.</summary>
        private readonly Predicate<MethodReference> FromMethodSelector;

        /// <summary>The type with methods to map to.</summary>
        private readonly Type ToType;

        /// <summary>The method to map to.</summary>
        private readonly string ToMethod;

        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="fromType">The type whose methods to remap.</param>
        /// <param name="toType">The type with methods to map to.</param>
        /// <param name="onlyIfPlatformChanged">Whether to only rewrite references if loading the assembly on a different platform than it was compiled on.</param>
        public MethodToAnotherStaticMethodRewriter(Type fromType, Predicate<MethodReference> fromMethodSelector, Type toType, string toMethod)
            : base( $"{fromType.Name} methods")
        {
            this.FromType = fromType;
            this.FromMethodSelector = fromMethodSelector;
            this.ToType = toType;
            this.ToMethod = toMethod;
        }

        /// <summary>Perform the predefined logic for an instruction if applicable.</summary>
        /// <param name="module">The assembly module containing the instruction.</param>
        /// <param name="cil">The CIL processor.</param>
        /// <param name="instruction">The instruction to handle.</param>
        /// <returns>Returns whether the type was changed.</returns>
        public override bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction)
        {
            if (!this.IsMatch(instruction))
                return false;

            instruction.Operand = module.ImportReference(this.ToType.GetMethod(this.ToMethod));
            return this.MarkRewritten();
        }


        /*********
        ** Protected methods
        *********/
        /// <summary>Get whether a CIL instruction matches.</summary>
        /// <param name="instruction">The IL instruction.</param>
        protected bool IsMatch(Instruction instruction)
        {
            MethodReference methodRef = RewriteHelper.AsMethodReference(instruction);
            return
                methodRef != null
                && methodRef.DeclaringType.FullName == this.FromType.FullName
                && this.FromMethodSelector.Invoke(methodRef);
        }
    }
}
