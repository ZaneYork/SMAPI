using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters
{
    /// <summary>Rewrites all references to a type.</summary>
    internal class ModuleReferenceRewriter : IInstructionHandler
    {
        /*********
        ** Accessors
        *********/
        /// <inheritdoc />
        public string DefaultPhrase { get; }

        /// <inheritdoc />
        public ISet<InstructionHandleResult> Flags { get; } = new HashSet<InstructionHandleResult>();

        /// <inheritdoc />
        public ISet<string> Phrases { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly string AssemblyName;

        private readonly AssemblyNameReference Target;


        public ModuleReferenceRewriter(string phrase, string assemblyName, Assembly target)
        {
            this.DefaultPhrase = $"{phrase} assembly ref";
            this.AssemblyName = assemblyName;
            this.Target = AssemblyNameReference.Parse(target.FullName);
        }

        public bool Handle(ModuleDefinition module)
        {
            if (!module.AssemblyReferences.Any(assembly => assembly.Name.Equals(this.AssemblyName)))
            {
                return false;
            }
            // add target assembly references
            module.AssemblyReferences.Add(this.Target);

            // rewrite type scopes to use target assemblies
            IEnumerable<TypeReference> typeReferences = module.GetTypeReferences()
                .Where(p => p.Scope.Name.Equals(this.AssemblyName))
                .OrderBy(p => p.FullName);
            foreach (TypeReference type in typeReferences)
                type.Scope = this.Target;

            // rewrite types using custom attributes
            foreach (TypeDefinition type in module.GetTypes())
            {
                foreach (CustomAttribute attr in type.CustomAttributes)
                {
                    foreach (CustomAttributeArgument conField in attr.ConstructorArguments)
                    {
                        if (conField.Value is TypeReference typeRef)
                            typeRef.Scope = this.Target;
                    }
                }
            }

            for (int i = module.AssemblyReferences.Count - 1; i >= 0; i--)
            {
                if(module.AssemblyReferences[i].Name.Equals(this.AssemblyName))
                {
                    module.AssemblyReferences.RemoveAt(i);
                }
            }

            this.Flags.Add(InstructionHandleResult.Rewritten);

            return true;
        }

        public bool Handle(ModuleDefinition module, TypeReference type, Action<TypeReference> replaceWith)
        {
            return false;
        }

        public bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction)
        {
            return false;
        }
    }
}
