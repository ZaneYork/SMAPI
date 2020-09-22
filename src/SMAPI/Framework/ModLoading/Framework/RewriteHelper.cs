using System;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace StardewModdingAPI.Framework.ModLoading.Framework
{
    /// <summary>Provides helper methods for field rewriters.</summary>
    internal static class RewriteHelper
    {
        /*********
        ** Fields
        *********/
        /// <summary>The comparer which heuristically compares type definitions.</summary>
        private static readonly TypeReferenceComparer TypeDefinitionComparer = new TypeReferenceComparer();


        /*********
        ** Public methods
        *********/
        /// <summary>Get the field reference from an instruction if it matches.</summary>
        /// <param name="instruction">The IL instruction.</param>
        public static FieldReference AsFieldReference(Instruction instruction)
        {
            return instruction.OpCode == OpCodes.Ldfld || instruction.OpCode == OpCodes.Ldsfld || instruction.OpCode == OpCodes.Stfld || instruction.OpCode == OpCodes.Stsfld || (instruction.OpCode == OpCodes.Ldtoken && instruction.Operand is FieldReference)
                ? (FieldReference)instruction.Operand
                : null;
        }

        /// <summary>Get whether the field is a reference to the expected type and field.</summary>
        /// <param name="instruction">The IL instruction.</param>
        /// <param name="fullTypeName">The full type name containing the expected field.</param>
        /// <param name="fieldName">The name of the expected field.</param>
        public static bool IsFieldReferenceTo(Instruction instruction, string fullTypeName, string fieldName)
        {
            FieldReference fieldRef = RewriteHelper.AsFieldReference(instruction);
            return RewriteHelper.IsFieldReferenceTo(fieldRef, fullTypeName, fieldName);
        }

        /// <summary>Get whether the field is a reference to the expected type and field.</summary>
        /// <param name="fieldRef">The field reference to check.</param>
        /// <param name="fullTypeName">The full type name containing the expected field.</param>
        /// <param name="fieldName">The name of the expected field.</param>
        public static bool IsFieldReferenceTo(FieldReference fieldRef, string fullTypeName, string fieldName)
        {
            return
                fieldRef != null
                && fieldRef.DeclaringType.FullName == fullTypeName
                && fieldRef.Name == fieldName;
        }

        /// <summary>Get the type reference from an instruction if it matches.</summary>
        /// <param name="instruction">The IL instruction.</param>
        public static TypeReference AsTypeReference(Instruction instruction)
        {
            return instruction.OpCode == OpCodes.Isinst || (instruction.OpCode == OpCodes.Ldtoken && instruction.Operand is TypeReference) ? (TypeReference)instruction.Operand : null;
        }

        /// <summary>Get the method reference from an instruction if it matches.</summary>
        /// <param name="instruction">The IL instruction.</param>
        public static MethodReference AsMethodReference(Instruction instruction)
        {
            return instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt || instruction.OpCode == OpCodes.Newobj || (instruction.OpCode == OpCodes.Ldtoken && instruction.Operand is MethodReference)
                ? (MethodReference)instruction.Operand
                : null;
        }

        /// <summary>Get the CIL instruction to load a value onto the stack.</summary>
        /// <param name="rawValue">The constant value to inject.</param>
        /// <returns>Returns the instruction, or <c>null</c> if the value type isn't supported.</returns>
        public static Instruction GetLoadValueInstruction(object rawValue)
        {
            return rawValue switch
            {
                null => Instruction.Create(OpCodes.Ldnull),
                bool value => Instruction.Create(value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0),
                int value => value switch // int32
                {
                    -1 => Instruction.Create(OpCodes.Ldc_I4_M1),
                    0 => Instruction.Create(OpCodes.Ldc_I4_0),
                    1 => Instruction.Create(OpCodes.Ldc_I4_1),
                    2 => Instruction.Create(OpCodes.Ldc_I4_2),
                    3 => Instruction.Create(OpCodes.Ldc_I4_3),
                    4 => Instruction.Create(OpCodes.Ldc_I4_4),
                    5 => Instruction.Create(OpCodes.Ldc_I4_5),
                    6 => Instruction.Create(OpCodes.Ldc_I4_6),
                    7 => Instruction.Create(OpCodes.Ldc_I4_7),
                    8 => Instruction.Create(OpCodes.Ldc_I4_8),
                    _ => Instruction.Create(OpCodes.Ldc_I4, value)
                },
                long value => Instruction.Create(OpCodes.Ldc_I8, value), // int64
                float value => Instruction.Create(OpCodes.Ldc_R4, value), // float32
                double value => Instruction.Create(OpCodes.Ldc_R8, value), // float64
                string value => Instruction.Create(OpCodes.Ldstr, value),
                _ => null
            };
        }

        /// <summary>Get the long equivalent for a short-jump op code.</summary>
        /// <param name="shortJumpCode">The short-jump op code.</param>
        /// <returns>Returns the new op code, or <c>null</c> if it isn't a short jump.</returns>
        public static OpCode? GetEquivalentLongJumpCode(OpCode shortJumpCode)
        {
            return shortJumpCode.Code switch
            {
                Code.Beq_S => OpCodes.Beq,
                Code.Bge_S => OpCodes.Bge,
                Code.Bge_Un_S => OpCodes.Bge_Un,
                Code.Bgt_S => OpCodes.Bgt,
                Code.Bgt_Un_S => OpCodes.Bgt_Un,
                Code.Ble_S => OpCodes.Ble,
                Code.Ble_Un_S => OpCodes.Ble_Un,
                Code.Blt_S => OpCodes.Blt,
                Code.Blt_Un_S => OpCodes.Blt_Un,
                Code.Bne_Un_S => OpCodes.Bne_Un,
                Code.Br_S => OpCodes.Br,
                Code.Brfalse_S => OpCodes.Brfalse,
                Code.Brtrue_S => OpCodes.Brtrue,
                _ => (OpCode?)null
            };
        }

        /// <summary>Get whether a type matches a type reference.</summary>
        /// <param name="type">The defined type.</param>
        /// <param name="reference">The type reference.</param>
        public static bool IsSameType(Type type, TypeReference reference)
        {
            //
            // duplicated by IsSameType(TypeReference, TypeReference) below
            //

            // same namespace & name
            if ((type.Namespace != reference.Namespace || type.Name != reference.Name) && !(reference.Namespace == "" && reference.FullName.Replace("/", "+") == type.FullName))
                return false;

            // same generic parameters
            if (type.IsGenericType)
            {
                if (!reference.IsGenericInstance)
                    return false;

                Type[] defGenerics = type.GetGenericArguments();
                TypeReference[] refGenerics = ((GenericInstanceType)reference).GenericArguments.ToArray();
                if (defGenerics.Length != refGenerics.Length)
                    return false;
                for (int i = 0; i < defGenerics.Length; i++)
                {
                    if (!RewriteHelper.IsSameType(defGenerics[i], refGenerics[i]))
                        return false;
                }
            }

            return true;
        }

        /// <summary>Get whether a type matches a type reference.</summary>
        /// <param name="type">The defined type.</param>
        /// <param name="reference">The type reference.</param>
        public static bool IsSameType(TypeReference type, TypeReference reference)
        {
            //
            // duplicated by IsSameType(Type, TypeReference) above
            //

            // same namespace & name
            if (type.Namespace != reference.Namespace || type.Name != reference.Name)
                return false;

            // same generic parameters
            if (type.IsGenericInstance)
            {
                if (!reference.IsGenericInstance)
                    return false;

                TypeReference[] defGenerics = ((GenericInstanceType)type).GenericArguments.ToArray();
                TypeReference[] refGenerics = ((GenericInstanceType)reference).GenericArguments.ToArray();
                if (defGenerics.Length != refGenerics.Length)
                    return false;
                for (int i = 0; i < defGenerics.Length; i++)
                {
                    if (!RewriteHelper.IsSameType(defGenerics[i], refGenerics[i]))
                        return false;
                }
            }

            return true;
        }

        /// <summary>Determine whether two type IDs look like the same type, accounting for placeholder values such as !0.</summary>
        /// <param name="typeA">The type ID to compare.</param>
        /// <param name="typeB">The other type ID to compare.</param>
        /// <returns>true if the type IDs look like the same type, false if not.</returns>
        public static bool LooksLikeSameType(TypeReference typeA, TypeReference typeB)
        {
            return RewriteHelper.TypeDefinitionComparer.Equals(typeA, typeB);
        }

        /// <summary>Get whether a method definition matches the signature expected by a method reference.</summary>
        /// <param name="definition">The method definition.</param>
        /// <param name="reference">The method reference.</param>
        public static bool HasMatchingSignature(MethodBase definition, MethodReference reference)
        {
            //
            // duplicated by HasMatchingSignature(MethodDefinition, MethodReference) below
            //

            // same name
            if (definition.Name != reference.Name)
                return false;

            // same return type
            if(definition is MethodInfo methodDefinition)
            {
                if(!RewriteHelper.IsSameType(methodDefinition.ReturnType, reference.ReturnType))
                    return false;
            }
            // same arguments
            ParameterInfo[] definitionParameters = definition.GetParameters();
            ParameterDefinition[] referenceParameters = reference.Parameters.ToArray();
            if (referenceParameters.Length != definitionParameters.Length)
                return false;
            for (int i = 0; i < referenceParameters.Length; i++)
            {
                if (!RewriteHelper.IsSameType(definitionParameters[i].ParameterType, referenceParameters[i].ParameterType))
                    return false;
            }
            return true;
        }

        /// <summary>Get whether a method definition matches the signature expected by a method reference.</summary>
        /// <param name="definition">The method definition.</param>
        /// <param name="reference">The method reference.</param>
        public static bool HasMatchingSignature(MethodDefinition definition, MethodReference reference)
        {
            //
            // duplicated by HasMatchingSignature(MethodBase, MethodReference) above
            //

            // same name
            if (definition.Name != reference.Name)
                return false;

            // same arguments
            ParameterDefinition[] definitionParameters = definition.Parameters.ToArray();
            ParameterDefinition[] referenceParameters = reference.Parameters.ToArray();
            if (referenceParameters.Length != definitionParameters.Length)
                return false;
            for (int i = 0; i < referenceParameters.Length; i++)
            {
                if (!RewriteHelper.IsSameType(definitionParameters[i].ParameterType, referenceParameters[i].ParameterType))
                    return false;
            }
            return true;
        }

        /// <summary>Get whether a type has a method whose signature matches the one expected by a method reference.</summary>
        /// <param name="type">The type to check.</param>
        /// <param name="reference">The method reference.</param>
        public static bool HasMatchingSignature(Type type, MethodReference reference)
        {
            if (reference.Name == ".ctor")
            {
                return type
                    .GetConstructors(BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public)
                    .Any(method => RewriteHelper.HasMatchingSignature(method, reference));
            }

            return type
               .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public)
               .Any(method => RewriteHelper.HasMatchingSignature(method, reference))
               ||
               type
               .GetConstructors(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
               .Any(method => RewriteHelper.HasMatchingSignature(method, reference));
        }

        /// <summary>
        /// get load instruction for specified type
        /// </summary>
        /// <param name="typename">target type's fullname</param>
        /// <returns>load instruction</returns>
        public static Instruction GetLoadValueInstruction(string typename)
        {
            return typename switch
            {
                "System.Void" => Instruction.Create(OpCodes.Nop),
                "System.Boolean" => Instruction.Create(OpCodes.Ldc_I4_0),
                "System.Byte" => Instruction.Create(OpCodes.Ldc_I4_0),
                "System.SByte" => Instruction.Create(OpCodes.Ldc_I4_0),
                "System.Char" => Instruction.Create(OpCodes.Ldc_I4_0),
                "System.Single" => Instruction.Create(OpCodes.Ldc_R4, (float)0),
                "System.Double" => Instruction.Create(OpCodes.Ldc_R8, (double)0),
                "System.Int32" => Instruction.Create(OpCodes.Ldc_I4_0),
                "System.UInt32" => Instruction.Create(OpCodes.Ldc_I4_0),
                "System.Int64" => Instruction.Create(OpCodes.Ldc_I8, (long)0),
                "System.UInt64" => Instruction.Create(OpCodes.Ldc_I8, (long)0),
                "System.Int16" => Instruction.Create(OpCodes.Ldc_I4_0),
                "System.UInt16" => Instruction.Create(OpCodes.Ldc_I4_0),
                _ => Instruction.Create(OpCodes.Ldnull),
            };
        }

        /// <summary>
        /// Append array of instruction to the target instruction
        /// </summary>
        /// <param name="il">processor</param>
        /// <param name="target">target instruction</param>
        /// <param name="instructions">array of instruction</param>
        public static void Append(this ILProcessor il, Instruction target, Instruction[] instructions)
        {
            for (int i = instructions.Length - 1; i >= 0; i--) il.InsertAfter(target, instructions[i]);
        }
    }
}
