using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Finders;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters
{
    internal class PropertyToFieldRewriter : PropertyFinder
    {
        /*********
        ** Fields
        *********/
        /// <summary>The full type name for which to find references.</summary>
        private readonly Type Type;

        /// <summary>The property name for which to find references.</summary>
        private readonly string PropertyName;

        /// <summary>The field name for which to replace references.</summary>
        private readonly string FieldName;

        /*********
        ** Public methods
        *********/
        /// <summary>
        /// Construct an instance.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="propertyName"></param>
        /// <param name="fullFieldName"></param>
        public PropertyToFieldRewriter(Type type, string propertyName, string fieldName) : base(type.FullName, propertyName, InstructionHandleResult.None)
        {
            this.Type = type;
            this.PropertyName = propertyName;
            this.FieldName = fieldName;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="module"></param>
        /// <param name="cil"></param>
        /// <param name="instruction"></param>
        /// <param name="replaceWith">Replaces the CIL instruction with a new one.</param>
        /// <returns>Returns whether the instruction was changed.</returns>
        public override bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction, Action<Instruction> replaceWith)
        {
            if (!this.IsMatch(instruction))
                return false;
            MethodReference methodRef = RewriteHelper.AsMethodReference(instruction);
            TypeReference typeRef = module.ImportReference(this.Type);
            FieldReference fieldRef = module.ImportReference(new FieldReference(this.FieldName, methodRef.ReturnType, typeRef));
            if (methodRef.Name.StartsWith("get_")) {
                replaceWith.Invoke(cil.Create(methodRef.HasThis ? OpCodes.Ldfld : OpCodes.Ldsfld, fieldRef));
            }
            else
            {
                replaceWith.Invoke(cil.Create(methodRef.HasThis ? OpCodes.Stfld : OpCodes.Stsfld, fieldRef));
            }
            return this.MarkRewritten();
        }
    }
}
