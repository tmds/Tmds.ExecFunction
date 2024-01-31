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
        public void TestArgVoidReturnTaskOfInt()
        {
            using (Process p = ExecFunction.Start(
                async () =>
                {
                    // Yield to make the method return and validate we wait for the async function to complete.
                    await Task.Yield();

                    return 42;
                }
            ))
            {
                p.WaitForExit();
                Assert.Equal(42, p.ExitCode);
            };
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestArgVoidReturnTask(bool throwException)
        {
            using (Process p = ExecFunction.Start(
                async (string[] args) =>
                {
                    // Yield to make the method return and validate we wait for the async function to complete.
                    await Task.Yield();

                    if (args[0] == "true")
                    {
                        throw new Exception();
                    }
                },
                new[] { throwException ? "true" : "false" }
            ))
            {
                p.WaitForExit();
                Assert.Equal(throwException ? 134 : 0, p.ExitCode);
            };
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
