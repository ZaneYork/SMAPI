using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters
{
    /// <summary>Rewrites method references from one parent type to another if the signatures match.</summary>
    internal class BrokenShortJumpRewriter : BaseInstructionHandler
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="fromType">The type whose methods to remap.</param>
        /// <param name="toType">The type with methods to map to.</param>
        /// <param name="nounPhrase">A brief noun phrase indicating what the instruction finder matches (or <c>null</c> to generate one).</param>
        public BrokenShortJumpRewriter(string nounPhrase = null)
            : base(nounPhrase ?? $"method's short jump")
        {
        }

        /// <inheritdoc />
        public override bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction)
        {
            // get method ref
            bool rewritten = false;
            // dynamic inserted instruction
            if (instruction.Offset == 0 && instruction.Previous != null)
                return false;
            // rewrite does not insert instruction
            if (!(instruction.Previous != null && instruction.Previous.Offset == 0
                || instruction.Next != null && instruction.Next.Offset == 0))
                return false;
            foreach (var ins in cil.Body.Instructions)
            {
                // dynamic inserted instruction
                if(ins.Offset == 0)
                    continue;
                OpCode targetOp = this.RewriteTarget(ins);
                // not a jump instruction
                if (targetOp == OpCodes.Nop)
                    continue;

                Instruction insJumpTo = (Instruction) ins.Operand;
                // jump forward
                if (insJumpTo.Offset > ins.Offset)
                {
                    // jump across the rewrite point
                    if(instruction.Offset > ins.Offset && instruction.Offset < insJumpTo.Offset)
                    {
                        // rewrite
                        ins.OpCode = targetOp;
                        rewritten = true;
                    }
                }
                // jump backward
                else
                {
                    // jump across the rewrite point
                    if(instruction.Offset > insJumpTo.Offset && instruction.Offset < ins.Offset)
                    {
                        // rewrite
                        ins.OpCode = targetOp;
                        rewritten = true;
                    }
                }
            }
            if(rewritten)
                return this.MarkRewritten();
            return false;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get whether a CIL instruction matches.</summary>
        /// <param name="methodRef">The method reference.</param>
        private OpCode RewriteTarget(Instruction instruction)
        {
            return instruction.OpCode.Code switch
            {
                Code.Beq_S => OpCodes.Beq,
                Code.Bge_S => OpCodes.Bge,
                Code.Bgt_S => OpCodes.Bgt,
                Code.Ble_S => OpCodes.Ble,
                Code.Blt_S => OpCodes.Blt,
                Code.Br_S => OpCodes.Br,
                Code.Brfalse_S => OpCodes.Brfalse,
                Code.Brtrue_S => OpCodes.Brtrue,
                Code.Bge_Un_S => OpCodes.Bge_Un,
                Code.Bgt_Un_S => OpCodes.Bgt_Un,
                Code.Ble_Un_S => OpCodes.Ble_Un,
                Code.Blt_Un_S => OpCodes.Blt_Un,
                Code.Bne_Un_S => OpCodes.Bne_Un,
                _ => OpCodes.Nop
            };
        }
    }
}
