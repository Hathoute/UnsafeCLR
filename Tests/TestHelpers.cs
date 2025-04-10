using System.Reflection;

namespace Tests;

class TestHelpers {
    internal static MethodInfo GetMethodInfo(Action fun) => fun.Method;
    internal static MethodInfo GetMethodInfo<T>(Action<T> fun) => fun.Method;
    internal static MethodInfo GetMethodInfo<T1, T2>(Action<T1, T2> fun) => fun.Method;
    internal static MethodInfo GetMethodInfo<TResult>(Func<TResult> fun) => fun.Method;
    internal static MethodInfo GetMethodInfo<T, TResult>(Func<T, TResult> fun) => fun.Method;
    internal static MethodInfo GetMethodInfo<T1, T2, TResult>(Func<T1, T2, TResult> fun) => fun.Method;
}

internal class RandomNumberObject {
    private static int _nextInstanceNumber;

    internal int Number {
        get;
    }

    internal RandomNumberObject() {
        Number = _nextInstanceNumber++;
    }

    public override bool Equals(object? obj) {
        return obj is RandomNumberObject other && Number == other.Number;
    }

    public override int GetHashCode() {
        return Number;
    }
}