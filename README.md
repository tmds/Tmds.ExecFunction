[![Travis](https://api.travis-ci.org/tmds/Tmds.ExecFunction.svg?branch=master)](https://travis-ci.org/tmds/Tmds.ExecFunction)
[![NuGet](https://img.shields.io/nuget/v/Tmds.ExecFunction.svg)](https://www.nuget.org/packages/Tmds.ExecFunction)

# Tmds.ExecFunction

Tmds.ExecFunction is a library that makes it simple to execute a function in a separate process.
This can be interesting for writing tests that require a separate process, or running some code with a different lifetime as the .NET application process.
The library is based on the corefx RemoteExecutorTestBase class.

# Supported platforms

This library supports .NET Core 2.0+ on Windows and Linux.

# Usage

The main method of the library is `ExecFunction.Start`. It accepts a delegate that is the function to execute in the remote process. The function can have the same signature of a .NET `Main`: a `void`/`string[]` argument, and a `void`/`int`/`Task`/`Task<int>` return type.

The method returns the started process as a `System.Diagnostics.Process`.

For example:
```cs
using (Process p = ExecFunction.Start(() => Console.WriteLine("Hello from child process!")))
{
    p.WaitForExit();
}
```

The `ProcessStartInfo` that is used to start the process can be configured, by adding a configuration delegate:
```cs
ExecFunction.Start(..., psi => psi.RedirectStandardOutput = true);
```

When `ExecFunction` is used from the `dotnet` host, it will work out-of-the box.
To make `ExecFunction` work from an application host (that is, when you've published your application as a native binary),
you need to add a hook in the main function:

```cs
static int Main(string[] args)
{
    if (ExecFunction.IsExecFunctionCommand(args))
    {
        return ExecFunction.Program.Main(args);
    }
    else
    {
        ExecFunction.Start(() => System.Console.WriteLine("Hello world!")).WaitForExit();
        return 0;
    }
}
```

# NuGet feed

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="tmds" value="https://www.myget.org/F/tmds/api/v3/index.json" />
  </packageSources>
</configuration>
```