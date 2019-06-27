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

        public FunctionExecutor(Action<ExecFunctionOptions> configure)
        {
            _configure = configure;
        }

        public Process Start(Action action, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Start(action, CombineConfigures(_configure, configure));

        public Process Start(Action<string[]> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Start(action, args, CombineConfigures(_configure, configure));

        public Process Start(Func<int> action, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Start(action, CombineConfigures(_configure, configure));

        public Process Start(Func<string[], int> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Start(action, args, CombineConfigures(_configure, configure));

        public Process Start(Func<Task> action, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Start(action, CombineConfigures(_configure, configure));

        public Process Start(Func<string[], Task> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Start(action, args, CombineConfigures(_configure, configure));

        public Process Start(Func<Task<int>> action, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Start(action, CombineConfigures(_configure, configure));

        public Process Start(Func<string[], Task<int>> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Start(action, args, CombineConfigures(_configure, configure));

        public void Run(Action action, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Run(action, CombineConfigures(_configure, configure));

        public void Run(Action<string[]> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Run(action, args, CombineConfigures(_configure, configure));

        public void Run(Func<int> action, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Run(action, CombineConfigures(_configure, configure));

        public void Run(Func<string[], int> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Run(action, args, CombineConfigures(_configure, configure));

        public void Run(Func<Task> action, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Run(action, CombineConfigures(_configure, configure));

        public void Run(Func<string[], Task> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Run(action, args, CombineConfigures(_configure, configure));

        public void Run(Func<Task<int>> action, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Run(action, CombineConfigures(_configure, configure));

        public void Run(Func<string[], Task<int>> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.Run(action, args, CombineConfigures(_configure, configure));

        public Task RunAsync(Action action, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.RunAsync(action, CombineConfigures(_configure, configure));

        public Task RunAsync(Action<string[]> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.RunAsync(action, args, CombineConfigures(_configure, configure));

        public Task RunAsync(Func<int> action, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.RunAsync(action, CombineConfigures(_configure, configure));

        public Task RunAsync(Func<string[], int> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.RunAsync(action, args, CombineConfigures(_configure, configure));

        public Task RunAsync(Func<Task> action, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.RunAsync(action, CombineConfigures(_configure, configure));

        public Task RunAsync(Func<string[], Task> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.RunAsync(action, args, CombineConfigures(_configure, configure));

        public Task RunAsync(Func<Task<int>> action, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.RunAsync(action, CombineConfigures(_configure, configure));

        public Task RunAsync(Func<string[], Task<int>> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => ExecFunction.RunAsync(action, args, CombineConfigures(_configure, configure));

        private static Action<ExecFunctionOptions> CombineConfigures(Action<ExecFunctionOptions> first, Action<ExecFunctionOptions> second)
        {
            if (first == null)
            {
                return second;
            }
            if (second == null)
            {
                return first;
            }
            return psi => { first.Invoke(psi); second.Invoke(psi); };
        }
    }
}