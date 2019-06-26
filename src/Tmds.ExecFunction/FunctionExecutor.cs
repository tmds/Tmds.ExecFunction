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
        private readonly Action<ProcessStartInfo> _configure;
        private readonly Action<Process> _onExit;

        public FunctionExecutor(Action<ProcessStartInfo> configure, Action<Process> onExit = null)
        {
            _configure = configure;
            _onExit = onExit;
        }

        public void Run(Action action, Action<ProcessStartInfo> configure = null)
        {
            using (Process process = ExecFunction.Start(action, CombineConfigures(_configure, configure)))
            {
                process.WaitForExit();
                _onExit?.Invoke(process);
            }
        }

        public void Run(Action<string[]> action, string[] args, Action<ProcessStartInfo> configure = null)
        {
            using (Process process = ExecFunction.Start(action, args, CombineConfigures(_configure, configure)))
            {
                process.WaitForExit();
                _onExit?.Invoke(process);
            }
        }

        public void Run(Func<int> action, Action<ProcessStartInfo> configure = null)
        {
            using (Process process = ExecFunction.Start(action, CombineConfigures(_configure, configure)))
            {
                process.WaitForExit();
                _onExit?.Invoke(process);
            }
        }

        public void Run(Func<string[], int> action, string[] args, Action<ProcessStartInfo> configure = null)
        {
            using (Process process = ExecFunction.Start(action, args, CombineConfigures(_configure, configure)))
            {
                process.WaitForExit();
                _onExit?.Invoke(process);
            }
        }

        public void Run(Func<Task> action, Action<ProcessStartInfo> configure = null)
        {
            using (Process process = ExecFunction.Start(action, CombineConfigures(_configure, configure)))
            {
                process.WaitForExit();
                _onExit?.Invoke(process);
            }
        }

        public void Run(Func<string[], Task> action, string[] args, Action<ProcessStartInfo> configure = null)
        {
            using (Process process = ExecFunction.Start(action, args, CombineConfigures(_configure, configure)))
            {
                process.WaitForExit();
                _onExit?.Invoke(process);
            }
        }

        public void Run(Func<Task<int>> action, Action<ProcessStartInfo> configure = null)
        {
            using (Process process = ExecFunction.Start(action, CombineConfigures(_configure, configure)))
            {
                process.WaitForExit();
                _onExit?.Invoke(process);
            }
        }

        public void Run(Func<string[], Task<int>> action, string[] args, Action<ProcessStartInfo> configure = null)
        {
            using (Process process = ExecFunction.Start(action, args, CombineConfigures(_configure, configure)))
            {
                process.WaitForExit();
                _onExit?.Invoke(process);
            }
        }

        private static Action<ProcessStartInfo> CombineConfigures(Action<ProcessStartInfo> first, Action<ProcessStartInfo> second)
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