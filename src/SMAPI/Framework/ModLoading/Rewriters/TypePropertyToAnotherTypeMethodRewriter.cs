using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Finders;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters
{
    internal class TypePropertyToAnotherTypeMethodRewriter : PropertyFinder
    {
        /*********
        ** Fields
        *********/
        /// <summary>The type whose field to which references should be rewritten to.</summary>
        private readonly Type ToType;

        /// <summary>The property name.</summary>
        private readonly string GetterName;
        /// <summary>The property name.</summary>
        private readonly string SetterName;
        /// <summary>The property name.</summary>
        private readonly string PropertyName;

        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="type">The type whose field to which references should be rewritten.</param>
        /// <param name="propertyName">The field name to rewrite.</param>
        /// <param name="targetPropertyName">The property name (if different).</param>
        public TypePropertyToAnotherTypeMethodRewriter(Type type, Type toType, string propertyName, string targetGetter = null, string targetSetter = null)
            : base(type.FullName, propertyName, InstructionHandleResult.None)
        {
            this.ToType = toType;
            this.PropertyName = propertyName;
            this.GetterName = targetGetter;
            this.SetterName = targetSetter;
        }

        /// <summary>Perform the predefined logic for an instruction if applicable.</summary>
        /// <param name="module">The assembly module containing the instruction.</param>
        /// <param name="cil">The CIL processor.</param>
        /// <param name="instruction">The instruction to handle.</param>
        /// <param name="replaceWith">Replaces the CIL instruction with a new one.</param>
        /// <returns>Returns whether the instruction was changed.</returns>
        public override bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction)
        {
            if (!this.IsMatch(instruction))
                return false;

            MethodReference methodRef = RewriteHelper.AsMethodReference(instruction);
            if (this.GetterName != null && methodRef.Name == "get_" + this.PropertyName)
            {
                methodRef = module.ImportReference(this.ToType.GetMethod(this.GetterName));
                instruction.OpCode = OpCodes.Callvirt;
                instruction.Operand = methodRef;
                return true;
            }
            if(this.SetterName != null && methodRef.Name == "set_" + this.PropertyName)
            {
                methodRef = module.ImportReference(this.ToType.GetMethod(this.SetterName));
                instruction.OpCode = OpCodes.Callvirt;
                instruction.Operand = methodRef;
                return true;
            }
            return this.MarkRewritten();
        }
    }
}
