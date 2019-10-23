using System;
using System.Diagnostics;
using Xunit;
using Tmds.Utils;
using System.Threading.Tasks;

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

        [Fact]
        public async Task RunAsync()
        {
            int? exitCode = null;

            await FunctionExecutor.RunAsync(
                () => 42,
                o => o.OnExit = process => exitCode = process.ExitCode);

            Assert.Equal(42, exitCode);
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

        [Fact]
        public async Task GatherConsoleOut()
        {
            string outText = null;
            FunctionExecutor FunctionExecutor = new FunctionExecutor(
                o =>
                {
                    o.StartInfo.RedirectStandardOutput= true;
                    o.OnExit = p =>
                    {
                        outText = p.StandardOutput.ReadToEnd();
                    };
                }
            );

            await FunctionExecutor.RunAsync(
                () => { Console.Write("hello world"); return 0; });

            Assert.Equal("hello world", outText);
        }

    }
}
