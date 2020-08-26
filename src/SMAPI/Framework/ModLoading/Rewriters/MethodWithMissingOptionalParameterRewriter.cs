using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters
{
    /// <summary>Rewrites references to methods which only broke because the definition has new optional parameters.</summary>
    internal class MethodWithMissingOptionalParameterRewriter : BaseInstructionHandler
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
        public MethodWithMissingOptionalParameterRewriter(string[] rewriteReferencesToAssemblies)
            : base(defaultPhrase: "methods with missing parameters") // ignored since we specify phrases
        {
            this.RewriteReferencesToAssemblies = new HashSet<string>(rewriteReferencesToAssemblies);
        }

        /// <inheritdoc />
        public override bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction, Action<Instruction> replaceWith)
        {
            // get method ref
            MethodReference methodRef = RewriteHelper.AsMethodReference(instruction);
            if (methodRef == null || !this.ShouldValidate(methodRef.DeclaringType))
                return false;

            // skip if not broken
            if (methodRef.Resolve() != null)
                return false;

            // get type
            var type = methodRef.DeclaringType.Resolve();
            if (type == null)
                return false;

            // get method definition
            MethodDefinition method = null;
            foreach (var match in type.Methods.Where(p => p.Name == methodRef.Name))
            {
                // reference matches initial parameters of definition
                if (methodRef.Parameters.Count >= match.Parameters.Count || !this.InitialParametersMatch(methodRef, match))
                    continue;

                // all remaining parameters in definition are optional
                if (!match.Parameters.Skip(methodRef.Parameters.Count).All(p => p.IsOptional))
                    continue;

                method = match;
                break;
            }
            if (method == null)
                return false;

            // get instructions to inject parameter values
            var loadInstructions = method.Parameters.Skip(methodRef.Parameters.Count)
                .Select(p => this.GetLoadValueInstruction(p.Constant))
                .ToArray();
            if (loadInstructions.Any(p => p == null))
                return false; // SMAPI needs to load the value onto the stack before the method call, but the optional parameter type wasn't recognized

            // rewrite method reference
            foreach (Instruction loadInstruction in loadInstructions)
                cil.InsertBefore(instruction, loadInstruction);
            instruction.Operand = module.ImportReference(method);

            this.Phrases.Add($"{methodRef.DeclaringType.Name}.{methodRef.Name} (added missing optional parameters)");
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

        /// <summary>Get whether every parameter in the method reference matches the exact order and type of the parameters in the method definition. This ignores extra parameters in the definition.</summary>
        /// <param name="methodRef">The method reference whose parameters to check.</param>
        /// <param name="method">The method definition whose parameters to check against.</param>
        private bool InitialParametersMatch(MethodReference methodRef, MethodDefinition method)
        {
            if (methodRef.Parameters.Count > method.Parameters.Count)
                return false;

            for (int i = 0; i < methodRef.Parameters.Count; i++)
            {
                if (!RewriteHelper.IsSameType(methodRef.Parameters[i].ParameterType, method.Parameters[i].ParameterType))
                    return false;
            }

            return true;
        }

        /// <summary>Get the CIL instruction to load a value onto the stack.</summary>
        /// <param name="rawValue">The constant value to inject.</param>
        /// <returns>Returns the instruction, or <c>null</c> if the value type isn't supported.</returns>
        private Instruction GetLoadValueInstruction(object rawValue)
        {
            return rawValue switch
            {
                null => Instruction.Create(OpCodes.Ldnull),
                bool value => Instruction.Create(value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0),
                int value => Instruction.Create(OpCodes.Ldc_I4, value), // int32
                long value => Instruction.Create(OpCodes.Ldc_I8, value), // int64
                float value => Instruction.Create(OpCodes.Ldc_R4, value), // float32
                double value => Instruction.Create(OpCodes.Ldc_R8, value), // float64
                string value => Instruction.Create(OpCodes.Ldstr, value),
                _ => null
            };
        }
    }
}
