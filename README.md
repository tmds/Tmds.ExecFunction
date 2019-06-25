# Tmds.ExecFunction

Tmds.ExecFunction is a library that makes it simple to execute a function in a separate process.
This can be interesting for writing tests that require a separate process, or running some code with a different lifetime as the .NET application process.
The library is based on the corefx RemoteExecutorTestBase class.

# Supported platforms

This library supports .NET Core 2.0+ on Windows and Linux.
