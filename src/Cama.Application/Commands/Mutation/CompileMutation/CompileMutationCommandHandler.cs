﻿using System.Threading;
using System.Threading.Tasks;
using Cama.Core.Execution.Compilation;
using MediatR;

namespace Cama.Application.Commands.Mutation.CompileMutation
{
    public class CompileMutationCommandHandler : IRequestHandler<CompileMutationCommand, CompilationResult>
    {
        private readonly Compiler _compiler;

        public CompileMutationCommandHandler(Compiler compiler)
        {
            _compiler = compiler;
        }

        public async Task<CompilationResult> Handle(CompileMutationCommand command, CancellationToken cancellationToken)
        {
            return await _compiler.CompileAsync(command.SavePath, command.Mutation);
        }
    }
}
