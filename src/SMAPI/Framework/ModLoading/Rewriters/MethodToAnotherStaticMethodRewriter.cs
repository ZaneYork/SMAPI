using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters
{
    /// <summary>Rewrites method references from one parent type to another if the signatures match.</summary>
    internal class MethodToAnotherStaticMethodRewriter : IInstructionHandler
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
        ** Accessors
        *********/
        /// <summary>A brief noun phrase indicating what the instruction finder matches.</summary>
        public string NounPhrase { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="fromType">The type whose methods to remap.</param>
        /// <param name="toType">The type with methods to map to.</param>
        /// <param name="onlyIfPlatformChanged">Whether to only rewrite references if loading the assembly on a different platform than it was compiled on.</param>
        public MethodToAnotherStaticMethodRewriter(Type fromType, Predicate<MethodReference> fromMethodSelector, Type toType, string toMethod)
        {
            this.FromType = fromType;
            this.FromMethodSelector = fromMethodSelector;
            this.ToType = toType;
            this.ToMethod = toMethod;
            this.NounPhrase = $"{fromType.Name} methods";
        }

        /// <summary>Perform the predefined logic for a method if applicable.</summary>
        /// <param name="module">The assembly module containing the instruction.</param>
        /// <param name="method">The method definition containing the instruction.</param>
        /// <param name="assemblyMap">Metadata for mapping assemblies to the current platform.</param>
        /// <param name="platformChanged">Whether the mod was compiled on a different platform.</param>
        public InstructionHandleResult Handle(ModuleDefinition module, MethodDefinition method, PlatformAssemblyMap assemblyMap, bool platformChanged)
        {
            return InstructionHandleResult.None;
        }

        /// <summary>Perform the predefined logic for an instruction if applicable.</summary>
        /// <param name="module">The assembly module containing the instruction.</param>
        /// <param name="cil">The CIL processor.</param>
        /// <param name="instruction">The instruction to handle.</param>
        /// <param name="assemblyMap">Metadata for mapping assemblies to the current platform.</param>
        /// <param name="platformChanged">Whether the mod was compiled on a different platform.</param>
        public InstructionHandleResult Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction, PlatformAssemblyMap assemblyMap, bool platformChanged)
        {
            if (!this.IsMatch(instruction, platformChanged))
                return InstructionHandleResult.None;

            instruction.Operand = module.ImportReference(this.ToType.GetMethod(this.ToMethod));
            return InstructionHandleResult.Rewritten;
        }


        /*********
        ** Protected methods
        *********/
        /// <summary>Get whether a CIL instruction matches.</summary>
        /// <param name="instruction">The IL instruction.</param>
        /// <param name="platformChanged">Whether the mod was compiled on a different platform.</param>
        protected bool IsMatch(Instruction instruction, bool platformChanged)
        {
            MethodReference methodRef = RewriteHelper.AsMethodReference(instruction);
            return
                methodRef != null
                && this.FromMethodSelector.Invoke(methodRef);
        }
    }
}
