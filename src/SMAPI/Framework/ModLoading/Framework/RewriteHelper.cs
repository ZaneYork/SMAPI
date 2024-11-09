using System;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace StardewModdingAPI.Framework.ModLoading.Framework;

/// <summary>Provides helper methods for field rewriters.</summary>
internal static class RewriteHelper
{
    /*********
    ** Fields
    *********/
    /// <summary>The comparer which heuristically compares type definitions.</summary>
    private static readonly TypeReferenceComparer TypeDefinitionComparer = new();


    /*********
    ** Public methods
    *********/
    /****
    ** CIL helpers
    ****/
    /// <summary>Get the field reference from an instruction if it matches.</summary>
    /// <param name="instruction">The IL instruction.</param>
    public static FieldReference? AsFieldReference(Instruction instruction)
    {
        return instruction.OpCode == OpCodes.Ldfld || instruction.OpCode == OpCodes.Ldsfld || instruction.OpCode == OpCodes.Stfld || instruction.OpCode == OpCodes.Stsfld
            ? (FieldReference)instruction.Operand
            : null;
    }

    /// <summary>Get the method reference from an instruction if it matches.</summary>
    /// <param name="instruction">The IL instruction.</param>
    public static MethodReference? AsMethodReference(Instruction instruction)
    {
        return instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt || instruction.OpCode == OpCodes.Newobj
            ? (MethodReference)instruction.Operand
            : null;
    }

    /// <summary>Get the CIL instruction to load a value onto the stack.</summary>
    /// <param name="rawValue">The constant value to inject.</param>
    /// <returns>Returns the instruction, or <c>null</c> if the value type isn't supported.</returns>
    public static Instruction? GetLoadValueInstruction(object? rawValue)
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
            _ => null
        };
    }

    /****
    ** Cecil helpers
    ****/
    /// <summary>Get the full name for a type in the Mono.Cecil format, like <c>Netcode.NetCollection`1&lt;StardewValley.Objects.Furniture&gt;</c>.</summary>
    /// <param name="type">The type info.</param>
    public static string GetFullCecilName(Type type)
    {
        if (type.IsGenericType)
        {
            Type[] genericTypes = type.GetGenericArguments();
            return $"{type.Namespace}.{type.Name}<{string.Join(",", genericTypes.Select(RewriteHelper.GetFullCecilName))}>";
        }

        Type? parentType = type.DeclaringType;
        return parentType != null
            ? $"{RewriteHelper.GetFullCecilName(parentType)}/{type.Name}"
            : $"{type.Namespace}.{type.Name}";
    }

    /// <summary>Get the resolved type for a Cecil type reference.</summary>
    /// <param name="type">The type reference.</param>
    public static Type? GetCSharpType(TypeReference type)
    {
        string typeName = RewriteHelper.GetReflectionName(type);
        return Type.GetType(typeName, false);
    }

    /// <summary>Get the .NET reflection full name for a Cecil type reference.</summary>
    /// <param name="type">The type reference.</param>
    public static string GetReflectionName(TypeReference type)
    {
        if (!type.IsGenericInstance)
            return $"{type.FullName},{type.Scope.Name}";

        var genericInstance = (GenericInstanceType)type;
        var genericArgs = genericInstance.GenericArguments.Select(row => "[" + RewriteHelper.GetReflectionName(row) + "]");
        return $"{genericInstance.Namespace}.{type.Name}[{string.Join(",", genericArgs)}],{type.Scope.Name}";
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
        if (type.Namespace != reference.Namespace || type.Name != reference.Name)
#if SMAPI_FOR_MOBILE
            if (!(reference.Namespace == "" && reference.FullName.Replace("/", "+") == type.FullName))
                return false;
#else
            return false;
#endif

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
    public static bool LooksLikeSameType(TypeReference? typeA, TypeReference? typeB)
    {
        return RewriteHelper.TypeDefinitionComparer.Equals(typeA, typeB);
    }

    /// <summary>Get whether a type reference and definition have the same namespace and name. This does <strong>not</strong> guarantee they point to the same type due to generics.</summary>
    /// <param name="typeReference">The type reference.</param>
    /// <param name="typeDefinition">The type definition.</param>
    /// <remarks>This avoids an issue where we can't compare <see cref="TypeReference.FullName"/> to <see cref="TypeReference.FullName"/> because of the different ways they handle generics (e.g. <c>List`1&lt;System.String&gt;</c> vs <c>List`1</c>).</remarks>
    public static bool HasSameNamespaceAndName(TypeReference? typeReference, TypeDefinition? typeDefinition)
    {
        return
            typeReference?.Namespace == typeDefinition?.Namespace
            && typeReference?.Name == typeDefinition?.Name;
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

#if SMAPI_FOR_MOBILE
        // same return type
        if(definition is MethodInfo methodDefinition)
        {
            if(!RewriteHelper.IsSameType(methodDefinition.ReturnType, reference.ReturnType))
                return false;
        }
#endif
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

#if SMAPI_FOR_MOBILE
        if (type.GetConstructors(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
               .Any(method => RewriteHelper.HasMatchingSignature(method, reference)))
            return true;

#endif
        return type
            .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public)
            .Any(method => RewriteHelper.HasMatchingSignature(method, reference));
    }

    /****
    ** Infrastructure helpers
    ****/
    /// <summary>Throw an exception indicating that a facade was constructed, which should never happen since they're only used through CIL rewrites.</summary>
    /// <exception cref="InvalidOperationException">An exception indicating the constructor was called incorrectly.</exception>
    public static void ThrowFakeConstructorCalled()
    {
        throw new InvalidOperationException("This constructor is only intended for the compiler and should never be called at runtime.");
    }
}
