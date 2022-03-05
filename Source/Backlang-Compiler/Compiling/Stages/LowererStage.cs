﻿using Flo;
using Backlang_Compiler.Compiling.Passes.Lowerer;

namespace Backlang_Compiler.Compiling.Stages
{
    public class LowererStage : IHandler<CompilerContext, CompilerContext>
    {
        private PassManager _optimization = new();
        public LowererStage()
        {
            _optimization.AddPass<OperatorAssignLowererPass>();
            _optimization.AddPass<IncrementOperatorLowererPass>();
        }

        public async Task<CompilerContext> HandleAsync(CompilerContext context, Func<CompilerContext, Task<CompilerContext>> next)
        {
            for (int i = 0; i < context.Trees.Count; i++)
            {
                context.Trees[i] = _optimization.Process(context.Trees[i]);
            }

            return await next.Invoke(context);
        }
    }
}
