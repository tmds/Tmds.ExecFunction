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
    public static class ProcessStartInfoExtensions
    {
        public static ProcessStartInfo WithRedirectedStdio(this ProcessStartInfo psi)
        {
            psi.RedirectStandardError = true;
            psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;
            return psi;
        }
    }

    public static partial class ExecFunction
    {
        public static readonly Action<ProcessStartInfo> RedirectStdio = psi => psi.WithRedirectedStdio();

        public static Process Start(Action action, Action<ProcessStartInfo> configure = null)
            => Start(GetMethodInfo(action), Array.Empty<string>(), configure);

        public static Process Start(Action<string[]> action, string[] args, Action<ProcessStartInfo> configure = null)
            => Start(GetMethodInfo(action), args ?? throw new ArgumentNullException(nameof(args)), configure);

        public static Process Start(Func<int> action, Action<ProcessStartInfo> configure = null)
            => Start(GetMethodInfo(action), Array.Empty<string>(), configure);

        public static Process Start(Func<string[], int> action, string[] args, Action<ProcessStartInfo> configure = null)
            => Start(GetMethodInfo(action), args ?? throw new ArgumentNullException(nameof(args)), configure);

        public static Process Start(Func<Task> action, Action<ProcessStartInfo> configure = null)
            => Start(GetMethodInfo(action), Array.Empty<string>(), configure);

        public static Process Start(Func<string[], Task> action, string[] args, Action<ProcessStartInfo> configure = null)
            => Start(GetMethodInfo(action), args ?? throw new ArgumentNullException(nameof(args)), configure);

        public static Process Start(Func<Task<int>> action, Action<ProcessStartInfo> configure = null)
            => Start(GetMethodInfo(action), Array.Empty<string>(), configure);

        public static Process Start(Func<string[], Task<int>> action, string[] args, Action<ProcessStartInfo> configure = null)
            => Start(GetMethodInfo(action), args ?? throw new ArgumentNullException(nameof(args)), configure);

        private static Process Start(MethodInfo method, string[] args, Action<ProcessStartInfo> configure)
            => Process.Start(CreateProcessStartInfo(method, args, configure));

        private static ProcessStartInfo CreateProcessStartInfo(MethodInfo method, string[] args, Action<ProcessStartInfo> configure)
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

            // Start the other process and return a wrapper for it to handle its lifetime and exit checking.
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.UseShellExecute = false;
            configure?.Invoke(psi);

            // If we need the host (if it exists), use it, otherwise target the console app directly.
            Type t = method.DeclaringType;
            Assembly a = t.GetTypeInfo().Assembly;
            string programArgs = PasteArguments.Paste(new string[] { a.FullName, t.FullName, method.Name });
            string functionArgs = PasteArguments.Paste(args);
            string fullArgs = HostArguments + " " + " " + programArgs + " " + functionArgs;

            psi.FileName = HostFilename;
            psi.Arguments = fullArgs;

            return psi;
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

            // application is running as 'dotnet exec'
            if (HostFilename.EndsWith("/dotnet"))
            {
                string execFunctionAssembly = typeof(ExecFunction).Assembly.Location;

                string entryAssemblyWithoutExtension = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                                                                    Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location));
                string[] appArguments = GetApplicationArguments();

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
                if (arguments[i] == name)
                {
                    return arguments[i + 1];
                }
            }
            return null;
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
            else
            {
                throw new PlatformNotSupportedException($"{nameof(GetApplicationArguments)} is unsupported on this platform");
            }
        }

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