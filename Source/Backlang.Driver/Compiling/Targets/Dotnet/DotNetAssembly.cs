﻿using Furesoft.Core.CodeDom.Compiler.Core;
using Furesoft.Core.CodeDom.Compiler.Core.Names;
using Furesoft.Core.CodeDom.Compiler.Core.TypeSystem;
using Furesoft.Core.CodeDom.Compiler.Pipeline;
using Furesoft.Core.CodeDom.Compiler.TypeSystem;
using Mono.Cecil;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace Backlang.Driver.Compiling.Targets.Dotnet;

public class DotNetAssembly : ITargetAssembly
{
    private readonly IAssembly _assembly;
    private readonly AssemblyContentDescription _description;
    private AssemblyDefinition _assemblyDefinition;

    public DotNetAssembly(AssemblyContentDescription description)
    {
        _assembly = description.Assembly;

        var name = new AssemblyNameDefinition(_assembly.FullName.ToString(),
            new Version(1, 0));

        _assemblyDefinition = AssemblyDefinition.CreateAssembly(name, description.Assembly.Name.ToString(), ModuleKind.Dll);

        _description = description;

        SetTargetFramework();

        var console = typeof(Console).Assembly.GetName();
        _assemblyDefinition.MainModule.AssemblyReferences.Add(AssemblyNameReference.Parse(console.FullName));
        _assemblyDefinition.MainModule.AssemblyReferences.Add(AssemblyNameReference.Parse(typeof(ExtensionAttribute).Assembly.GetName().FullName));
    }

    public void WriteTo(Stream output)
    {
        foreach (DescribedType type in _assembly.Types)
        {
            var clrType = new TypeDefinition(type.FullName.Qualifier.ToString(),
                type.Name.ToString(), TypeAttributes.Class);

            if (type.IsPrivate)
            {
                clrType.Attributes |= TypeAttributes.NestedPrivate;
            }
            else
            {
                clrType.Attributes |= TypeAttributes.Public;
            }
            if (type.IsStatic)
            {
                clrType.Attributes |= TypeAttributes.Abstract;
                clrType.Attributes |= TypeAttributes.Sealed;
            }

            if (type.IsAbstract)
            {
                clrType.Attributes |= TypeAttributes.Abstract;
            }

            clrType.IsInterface = type.IsInterfaceType;

            if (type.BaseTypes.Any())
            {
                foreach (var t in type.BaseTypes)
                {
                    if (t.Name.ToString() == "ValueType")
                    {
                        clrType.BaseType = _assemblyDefinition.MainModule.ImportReference(typeof(ValueType));

                        clrType.ClassSize = 1;
                        clrType.PackingSize = 0;
                    }
                    else
                    {
                        clrType.BaseType = Resolve(t.FullName);
                    }
                }
            }
            else
            {
                clrType.BaseType = _assemblyDefinition.MainModule.ImportReference(typeof(object));
            }

            foreach (DescribedField field in type.Fields)
            {
                var fieldDefinition = new FieldDefinition(field.Name.ToString(), FieldAttributes.Public, Resolve(field.FieldType.FullName));

                var specialName = field.Attributes.GetAll().FirstOrDefault(_ => _.AttributeType.Name.ToString() == "SpecialNameAttribute");

                fieldDefinition.IsRuntimeSpecialName = specialName != null;
                fieldDefinition.IsSpecialName = specialName != null;
                fieldDefinition.IsStatic = field.IsStatic;
                fieldDefinition.IsInitOnly = !field.Owns(Attributes.Mutable);

                if (clrType.IsEnum || field.InitialValue != null)
                {
                    fieldDefinition.Constant = field.InitialValue;

                    if (field.Name.ToString() != "value__")
                    {
                        fieldDefinition.IsRuntimeSpecialName = false;
                        fieldDefinition.IsSpecialName = false;
                        fieldDefinition.IsLiteral = true;
                    }
                }

                clrType.Fields.Add(fieldDefinition);
            }

            foreach (DescribedBodyMethod m in type.Methods)
            {
                var returnType = m.ReturnParameter.Type;
                var clrMethod = GetMethodDefinition(m, returnType);

                if (m.IsOverride)
                {
                    clrMethod.IsHideBySig = true;
                    clrMethod.IsVirtual = true;
                }

                clrMethod.IsAbstract = m.IsAbstract;
                clrMethod.IsHideBySig = m.Owns(Attributes.Mutable);

                if (m.Body != null)
                {
                    clrMethod.HasThis = false;
                    MethodBodyCompiler.Compile(m, clrMethod, _assemblyDefinition);
                }

                var attributes = m.Attributes.GetAll();
                if (attributes.Any())
                {
                    foreach (var attr in attributes)
                    {
                        if (attr.AttributeType.Name.ToString() == AccessModifierAttribute.AttributeName)
                        {
                            continue;
                        }

                        if (attr.AttributeType.Name.ToString() == "ExtensionAttribute")
                        {
                            var attrCtor = _assemblyDefinition.MainModule.ImportReference(typeof(ExtensionAttribute).GetConstructors().First());
                            var ca = new CustomAttribute(attrCtor);
                            clrType.IsBeforeFieldInit = false;
                            clrMethod.IsHideBySig = true;

                            clrMethod.CustomAttributes.Add(ca);
                        }
                    }
                }

                clrType.Methods.Add(clrMethod);
            }

            _assemblyDefinition.MainModule.Types.Add(clrType);
        }

        _assemblyDefinition.Write(output);

        output.Close();
    }

    private static MethodAttributes GetMethodAttributes(IMember member)
    {
        MethodAttributes attr = 0;

        var mod = member.GetAccessModifier();

        if (mod.HasFlag(AccessModifier.Public))
        {
            attr |= MethodAttributes.Public;
        }
        else if (mod.HasFlag(AccessModifier.Protected))
        {
            attr |= MethodAttributes.Family;
        }
        else if (mod.HasFlag(AccessModifier.Private))
        {
            attr |= MethodAttributes.Private;
        }
        else
        {
            attr |= MethodAttributes.Assembly;
        }

        return attr;
    }

    private MethodDefinition GetMethodDefinition(DescribedBodyMethod m, IType returnType)
    {
        var clrMethod = new MethodDefinition(m.Name.ToString(),
                                GetMethodAttributes(m),
                               Resolve(returnType == null ? new SimpleName("Void").Qualify("System") : returnType.FullName));

        if (m == _description.EntryPoint)
        {
            _assemblyDefinition.EntryPoint = clrMethod;
        }

        foreach (var p in m.Parameters)
        {
            var param = new ParameterDefinition(p.Name.ToString(), ParameterAttributes.None, Resolve(p.Type.FullName));
            if (p.HasDefault)
            {
                param.Constant = p.DefaultValue;
                param.IsOptional = true;
            }
            clrMethod.Parameters.Add(param);
        }

        clrMethod.IsStatic = m.IsStatic;
        if (m.IsConstructor)
        {
            clrMethod.IsRuntimeSpecialName = true;
            clrMethod.IsSpecialName = true;
            clrMethod.Name = ".ctor";
            clrMethod.IsStatic = false;
        }
        return clrMethod;
    }

    private TypeReference Resolve(QualifiedName name)
    {
        var type = Type.GetType(name.ToString());

        if (type == null)
        {
            return new TypeReference(name.Qualifier.ToString(),
                name.FullyUnqualifiedName.ToString(), _assemblyDefinition.MainModule, null);
        }

        var resolvedType = _assemblyDefinition.MainModule.ImportReference(type);
        return resolvedType;
    }

    private void SetTargetFramework()
    {
        var tf = _assemblyDefinition.MainModule.ImportReference(typeof(TargetFrameworkAttribute).GetConstructors().First());

        var item = new CustomAttribute(tf);
        item.ConstructorArguments.Add(
            new CustomAttributeArgument(_assemblyDefinition.MainModule.ImportReference(typeof(string)), ".NETCoreApp,Version=v7.0"));

        _assemblyDefinition.CustomAttributes.Add(item);
    }
}