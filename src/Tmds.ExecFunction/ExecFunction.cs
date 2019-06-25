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

        internal static partial class PasteArguments
        {
            internal static void AppendArgument(StringBuilder stringBuilder, string argument)
            {
                if (stringBuilder.Length != 0)
                {
                    stringBuilder.Append(' ');
                }

                // Parsing rules for non-argv[0] arguments:
                //   - Backslash is a normal character except followed by a quote.
                //   - 2N backslashes followed by a quote ==> N literal backslashes followed by unescaped quote
                //   - 2N+1 backslashes followed by a quote ==> N literal backslashes followed by a literal quote
                //   - Parsing stops at first whitespace outside of quoted region.
                //   - (post 2008 rule): A closing quote followed by another quote ==> literal quote, and parsing remains in quoting mode.
                if (argument.Length != 0 && ContainsNoWhitespaceOrQuotes(argument))
                {
                    // Simple case - no quoting or changes needed.
                    stringBuilder.Append(argument);
                }
                else
                {
                    stringBuilder.Append(Quote);
                    int idx = 0;
                    while (idx < argument.Length)
                    {
                        char c = argument[idx++];
                        if (c == Backslash)
                        {
                            int numBackSlash = 1;
                            while (idx < argument.Length && argument[idx] == Backslash)
                            {
                                idx++;
                                numBackSlash++;
                            }

                            if (idx == argument.Length)
                            {
                                // We'll emit an end quote after this so must double the number of backslashes.
                                stringBuilder.Append(Backslash, numBackSlash * 2);
                            }
                            else if (argument[idx] == Quote)
                            {
                                // Backslashes will be followed by a quote. Must double the number of backslashes.
                                stringBuilder.Append(Backslash, numBackSlash * 2 + 1);
                                stringBuilder.Append(Quote);
                                idx++;
                            }
                            else
                            {
                                // Backslash will not be followed by a quote, so emit as normal characters.
                                stringBuilder.Append(Backslash, numBackSlash);
                            }

                            continue;
                        }

                        if (c == Quote)
                        {
                            // Escape the quote so it appears as a literal. This also guarantees that we won't end up generating a closing quote followed
                            // by another quote (which parses differently pre-2008 vs. post-2008.)
                            stringBuilder.Append(Backslash);
                            stringBuilder.Append(Quote);
                            continue;
                        }

                        stringBuilder.Append(c);
                    }

                    stringBuilder.Append(Quote);
                }
            }

            private static bool ContainsNoWhitespaceOrQuotes(string s)
            {
                for (int i = 0; i < s.Length; i++)
                {
                    char c = s[i];
                    if (char.IsWhiteSpace(c) || c == Quote)
                    {
                        return false;
                    }
                }

                return true;
            }

            /// <summary>
            /// Repastes a set of arguments into a linear string that parses back into the originals under pre- or post-2008 VC parsing rules.
            /// </summary>
            internal static string Paste(IEnumerable<string> arguments, bool pasteFirstArgumentUsingArgV0Rules = false)
            {
                /// On Windows: The rules for parsing the executable name (argv[0]) are special, so you must indicate whether the first argument actually is argv[0].
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var stringBuilder = new StringBuilder();

                    foreach (string argument in arguments)
                    {
                        if (pasteFirstArgumentUsingArgV0Rules)
                        {
                            pasteFirstArgumentUsingArgV0Rules = false;

                            // Special rules for argv[0]
                            //   - Backslash is a normal character.
                            //   - Quotes used to include whitespace characters.
                            //   - Parsing ends at first whitespace outside quoted region.
                            //   - No way to get a literal quote past the parser.

                            bool hasWhitespace = false;
                            foreach (char c in argument)
                            {
                                if (c == Quote)
                                {
                                    throw new ApplicationException("The argv[0] argument cannot include a double quote.");
                                }
                                if (char.IsWhiteSpace(c))
                                {
                                    hasWhitespace = true;
                                }
                            }
                            if (argument.Length == 0 || hasWhitespace)
                            {
                                stringBuilder.Append(Quote);
                                stringBuilder.Append(argument);
                                stringBuilder.Append(Quote);
                            }
                            else
                            {
                                stringBuilder.Append(argument);
                            }
                        }
                        else
                        {
                            AppendArgument(stringBuilder, argument);
                        }
                    }

                    return stringBuilder.ToString();
                }
                /// On Unix: the rules for parsing the executable name (argv[0]) are ignored.
                else
                {
                    var stringBuilder = new StringBuilder();
                    foreach (string argument in arguments)
                    {
                        AppendArgument(stringBuilder, argument);
                    }
                    return stringBuilder.ToString();
                }

            }

            private const char Quote = '\"';
            private const char Backslash = '\\';
        }
    }
}