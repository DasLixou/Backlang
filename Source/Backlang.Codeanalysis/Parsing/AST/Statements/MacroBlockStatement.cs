﻿using Loyc.Syntax;

namespace Backlang.Codeanalysis.Parsing.AST.Statements;

public sealed class MacroBlockStatement : IParsePoint<LNode>
{
    public static LNode Parse(TokenIterator iterator, Parser parser)
    {
        var nameExpression = LNode.Id(iterator.NextToken().Text);

        if (iterator.Current.Type == TokenType.OpenParen)
        {
            iterator.NextToken();

            var arguments = Expression.ParseList(parser, TokenType.CloseParen);

            if (iterator.Current.Type == TokenType.OpenCurly)
            {
                //custom code block with arguments
                var body = Statement.ParseBlock(parser);
                arguments = arguments.Add(LNode.Call(CodeSymbols.Braces, body));

                return LNode.Call(nameExpression, arguments).SetStyle(NodeStyle.StatementBlock).SetStyle(NodeStyle.Special);
            }

            return LNode.Call(nameExpression, arguments);
        }
        else if (iterator.Current.Type == TokenType.OpenCurly)
        {
            //custom code block without arguments
            var body = Statement.ParseBlock(parser);
            var arguments = LNode.List(LNode.Missing);
            arguments = arguments.Add(LNode.Call(CodeSymbols.Braces, body));

            return LNode.Call(nameExpression, arguments).SetStyle(NodeStyle.StatementBlock).SetStyle(NodeStyle.Special);
        }

        return LNode.Missing;
    }
}