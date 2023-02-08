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

        private readonly Version Version;

        private readonly Dictionary<string, AssemblyNameReference> TargetMap = new();


        public ModuleReferenceRewriter(string phrase, string assemblyName, Version version, Assembly[] assemblies)
        {
            this.DefaultPhrase = $"{phrase} assembly ref";
            this.AssemblyName = assemblyName;
            this.Version = version;
            foreach (var assembly in assemblies)
            {
                AssemblyNameReference target = AssemblyNameReference.Parse(assembly.FullName);
                var map = assembly.GetTypes().ToDictionary(p => p.FullName, p => target);
                foreach (KeyValuePair<string,AssemblyNameReference> pair in map)
                {
                    this.TargetMap.TryAdd(pair.Key, pair.Value);
                }
            }
        }

        private bool IsMatch(AssemblyNameReference reference)
        {
            if (this.AssemblyName.EndsWith('.'))
            {
                if (reference.Name.Equals(this.AssemblyName) || reference.Name.StartsWith(this.AssemblyName))
                    return reference.Version.CompareTo(this.Version) >= 0;
            }
            else
            {
                if (reference.Name.Equals(this.AssemblyName))
                    return reference.Version.CompareTo(this.Version) >= 0;
            }

            return false;
        }

        private bool IsMatch(TypeReference reference)
        {
            if (this.AssemblyName.EndsWith('.')) {
                if(reference.Scope.Name.Equals(this.AssemblyName) || reference.Scope.Name.StartsWith(this.AssemblyName))
                {
                    return this.TargetMap.ContainsKey(reference.FullName.Split('/')[0]);
                }
            }
            return reference.Scope.Name.Equals(this.AssemblyName) && this.TargetMap.ContainsKey(reference.FullName.Split('/')[0]);
        }

        public bool Handle(ModuleDefinition module)
        {
            if (!module.AssemblyReferences.Any(this.IsMatch))
            {
                return false;
            }

            // rewrite type scopes to use target assemblies
            IEnumerable<TypeReference> typeReferences = module.GetTypeReferences()
                .Where(this.IsMatch)
                .OrderBy(p => p.FullName);
            HashSet<string> assembliesAdded = new();
            foreach (TypeReference type in typeReferences)
            {
                AssemblyNameReference target = this.TargetMap[type.FullName.Split('/')[0]];
                // add target assembly references
                if (!module.AssemblyReferences.Contains(target) && !assembliesAdded.Contains(target.FullName))
                {
                    module.AssemblyReferences.Add(target);
                    assembliesAdded.Add(target.FullName);
                }
                type.Scope = target;
            }

            // rewrite types using custom attributes
            foreach (TypeDefinition type in module.GetTypes())
            {
                foreach (CustomAttribute attr in type.CustomAttributes)
                {
                    foreach (CustomAttributeArgument conField in attr.ConstructorArguments)
                    {
                        if (conField.Value is TypeReference typeRef)
                            if (this.TargetMap.ContainsKey(typeRef.FullName))
                            {
                                AssemblyNameReference target = this.TargetMap[type.FullName.Split('/')[0]];
                                // add target assembly references
                                if (!module.AssemblyReferences.Contains(target) && !assembliesAdded.Contains(target.FullName))
                                {
                                    module.AssemblyReferences.Add(target);
                                    assembliesAdded.Add(target.FullName);
                                }
                                type.Scope = target;
                            }
                    }
                }
            }

            for (int i = module.AssemblyReferences.Count - 1; i >= 0; i--)
            {
                if(this.IsMatch(module.AssemblyReferences[i]))
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
