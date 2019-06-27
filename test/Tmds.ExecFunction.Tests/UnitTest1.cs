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
            FunctionExecutor.Run(
                (string[] args) => 
                {
                    Assert.Equal("arg1", args[0]);
                    Assert.Equal("arg2", args[1]);
                },
                new string[] { "arg1", "arg2" }
            );
        }

        private FunctionExecutor FunctionExecutor = new FunctionExecutor(
            o =>
            {
                o.StartInfo.RedirectStandardError = true;
                o.OnExit = p =>
                {
                    if (p.ExitCode != 0)
                    {
                        string message = $"Function exit code failed with exit code: {p.ExitCode}" + Environment.NewLine +
                                         p.StandardError.ReadToEnd();
                        throw new Xunit.Sdk.XunitException(message);
                    }
                };
            }
        );
    }
}
