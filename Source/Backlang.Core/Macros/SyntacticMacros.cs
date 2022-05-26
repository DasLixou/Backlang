﻿using LeMP;
using Loyc;
using Loyc.Syntax;
using System.Globalization;

namespace Backlang.Core.Macros;

public static partial class BuiltInMacros
{
    [LexicalMacro("left /= right;", "Convert to left = left / something", "'/=", Mode = MacroMode.MatchIdentifierOrCall)]
    public static LNode DivEquals(LNode @operator, IMacroContext context)
    {
        return ConverToAssignment(@operator, CodeSymbols.Div);
    }

    [LexicalMacro("operator", "Convert to public static op_", "#fn", Mode = MacroMode.MatchIdentifierOrCall)]
    public static LNode ExpandOperator(LNode @operator, IMacroContext context)
    {
        var operatorAttribute = LNode.Id((Symbol)"#operator");
        if (@operator.Attrs.Contains(operatorAttribute))
        {
            var newAttrs = new LNodeList() { LNode.Id(CodeSymbols.Public), LNode.Id(CodeSymbols.Static) };
            var modChanged = @operator.WithoutAttr(operatorAttribute).WithAttrs(newAttrs);
            var fnName = @operator.Args[1];

            var titleCaseName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(fnName.Name.Name);

            var newTarget = LNode.Id("op_" + titleCaseName);
            return modChanged.WithArgChanged(1, newTarget);
        }

        return @operator;
    }

    [LexicalMacro("left -= right;", "Convert to left = left - something", "'-=", Mode = MacroMode.MatchIdentifierOrCall)]
    public static LNode MinusEquals(LNode @operator, IMacroContext context)
    {
        return ConverToAssignment(@operator, CodeSymbols.Sub);
    }

    [LexicalMacro("left *= right;", "Convert to left = left * something", "'*=", Mode = MacroMode.MatchIdentifierOrCall)]
    public static LNode MulEquals(LNode @operator, IMacroContext context)
    {
        return ConverToAssignment(@operator, CodeSymbols.Mul);
    }

    [LexicalMacro("left += right;", "Convert to left = left + something", "'+=", Mode = MacroMode.MatchIdentifierOrCall)]
    public static LNode PlusEquals(LNode @operator, IMacroContext context)
    {
        return ConverToAssignment(@operator, CodeSymbols.Add);
    }

    private static LNode ConverToAssignment(LNode @operator, Symbol symbol)
    {
        var arg1 = @operator.Args[0];
        var arg2 = @operator.Args[1];

        return F.Call(CodeSymbols.Assign, arg1, F.Call(symbol, arg1, arg2));
    }
}