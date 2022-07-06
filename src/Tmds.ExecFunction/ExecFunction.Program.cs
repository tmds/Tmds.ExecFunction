// Copyright 2019 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Tmds.Utils
{
    public static partial class ExecFunction
    {
        public const string CommandName = "execfunc";
        public static int UnhandledExceptionExitCode = 128 + 6; // SIGABRT exit code

        public static bool IsExecFunctionCommand(string[] args)
            => args.Length >= 1 && args[0] == CommandName;

        /// <summary>
        /// Provides an entry point in a new process that will load a specified method and invoke it.
        /// </summary>
        public static class Program
        {
            public static int Main(string[] args)
            {
                int argsLength = args.Length;
                int argIdx = 0;
                // Strip CommandName.
                if (argsLength > 0 && args[0] == CommandName)
                {
                    argsLength--;
                    argIdx++;
                }

                // The program expects to be passed the target assembly name to load, the type
                // from that assembly to find, and the method from that assembly to invoke.
                // Any additional arguments are passed as strings to the method.
                if (argsLength < 3)
                {
                    Console.Error.WriteLine("Usage: {0} assemblyName typeName methodName [additionalArgs]", typeof(Program).GetTypeInfo().Assembly.GetName().Name);
                    Environment.Exit(-1);
                    return -1;
                }

                string assemblyName = args[argIdx++];
                string typeName = args[argIdx++];
                string methodName = args[argIdx++];
                string[] additionalArgs = args.SubArray(3);
                bool enableDebuggerAttach = bool.Parse(args[argIdx++]);
                string parentProcessIdStr = args[argIdx++];

                if (enableDebuggerAttach)
                {
                    int parentProcessId = int.Parse(parentProcessIdStr);
                    DebuggerAttacher.TryAttach(parentProcessId);
                }

                // Load the specified assembly, type, and method, then invoke the method.
                // The program's exit code is the return value of the invoked method.
                Assembly a = null;
                Type t = null;
                MethodInfo mi = null;
                object instance = null;
                int exitCode = 0;
                try
                {
                    // Create the class if necessary
                    a = Assembly.Load(new AssemblyName(assemblyName));
                    t = a.GetType(typeName);
                    mi = t.GetTypeInfo().GetDeclaredMethod(methodName);
                    if (!mi.IsStatic)
                    {
                        instance = Activator.CreateInstance(t);
                    }

                    // Invoke the method
                    object result;
                    if (mi.GetParameters().Length == 0)
                    {
                        result = mi.Invoke(instance, null);
                    }
                    else
                    {
                        result = mi.Invoke(instance, new object[] { additionalArgs });
                    }

                    if (result is Task<int> task)
                    {
                        exitCode = task.GetAwaiter().GetResult();
                    }
                    else if (result is int exit)
                    {
                        exitCode = exit;
                    }
                }
                catch (Exception exc)
                {
                    if (exc is TargetInvocationException && exc.InnerException != null)
                        exc = exc.InnerException;

                    Console.Error.Write("Unhandled exception: ");
                    Console.Error.WriteLine(exc);

                    exitCode = UnhandledExceptionExitCode;
                }
                finally
                {
                    (instance as IDisposable)?.Dispose();
                }

                return exitCode;
            }
        }

        private static T[] SubArray<T>(this T[] data, int index)
        {
            int length = data.Length - index;
            if (length == 0)
            {
                return Array.Empty<T>();
            }
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
    }
}
