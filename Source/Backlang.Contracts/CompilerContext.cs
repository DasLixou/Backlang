﻿using Backlang.Codeanalysis.Parsing;
using Backlang.Codeanalysis.Parsing.AST;
using Backlang.Contracts.Scoping;
using CommandLine;
using Furesoft.Core.CodeDom.Compiler.Core;
using Furesoft.Core.CodeDom.Compiler.Core.TypeSystem;
using Loyc.Syntax;

namespace Backlang.Contracts;

#nullable disable

public sealed class CompilerContext
{
    public IEnumerable<IMethod> writeMethods;

    public ICompilationTarget CompilationTarget;
    public PluginContainer Plugins;

    public DescribedAssembly Assembly { get; set; }

    public TypeResolver Binder { get; set; } = new();

    public Scope GlobalScope { get; } = new(null);

    public List<MethodBodyCompilation> BodyCompilations { get; set; } = new();

    public TypeEnvironment Environment { get; set; }

    public DescribedType ExtensionsType { get; set; }

    [Option('i', "input", Required = true, HelpText = "Input files to be compiled.")]
    public IEnumerable<string> InputFiles { get; set; }

    public string[] MacroReferences { get; set; }
    public List<Message> Messages { get; set; } = new();

    [Option('o', "output", Required = true, HelpText = "Output filename")]
    public string OutputFilename { get; set; }

    public string OutputPath { get; set; }

    [Option('p', "print-tree", Required = false, HelpText = "Output files as tree")]
    public bool OutputTree { get; set; }

    [Option('t', "type", Required = false, HelpText = "Outputtype")]
    public string OutputType { get; set; }

    public string ProjectFile { get; set; }

    [Option('r', "reference", Required = false, HelpText = "References of the assembly")]
    public IEnumerable<string> References { get; set; } = Array.Empty<string>();

    public string ResultingOutputPath { get; set; }

    [Option("target", Required = false, HelpText = "For which platform to compile to")]
    public string Target { get; set; }

    public string TempOutputPath { get; set; }
    public List<CompilationUnit> Trees { get; set; } = new();

    [Option('e', longName: "resource", HelpText = "Embedd files into the assembly as resource")]
    public IEnumerable<string> EmbeddedResource { get; set; }

    public void AddError(LNode node, string msg)
    {
        if (node.Range.Source is not SourceFile<StreamCharSource>) return;

        Messages.Add(Message.Error((SourceFile<StreamCharSource>)node.Range.Source, msg, node.Range.Start.Line, node.Range.Start.Column));
    }
}