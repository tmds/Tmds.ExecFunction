using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Tmds.Utils
{
    public static class DebuggerAttacher
    {
        public const string VsDebuggerLibraryFileName = "Tmds.ExecFunction.VsDebugger.dll";

        public static void TryAttach(int targetProcessId)
        {
            if (Debugger.IsAttached
                || Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                return;
            }
            string dllPath = Path.Combine(Environment.CurrentDirectory, VsDebuggerLibraryFileName);

            if (!File.Exists(dllPath))
            {
                return;
            }

            Assembly assembly = Assembly.LoadFile(dllPath);

            MethodInfo attachMethod = assembly.GetType("System.DebuggerAttachHelper", false, true)
                                              ?.GetMethod("AttachTo", BindingFlags.Public | BindingFlags.Static);

            attachMethod?.Invoke(null, new object[] { targetProcessId });
        }
    }
}