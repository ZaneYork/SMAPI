using System.Collections.Generic;
using System.Linq;
using Harmony;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Finders
{
    /// <summary>Finds references to a field, property, or method which no longer exists.</summary>
    /// <remarks>This implementation is purely heuristic. It should never return a false positive, but won't detect all cases.</remarks>
    internal class ReferenceToMissingMemberRewriter : BaseInstructionHandler
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
        public ReferenceToMissingMemberRewriter(string[] validateReferencesToAssemblies)
            : base(defaultPhrase: "")
        {
            this.ValidateReferencesToAssemblies = new HashSet<string>(validateReferencesToAssemblies);
        }

        /// <inheritdoc />
        public override bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction)
        {
            // field reference
            FieldReference fieldRef = RewriteHelper.AsFieldReference(instruction);
            if (fieldRef != null && this.ShouldValidate(fieldRef.DeclaringType))
            {
                FieldDefinition target = fieldRef.Resolve();
                if (target == null || target.HasConstant)
                {
                    Instruction[] instructions = ReferenceToMissingMemberRewriter.GetStackInstructionsByInstruction(instruction);
                    instruction.OpCode = OpCodes.Nop;
                    instruction.Operand = null;
                    cil.Append(instruction, instructions);
                    this.MarkFlag(InstructionHandleResult.Rewritten, $"reference to {fieldRef.DeclaringType.FullName}.{fieldRef.Name} (no such field)");
#if SMAPI_FOR_MOBILE
                    this.Phrases.Add($"{cil.Body.Method.FullName} => {cil.Body.Instructions.Select(ins => ins.ToString()).Join(null, ";")}");
#endif
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
                    string phrase;
                    if (this.IsProperty(methodRef))
                        phrase = $"reference to {methodRef.DeclaringType.FullName}.{methodRef.Name.Substring(4)} (no such property)";
                    else if (methodRef.Name == ".ctor")
                        phrase = $"reference to {methodRef.DeclaringType.FullName}.{methodRef.Name} (no matching constructor)";
                    else
                        phrase = $"reference to {methodRef.DeclaringType.FullName}.{methodRef.Name} (no such method)";

                    Instruction[] instructions = ReferenceToMissingMemberRewriter.GetStackInstructionsByInstruction(instruction);
                    instruction.OpCode = OpCodes.Nop;
                    instruction.Operand = null;
                    cil.Append(instruction, instructions);
                    this.MarkFlag(InstructionHandleResult.Rewritten, phrase);
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

        private static Instruction[] GetStackInstructionsByInstruction(Instruction instruction)
        {
            Instruction[] instructionsPop = ReferenceToMissingMemberRewriter.GetStackInstructionsByStackBehaviour(instruction.OpCode.StackBehaviourPop);
            Instruction[] instructionsPush;
            FieldReference fieldRef = RewriteHelper.AsFieldReference(instruction);
            if (fieldRef != null)
            {
                OpCode pushOp = RewriteHelper.GetLoadValueInstruction(fieldRef.FieldType.FullName).OpCode;
                instructionsPush = ReferenceToMissingMemberRewriter.GetStackInstructionsByStackBehaviour(instruction.OpCode.StackBehaviourPush, pushOp);
            }
            else
                instructionsPush = ReferenceToMissingMemberRewriter.GetStackInstructionsByStackBehaviour(instruction.OpCode.StackBehaviourPush);
            if (instructionsPop == null)
            {
                MethodReference methodRef = RewriteHelper.AsMethodReference(instruction);
                if (methodRef != null)
                {
                    int thisInsCount = methodRef.HasThis ? instruction.OpCode == OpCodes.Newobj ? 0 : 1: 0;
                    instructionsPop = ReferenceToMissingMemberRewriter.DuplicateInstruction(OpCodes.Pop, methodRef.Parameters.Count + thisInsCount);
                }
                else
                    instructionsPop = new Instruction[] { };
            }
            if (instructionsPush == null)
            {
                MethodReference methodRef = RewriteHelper.AsMethodReference(instruction);
                if (methodRef != null && methodRef.ReturnType.FullName != "System.Void")
                    instructionsPush = new[] {RewriteHelper.GetLoadValueInstruction(methodRef.ReturnType.FullName)};
                else
                    instructionsPush = new Instruction[] { };
            }

            return instructionsPop.Concat(instructionsPush.ToList()).ToArray();
        }

        private static Instruction[] DuplicateInstruction(OpCode opCode, int count)
        {
            Instruction[] instructions = new Instruction[count];
            for (int i = 0; i < instructions.Length; i++) instructions[i] = Instruction.Create(opCode);
            return instructions;
        }

        private static Instruction[] GetStackInstructionsByStackBehaviour(StackBehaviour stackBehaviour, OpCode? pushOp = null)
        {
            return stackBehaviour switch
            {
                StackBehaviour.Pop0 => new Instruction[] { },
                StackBehaviour.Pop1 => ReferenceToMissingMemberRewriter.DuplicateInstruction(OpCodes.Pop, 1),
                StackBehaviour.Pop1_pop1 => ReferenceToMissingMemberRewriter.DuplicateInstruction(OpCodes.Pop, 2),
                StackBehaviour.Popi => ReferenceToMissingMemberRewriter.DuplicateInstruction(OpCodes.Pop, 1),
                StackBehaviour.Popi_pop1 => ReferenceToMissingMemberRewriter.DuplicateInstruction(OpCodes.Pop, 3),
                StackBehaviour.Popi_popi => ReferenceToMissingMemberRewriter.DuplicateInstruction(OpCodes.Pop, 3),
                StackBehaviour.Popi_popi8 => ReferenceToMissingMemberRewriter.DuplicateInstruction(OpCodes.Pop, 3),
                StackBehaviour.Popi_popi_popi => ReferenceToMissingMemberRewriter.DuplicateInstruction(OpCodes.Pop, 3),
                StackBehaviour.Popi_popr4 => ReferenceToMissingMemberRewriter.DuplicateInstruction(OpCodes.Pop, 3),
                StackBehaviour.Popi_popr8 => ReferenceToMissingMemberRewriter.DuplicateInstruction(OpCodes.Pop, 3),
                StackBehaviour.Popref => ReferenceToMissingMemberRewriter.DuplicateInstruction(OpCodes.Pop, 1),
                StackBehaviour.Popref_pop1 => ReferenceToMissingMemberRewriter.DuplicateInstruction(OpCodes.Pop, 3),
                StackBehaviour.Popref_popi => ReferenceToMissingMemberRewriter.DuplicateInstruction(OpCodes.Pop, 3),
                StackBehaviour.Popref_popi_popi => ReferenceToMissingMemberRewriter.DuplicateInstruction(OpCodes.Pop, 3),
                StackBehaviour.Popref_popi_popi8 => ReferenceToMissingMemberRewriter.DuplicateInstruction(OpCodes.Pop, 3),
                StackBehaviour.Popref_popi_popr4 => ReferenceToMissingMemberRewriter.DuplicateInstruction(OpCodes.Pop, 3),
                StackBehaviour.Popref_popi_popr8 => ReferenceToMissingMemberRewriter.DuplicateInstruction(OpCodes.Pop, 3),
                StackBehaviour.Popref_popi_popref => ReferenceToMissingMemberRewriter.DuplicateInstruction(OpCodes.Pop, 3),
                StackBehaviour.Push0 => new Instruction[] { },
                StackBehaviour.Push1 => ReferenceToMissingMemberRewriter.DuplicateInstruction(pushOp ?? OpCodes.Ldnull, 1),
                StackBehaviour.Push1_push1 => ReferenceToMissingMemberRewriter.DuplicateInstruction(pushOp ?? OpCodes.Ldnull, 2),
                StackBehaviour.Pushi => new[] {Instruction.Create(OpCodes.Ldc_I4_M1)},
                StackBehaviour.Pushi8 => new[] {Instruction.Create(OpCodes.Ldc_I8, (long)-1)},
                StackBehaviour.Pushr4 => new[] {Instruction.Create(OpCodes.Ldc_R4, (float)-1)},
                StackBehaviour.Pushr8 => new[] {Instruction.Create(OpCodes.Ldc_R8, (double)-1)},
                StackBehaviour.Pushref => ReferenceToMissingMemberRewriter.DuplicateInstruction(OpCodes.Ldnull, 1),
                StackBehaviour.PopAll => null,
                StackBehaviour.Varpop => null,
                StackBehaviour.Varpush => null,
                _ => null
            };
        }

    }
}
