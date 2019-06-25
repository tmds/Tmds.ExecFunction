using System;
using System.Diagnostics;
using Xunit;
using Tmds.Utils;

namespace Tmds.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void TestArgVoidReturnInt()
        {
            using (Process p = ExecFunction.Start(() => 42))
            {
                p.WaitForExit();
                Assert.Equal(42, p.ExitCode);
            }
        }

        [Fact]
        public void TestArgStringArrayReturnVoid()
        {
            using (Process p = ExecFunction.Start((string[] args) => 
            {
                Assert.Equal("arg1", args[0]);
                Assert.Equal("arg2", args[1]);
            },
                new string[] { "arg1", "arg2" },
                ExecFunction.RedirectStdio))
            {
                p.WaitForExit();
                Assert.True(p.ExitCode == 0, p.StandardError.ReadToEnd());
            }
        }
    }
}
