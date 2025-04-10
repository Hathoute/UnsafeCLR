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
        OriginalStaticMethods.Action();
        ReplacingStaticMethods.Action();
        
        OriginalMock.Verify(m => m.Action(), Times.Once);
        ReplacingMock.Verify(m => m.Action(), Times.Once);
    }

    [Fact]
    public void TestReplaceActionMethod() {
        using (CLRHelper.ReplaceStaticMethod(TestHelpers.GetMethodInfo(OriginalStaticMethods.Action), TestHelpers.GetMethodInfo(ReplacingStaticMethods.Action))) {
            OriginalStaticMethods.Action();
            ReplacingStaticMethods.Action();
        }
        
        OriginalMock.Verify(m => m.Action(), Times.Never);
        ReplacingMock.Verify(m => m.Action(), Times.Exactly(2));
    }

    [Fact]
    public void TestReplaceCallOriginalMethod() {
        using (var replacement = CLRHelper.ReplaceStaticMethod(TestHelpers.GetMethodInfo(OriginalStaticMethods.Action), TestHelpers.GetMethodInfo(ReplacingStaticMethods.Action))) {
            replacement.OriginalMethod.Invoke(null, null);
        }
        
        OriginalMock.Verify(m => m.Action(), Times.Once);
        ReplacingMock.Verify(m => m.Action(), Times.Never);
    }

    [Fact]
    public void TestReplaceActionWithPrimitive() {
        using (var replacement = CLRHelper.ReplaceStaticMethod(TestHelpers.GetMethodInfo<int>(OriginalStaticMethods.ActionWithPrimitive), TestHelpers.GetMethodInfo<int>(ReplacingStaticMethods.ActionWithPrimitive))) {
            OriginalStaticMethods.ActionWithPrimitive(1);
            ReplacingStaticMethods.ActionWithPrimitive(2);
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
        
        using (var replacement = CLRHelper.ReplaceStaticMethod(TestHelpers.GetMethodInfo<object>(OriginalStaticMethods.ActionWithObject), TestHelpers.GetMethodInfo<object>(ReplacingStaticMethods.ActionWithObject))) {
            OriginalStaticMethods.ActionWithObject(param1);
            ReplacingStaticMethods.ActionWithObject(param2);
            replacement.OriginalMethod.Invoke(null, new object[] { param3 });
        }
        
        ReplacingMock.Verify(m => m.ActionWithObject(param1));
        ReplacingMock.Verify(m => m.ActionWithObject(param2));
        OriginalMock.Verify(m => m.ActionWithObject(param3));
    }
    
    [Fact]
    public void TestReplaceActionWithParameters() {
        using (var replacement = CLRHelper.ReplaceStaticMethod(TestHelpers.GetMethodInfo<int, string>(OriginalStaticMethods.ActionWithParameters), TestHelpers.GetMethodInfo<int, string>(ReplacingStaticMethods.ActionWithParameters))) {
            OriginalStaticMethods.ActionWithParameters(1, "original");
            ReplacingStaticMethods.ActionWithParameters(2, "replacement");
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
        
        using (var replacement = CLRHelper.ReplaceStaticMethod(TestHelpers.GetMethodInfo(OriginalStaticMethods.Func), TestHelpers.GetMethodInfo(ReplacingStaticMethods.Func))) {
            Assert.Equal("replacing", OriginalStaticMethods.Func());
            Assert.Equal("replacing", ReplacingStaticMethods.Func());
            Assert.Equal("original", replacement.OriginalMethod.Invoke(null, null));
        }
        
        ReplacingMock.Verify(m => m.Func(), Times.Exactly(2));
        OriginalMock.Verify(m => m.Func(), Times.Once);
    }

    [Fact]
    public void TestReplaceFuncWithPrimitive() {
        OriginalMock.Setup(m => m.FuncWithPrimitive(It.IsAny<int>())).Returns(1);
        ReplacingMock.Setup(m => m.FuncWithPrimitive(It.IsAny<int>())).Returns(2);
        
        using (var replacement = CLRHelper.ReplaceStaticMethod(TestHelpers.GetMethodInfo<int, int>(OriginalStaticMethods.FuncWithPrimitive), TestHelpers.GetMethodInfo<int, int>(ReplacingStaticMethods.FuncWithPrimitive))) {
            Assert.Equal(2, OriginalStaticMethods.FuncWithPrimitive(1));
            Assert.Equal(2, ReplacingStaticMethods.FuncWithPrimitive(2));
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
        
        using (var replacement = CLRHelper.ReplaceStaticMethod(TestHelpers.GetMethodInfo<object, string>(OriginalStaticMethods.FuncWithObject), TestHelpers.GetMethodInfo<object, string>(ReplacingStaticMethods.FuncWithObject))) {
            Assert.Equal("replacing", OriginalStaticMethods.FuncWithObject(param1));
            Assert.Equal("replacing", ReplacingStaticMethods.FuncWithObject(param2));
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
        
        using (var replacement = CLRHelper.ReplaceStaticMethod(TestHelpers.GetMethodInfo<int, string, string>(OriginalStaticMethods.FuncWithParameters), TestHelpers.GetMethodInfo<int, string, string>(ReplacingStaticMethods.FuncWithParameters))) {
            Assert.Equal("replacing", OriginalStaticMethods.FuncWithParameters(1, "original"));
            Assert.Equal("replacing", ReplacingStaticMethods.FuncWithParameters(2, "replacement"));
            Assert.Equal("original", replacement.OriginalMethod.Invoke(null, new object[] { 3, "invoke" }));
        }
        
        ReplacingMock.Verify(m => m.FuncWithParameters(1, "original"));
        ReplacingMock.Verify(m => m.FuncWithParameters(2, "replacement"));
        OriginalMock.Verify(m => m.FuncWithParameters(3, "invoke"));
    }
    
        
    private static class OriginalStaticMethods {
        
        private static IMethods _delegate => OriginalMock.Object;
        
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


    private static class ReplacingStaticMethods {
        
        private static IMethods _delegate => ReplacingMock.Object;
        
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
}
