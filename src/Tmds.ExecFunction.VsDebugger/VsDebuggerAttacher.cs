//MIT License

//Copyright (c) 2019 Cy Scott

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

// implementation sourced from https://github.com/CyAScott/AppDomainAlternative/

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

using EnvDTE;

namespace System
{
    public static class VsDebuggerAttacher
    {
        [ComImport, Guid("00000016-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IOleMessageFilter
        {
            [PreserveSig]
            int HandleInComingCall(int dwCallType, IntPtr hTaskCaller, int dwTickCount, IntPtr lpInterfaceInfo);

            [PreserveSig]
            int RetryRejectedCall(IntPtr hTaskCallee, int dwTickCount, int dwRejectType);

            [PreserveSig]
            int MessagePending(IntPtr hTaskCallee, int dwTickCount, int dwPendingType);
        }

        public static void AttachToTargetProcessDebugger(int pid, int targetPid)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                Console.Error.WriteLine($"{nameof(VsDebuggerAttacher)}.{nameof(AttachToTargetProcessDebugger)} can not run except windows. Nothing will happen for process {pid} and {targetPid}.");
                return;
            }

            if (Threading.Thread.CurrentThread.GetApartmentState() == Threading.ApartmentState.STA)
            {
                InternalAttachToTargetProcessDebugger(pid, targetPid);
            }
            else
            {
                var thread = new Threading.Thread(() =>
                {
                    InternalAttachToTargetProcessDebugger(pid, targetPid);
                });
                thread.SetApartmentState(Threading.ApartmentState.STA);
                thread.Name = $"VsDebuggerAttacher {pid} -> {targetPid}'s debugger";
                thread.Start();
                thread.Join();
            }
        }

        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);

        private static IEnumerable<DTE> GetInstances()
        {
            int retVal = GetRunningObjectTable(0, out IRunningObjectTable rot);

            if (retVal == 0)
            {
                rot.EnumRunning(out IEnumMoniker enumMoniker);

                IntPtr fetched = IntPtr.Zero;
                IMoniker[] moniker = new IMoniker[1];
                while (enumMoniker.Next(1, moniker, fetched) == 0)
                {
                    CreateBindCtx(0, out IBindCtx bindCtx);
                    moniker[0].GetDisplayName(bindCtx, null, out string displayName);
                    bool isVisualStudio = displayName.StartsWith("!VisualStudio");
                    if (isVisualStudio)
                    {
                        rot.GetObject(moniker[0], out object obj);
                        if (obj is DTE dte)
                        {
                            yield return dte;
                        }
                    }
                }
            }
        }

        [DllImport("ole32.dll")]
        private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);

        private static void InternalAttachToTargetProcessDebugger(int pid, int targetPid)
        {
            if (pid < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pid));
            }

            if (targetPid < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetPid));
            }

            IEnumerable<DTE> dteInstances = GetInstances();

            DTE dte = null;

            //try 3 times
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    dte = dteInstances.SingleOrDefault(x => x.Debugger.DebuggedProcesses.OfType<Process>().Any(y => y.ProcessID == targetPid));
                }
                catch (COMException)
                {
                    //Wait init
                    Threading.Thread.Sleep(500);
                }
            }

            if (dte == null)
            {
                throw new InvalidOperationException($"Unable to find the DTE instance for the process id \"{targetPid}\"");
            }

            MessageFilter.Register();

            try
            {
                Process target;
                try
                {
                    target = dte.Debugger.LocalProcesses.OfType<Process>().FirstOrDefault(process => process.ProcessID == pid);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Unable to find process {pid} to debug.", ex);
                }

                if (target == null)
                {
                    throw new InvalidOperationException($"Unable to find process ID: {pid}");
                }

                try
                {
                    target.Attach();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Unable to attach the debugger for process ID: {pid}", ex);
                }
            }
            finally
            {
                MessageFilter.Revoke();
            }
        }

        private sealed class MessageFilter : IOleMessageFilter
        {
            private const int Handled = 0, RetryAllowed = 2, Retry = 99, Cancel = -1, WaitAndDispatch = 2;

            public static void CoRegisterMessageFilter(IOleMessageFilter newFilter)
            {
                CoRegisterMessageFilter(newFilter, out _);
            }

            [DllImport("Ole32.dll")]
            public static extern int CoRegisterMessageFilter(IOleMessageFilter newFilter, out IOleMessageFilter oldFilter);

            public static void Register()
            {
                CoRegisterMessageFilter(new MessageFilter());
            }

            public static void Revoke()
            {
                CoRegisterMessageFilter(null);
            }

            int IOleMessageFilter.HandleInComingCall(int dwCallType, IntPtr hTaskCaller, int dwTickCount, IntPtr lpInterfaceInfo)
            {
                return Handled;
            }

            int IOleMessageFilter.MessagePending(IntPtr hTaskCallee, int dwTickCount, int dwPendingType)
            {
                return WaitAndDispatch;
            }

            int IOleMessageFilter.RetryRejectedCall(IntPtr hTaskCallee, int dwTickCount, int dwRejectType)
            {
                return dwRejectType == RetryAllowed ? Retry : Cancel;
            }
        }
    }
}