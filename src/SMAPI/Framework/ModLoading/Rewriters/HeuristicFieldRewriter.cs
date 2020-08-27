using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters
{
    /// <summary>Automatically fix references to fields that have been replaced by a property.</summary>
    internal class HeuristicFieldRewriter : BaseInstructionHandler
    {
        /*********
        ** Fields
        *********/
        /// <summary>The assembly names to which to rewrite broken references.</summary>
        private readonly HashSet<string> RewriteReferencesToAssemblies;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="rewriteReferencesToAssemblies">The assembly names to which to rewrite broken references.</param>
        public HeuristicFieldRewriter(string[] rewriteReferencesToAssemblies)
            : base(defaultPhrase: "field changed to property") // ignored since we specify phrases
        {
            this.RewriteReferencesToAssemblies = new HashSet<string>(rewriteReferencesToAssemblies);
        }

        /// <inheritdoc />
        public override bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction)
        {
            // get field ref
            FieldReference fieldRef = RewriteHelper.AsFieldReference(instruction);
            if (fieldRef == null || !this.ShouldValidate(fieldRef.DeclaringType))
                return false;

            // skip if not broken
            if (fieldRef.Resolve() != null)
                return false;

            // get equivalent property
            PropertyDefinition property = fieldRef.DeclaringType.Resolve().Properties.FirstOrDefault(p => p.Name == fieldRef.Name);
            MethodDefinition method = instruction.OpCode == OpCodes.Ldsfld || instruction.OpCode == OpCodes.Ldfld
                ? property?.GetMethod
                : property?.SetMethod;
            if (method == null)
                return false;

            // rewrite field to property
            instruction.OpCode = OpCodes.Call;
            instruction.Operand = module.ImportReference(method);

            this.Phrases.Add($"{fieldRef.DeclaringType.Name}.{fieldRef.Name} (field => property)");
            return this.MarkRewritten();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Whether references to the given type should be validated.</summary>
        /// <param name="type">The type reference.</param>
        private bool ShouldValidate(TypeReference type)
        {
            return type != null && this.RewriteReferencesToAssemblies.Contains(type.Scope.Name);
        }
    }
}
