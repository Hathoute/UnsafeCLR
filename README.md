# UnsafeCLR

**UnsafeCLR** is a .NET library that allows for **runtime method replacement** (also known as method hooking or patching) on the Common Language Runtime (CLR). This can be used for advanced scenarios such as mocking, hot-patching, or instrumentation—**without recompilation** or modifying original source code.

> ⚠️ This library uses unsafe and low-level operations. Use it with caution and only when absolutely necessary.


## Features

- Replace **instance** methods at runtime
- Replace **static** methods at runtime
- Minimal and straightforward API
- Compatible with modern .NET runtimes (tested on .NET 6/7/8/9)


## Limitations

- **Inlined methods** will not be replaced, or the replacement could be permanant since the
code of the called method will be baked into the caller. (A workaround is to [disable JIT inlining](https://github.com/steveharter/dotnet_coreclr/blob/master/Documentation/building/viewing-jit-dumps.md#setting-configuration-variables): `COMPlus_JitNoInline=1`)
- **.NET 6** on **ARM64** is not supported.


## Getting Started

### Installation

```bash
dotnet add package UnsafeCLR
```


## API

### `UnsafeCLR.ReplaceInstanceMethod`

```csharp
public static MethodReplacement ReplaceInstanceMethod(
    Type originalInstanceType,
    MethodInfo originalMethod,
    MethodInfo replacingMethod
)
```

- Replaces an **instance method** at runtime.

### `UnsafeCLR.ReplaceStaticMethod`

```csharp
public static MethodReplacement ReplaceStaticMethod(
    MethodInfo originalMethod,
    MethodInfo replacingMethod
)
```

- Replaces a **static method** at runtime.


## Warnings

- This library uses low-level patching and may break with future CLR versions.
- Not suitable for use in high-security or production-critical environments.
- Ensure method signatures match exactly (return types and parameters).


## Example Use Cases

- Mocking static methods
- Hot-fixing logic in running apps (debugging/instrumentation)


### Example: Replace an Instance Method

```csharp
using UnsafeCLR;
using System.Reflection;

class Program {
    static MethodInfo GetMethodInfo<T, TRes>(Func<T, TRes> fun) => fun.Method;
    static MethodInfo GetMethodInfo<T1, T2, TRes>(Func<T1, T2, TRes> fun) => fun.Method;
    
    public class MyClass {
        public int MyFunc(int param1) {
            // original code
            return param1 + param1;
        }
    }

    public static int MyReplacement(MyClass instance, int param1) {
        // replacing code;
        return param1 * param1;
    }

    public static void Main() {
        var instance = new MyClass();
        using (var replacement = CLRHelper.ReplaceInstanceMethod(typeof(MyClass), GetMethodInfo<int, int>(instance.MyFunc), GetMethodInfo<MyClass, int, int>(MyReplacement))) {
            Console.WriteLine("Inside 'using'");
            Console.WriteLine($"instance.MyFunc = {instance.MyFunc(10)}");
            Console.WriteLine($"MyReplacement = {MyReplacement(instance, 10)}");
            Console.WriteLine($"OriginalMethod.Invoke = { (int) replacement.OriginalMethod.Invoke(null, new object[] { instance, 10 }) }");
        }
        Console.WriteLine("Outside 'using'");
        Console.WriteLine($"instance.MyFunc = {instance.MyFunc(10)}");
        Console.WriteLine($"MyReplacement = {MyReplacement(instance, 10)}");
    }
}
```


## License

See [LICENSE.md](LICENSE.md)

