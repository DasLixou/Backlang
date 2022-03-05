﻿namespace Backlang_Compiler;

public class Scope
{
    public List<string> ParameterNames { get; } = new();

    public uint GetParameterIndex(string name)
    {
        return (uint) ParameterNames.IndexOf(name);
    }

    public bool IsParameter(string name)
    {
        return ParameterNames.Contains(name);
    }
}
