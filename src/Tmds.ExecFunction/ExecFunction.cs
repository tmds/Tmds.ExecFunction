// Copyright 2019 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Tmds.Utils
{
    public static partial class ExecFunction
    {
        public static Process Start(Action action, Action<ExecFunctionOptions> configure = null)
            => Start(GetMethodInfo(action), Array.Empty<string>(), configure).process;

        public static Process Start(Action<string[]> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => Start(GetMethodInfo(action), args ?? throw new ArgumentNullException(nameof(args)), configure).process;

        public static Process Start(Func<int> action, Action<ExecFunctionOptions> configure = null)
            => Start(GetMethodInfo(action), Array.Empty<string>(), configure).process;

        public static Process Start(Func<string[], int> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => Start(GetMethodInfo(action), args ?? throw new ArgumentNullException(nameof(args)), configure).process;

        public static Process Start(Func<Task> action, Action<ExecFunctionOptions> configure = null)
            => Start(GetMethodInfo(action), Array.Empty<string>(), configure).process;

        public static Process Start(Func<string[], Task> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => Start(GetMethodInfo(action), args ?? throw new ArgumentNullException(nameof(args)), configure).process;

        public static Process Start(Func<Task<int>> action, Action<ExecFunctionOptions> configure = null)
            => Start(GetMethodInfo(action), Array.Empty<string>(), configure).process;

        public static Process Start(Func<string[], Task<int>> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => Start(GetMethodInfo(action), args ?? throw new ArgumentNullException(nameof(args)), configure).process;

        public static void Run(Action action, Action<ExecFunctionOptions> configure = null)
            => Start(GetMethodInfo(action), Array.Empty<string>(), configure, waitForExit: true);

        public static void Run(Action<string[]> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => Start(GetMethodInfo(action), args ?? throw new ArgumentNullException(nameof(args)), configure, waitForExit: true);

        public static void Run(Func<int> action, Action<ExecFunctionOptions> configure = null)
            => Start(GetMethodInfo(action), Array.Empty<string>(), configure, waitForExit: true);

        public static void Run(Func<string[], int> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => Start(GetMethodInfo(action), args ?? throw new ArgumentNullException(nameof(args)), configure, waitForExit: true);

        public static void Run(Func<Task> action, Action<ExecFunctionOptions> configure = null)
            => Start(GetMethodInfo(action), Array.Empty<string>(), configure, waitForExit: true);

        public static void Run(Func<string[], Task> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => Start(GetMethodInfo(action), args ?? throw new ArgumentNullException(nameof(args)), configure, waitForExit: true);

        public static void Run(Func<Task<int>> action, Action<ExecFunctionOptions> configure = null)
            => Start(GetMethodInfo(action), Array.Empty<string>(), configure, waitForExit: true);

        public static void Run(Func<string[], Task<int>> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => Start(GetMethodInfo(action), args ?? throw new ArgumentNullException(nameof(args)), configure, waitForExit: true);

        public static Task RunAsync(Action action, Action<ExecFunctionOptions> configure = null)
            => Start(GetMethodInfo(action), Array.Empty<string>(), configure, returnTask: true).exitedTask;

        public static Task RunAsync(Action<string[]> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => Start(GetMethodInfo(action), args ?? throw new ArgumentNullException(nameof(args)), configure, returnTask: true).exitedTask;

        public static Task RunAsync(Func<int> action, Action<ExecFunctionOptions> configure = null)
            => Start(GetMethodInfo(action), Array.Empty<string>(), configure, returnTask: true).exitedTask;

        public static Task RunAsync(Func<string[], int> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => Start(GetMethodInfo(action), args ?? throw new ArgumentNullException(nameof(args)), configure, returnTask: true).exitedTask;

        public static Task RunAsync(Func<Task> action, Action<ExecFunctionOptions> configure = null)
            => Start(GetMethodInfo(action), Array.Empty<string>(), configure, returnTask: true).exitedTask;

        public static Task RunAsync(Func<string[], Task> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => Start(GetMethodInfo(action), args ?? throw new ArgumentNullException(nameof(args)), configure, returnTask: true).exitedTask;

        public static Task RunAsync(Func<Task<int>> action, Action<ExecFunctionOptions> configure = null)
            => Start(GetMethodInfo(action), Array.Empty<string>(), configure, returnTask: true).exitedTask;

        public static Task RunAsync(Func<string[], Task<int>> action, string[] args, Action<ExecFunctionOptions> configure = null)
            => Start(GetMethodInfo(action), args ?? throw new ArgumentNullException(nameof(args)), configure, returnTask: true).exitedTask;

        private static (Process process, Task exitedTask) Start(MethodInfo method, string[] args, Action<ExecFunctionOptions> configure, bool waitForExit = false, bool returnTask = false)
        {
            Process process = null;
            try
            {
                process = new Process();

                ExecFunctionOptions options = new ExecFunctionOptions(process.StartInfo);
                ConfigureProcessStartInfoForMethodInvocation(method, args, options.StartInfo);
                configure?.Invoke(options);

                TaskCompletionSource<bool> tcs = null;
                if (returnTask == true)
                {
                    tcs = new TaskCompletionSource<bool>();
                }

                if (options.OnExit != null || tcs != null)
                {
                    process.EnableRaisingEvents = true;
                    process.Exited += (_1, _2) =>
                    {
                        options.OnExit(process);

                        if (tcs != null)
                        {
                            tcs?.SetResult(true);
                            process.Dispose();
                        }
                    };
                }

                process.Start();

                if (waitForExit)
                {
                    process.WaitForExit();
                }

                return (process, tcs?.Task);
            }
            catch
            {
                process?.Dispose();
                throw;
            }
            finally
            {
                if (waitForExit)
                {
                    process?.Dispose();
                }
            }
        }

        private static void ConfigureProcessStartInfoForMethodInvocation(MethodInfo method, string[] args, ProcessStartInfo psi)
        {
            if (method.ReturnType != typeof(void) &&
                method.ReturnType != typeof(int) &&
                method.ReturnType != typeof(Task<int>))
            {
                throw new ArgumentException("method has an invalid return type", nameof(method));
            }
            if (method.GetParameters().Length > 1)
            {
                throw new ArgumentException("method has more than one argument argument", nameof(method));
            }
            if (method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType != typeof(string[]))
            {
                throw new ArgumentException("method has non string[] argument", nameof(method));
            }

            // If we need the host (if it exists), use it, otherwise target the console app directly.
            Type t = method.DeclaringType;
            Assembly a = t.GetTypeInfo().Assembly;

            bool enableDebuggerAttach = Debugger.IsAttached && Environment.OSVersion.Platform == PlatformID.Win32NT;
            int pid = Process.GetCurrentProcess().Id;

            string programArgs = PasteArguments.Paste(new string[] { a.FullName, t.FullName, method.Name, enableDebuggerAttach.ToString(System.Globalization.CultureInfo.InvariantCulture), pid.ToString(System.Globalization.CultureInfo.InvariantCulture) });
            string functionArgs = PasteArguments.Paste(args);
            string fullArgs = HostArguments + " " + " " + programArgs + " " + functionArgs;

            psi.FileName = HostFilename;
            psi.Arguments = fullArgs;
        }

        private static MethodInfo GetMethodInfo(Delegate d)
        {
            // RemoteInvoke doesn't support marshaling state on classes associated with
            // the delegate supplied (often a display class of a lambda).  If such fields
            // are used, odd errors result, e.g. NullReferenceExceptions during the remote
            // execution.  Try to ward off the common cases by proactively failing early
            // if it looks like such fields are needed.
            if (d.Target != null)
            {
                // The only fields on the type should be compiler-defined (any fields of the compiler's own
                // making generally include '<' and '>', as those are invalid in C# source).  Note that this logic
                // may need to be revised in the future as the compiler changes, as this relies on the specifics of
                // actually how the compiler handles lifted fields for lambdas.
                Type targetType = d.Target.GetType();
                FieldInfo[] fields = targetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (var fi in fields)
                {
                    if (fi.Name.IndexOf('<') == -1)
                    {
                        throw new ArgumentException($"Field marshaling is not supported by {nameof(ExecFunction)}: {fi.Name}", "method");
                    }
                }
            }

            return d.GetMethodInfo();
        }

        static ExecFunction()
        {
            HostFilename = Process.GetCurrentProcess().MainModule.FileName;
            string[] appArguments = null;

            // application is running as 'testhost'
            // try to find parent 'dotnet' host process.
            if (HostFilename.EndsWith("/testhost") || HostFilename.EndsWith("\\testhost.exe"))
            {
                HostFilename = null;

                appArguments = GetApplicationArguments();
                string parentProcessIdRaw = GetApplicationArgument(appArguments, "--parentprocessid");
                if (parentProcessIdRaw != null)
                {
                    int parentProcessId = int.Parse(parentProcessIdRaw);

                    Process proc = Process.GetProcessById(parentProcessId);
                    HostFilename = proc.MainModule.FileName;
                }

                if (HostFilename == null ||
                   !((HostFilename.EndsWith("/dotnet") || HostFilename.EndsWith("\\dotnet.exe"))))
                {
                    throw new NotSupportedException("Application is running as testhost, unable to determine parent 'dotnet' process.");
                }
            }

            // application is running as 'dotnet exec'.
            if (HostFilename.EndsWith("/dotnet") || HostFilename.EndsWith("\\dotnet.exe"))
            {
                string execFunctionAssembly = typeof(ExecFunction).Assembly.Location;

                string entryAssemblyWithoutExtension = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                                                                    Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location));
                appArguments = appArguments ?? GetApplicationArguments();

                string runtimeConfigFile = GetApplicationArgument(appArguments, "--runtimeconfig");
                if (runtimeConfigFile == null)
                {
                    runtimeConfigFile = entryAssemblyWithoutExtension + ".runtimeconfig.json";
                }

                string depsFile = GetApplicationArgument(appArguments, "--depsfile");
                if (depsFile == null)
                {
                    depsFile = entryAssemblyWithoutExtension + ".deps.json";
                }

                HostArguments = PasteArguments.Paste(new string[] { "exec", "--runtimeconfig", runtimeConfigFile, "--depsfile", depsFile, execFunctionAssembly });
            }
            // application is an apphost. Main method must call 'RunFunction.Program.Main' for CommandName.
            else
            {
                HostArguments = CommandName;
            }
        }

        private static string GetApplicationArgument(string[] arguments, string name)
        {
            for (int i = 0; i < arguments.Length - 1; i++)
            {
                if (string.Equals(arguments[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return arguments[i + 1];
                }
            }
            return null;
        }

        private static string[] GetOSXCommandLineArguments()
        {
            // The following logic is based on https://gist.github.com/nonowarn/770696
            // Set up the mib array and the query for process maximum args size
            var mib = new int[3];
            int mibLength = 2;
            mib[0] = MACOS_CTL_KERN;
            mib[1] = MACOS_KERN_ARGMAX;

            int size = IntPtr.Size / 2;
            int argmax = 0;
            var argv = new List<string>();

            var mibHandle = GCHandle.Alloc(mib, GCHandleType.Pinned);
            try
            {
                var mibPtr = mibHandle.AddrOfPinnedObject();

                // Get the process args size
                SysCtl(mibPtr, mibLength, ref argmax, ref size, IntPtr.Zero, 0);

                // Get the PID so we can query this process' args
                var pid = Process.GetCurrentProcess().Id;

                // Now read the process args into the allocated space
                IntPtr procargs = Marshal.AllocHGlobal(argmax);
                try
                {
                    mib[0] = MACOS_CTL_KERN;
                    mib[1] = MACOS_KERN_PROCARGS2;
                    mib[2] = pid;
                    mibLength = 3;

                    SysCtl(mibPtr, mibLength, procargs, ref argmax, IntPtr.Zero, 0);

                    // The memory block we're reading is a series of null-terminated strings
                    // that looks something like this:
                    //
                    // | argc      | <int> is always 4 bytes long even on 64bit architectures
                    // | exec_path | ... \0\0\0\0 * ?
                    // | argv[0]   | ... \0
                    // | argv[1]   | ... \0
                    // | argv[2]   | ... \0
                    //   ...
                    // | env[0]    | ... \0  (VALUE = SOMETHING\0)

                    // Read argc
                    var argc = Marshal.ReadInt32(procargs);

                    // Skip over argc
                    var argvPtr = IntPtr.Add(procargs, sizeof(int));

                    // Skip over exec_path
                    var offset = 0;
                    while (Marshal.ReadByte(argvPtr, offset) != 0) { offset++; }
                    while (Marshal.ReadByte(argvPtr, offset) == 0) { offset++; }
                    argvPtr = IntPtr.Add(argvPtr, offset);

                    // Start reading argv
                    for (var i = 0; i < argc; i++)
                    {
                        offset = 0;
                        // Keep reading bytes until we find a null-terminated string
                        while (Marshal.ReadByte(argvPtr, offset) != 0) { offset++; }
                        var arg = Marshal.PtrToStringAnsi(argvPtr, offset);
                        argv.Add(arg);

                        // Move pointer to the start of the next arg (= currentArg + \0)
                        argvPtr = IntPtr.Add(argvPtr, offset + sizeof(byte));
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(procargs);
                }
            }
            finally
            {
                mibHandle.Free();
            }

            return argv.ToArray();
        }

        private static string[] GetApplicationArguments()
        {
            // Environment.GetCommandLineArgs doesn't include arguments passed to the runtime.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return File.ReadAllText($"/proc/{Process.GetCurrentProcess().Id}/cmdline").Split(new[] { '\0' });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                System.IntPtr ptr = GetCommandLine();
                string commandLine = Marshal.PtrToStringAuto(ptr);
                return CommandLineToArgs(commandLine);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GetOSXCommandLineArguments();
            }
            else
            {
                throw new PlatformNotSupportedException($"{nameof(GetApplicationArguments)} is unsupported on this platform");
            }
        }

        private const int MACOS_CTL_KERN = 1;
        private const int MACOS_KERN_ARGMAX = 8;
        private const int MACOS_KERN_PROCARGS2 = 49;

        [DllImport("libc",
            EntryPoint = "sysctl",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi,
            SetLastError = true)]
        private static extern int SysCtl(IntPtr mib, int mibLength, ref int oldp, ref int oldlenp, IntPtr newp, int newlenp);

        [DllImport("libc",
            EntryPoint = "sysctl",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi,
            SetLastError = true)]
        private static extern int SysCtl(IntPtr mib, int mibLength, IntPtr oldp, ref int oldlenp, IntPtr newp, int newlenp);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern System.IntPtr GetCommandLine();

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern IntPtr CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);

        public static string[] CommandLineToArgs(string commandLine)
        {
            int argc;
            var argv = CommandLineToArgvW(commandLine, out argc);
            if (argv == IntPtr.Zero)
                throw new System.ComponentModel.Win32Exception();
            try
            {
                var args = new string[argc];
                for (var i = 0; i < args.Length; i++)
                {
                    var p = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                    args[i] = Marshal.PtrToStringUni(p);
                }

                return args;
            }
            finally
            {
                Marshal.FreeHGlobal(argv);
            }
        }

        private static readonly string HostFilename;
        private static readonly string HostArguments;
    }
}
