﻿using Backlang.Codeanalysis.Parsing.AST.Statements;
using Loyc.Syntax;

namespace Backlang.Codeanalysis.Parsing.AST.Declarations;

public sealed class TypeMemberDeclaration
{
    public static LNode Parse(TokenIterator iterator, Parser parser)
    {
        Annotation.TryParse(parser, out var annotations);
        Modifier.TryParse(parser, out var modifiers);

        LNode declaration = LNode.Missing;

        if (iterator.IsMatch(TokenType.Function))
        {
            declaration = ParseFunction(iterator, parser);
        }
        else if (iterator.IsMatch(TokenType.Property))
        {
            declaration = ParseProperty(iterator, parser);
        }
        else if (iterator.IsMatch(TokenType.Let))
        {
            declaration = ParseField(iterator, parser);
        }
        else
        {
            parser.Messages.Add(Message.Error(parser.Document, $"Expected Function, Property or Field-declaration for Type, but got {iterator.Current}", iterator.Current.Line, iterator.Current.Column));
            iterator.NextToken();
        }

        return declaration.PlusAttrs(annotations).PlusAttrs(modifiers);
    }

    public static LNode ParseField(TokenIterator iterator, Parser parser)
    {
        iterator.Match(TokenType.Let);
        return VariableStatement.Parse(iterator, parser);
    }

    public static LNode ParseFunction(TokenIterator iterator, Parser parser)
    {
        var keywordToken = iterator.Match(TokenType.Function);
        var result = Signature.Parse(parser);
        iterator.Match(TokenType.Semicolon);

        return result.WithRange(keywordToken, iterator.Prev);
    }

    public static LNode ParseProperty(TokenIterator iterator, Parser parser)
    {
        var keywordToken = iterator.Match(TokenType.Property);
        LNode type = LNode.Missing;
        LNode value = LNode.Missing;
        var nameToken = iterator.Match(TokenType.Identifier);
        var name = LNode.Id(nameToken.Text);

        if (iterator.IsMatch(TokenType.Colon))
        {
            iterator.NextToken();

            type = TypeLiteral.Parse(iterator, parser);
        }

        iterator.Match(TokenType.OpenCurly);

        LNode getter = LNode.Missing;
        LNode setter = LNode.Missing;

        var needModifier = false;
        LNodeList modifier;

        Modifier.TryParse(parser, out modifier);
        if (iterator.IsMatch(TokenType.Get))
        {
            iterator.NextToken();
            LNodeList args = LNode.List();
            if (iterator.IsMatch(TokenType.Semicolon))
            {
                iterator.NextToken();
            }
            else
            {
                args.Add(Statement.ParseBlock(parser));
            }
            getter = LNode.Call(CodeSymbols.get, args).WithAttrs(modifier);
            needModifier = true;
        }

        if (needModifier) Modifier.TryParse(parser, out modifier);
        if (iterator.IsMatch(TokenType.Set))
        {
            iterator.NextToken();
            LNodeList args = LNode.List();
            if (iterator.IsMatch(TokenType.Semicolon))
            {
                iterator.NextToken();
            }
            else
            {
                args.Add(Statement.ParseBlock(parser));
            }
            setter = LNode.Call(CodeSymbols.set, args).WithAttrs(modifier);
        } else if (iterator.IsMatch(TokenType.Init))
        {
            iterator.NextToken();
            LNodeList args = LNode.List();
            if (iterator.IsMatch(TokenType.Semicolon))
            {
                iterator.NextToken();
            }
            else
            {
                args.Add(Statement.ParseBlock(parser));
            }
            setter = LNode.Call(Symbols.init, args).WithAttrs(modifier);
        }

        iterator.Match(TokenType.CloseCurly);

        if (iterator.IsMatch(TokenType.EqualsToken))
        {
            iterator.NextToken();

            value = Expression.Parse(parser);

            iterator.Match(TokenType.Semicolon);
        }

        return SyntaxTree.Property(type, name, getter, setter, value).WithRange(keywordToken, iterator.Prev);
    }
}