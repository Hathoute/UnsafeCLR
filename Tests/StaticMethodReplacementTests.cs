using Moq;
using UnsafeCLR;

namespace Tests;

public class StaticMethodReplacementTests : IDisposable {

    internal static Mock<IMethods> OriginalMock;
    internal static Mock<IMethods> ReplacingMock;


    public StaticMethodReplacementTests() {
        OriginalMock = new Mock<IMethods>();
        ReplacingMock = new Mock<IMethods>();
    }

    // teardown
    public void Dispose() {
        OriginalMock.VerifyNoOtherCalls();
        ReplacingMock.VerifyNoOtherCalls();
        GC.SuppressFinalize(this);
    }
    
    [Fact]
    public void TestMethodCalls() {
        OriginalMethods.Action();
        ReplacingMethods.Action();
        
        OriginalMock.Verify(m => m.Action(), Times.Once);
        ReplacingMock.Verify(m => m.Action(), Times.Once);
    }

    [Fact]
    public void TestReplaceActionMethod() {
        using (CLRHelper.ReplaceStaticMethod(TestHelpers.GetMethodInfo(OriginalMethods.Action), TestHelpers.GetMethodInfo(ReplacingMethods.Action))) {
            OriginalMethods.Action();
            ReplacingMethods.Action();
        }
        
        OriginalMock.Verify(m => m.Action(), Times.Never);
        ReplacingMock.Verify(m => m.Action(), Times.Exactly(2));
    }

    [Fact]
    public void TestReplaceCallOriginalMethod() {
        using (var replacement = CLRHelper.ReplaceStaticMethod(TestHelpers.GetMethodInfo(OriginalMethods.Action), TestHelpers.GetMethodInfo(ReplacingMethods.Action))) {
            replacement.OriginalMethod.Invoke(null, null);
        }
        
        OriginalMock.Verify(m => m.Action(), Times.Once);
        ReplacingMock.Verify(m => m.Action(), Times.Never);
    }

    [Fact]
    public void TestReplaceActionWithPrimitive() {
        using (var replacement = CLRHelper.ReplaceStaticMethod(TestHelpers.GetMethodInfo<int>(OriginalMethods.ActionWithPrimitive), TestHelpers.GetMethodInfo<int>(ReplacingMethods.ActionWithPrimitive))) {
            OriginalMethods.ActionWithPrimitive(1);
            ReplacingMethods.ActionWithPrimitive(2);
            replacement.OriginalMethod.Invoke(null, new object[] { 3 });
        }
        
        ReplacingMock.Verify(m => m.ActionWithPrimitive(1));
        ReplacingMock.Verify(m => m.ActionWithPrimitive(2));
        OriginalMock.Verify(m => m.ActionWithPrimitive(3));
    }

    [Fact]
    public void TestReplaceActionWithObject() {
        var param1 = new RandomNumberObject();
        var param2 = new RandomNumberObject();
        var param3 = new RandomNumberObject();
        
        using (var replacement = CLRHelper.ReplaceStaticMethod(TestHelpers.GetMethodInfo<object>(OriginalMethods.ActionWithObject), TestHelpers.GetMethodInfo<object>(ReplacingMethods.ActionWithObject))) {
            OriginalMethods.ActionWithObject(param1);
            ReplacingMethods.ActionWithObject(param2);
            replacement.OriginalMethod.Invoke(null, new object[] { param3 });
        }
        
        ReplacingMock.Verify(m => m.ActionWithObject(param1));
        ReplacingMock.Verify(m => m.ActionWithObject(param2));
        OriginalMock.Verify(m => m.ActionWithObject(param3));
    }
    
    [Fact]
    public void TestReplaceActionWithParameters() {
        using (var replacement = CLRHelper.ReplaceStaticMethod(TestHelpers.GetMethodInfo<int, string>(OriginalMethods.ActionWithParameters), TestHelpers.GetMethodInfo<int, string>(ReplacingMethods.ActionWithParameters))) {
            OriginalMethods.ActionWithParameters(1, "original");
            ReplacingMethods.ActionWithParameters(2, "replacement");
            replacement.OriginalMethod.Invoke(null, new object[] { 3, "invoke" });
        }
        
        ReplacingMock.Verify(m => m.ActionWithParameters(1, "original"));
        ReplacingMock.Verify(m => m.ActionWithParameters(2, "replacement"));
        OriginalMock.Verify(m => m.ActionWithParameters(3, "invoke"));
    }

    [Fact]
    public void TestReplaceFunc() {
        OriginalMock.Setup(m => m.Func()).Returns("original");
        ReplacingMock.Setup(m => m.Func()).Returns("replacing");
        
        using (var replacement = CLRHelper.ReplaceStaticMethod(TestHelpers.GetMethodInfo(OriginalMethods.Func), TestHelpers.GetMethodInfo(ReplacingMethods.Func))) {
            Assert.Equal("replacing", OriginalMethods.Func());
            Assert.Equal("replacing", ReplacingMethods.Func());
            Assert.Equal("original", replacement.OriginalMethod.Invoke(null, null));
        }
        
        ReplacingMock.Verify(m => m.Func(), Times.Exactly(2));
        OriginalMock.Verify(m => m.Func(), Times.Once);
    }

    [Fact]
    public void TestReplaceFuncWithPrimitive() {
        OriginalMock.Setup(m => m.FuncWithPrimitive(It.IsAny<int>())).Returns(1);
        ReplacingMock.Setup(m => m.FuncWithPrimitive(It.IsAny<int>())).Returns(2);
        
        using (var replacement = CLRHelper.ReplaceStaticMethod(TestHelpers.GetMethodInfo<int, int>(OriginalMethods.FuncWithPrimitive), TestHelpers.GetMethodInfo<int, int>(ReplacingMethods.FuncWithPrimitive))) {
            Assert.Equal(2, OriginalMethods.FuncWithPrimitive(1));
            Assert.Equal(2, ReplacingMethods.FuncWithPrimitive(2));
            Assert.Equal(1, replacement.OriginalMethod.Invoke(null, new object[] { 3 }));
        }
        
        ReplacingMock.Verify(m => m.FuncWithPrimitive(1));
        ReplacingMock.Verify(m => m.FuncWithPrimitive(2));
        OriginalMock.Verify(m => m.FuncWithPrimitive(3));
    }

    [Fact]
    public void TestReplaceFuncWithObject() {
        var param1 = new RandomNumberObject();
        var param2 = new RandomNumberObject();
        var param3 = new RandomNumberObject();
        
        OriginalMock.Setup(m => m.FuncWithObject(It.IsAny<object>())).Returns("original");
        ReplacingMock.Setup(m => m.FuncWithObject(It.IsAny<object>())).Returns("replacing");
        
        using (var replacement = CLRHelper.ReplaceStaticMethod(TestHelpers.GetMethodInfo<object, string>(OriginalMethods.FuncWithObject), TestHelpers.GetMethodInfo<object, string>(ReplacingMethods.FuncWithObject))) {
            Assert.Equal("replacing", OriginalMethods.FuncWithObject(param1));
            Assert.Equal("replacing", ReplacingMethods.FuncWithObject(param2));
            Assert.Equal("original", replacement.OriginalMethod.Invoke(null, new object[] { param3 }));
        }
        
        ReplacingMock.Verify(m => m.FuncWithObject(param1));
        ReplacingMock.Verify(m => m.FuncWithObject(param2));
        OriginalMock.Verify(m => m.FuncWithObject(param3));
    }
    
    [Fact]
    public void TestReplaceFuncWithParameters() {
        OriginalMock.Setup(m => m.FuncWithParameters(It.IsAny<int>(), It.IsAny<string>())).Returns("original");
        ReplacingMock.Setup(m => m.FuncWithParameters(It.IsAny<int>(), It.IsAny<string>())).Returns("replacing");
        
        using (var replacement = CLRHelper.ReplaceStaticMethod(TestHelpers.GetMethodInfo<int, string, string>(OriginalMethods.FuncWithParameters), TestHelpers.GetMethodInfo<int, string, string>(ReplacingMethods.FuncWithParameters))) {
            Assert.Equal("replacing", OriginalMethods.FuncWithParameters(1, "original"));
            Assert.Equal("replacing", ReplacingMethods.FuncWithParameters(2, "replacement"));
            Assert.Equal("original", replacement.OriginalMethod.Invoke(null, new object[] { 3, "invoke" }));
        }
        
        ReplacingMock.Verify(m => m.FuncWithParameters(1, "original"));
        ReplacingMock.Verify(m => m.FuncWithParameters(2, "replacement"));
        OriginalMock.Verify(m => m.FuncWithParameters(3, "invoke"));
    }
}

public interface IMethods {
    void Action();
    void ActionWithPrimitive(int param1);
    void ActionWithObject(object param1);
    void ActionWithParameters(int param1, string param2);

    string Func();
    int FuncWithPrimitive(int param1);
    string FuncWithObject(object param1);
    string FuncWithParameters(int param1, string param2);
}

public static class OriginalMethods {
    
    private static IMethods _delegate => StaticMethodReplacementTests.OriginalMock.Object;
    
    public static void Action() {
        _delegate.Action();
    }

    public static void ActionWithPrimitive(int param1) {
        _delegate.ActionWithPrimitive(param1);
    }

    public static void ActionWithObject(object param1) {
        _delegate.ActionWithObject(param1);
    }

    public static void ActionWithParameters(int param1, string param2) {
        _delegate.ActionWithParameters(param1, param2);
    }

    public static string Func() {
        return _delegate.Func();
    }

    public static int FuncWithPrimitive(int param1) {
        return _delegate.FuncWithPrimitive(param1);
    }

    public static string FuncWithObject(object param1) {
        return _delegate.FuncWithObject(param1);
    }

    public static string FuncWithParameters(int param1, string param2) {
        return _delegate.FuncWithParameters(param1, param2);
    }
}


public static class ReplacingMethods {
    
    private static IMethods _delegate => StaticMethodReplacementTests.ReplacingMock.Object;
    
    public static void Action() {
        _delegate.Action();
    }

    public static void ActionWithPrimitive(int param1) {
        _delegate.ActionWithPrimitive(param1);
    }

    public static void ActionWithObject(object param1) {
        _delegate.ActionWithObject(param1);
    }

    public static void ActionWithParameters(int param1, string param2) {
        _delegate.ActionWithParameters(param1, param2);
    }

    public static string Func() {
        return _delegate.Func();
    }

    public static int FuncWithPrimitive(int param1) {
        return _delegate.FuncWithPrimitive(param1);
    }

    public static string FuncWithObject(object param1) {
        return _delegate.FuncWithObject(param1);
    }

    public static string FuncWithParameters(int param1, string param2) {
        return _delegate.FuncWithParameters(param1, param2);
    }
}