using System.Reflection;
using UnsafeCLR.CLR.Method;
using UnsafeCLR.CLR.Shared;

namespace UnsafeCLR.CLR;

internal static class CLRProvider {

    internal static IMethodDesc OfMethod(RuntimeMethodHandle methodHandle) {
        var version = Environment.Version;
        IConstantProvider constantProvider = version.Major switch {
            6 or 7 or 8 => new DotNet8.Constants(),
            9 or 10 => new DotNet9.Constants(),
            _ => throw new NotImplementedException($"dotnet version {version.Major}.{version.Minor}.{version.Build} is not supported.")
        };

        return new CLR_MethodDesc(constantProvider, methodHandle.Value);
    }
}