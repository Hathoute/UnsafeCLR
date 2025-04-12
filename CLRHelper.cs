using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnsafeCLR.Architecture;
using UnsafeCLR.CLR.Method;
using UnsafeCLR.CLR.Type;

namespace UnsafeCLR;

public static class CLRHelper {

    private static readonly IInstructionPatcher InstructionPatcher = RuntimeInformation.ProcessArchitecture switch {
        System.Runtime.InteropServices.Architecture.X64 => new InstructionPatcherX64(),
        System.Runtime.InteropServices.Architecture.Arm64 => new InstructionPatcherArm64(),
        _ => throw new NotImplementedException($"Architecture {RuntimeInformation.ProcessArchitecture} is not supported")
    };

    public static MethodReplacement ReplaceInstanceMethod(Type originalInstanceType, MethodInfo originalMethod, MethodInfo replacingMethod) {
        ArgumentNullException.ThrowIfNull(originalInstanceType);
        ArgumentNullException.ThrowIfNull(originalMethod);
        ArgumentNullException.ThrowIfNull(replacingMethod);
        
        if (originalMethod is not { IsPublic: true, IsStatic: false }) {
            throw new ArgumentException("Original method must be a public non-static method");
        }
        if (replacingMethod is not { IsPublic: true, IsStatic: true }) {
            throw new ArgumentException("Replacing method must be a public static method");
        }

        var dynamicOriginalMethod = CreateDynamicMethod(originalInstanceType, originalMethod);
        ReplaceMethodInternal(dynamicOriginalMethod, originalMethod);
        var replacement = ReplaceMethodInternal(originalMethod, replacingMethod);
        
        return new MethodReplacement(dynamicOriginalMethod, replacement[0], replacement[1], InstructionPatcher);
    }

    public static MethodReplacement ReplaceStaticMethod(MethodInfo originalMethod, MethodInfo replacingMethod) {
        ArgumentNullException.ThrowIfNull(originalMethod);
        ArgumentNullException.ThrowIfNull(replacingMethod);
        
        if (originalMethod is not { IsPublic: true, IsStatic: true } || replacingMethod is not { IsPublic: true, IsStatic: true }) {
            throw new ArgumentException("Both methods must be public static methods");
        }
        
        var dynamicOriginalMethod = CreateDynamicMethod(null, originalMethod);
        ReplaceMethodInternal(dynamicOriginalMethod, originalMethod);
        var replacement = ReplaceMethodInternal(originalMethod, replacingMethod);

        return new MethodReplacement(dynamicOriginalMethod, replacement[0], replacement[1], InstructionPatcher);
    }
    
    private static unsafe IntPtr[] ReplaceMethodInternal(MethodInfo srcMethod, MethodInfo dstMethod) {
        // We skip the first parameter of the dstMethod if the srcMethod is non-static and dstMethod is static,
        // since the first parameter is 'this' instance.
        var dstParams = dstMethod.GetParameters()
            .Skip(!srcMethod.IsStatic && dstMethod.IsStatic ? 1 : 0)
            .ToArray();
        // Same thing for srcParams
        var srcParams = srcMethod.GetParameters()
            .Skip(!dstMethod.IsStatic && srcMethod.IsStatic ? 1 : 0)
            .ToArray();
        
        ValidateCompatibleParameters(srcParams, dstParams);

        if (srcMethod.ReturnType != dstMethod.ReturnType) {
            throw new ArgumentException("Source and destination methods do not have the same return type.");
        }
        
        var srcMethodDesc = MethodDescOfMethod(srcMethod);
        var srcJmpInstruction = *(IntPtr*)srcMethodDesc.GetAddrOfSlot();
        var srcOriginalJmpAddress = InstructionPatcher.FindJumpAbsoluteAddress(srcJmpInstruction);
        
        var dstMethodDesc = MethodDescOfMethod(dstMethod);
        var dstJmpInstruction = *(IntPtr*) dstMethodDesc.GetAddrOfSlot();
        var dstNativeFuncAbsoluteAddr = InstructionPatcher.FindJumpAbsoluteAddress(dstJmpInstruction);
        
        InstructionPatcher.PatchJumpWithAbsoluteAddress(srcJmpInstruction, dstNativeFuncAbsoluteAddr);
        
        return new [] {srcJmpInstruction, srcOriginalJmpAddress};
    }

    private static CLR_MethodDesc MethodDescOfMethod(MethodInfo methodInfo) {
        var methodHandle = GetHandleForMethod(methodInfo);
        RuntimeHelpers.PrepareMethod(methodHandle);
        
        return CLR_MethodDesc.ofMethod(methodHandle);
    }

    private static RuntimeMethodHandle GetHandleForMethod(MethodInfo methodInfo) {
        if (methodInfo is not DynamicMethod dynamicMethod) {
            return methodInfo.MethodHandle;
        }

        var descriptorMethod =
            typeof(DynamicMethod).GetMethod("GetMethodDescriptor", BindingFlags.Instance | BindingFlags.NonPublic);
        var handle = descriptorMethod.Invoke(dynamicMethod, Array.Empty<object>());
        if (handle is not RuntimeMethodHandle runtimeMethodHandle) {
            throw new ArgumentException("Cannot get method descriptor for given DynamicMethod");
        }
            
        return runtimeMethodHandle;

    }
    
    private static DynamicMethod CreateDynamicMethod(Type? instanceType, MethodInfo clone) {
        if ((clone.CallingConvention & ~(CallingConventions.Standard | CallingConventions.HasThis)) != 0) {
            throw new NotSupportedException($"Cannot create dynamic method for method of calling convention {clone.CallingConvention}, only 'Standard' and 'HasThis' are allowed");
        }
        
        var parameters = DynamicMethodParameters(instanceType, clone);
        
        var dynamicMethod = new DynamicMethod("MethodName", MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard, clone.ReturnType, parameters, typeof(CLRHelper), true);
        var il = dynamicMethod.GetILGenerator();
        if (clone.ReturnType != typeof(void)) {
            // Produce a valid object to return 
            il.Emit(OpCodes.Ldnull);
        }
        
        il.Emit(OpCodes.Ret);
        return dynamicMethod;
    }

    private static Type[] DynamicMethodParameters(Type? instanceType, MethodInfo clone) {
        if (!clone.IsStatic && instanceType is null) {
            throw new ArgumentNullException(nameof(instanceType), "Instance type cannot be null when cloned method is not static");
        }
        
        var parameters = clone.GetParameters().Select(x => x.ParameterType).ToArray();
        var typeParameters = new Type[(clone.IsStatic ? 0 : 1) + parameters.Length];
        if (!clone.IsStatic) {
            typeParameters[0] = instanceType!;
        }
        Array.Copy(parameters, 0, typeParameters, (clone.IsStatic ? 0 : 1), parameters.Length);
        return typeParameters;
    }
    
    private static void ValidateCompatibleParameters(ParameterInfo[] originalParameters, ParameterInfo[] replacingParameters) {
        if (originalParameters.Length != replacingParameters.Length) {
            throw new ArgumentException($"Parameter length mismatch");
        }

        for (var i = 0; i < originalParameters.Length; i++) {
            var originalParameter = originalParameters[i];
            var replacingParameter = replacingParameters[i];

            if (originalParameter.ParameterType != replacingParameter.ParameterType) {
                throw new ArgumentException($"Parameter types at position {i + 1} are not compatible. " +
                                            $"Original: {originalParameter.ParameterType}, Replacing: {replacingParameter.ParameterType}");
            }

            if (originalParameter.Attributes != replacingParameter.Attributes) {
                throw new ArgumentException($"Parameter attributes at position {i + 1} are not compatible. " +
                                            $"Original: {originalParameter.Attributes}, Replacing: {replacingParameter.Attributes}");
            }
        }
    }
}

public sealed class MethodReplacement : IDisposable {

    private readonly IntPtr _methodJmpAddress;
    private readonly IntPtr _originalMethodImpl;
    private readonly IInstructionPatcher _instructionPatcher;

    internal MethodReplacement(DynamicMethod originalMethod, IntPtr methodJmpAddress, IntPtr originalMethodImpl, IInstructionPatcher instructionPatcher) {
        _methodJmpAddress = methodJmpAddress;
        _originalMethodImpl = originalMethodImpl;
        OriginalMethod = originalMethod;
        _instructionPatcher = instructionPatcher;
    }

    public DynamicMethod OriginalMethod {
        get;
    }

    public void Dispose() {
        _instructionPatcher.PatchJumpWithAbsoluteAddress(_methodJmpAddress, _originalMethodImpl);
    }
}