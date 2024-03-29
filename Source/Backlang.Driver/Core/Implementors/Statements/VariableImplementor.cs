﻿using Backlang.Codeanalysis.Parsing.AST;
using Backlang.Contracts;
using Backlang.Contracts.Scoping;
using Backlang.Contracts.Scoping.Items;
using Backlang.Driver.Compiling.Stages;
using Backlang.Driver.Compiling.Stages.CompilationStages;
using Furesoft.Core.CodeDom.Compiler;
using Furesoft.Core.CodeDom.Compiler.Core;
using Furesoft.Core.CodeDom.Compiler.Core.Names;
using Loyc.Syntax;

namespace Backlang.Driver.Core.Implementors.Statements;

public class VariableImplementor : IStatementImplementor
{
    public BasicBlockBuilder Implement(CompilerContext context, IMethod method, BasicBlockBuilder block,
        LNode node, QualifiedName? modulename, Scope scope)
    {
        var decl = node.Args[1];

        var name = ConversionUtils.GetQualifiedName(node.Args[0]);

        var elementType = TypeInheritanceStage.ResolveTypeWithModule(node.Args[0], context, modulename.Value, name);

        var deducedType = TypeDeducer.Deduce(decl.Args[1], scope, context);

        if (elementType == null)
        {
            elementType = deducedType;
        }
        else
        {
            //ToDo: check for implicit cast
            context.AddError(node, $"Type mismatch {elementType} {deducedType}");
        }

        var varname = decl.Args[0].Name.Name;
        var isMutable = node.Attrs.Contains(LNode.Id(Symbols.Mutable));

        if (scope.Add(new VariableScopeItem
        {
            Name = varname,
            IsMutable = isMutable,
            Parameter = new Parameter(elementType, varname)
        }))
        {
            block.AppendParameter(new BlockParameter(elementType, varname));
        }
        else
        {
            context.AddError(decl.Args[0], $"{varname} already declared");
        }

        if (deducedType == null) return null;

        ImplementationStage.AppendExpression(block, decl.Args[1], elementType, context, scope);

        block.AppendInstruction(Instruction.CreateAlloca(elementType));

        return block;
    }
}