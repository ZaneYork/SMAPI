using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace StardewModdingAPI.Framework.ModLoading.Framework
{
    /// <summary>Handles recursively rewriting loaded assembly code.</summary>
    internal class RecursiveRewriter
    {
        /*********
        ** Delegates
        *********/
        /// <summary>Rewrite a type reference in the assembly code.</summary>
        /// <param name="type">The current type reference.</param>
        /// <param name="replaceWith">Replaces the type reference with the given type.</param>
        /// <returns>Returns whether the type was changed.</returns>
        public delegate bool RewriteTypeDelegate(TypeReference type, Action<TypeReference> replaceWith);

        /// <summary>Rewrite a CIL instruction in the assembly code.</summary>
        /// <param name="instruction">The current CIL instruction.</param>
        /// <param name="cil">The CIL instruction processor.</param>
        /// <param name="replaceWith">Replaces the CIL instruction with the given instruction.</param>
        /// <returns>Returns whether the instruction was changed.</returns>
        public delegate bool RewriteInstructionDelegate(Instruction instruction, ILProcessor cil, Action<Instruction> replaceWith);


        /*********
        ** Accessors
        *********/
        /// <summary>The module to rewrite.</summary>
        public ModuleDefinition Module { get; }

        /// <summary>Handle or rewrite a type reference if needed.</summary>
        public RewriteTypeDelegate RewriteTypeImpl { get; }

        /// <summary>Handle or rewrite a CIL instruction if needed.</summary>
        public RewriteInstructionDelegate RewriteInstructionImpl { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="module">The module to rewrite.</param>
        /// <param name="rewriteType">Handle or rewrite a type reference if needed.</param>
        /// <param name="rewriteInstruction">Handle or rewrite a CIL instruction if needed.</param>
        public RecursiveRewriter(ModuleDefinition module, RewriteTypeDelegate rewriteType, RewriteInstructionDelegate rewriteInstruction)
        {
            this.Module = module;
            this.RewriteTypeImpl = rewriteType;
            this.RewriteInstructionImpl = rewriteInstruction;
        }

        /// <summary>Rewrite the loaded module code.</summary>
        /// <returns>Returns whether the module was modified.</returns>
        public bool RewriteModule()
        {
            bool anyRewritten = false;

            foreach (TypeDefinition type in this.Module.GetTypes())
            {
                if (type.BaseType == null)
                    continue; // special type like <Module>

                anyRewritten |= this.RewriteCustomAttributes(type.CustomAttributes);
                anyRewritten |= this.RewriteGenericParameters(type.GenericParameters);

                foreach (InterfaceImplementation @interface in type.Interfaces)
                    anyRewritten |= this.RewriteTypeReference(@interface.InterfaceType, newType => @interface.InterfaceType = newType);

                if (type.BaseType.FullName != "System.Object")
                    anyRewritten |= this.RewriteTypeReference(type.BaseType, newType => type.BaseType = newType);

                foreach (MethodDefinition method in type.Methods)
                {
                    anyRewritten |= this.RewriteTypeReference(method.ReturnType, newType => method.ReturnType = newType);
                    anyRewritten |= this.RewriteGenericParameters(method.GenericParameters);
                    anyRewritten |= this.RewriteCustomAttributes(method.CustomAttributes);

                    foreach (ParameterDefinition parameter in method.Parameters)
                        anyRewritten |= this.RewriteTypeReference(parameter.ParameterType, newType => parameter.ParameterType = newType);

                    foreach (var methodOverride in method.Overrides)
                        anyRewritten |= this.RewriteMethodReference(methodOverride);

                    if (method.HasBody)
                    {
                        foreach (VariableDefinition variable in method.Body.Variables)
                            anyRewritten |= this.RewriteTypeReference(variable.VariableType, newType => variable.VariableType = newType);

                        // check CIL instructions
                        ILProcessor cil = method.Body.GetILProcessor();
                        Collection<Instruction> instructions = cil.Body.Instructions;
                        for (int i = 0; i < instructions.Count; i++)
                        {
                            var instruction = instructions[i];
                            if (instruction.OpCode.Code == Code.Nop)
                                continue;

                            anyRewritten |= this.RewriteInstruction(instruction, cil, newInstruction =>
                            {
                                anyRewritten = true;
                                cil.Replace(instruction, newInstruction);
                                instruction = newInstruction;
                            });
                        }
                    }
                }
            }

            return anyRewritten;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Rewrite a CIL instruction if needed.</summary>
        /// <param name="instruction">The current CIL instruction.</param>
        /// <param name="cil">The CIL instruction processor.</param>
        /// <param name="replaceWith">Replaces the CIL instruction with a new one.</param>
        private bool RewriteInstruction(Instruction instruction, ILProcessor cil, Action<Instruction> replaceWith)
        {
            bool rewritten = false;

            // field reference
            FieldReference fieldRef = RewriteHelper.AsFieldReference(instruction);
            if (fieldRef != null)
            {
                rewritten |= this.RewriteTypeReference(fieldRef.DeclaringType, newType => fieldRef.DeclaringType = newType);
                rewritten |= this.RewriteTypeReference(fieldRef.FieldType, newType => fieldRef.FieldType = newType);
            }

            // method reference
            MethodReference methodRef = RewriteHelper.AsMethodReference(instruction);
            if (methodRef != null)
                this.RewriteMethodReference(methodRef);

            // type reference
            if (instruction.Operand is TypeReference typeRef)
                rewritten |= this.RewriteTypeReference(typeRef, newType => replaceWith(cil.Create(instruction.OpCode, newType)));

            // instruction itself
            // (should be done after the above type rewrites to ensure valid types)
            rewritten |= this.RewriteInstructionImpl(instruction, cil, newInstruction =>
            {
                rewritten = true;
                cil.Replace(instruction, newInstruction);
                instruction = newInstruction;
            });

            return rewritten;
        }

        /// <summary>Rewrite a method reference if needed.</summary>
        /// <param name="methodRef">The current method reference.</param>
        private bool RewriteMethodReference(MethodReference methodRef)
        {
            bool rewritten = false;

            rewritten |= this.RewriteTypeReference(methodRef.DeclaringType, newType =>
            {
                // note: generic methods are wrapped into a MethodSpecification which doesn't allow changing the
                // declaring type directly. For our purposes we want to change all generic versions of a matched
                // method anyway, so we can use GetElementMethod to get the underlying method here.
                methodRef.GetElementMethod().DeclaringType = newType;
            });
            rewritten |= this.RewriteTypeReference(methodRef.ReturnType, newType => methodRef.ReturnType = newType);

            foreach (var parameter in methodRef.Parameters)
                rewritten |= this.RewriteTypeReference(parameter.ParameterType, newType => parameter.ParameterType = newType);

            if (methodRef is GenericInstanceMethod genericRef)
            {
                for (int i = 0; i < genericRef.GenericArguments.Count; i++)
                    rewritten |= this.RewriteTypeReference(genericRef.GenericArguments[i], newType => genericRef.GenericArguments[i] = newType);
            }

            return rewritten;
        }

        /// <summary>Rewrite a type reference if needed.</summary>
        /// <param name="type">The current type reference.</param>
        /// <param name="replaceWith">Replaces the type reference with a new one.</param>
        private bool RewriteTypeReference(TypeReference type, Action<TypeReference> replaceWith)
        {
            bool rewritten = false;

            // type
            rewritten |= this.RewriteTypeImpl(type, newType =>
            {
                type = newType;
                replaceWith(newType);
                rewritten = true;
            });

            // generic arguments
            if (type is GenericInstanceType genericType)
            {
                for (int i = 0; i < genericType.GenericArguments.Count; i++)
                    rewritten |= this.RewriteTypeReference(genericType.GenericArguments[i], typeRef => genericType.GenericArguments[i] = typeRef);
            }

            // generic parameters (e.g. constraints)
            rewritten |= this.RewriteGenericParameters(type.GenericParameters);

            return rewritten;
        }

        /// <summary>Rewrite custom attributes if needed.</summary>
        /// <param name="attributes">The current custom attributes.</param>
        private bool RewriteCustomAttributes(Collection<CustomAttribute> attributes)
        {
            bool rewritten = false;

            for (int attrIndex = 0; attrIndex < attributes.Count; attrIndex++)
            {
                CustomAttribute attribute = attributes[attrIndex];
                bool curChanged = false;

                // attribute type
                TypeReference newAttrType = null;
                rewritten |= this.RewriteTypeReference(attribute.AttributeType, newType =>
                {
                    newAttrType = newType;
                    curChanged = true;
                });

                // constructor arguments
                TypeReference[] argTypes = new TypeReference[attribute.ConstructorArguments.Count];
                for (int i = 0; i < argTypes.Length; i++)
                {
                    var arg = attribute.ConstructorArguments[i];

                    argTypes[i] = arg.Type;
                    rewritten |= this.RewriteTypeReference(arg.Type, newType =>
                    {
                        argTypes[i] = newType;
                        curChanged = true;
                    });
                }

                // swap attribute
                if (curChanged)
                {
                    // get constructor
                    MethodDefinition constructor = (newAttrType ?? attribute.AttributeType)
                        .Resolve()
                        .Methods
                        .Where(method => method.IsConstructor)
                        .FirstOrDefault(ctor => RewriteHelper.HasMatchingSignature(ctor, attribute.Constructor));
                    if (constructor == null)
                        throw new InvalidOperationException($"Can't rewrite attribute type '{attribute.AttributeType.FullName}' to '{newAttrType?.FullName}', no equivalent constructor found.");

                    // create new attribute
                    var newAttr = new CustomAttribute(this.Module.ImportReference(constructor));
                    for (int i = 0; i < argTypes.Length; i++)
                        newAttr.ConstructorArguments.Add(new CustomAttributeArgument(argTypes[i], attribute.ConstructorArguments[i].Value));
                    foreach (var prop in attribute.Properties)
                        newAttr.Properties.Add(new CustomAttributeNamedArgument(prop.Name, prop.Argument));
                    foreach (var field in attribute.Fields)
                        newAttr.Fields.Add(new CustomAttributeNamedArgument(field.Name, field.Argument));

                    // swap attribute
                    attributes[attrIndex] = newAttr;
                    rewritten = true;
                }
            }

            return rewritten;
        }

        /// <summary>Rewrites generic type parameters if needed.</summary>
        /// <param name="parameters">The current generic type parameters.</param>
        private bool RewriteGenericParameters(Collection<GenericParameter> parameters)
        {
            bool anyChanged = false;

            for (int i = 0; i < parameters.Count; i++)
            {
                TypeReference parameter = parameters[i];
                anyChanged |= this.RewriteTypeReference(parameter, newType => parameters[i] = new GenericParameter(parameter.Name, newType));
            }

            return anyChanged;
        }
    }
}
