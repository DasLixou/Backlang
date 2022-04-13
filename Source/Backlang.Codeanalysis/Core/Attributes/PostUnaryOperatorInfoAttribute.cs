﻿namespace Backlang.Codeanalysis.Core.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public sealed class PostUnaryOperatorInfoAttribute : OperatorInfoAttribute
{
    public PostUnaryOperatorInfoAttribute(int precedence) : base(precedence, isUnary: true, isPostUnary: true)
    {
    }
}
