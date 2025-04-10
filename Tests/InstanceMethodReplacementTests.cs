using Moq;
using UnsafeCLR;

namespace Tests;

public class InstanceMethodReplacementTests : IDisposable {

    private static Mock<IMethods> _originalMock;
    private static Mock<IMethods> _replacingMock;
    private static OriginalInstanceMethods _originalInstance;


    public InstanceMethodReplacementTests() {
        _originalMock = new Mock<IMethods>();
        _replacingMock = new Mock<IMethods>();
        _originalInstance = new OriginalInstanceMethods(_originalMock.Object);
    }

    // teardown
    public void Dispose() {
        _originalMock.VerifyNoOtherCalls();
        _replacingMock.VerifyNoOtherCalls();
        GC.SuppressFinalize(this);
    }
    
    [Fact]
    public void TestMethodCalls() {
        _originalInstance.Action();
        ReplacingMethods.Action(_originalInstance);
        
        _originalMock.Verify(m => m.Action(), Times.Once);
        _replacingMock.Verify(m => m.Action(), Times.Once);
    }

    [Fact]
    public void TestReplaceActionMethod() {
        using (CLRHelper.ReplaceInstanceMethod(typeof(OriginalInstanceMethods), TestHelpers.GetMethodInfo(_originalInstance.Action), TestHelpers.GetMethodInfo<OriginalInstanceMethods>(ReplacingMethods.Action))) {
            _originalInstance.Action();
            ReplacingMethods.Action(_originalInstance);
        }
        
        _originalMock.Verify(m => m.Action(), Times.Never);
        _replacingMock.Verify(m => m.Action(), Times.Exactly(2));
    }
    
    [Fact]
    public void TestReplaceCallOriginalMethod() {
        using (var replacement = CLRHelper.ReplaceInstanceMethod(typeof(OriginalInstanceMethods), TestHelpers.GetMethodInfo(_originalInstance.Action), TestHelpers.GetMethodInfo<OriginalInstanceMethods>(ReplacingMethods.Action))) {
            replacement.OriginalMethod.Invoke(null, new object[]{ _originalInstance });
        }
        
        _originalMock.Verify(m => m.Action(), Times.Once);
        _replacingMock.Verify(m => m.Action(), Times.Never);
    }

    [Fact]
    public void TestReplaceActionWithPrimitive() {
        using (var replacement = CLRHelper.ReplaceInstanceMethod(typeof(OriginalInstanceMethods), TestHelpers.GetMethodInfo<int>(_originalInstance.ActionWithPrimitive), TestHelpers.GetMethodInfo<OriginalInstanceMethods, int>(ReplacingMethods.ActionWithPrimitive))) {
            _originalInstance.ActionWithPrimitive(1);
            ReplacingMethods.ActionWithPrimitive(_originalInstance, 2);
            replacement.OriginalMethod.Invoke(null, new object[] { _originalInstance, 3 });
        }
        
        _replacingMock.Verify(m => m.ActionWithPrimitive(1));
        _replacingMock.Verify(m => m.ActionWithPrimitive(2));
        _originalMock.Verify(m => m.ActionWithPrimitive(3));
    }

    [Fact]
    public void TestReplaceActionWithObject() {
        var param1 = new RandomNumberObject();
        var param2 = new RandomNumberObject();
        var param3 = new RandomNumberObject();
        
        using (var replacement = CLRHelper.ReplaceInstanceMethod(typeof(OriginalInstanceMethods), TestHelpers.GetMethodInfo<object>(_originalInstance.ActionWithObject), TestHelpers.GetMethodInfo<OriginalInstanceMethods, object>(ReplacingMethods.ActionWithObject))) {
            _originalInstance.ActionWithObject(param1);
            ReplacingMethods.ActionWithObject(_originalInstance, param2);
            replacement.OriginalMethod.Invoke(null, new object[] { _originalInstance, param3 });
        }
        
        _replacingMock.Verify(m => m.ActionWithObject(param1));
        _replacingMock.Verify(m => m.ActionWithObject(param2));
        _originalMock.Verify(m => m.ActionWithObject(param3));
    }
    
    [Fact]
    public void TestReplaceActionWithParameters() {
        using (var replacement = CLRHelper.ReplaceInstanceMethod(typeof(OriginalInstanceMethods), TestHelpers.GetMethodInfo<int, string>(_originalInstance.ActionWithParameters), TestHelpers.GetMethodInfo<OriginalInstanceMethods, int, string>(ReplacingMethods.ActionWithParameters))) {
            _originalInstance.ActionWithParameters(1, "original");
            ReplacingMethods.ActionWithParameters(_originalInstance, 2, "replacement");
            replacement.OriginalMethod.Invoke(null, new object[] { _originalInstance, 3, "invoke" });
        }
        
        _replacingMock.Verify(m => m.ActionWithParameters(1, "original"));
        _replacingMock.Verify(m => m.ActionWithParameters(2, "replacement"));
        _originalMock.Verify(m => m.ActionWithParameters(3, "invoke"));
    }
    
    [Fact]
    public void TestReplaceFunc() {
        _originalMock.Setup(m => m.Func()).Returns("original");
        _replacingMock.Setup(m => m.Func()).Returns("replacing");
        
        using (var replacement = CLRHelper.ReplaceInstanceMethod(typeof(OriginalInstanceMethods),  TestHelpers.GetMethodInfo(_originalInstance.Func), TestHelpers.GetMethodInfo<OriginalInstanceMethods, string>(ReplacingMethods.Func))) {
            Assert.Equal("replacing", _originalInstance.Func());
            Assert.Equal("replacing", ReplacingMethods.Func(_originalInstance));
            Assert.Equal("original", replacement.OriginalMethod.Invoke(null, new object[] { _originalInstance }));
        }
        
        _replacingMock.Verify(m => m.Func(), Times.Exactly(2));
        _originalMock.Verify(m => m.Func(), Times.Once);
    }

    [Fact]
    public void TestReplaceFuncWithPrimitive() {
        _originalMock.Setup(m => m.FuncWithPrimitive(It.IsAny<int>())).Returns(1);
        _replacingMock.Setup(m => m.FuncWithPrimitive(It.IsAny<int>())).Returns(2);
        
        using (var replacement = CLRHelper.ReplaceInstanceMethod(typeof(OriginalInstanceMethods),  TestHelpers.GetMethodInfo<int, int>(_originalInstance.FuncWithPrimitive), TestHelpers.GetMethodInfo<OriginalInstanceMethods, int, int>(ReplacingMethods.FuncWithPrimitive))) {
            Assert.Equal(2, _originalInstance.FuncWithPrimitive(1));
            Assert.Equal(2, ReplacingMethods.FuncWithPrimitive(_originalInstance, 2));
            Assert.Equal(1, replacement.OriginalMethod.Invoke(null, new object[] { _originalInstance, 3 }));
        }
        
        _replacingMock.Verify(m => m.FuncWithPrimitive(1));
        _replacingMock.Verify(m => m.FuncWithPrimitive(2));
        _originalMock.Verify(m => m.FuncWithPrimitive(3));
    }

    [Fact]
    public void TestReplaceFuncWithObject() {
        var param1 = new RandomNumberObject();
        var param2 = new RandomNumberObject();
        var param3 = new RandomNumberObject();
        
        _originalMock.Setup(m => m.FuncWithObject(It.IsAny<object>())).Returns("original");
        _replacingMock.Setup(m => m.FuncWithObject(It.IsAny<object>())).Returns("replacing");
        
        using (var replacement = CLRHelper.ReplaceInstanceMethod(typeof(OriginalInstanceMethods),  TestHelpers.GetMethodInfo<object, string>(_originalInstance.FuncWithObject), TestHelpers.GetMethodInfo<OriginalInstanceMethods, object, string>(ReplacingMethods.FuncWithObject))) {
            Assert.Equal("replacing", _originalInstance.FuncWithObject(param1));
            Assert.Equal("replacing", ReplacingMethods.FuncWithObject(_originalInstance, param2));
            Assert.Equal("original", replacement.OriginalMethod.Invoke(null, new object[] { _originalInstance, param3 }));
        }
        
        _replacingMock.Verify(m => m.FuncWithObject(param1));
        _replacingMock.Verify(m => m.FuncWithObject(param2));
        _originalMock.Verify(m => m.FuncWithObject(param3));
    }
    
    [Fact]
    public void TestReplaceFuncWithParameters() {
        _originalMock.Setup(m => m.FuncWithParameters(It.IsAny<int>(), It.IsAny<string>())).Returns("original");
        _replacingMock.Setup(m => m.FuncWithParameters(It.IsAny<int>(), It.IsAny<string>())).Returns("replacing");
        
        using (var replacement = CLRHelper.ReplaceInstanceMethod(typeof(OriginalInstanceMethods),  TestHelpers.GetMethodInfo<int, string, string>(_originalInstance.FuncWithParameters), TestHelpers.GetMethodInfo<OriginalInstanceMethods, int, string, string>(ReplacingMethods.FuncWithParameters))) {
            Assert.Equal("replacing", _originalInstance.FuncWithParameters(1, "original"));
            Assert.Equal("replacing", ReplacingMethods.FuncWithParameters(_originalInstance, 2, "replacement"));
            Assert.Equal("original", replacement.OriginalMethod.Invoke(null, new object[] { _originalInstance, 3, "invoke" }));
        }
        
        _replacingMock.Verify(m => m.FuncWithParameters(1, "original"));
        _replacingMock.Verify(m => m.FuncWithParameters(2, "replacement"));
        _originalMock.Verify(m => m.FuncWithParameters(3, "invoke"));
    }
    
    private class OriginalInstanceMethods {

        private readonly IMethods _delegate;

        public OriginalInstanceMethods(IMethods @delegate) {
            _delegate = @delegate;
        }


        public void Action() {
            _delegate.Action();
        }

        public void ActionWithPrimitive(int param1) {
            _delegate.ActionWithPrimitive(param1);
        }

        public void ActionWithObject(object param1) {
            _delegate.ActionWithObject(param1);
        }

        public void ActionWithParameters(int param1, string param2) {
            _delegate.ActionWithParameters(param1, param2);
        }

        public string Func() {
            return _delegate.Func();
        }

        public int FuncWithPrimitive(int param1) {
            return _delegate.FuncWithPrimitive(param1);
        }

        public string FuncWithObject(object param1) {
            return _delegate.FuncWithObject(param1);
        }

        public string FuncWithParameters(int param1, string param2) {
            return _delegate.FuncWithParameters(param1, param2);
        }
    }
    
    private static class ReplacingMethods {
        
        private static IMethods @delegate => _replacingMock.Object;

        private static OriginalInstanceMethods originalInstance =>
            InstanceMethodReplacementTests._originalInstance;
        
        public static void Action(OriginalInstanceMethods @this) {
            Assert.Equal(originalInstance, @this);
            @delegate.Action();
        }

        public static void ActionWithPrimitive(OriginalInstanceMethods @this, int param1) {
            Assert.Equal(originalInstance, @this);
            @delegate.ActionWithPrimitive(param1);
        }

        public static void ActionWithObject(OriginalInstanceMethods @this, object param1) {
            Assert.Equal(originalInstance, @this);
            @delegate.ActionWithObject(param1);
        }

        public static void ActionWithParameters(OriginalInstanceMethods @this, int param1, string param2) {
            Assert.Equal(originalInstance, @this);
            @delegate.ActionWithParameters(param1, param2);
        }

        public static string Func(OriginalInstanceMethods @this) {
            Assert.Equal(originalInstance, @this);
            return @delegate.Func();
        }

        public static int FuncWithPrimitive(OriginalInstanceMethods @this, int param1) {
            Assert.Equal(originalInstance, @this);
            return @delegate.FuncWithPrimitive(param1);
        }

        public static string FuncWithObject(OriginalInstanceMethods @this, object param1) {
            Assert.Equal(originalInstance, @this);
            return @delegate.FuncWithObject(param1);
        }

        public static string FuncWithParameters(OriginalInstanceMethods @this, int param1, string param2) {
            Assert.Equal(originalInstance, @this);
            return @delegate.FuncWithParameters(param1, param2);
        }
    }
}