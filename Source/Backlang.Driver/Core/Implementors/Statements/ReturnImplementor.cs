﻿using Backlang.Contracts;
using Backlang.Contracts.Scoping;
using Furesoft.Core.CodeDom.Compiler;
using Furesoft.Core.CodeDom.Compiler.Core;
using Furesoft.Core.CodeDom.Compiler.Core.Names;
using Furesoft.Core.CodeDom.Compiler.Flow;
using Loyc.Syntax;
using static Backlang.Driver.Compiling.Stages.CompilationStages.ImplementationStage;

namespace Backlang.Driver.Core.Implementors.Statements;

public class ReturnImplementor : IStatementImplementor
{
    public BasicBlockBuilder Implement(CompilerContext context, IMethod method, BasicBlockBuilder block,
        LNode node, QualifiedName? modulename, Scope scope)
    {
        if (node.ArgCount == 1)
        {
            var valueNode = node.Args[0];

            AppendExpression(block, valueNode,
                TypeDeducer.Deduce(valueNode, scope, context), context, scope);

            block.Flow = new ReturnFlow();
        }
        else
        {
            block.Flow = new ReturnFlow();
        }
        return block;
    }
}