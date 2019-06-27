using System;
using System.Diagnostics;

namespace Tmds.Utils
{
    public class ExecFunctionOptions
    {
        internal ExecFunctionOptions(ProcessStartInfo psi)
        {
            StartInfo = psi;
        }

        public ProcessStartInfo StartInfo { get; }

        public Action<Process> OnExit { get; set; }
    }
}