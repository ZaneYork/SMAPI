using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Finders;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters
{
    internal class TypeFieldToAnotherTypeFieldRewriter : FieldFinder
    {
        /*********
        ** Fields
        *********/
        /// <summary>The type whose field to which references should be rewritten.</summary>
        private readonly Type Type;

        /// <summary>The type whose field to which references should be rewritten to.</summary>
        private readonly Type ToType;

        /// <summary>The field name.</summary>
        private readonly string FieldName;

        private readonly string TargetPropertyName;

        private readonly IMonitor Monitor;

        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="type">The type whose field to which references should be rewritten.</param>
        /// <param name="fieldName">The field name to rewrite.</param>
        /// <param name="propertyName">The property name (if different).</param>
        public TypeFieldToAnotherTypeFieldRewriter(Type type, Type toType, string fieldName, IMonitor monitor, string targetPropertyName = null)
            : base(type.FullName, fieldName, InstructionHandleResult.None)
        {
            this.Monitor = monitor;
            this.Type = type;
            this.ToType = toType;
            this.FieldName = fieldName;
            this.TargetPropertyName = targetPropertyName;
        }

        /// <summary>Perform the predefined logic for an instruction if applicable.</summary>
        /// <param name="module">The assembly module containing the instruction.</param>
        /// <param name="cil">The CIL processor.</param>
        /// <param name="instruction">The instruction to handle.</param>
        public override bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction)
        {
            if (!this.IsMatch(instruction))
                return false;

            try
            {
                MethodReference method = module.ImportReference(this.Type.GetMethod($"get_{this.FieldName}"));
                MethodReference property = module.ImportReference(this.ToType.GetProperty(this.TargetPropertyName).GetGetMethod());

                cil.InsertAfter(instruction, cil.Create(OpCodes.Callvirt, property));
                instruction.OpCode = OpCodes.Call;
                instruction.Operand = method;
            }
            catch (Exception e)
            {
                this.Monitor.Log(e.Message);
                this.Monitor.Log(e.StackTrace);
            }

            return this.MarkRewritten();
        }
    }
}
