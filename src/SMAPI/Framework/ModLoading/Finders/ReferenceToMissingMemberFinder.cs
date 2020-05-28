using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Finders
{
    /// <summary>Finds references to a field, property, or method which no longer exists.</summary>
    /// <remarks>This implementation is purely heuristic. It should never return a false positive, but won't detect all cases.</remarks>
    internal class ReferenceToMissingMemberFinder : BaseInstructionHandler
    {
        /*********
        ** Fields
        *********/
        /// <summary>The assembly names to which to heuristically detect broken references.</summary>
        private readonly HashSet<string> ValidateReferencesToAssemblies;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="validateReferencesToAssemblies">The assembly names to which to heuristically detect broken references.</param>
        public ReferenceToMissingMemberFinder(string[] validateReferencesToAssemblies)
            : base(defaultPhrase: "")
        {
            this.ValidateReferencesToAssemblies = new HashSet<string>(validateReferencesToAssemblies);
        }

        /// <summary>Rewrite a CIL instruction reference if needed.</summary>
        /// <param name="module">The assembly module containing the instruction.</param>
        /// <param name="cil">The CIL processor.</param>
        /// <param name="instruction">The CIL instruction to handle.</param>
        /// <param name="replaceWith">Replaces the CIL instruction with a new one.</param>
        /// <returns>Returns whether the instruction was changed.</returns>
        public override bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction, Action<Instruction> replaceWith)
        {
            // field reference
            FieldReference fieldRef = RewriteHelper.AsFieldReference(instruction);
            if (fieldRef != null && this.ShouldValidate(fieldRef.DeclaringType))
            {
                FieldDefinition target = fieldRef.DeclaringType.Resolve()?.Fields.FirstOrDefault(p => p.Name == fieldRef.Name);
                if (target == null)
                {
                    this.MarkFlag(InstructionHandleResult.NotCompatible, $"reference to {fieldRef.DeclaringType.FullName}.{fieldRef.Name} (no such field)");
                    return false;
                }
            }

            // method reference
            MethodReference methodRef = RewriteHelper.AsMethodReference(instruction);
            if (methodRef != null && this.ShouldValidate(methodRef.DeclaringType) && !this.IsUnsupported(methodRef))
            {
                MethodDefinition target = methodRef.Resolve();
                if (target == null)
                {
                    string phrase = null;
                    if (this.IsProperty(methodRef))
                        phrase = $"reference to {methodRef.DeclaringType.FullName}.{methodRef.Name.Substring(4)} (no such property)";
                    else if (methodRef.Name == ".ctor")
                        phrase = $"reference to {methodRef.DeclaringType.FullName}.{methodRef.Name} (no matching constructor)";
                    else
                        phrase = $"reference to {methodRef.DeclaringType.FullName}.{methodRef.Name} (no such method)";

                    this.MarkFlag(InstructionHandleResult.NotCompatible, phrase);
                    return false;
                }
            }

            return false;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Whether references to the given type should be validated.</summary>
        /// <param name="type">The type reference.</param>
        private bool ShouldValidate(TypeReference type)
        {
            return type != null && this.ValidateReferencesToAssemblies.Contains(type.Scope.Name);
        }

        /// <summary>Get whether a method reference is a special case that's not currently supported (e.g. array methods).</summary>
        /// <param name="method">The method reference.</param>
        private bool IsUnsupported(MethodReference method)
        {
            return
                method.DeclaringType.Name.Contains("["); // array methods
        }

        /// <summary>Get whether a method reference is a property getter or setter.</summary>
        /// <param name="method">The method reference.</param>
        private bool IsProperty(MethodReference method)
        {
            return method.Name.StartsWith("get_") || method.Name.StartsWith("set_");
        }
    }
}
