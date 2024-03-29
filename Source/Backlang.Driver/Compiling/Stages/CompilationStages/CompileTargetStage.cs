﻿using Backlang.Codeanalysis.Parsing;
using Backlang.Contracts;
using Backlang.Core.CompilerService;
using Flo;
using Furesoft.Core.CodeDom.Compiler.Core;
using Furesoft.Core.CodeDom.Compiler.Pipeline;
using Furesoft.Core.CodeDom.Compiler.TypeSystem;

namespace Backlang.Driver.Compiling.Stages.CompilationStages;

public sealed class CompileTargetStage : IHandler<CompilerContext, CompilerContext>
{
    public async Task<CompilerContext> HandleAsync(CompilerContext context, Func<CompilerContext, Task<CompilerContext>> next)
    {
        context.Trees = null;

        if (!context.Messages.Any())
        {
            var description = GetDescription(context);

            context.CompilationTarget.BeforeCompiling(context);

            var assembly = context.CompilationTarget.Compile(description);
            var resultPath = Path.Combine(context.TempOutputPath,
                            context.OutputFilename);

            if (File.Exists(resultPath))
            {
                File.Delete(resultPath);
            }

            assembly.WriteTo(File.OpenWrite(resultPath));

            context.CompilationTarget.AfterCompiling(context);
        }

        return await next.Invoke(context);
    }

    private static AssemblyContentDescription GetDescription(CompilerContext context)
    {
        var attributes = new AttributeMap();

        if (context.OutputType == "MacroLib")
        {
            attributes = new AttributeMap(new DescribedAttribute(Utils.ResolveType(context.Binder, typeof(MacroLibAttribute))));
        }

        var entryPoint = GetEntryPoint(context);

        context.Assembly.IsLibrary = entryPoint == null && context.OutputType == "Library";

        return new(context.Assembly.Name.Qualify(),
            attributes, context.Assembly, entryPoint, context.Environment);
    }

    private static IMethod GetEntryPoint(CompilerContext context)
    {
        if (string.IsNullOrEmpty(context.OutputType))
        {
            context.OutputType = "Exe";
        }

        if (context.OutputType != "Exe")
        {
            return null;
        }

        var entryPoint = context.Assembly.Types
            .FirstOrDefault(_ => _.FullName.ToString() == $".{Names.ProgramClass}")
            .Methods.FirstOrDefault(_ => _.Name.ToString() == Names.MainMethod && _.IsStatic);

        if (entryPoint == null)
        {
            context.Messages.Add(Message.Error("Got OutputType 'Exe' but couldn't find entry point."));
        }

        return entryPoint;
    }
}