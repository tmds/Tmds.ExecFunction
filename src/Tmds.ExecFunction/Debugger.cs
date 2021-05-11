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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Tmds.Utils
{

    /// <summary>
    /// A wrapper for a <see cref="Process"/>.
    /// </summary>
    public abstract class Debugger
    {
        #region Visual Studio Debugger Support

        /// <summary>
        /// The environment variable name to use when finding the correct DTE version to use for attaching the debugger.
        /// </summary>
        public const string VsDteEnvironmentVariableName = "__VsDteEnvironmentVariableName__";

        /// <summary>
        /// Where the VsBugger.exe file is located.
        /// </summary>
        protected static string VsDebuggerLocation { get; }

        /// <summary>
        /// The Visual Studio DTE version to use for debugging.
        /// </summary>
        protected static string DteVersion { get; }

#pragma warning disable 1591
#pragma warning disable IDE1006 // Naming Styles
        // ReSharper disable InconsistentNaming
        // ReSharper disable StringLiteralTypo
        // ReSharper disable UnusedMember.Local
        [DllImport("ole32.dll")]
        internal static extern int CLSIDFromProgID([MarshalAs(UnmanagedType.LPWStr)] string lpszProgID, out Guid pclsid);
#pragma warning restore 1591
#pragma warning restore IDE1006 // Naming Styles
        // ReSharper restore InconsistentNaming
        // ReSharper restore StringLiteralTypo
        // ReSharper restore UnusedMember.Local

        internal static bool TryToAttachDebugger(int pid)
        {
            if (!DebuggingSupported)
            {
                Debug.WriteLine("No Debugger Found");
                return false;
            }

            try
            {
                using (var command = Process.Start(new ProcessStartInfo(VsDebuggerLocation, $"-pid:{pid} -ppid:{Process.GetCurrentProcess().Id} -dtev:{DteVersion} -debugger:attach")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                }))
                {
                    // ReSharper disable once PossibleNullReferenceException
                    command.WaitForExit(30000);//wait for 30 seconds to attach the debugger

                    if (!command.HasExited)
                    {
                        command.Kill();
                        throw new TimeoutException("Timed out when trying to attach debugger to process.");
                    }

                    var output = command.StandardOutput.ReadToEnd();

                    if (command.ExitCode != 1)
                    {
                        throw new Exception(output);
                    }
                }

                return true;
            }
            catch (Exception error)
            {
                Debug.WriteLine($"Unable to attach debugger to process ({pid}): {error}");

                return false;
            }
        }

        internal static void DetachDebugger(int pid)
        {
            try
            {
                using (var command = Process.Start(new ProcessStartInfo(VsDebuggerLocation, $"-pid:{pid} -dtev:{DteVersion} -debugger:detach")
                {
                    RedirectStandardOutput = true
                }))
                {
                    // ReSharper disable once PossibleNullReferenceException
                    command.WaitForExit(30000);//wait for 30 seconds to attach the debugger

                    if (!command.HasExited)
                    {
                        command.Kill();
                        throw new TimeoutException("Timed out when trying to attach debugger to process.");
                    }

                    if (command.ExitCode != 1)
                    {
                        throw new Exception(command.StandardOutput.ReadToEnd());
                    }
                }
            }
            catch (Exception error)
            {
                Debug.WriteLine($"Unable to detach debugger to process ({pid}): {error}");
            }
        }

        #endregion

        static Debugger()
        {
            try
            {
                // this path assumes we are in a nuget package
                VsDebuggerLocation = Path.Combine(Path.GetDirectoryName(typeof(Debugger).Assembly.Location) ?? "", "..", "..", "contentFiles", "VsDebugger", "VsDebugger.exe");

                if (!File.Exists(VsDebuggerLocation))
                {
                    VsDebuggerLocation = null;
                    return;
                }

                /* The DTE version for Visual Studio is needed to attach the debugger to a child process.
                 * The best way to find this information is to use the VsWhere utility developed by Microsoft.
                 *
                 * The DTE version can be set manually using an environment variable. This will be useful for
                 * dev machines that have multiple versions of Visual Studio installed.
                 *
                 * This means that attaching the debugger to a child process is only supported on Windows.
                 */
                DteVersion = Environment.GetEnvironmentVariable(VsDteEnvironmentVariableName);

                if (string.IsNullOrEmpty(DteVersion) || CLSIDFromProgID(DteVersion, out var classId) == 0 && classId != Guid.Empty)
                {
                    var programFiles = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
                    var vsWherePath = Path.Combine(programFiles, "Microsoft Visual Studio\\Installer\\vswhere.exe");
                    if (!File.Exists(vsWherePath))
                    {
                        throw new Exception($"Unable to Find vswhere. Enure that it is installed.");
                    }
                    else
                    {
                        using (var process = new Process())
                        {
                            process.StartInfo.UseShellExecute = false;
                            process.StartInfo.FileName = vsWherePath;
                            process.StartInfo.RedirectStandardOutput = true;
                            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                            process.Start();

                            var output = process.StandardOutput.ReadToEnd();

                            process.WaitForExit();

                            using (var reader = new StringReader(output))
                            {
                                var regex = new Regex(@"^\s*installationVersion\s*:\s*(?<major>\d+)(\.\d+)*$", RegexOptions.IgnoreCase);
                                while (reader.Peek() > -1)
                                {
                                    var match = regex.Match(reader.ReadLine() ?? "");
                                    if (match.Success)
                                    {
                                        DteVersion = $"{match.Groups["major"].Value}.0";
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                //confirm the dte version is valid
                if (CLSIDFromProgID($"VisualStudio.DTE.{DteVersion}", out classId) != 0 || classId == Guid.Empty)
                {
                    throw new Exception($"Unable to Find DTE Version \"VisualStudio.DTE.{DteVersion}\". Try setting the environment variable \"{VsDteEnvironmentVariableName}\" to the correct value for this machine.");
                }

                DebuggingSupported = true;
            }
            catch (Exception error)
            {
                DteVersion = null;
                DebuggingSupported = false;

                Debug.WriteLine($"Unable to find debugger: {error}");
            }
        }

        internal Debugger()
        {
        }

        /// <summary>
        /// The process for the domain.
        /// </summary>
        public abstract Process Process { get; }

        /// <summary>
        /// True if debugging child processes is supported.
        /// </summary>
        public static bool DebuggingSupported { get; }

    }

}
