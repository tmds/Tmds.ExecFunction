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

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using EnvDTE;
using OsProcess = System.Diagnostics.Process;

namespace VsDebugger
{
    public static class Program
    {
        public static int Main(string[] commandArgs)
        {
            var args = commandArgs
                .Where(arg => arg.Length > 0 && arg[0] == '-')
                .Select(arg => arg.Split(new [] { ':' }, 2))
                .ToDictionary(arg => arg[0].Substring(1).Trim(), arg => arg.Length == 1 ? "" : arg[1].Trim(), StringComparer.OrdinalIgnoreCase);

            if (!args.TryGetValue("pid", out var pidStr) || !int.TryParse(pidStr, out var pid))
            {
                Console.WriteLine("Missing command line argument \"-pid:...\" with the target process id.");
                return 2;
            }

            AttachMode attachMode;
            try
            {
                if (!args.TryGetValue("debugger", out var mode) || string.Equals(mode, "attach", StringComparison.OrdinalIgnoreCase))
                {
                    attachMode = AttachMode.Attach;
                }
                else if (string.Equals(mode, "detach", StringComparison.OrdinalIgnoreCase))
                {
                    attachMode = AttachMode.Detach;
                }
                else
                {
                    throw new Exception();
                }
            }
            catch
            {
                Console.WriteLine($"Unable to attach debugger to process ID: {pid}");
                return 4;
            }

            int parentProcessId;
            try
            {
                if (!args.TryGetValue("ppid", out var parentProcessIdStr) || !int.TryParse(parentProcessIdStr, out parentProcessId))
                {
                    parentProcessId = -1;
                }
            }
            catch
            {
                parentProcessId = -1;
            }

            var dteInstances = GetInstances();
            var dte = dteInstances.SingleOrDefault(x => x.Debugger.DebuggedProcesses.OfType<Process>().Any(y => y.ProcessID == parentProcessId));
            if(dte == null)
            {
                Console.WriteLine($"Unable to find the DTE instance for the parent process id \"{parentProcessId}\"");
                return 3;
            }

            MessageFilter.Register();

            Process target;
            try
            {
                if (attachMode == AttachMode.Detach)
                {
                    target = dte.Debugger.DebuggedProcesses.OfType<Process>().FirstOrDefault(process => process.ProcessID == pid);
                }
                else
                {
                    Console.WriteLine($"Ppid: {parentProcessId}");
                    var parentProcess = parentProcessId == -1 ? null :
                        dte.Debugger.DebuggedProcesses.OfType<Process>().FirstOrDefault(process => process.ProcessID == parentProcessId) ??
                        dte.Debugger.LocalProcesses.OfType<Process>().FirstOrDefault(process => process.ProcessID == parentProcessId);

                    target = (parentProcess?.Parent?.LocalProcesses ?? dte.Debugger.LocalProcesses).OfType<Process>().FirstOrDefault(process => process.ProcessID == pid);
                }
            }
            catch (Exception error)
            {
                Console.WriteLine($"Unable to find processes to debug: {error}");
                return 5;
            }

            if (target == null)
            {
                Console.WriteLine($"Unable to find process ID: {pid}");
                return 6;
            }

            try
            {
                if (attachMode == AttachMode.Attach)
                {
                    target.Attach();
                }
                else
                {
                    target.Detach(false);
                }
            }
            catch
            {
                Console.WriteLine($"Unable to attach/detach the debugger to/from process ID: {pid}");
                return 7;
            }

            MessageFilter.Revoke();

            return 1;
        }

        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);

        [DllImport("ole32.dll")]
        private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);

        static System.Collections.Generic.IEnumerable<DTE> GetInstances()
        {
            IRunningObjectTable rot;
            IEnumMoniker enumMoniker;
            int retVal = GetRunningObjectTable(0, out rot);

            if (retVal == 0)
            {
                rot.EnumRunning(out enumMoniker);

                IntPtr fetched = IntPtr.Zero;
                IMoniker[] moniker = new IMoniker[1];
                while (enumMoniker.Next(1, moniker, fetched) == 0)
                {
                    IBindCtx bindCtx;
                    CreateBindCtx(0, out bindCtx);
                    string displayName;
                    moniker[0].GetDisplayName(bindCtx, null, out displayName);
                    bool isVisualStudio = displayName.StartsWith("!VisualStudio");
                    if (isVisualStudio)
                    {
                        object obj;
                        rot.GetObject(moniker[0], out obj);
                        var dte = obj as DTE;
                        yield return dte;
                    }
                }
            }
        }
    }

    public enum AttachMode
    {
        Attach,
        Detach
    }

    [ComImport, Guid("00000016-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IOleMessageFilter
    {
        [PreserveSig]
        int HandleInComingCall(int dwCallType, IntPtr hTaskCaller, int dwTickCount, IntPtr lpInterfaceInfo);

        [PreserveSig]
        int RetryRejectedCall(IntPtr hTaskCallee, int dwTickCount, int dwRejectType);

        [PreserveSig]
        int MessagePending(IntPtr hTaskCallee, int dwTickCount, int dwPendingType);
    }

    public class MessageFilter : IOleMessageFilter
    {
        private const int handled = 0, retryAllowed = 2, retry = 99, cancel = -1, waitAndDispatch = 2;

        int IOleMessageFilter.HandleInComingCall(int dwCallType, IntPtr hTaskCaller, int dwTickCount, IntPtr lpInterfaceInfo) => handled;

        int IOleMessageFilter.RetryRejectedCall(IntPtr hTaskCallee, int dwTickCount, int dwRejectType) => dwRejectType == retryAllowed ? retry : cancel;

        int IOleMessageFilter.MessagePending(IntPtr hTaskCallee, int dwTickCount, int dwPendingType) => waitAndDispatch;

        public static void Register() => CoRegisterMessageFilter(new MessageFilter());

        public static void Revoke() => CoRegisterMessageFilter(null);

        public static void CoRegisterMessageFilter(IOleMessageFilter newFilter) => CoRegisterMessageFilter(newFilter, out _);

        [DllImport("Ole32.dll")]
        public static extern int CoRegisterMessageFilter(IOleMessageFilter newFilter, out IOleMessageFilter oldFilter);
    }

}
