using System.Diagnostics;

namespace System
{
    public static class DebuggerAttachHelper
    {
        public static void AttachTo(int targetProcessId)
        {
            VsDebuggerAttacher.AttachToTargetProcessDebugger(Process.GetCurrentProcess().Id, targetProcessId);
        }
    }
}