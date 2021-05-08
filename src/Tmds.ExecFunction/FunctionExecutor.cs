// Copyright 2019 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Tmds.Utils
{
    public class FunctionExecutor
    {
        private readonly Action<ExecFunctionOptions> _configure;
        private readonly ExecFunction.ExecFunctionHostArgs _execFunctionHostArgs;

        public FunctionExecutor(Action<ExecFunctionOptions> configure, ExecFunction.ExecFunctionHostArgs execFunctionHostArgs = null)
        {
            _configure = configure;
            _execFunctionHostArgs = execFunctionHostArgs;
        }

        public Process Start(Action action, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Start(action, _configure + configure.Invoke, _execFunctionHostArgs);

        public Process Start(Action<string[]> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Start(action, args, _configure + configure, _execFunctionHostArgs);

        public Process Start(Func<int> action, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Start(action, _configure + configure, _execFunctionHostArgs);

        public Process Start(Func<string[], int> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Start(action, args, _configure + configure, _execFunctionHostArgs);

        public Process Start(Func<Task> action, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Start(action, _configure + configure, _execFunctionHostArgs);

        public Process Start(Func<string[], Task> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Start(action, args, _configure + configure, _execFunctionHostArgs);

        public Process Start(Func<Task<int>> action, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Start(action, _configure + configure, _execFunctionHostArgs);

        public Process Start(Func<string[], Task<int>> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Start(action, args, _configure + configure, _execFunctionHostArgs);

        public void Run(Action action, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Run(action, _configure + configure, _execFunctionHostArgs);

        public void Run(Action<string[]> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Run(action, args, _configure + configure, _execFunctionHostArgs);

        public void Run(Func<int> action, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Run(action, _configure + configure, _execFunctionHostArgs);

        public void Run(Func<string[], int> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Run(action, args, _configure + configure, _execFunctionHostArgs);

        public void Run(Func<Task> action, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Run(action, _configure + configure, _execFunctionHostArgs);

        public void Run(Func<string[], Task> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Run(action, args, _configure + configure, _execFunctionHostArgs);

        public void Run(Func<Task<int>> action, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Run(action, _configure + configure, _execFunctionHostArgs);

        public void Run(Func<string[], Task<int>> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Run(action, args, _configure + configure, _execFunctionHostArgs);

        public Task RunAsync(Action action, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.RunAsync(action, _configure + configure, _execFunctionHostArgs);

        public Task RunAsync(Action<string[]> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.RunAsync(action, args, _configure + configure, _execFunctionHostArgs);

        public Task RunAsync(Func<int> action, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.RunAsync(action, _configure + configure, _execFunctionHostArgs);

        public Task RunAsync(Func<string[], int> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.RunAsync(action, args, _configure + configure, _execFunctionHostArgs);

        public Task RunAsync(Func<Task> action, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.RunAsync(action, _configure + configure, _execFunctionHostArgs);

        public Task RunAsync(Func<string[], Task> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.RunAsync(action, args, _configure + configure, _execFunctionHostArgs);

        public Task RunAsync(Func<Task<int>> action, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.RunAsync(action, _configure + configure, _execFunctionHostArgs);

        public Task RunAsync(Func<string[], Task<int>> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.RunAsync(action, args, _configure + configure, _execFunctionHostArgs);
    }
}