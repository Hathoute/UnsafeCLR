using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using UnsafeCLR.CLR.Method;
using UnsafeCLR.CLR.Type;

namespace UnsafeCLR;

public static class CLRHelper {

    public static MethodBase GetMethodBase(MethodInfo methodInfo) {
        if (methodInfo.DeclaringType == null) {
            throw new NotImplementedException("GetMethodBase");
        }
        
        return methodInfo.DeclaringType.GetMethod(methodInfo.Name)!;
    }

    public static MethodReplacement ReplaceInstanceMethod(System.Type originalInstanceType, MethodInfo originalMethod, MethodInfo replacingMethod) {
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
        
        return new MethodReplacement(dynamicOriginalMethod, replacement[0], replacement[1]);
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

        return new MethodReplacement(dynamicOriginalMethod, replacement[0], replacement[1]);
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
        var srcOriginalJmpAddress = UnsafeOperations.GetJmpAbsoluteAddress(srcJmpInstruction);
        
        var dstMethodDesc = MethodDescOfMethod(dstMethod);
        var dstJmpInstruction = *(IntPtr*) dstMethodDesc.GetAddrOfSlot();
        var dstNativeFuncAbsoluteAddr = UnsafeOperations.GetJmpAbsoluteAddress(dstJmpInstruction);
        
        UnsafeOperations.PatchJumpWithAbsoluteAddress(srcJmpInstruction, dstNativeFuncAbsoluteAddr);
        
        return new [] {srcJmpInstruction, srcOriginalJmpAddress};
    }

    internal static CLR_MethodTable MethodTableOfType(System.Type type) {
        return CLR_MethodTable.ofType(type.TypeHandle);
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
    
    private static DynamicMethod CreateDynamicMethod(System.Type? instanceType, MethodInfo clone) {
        if ((clone.CallingConvention & ~(CallingConventions.Standard | CallingConventions.HasThis)) != 0) {
            throw new NotSupportedException($"Cannot create dynamic method for method of calling convention {clone.CallingConvention}, only 'Standard' and 'HasThis' are allowed");
        }
        
        var parameters = DynamicMethodParameters(instanceType, clone);
        
        var dynamicMethod = new DynamicMethod("MethodName", MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard, clone.ReturnType, parameters, typeof(CLRHelper), true);
        var il = dynamicMethod.GetILGenerator();
        il.Emit(OpCodes.Nop);
        il.Emit(OpCodes.Nop);
        il.Emit(OpCodes.Nop);
        il.Emit(OpCodes.Nop);
        il.Emit(OpCodes.Ret);
        return dynamicMethod;
    }

    private static System.Type[] DynamicMethodParameters(System.Type? instanceType, MethodInfo clone) {
        if (!clone.IsStatic && instanceType is null) {
            throw new ArgumentNullException(nameof(instanceType), "Instance type cannot be null when cloned method is not static");
        }
        
        var parameters = clone.GetParameters().Select(x => x.ParameterType).ToArray();
        var typeParameters = new System.Type[(clone.IsStatic ? 0 : 1) + parameters.Length];
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

    internal MethodReplacement(DynamicMethod originalMethod, IntPtr methodJmpAddress, IntPtr originalMethodImpl) {
        _methodJmpAddress = methodJmpAddress;
        _originalMethodImpl = originalMethodImpl;
        OriginalMethod = originalMethod;
    }

    public DynamicMethod OriginalMethod {
        get;
    }

    public void Dispose() {
        UnsafeOperations.PatchJumpWithAbsoluteAddress(_methodJmpAddress, _originalMethodImpl);
    }
}

static unsafe class UnsafeOperations {

    internal static T Read<T>(IntPtr address, int offset = 0) where T : struct {
        return *(T*)(address + offset);
    }

    internal static IntPtr GetJmpAbsoluteAddress(IntPtr jmpInstructionAddr) {
        var instruction = Read<byte>(jmpInstructionAddr);
        switch (instruction) {
            case 0xE9:  // jump relative
                var relativeOffset = Read<int>(jmpInstructionAddr + 1);
                return IntPtr.Add(jmpInstructionAddr, relativeOffset + 5);
            case 0xFF:  // jump far
                var addressPtr = GetJumpFarAbsoluteIndirectAddress(jmpInstructionAddr);
                return *addressPtr;
            default:
                throw new ArgumentException("jmpInstructionAddr does not point to a JMP instruction");
        }
    }

    internal static void PatchJumpWithAbsoluteAddress(IntPtr jmpInstructionAddr, IntPtr jmpAddress) {
        var instruction = Read<byte>(jmpInstructionAddr);
        switch (instruction) {
            case 0xE9:
                var displacementFromJmpToAddr = GetJmpRelativeAddress(jmpInstructionAddr, jmpAddress);
                var displacementPtr = (int*) IntPtr.Add(jmpInstructionAddr, 1);
                *displacementPtr = displacementFromJmpToAddr;
                break;
            case 0xFF:
                var addressPtr = GetJumpFarAbsoluteIndirectAddress(jmpInstructionAddr);
                *addressPtr = jmpAddress;
                break;
            default:
                throw new ArgumentException("jmpInstructionAddr does not point to a JMP instruction");
        }
    }

    private static int GetJmpRelativeAddress(IntPtr jmpInstructionAddr, IntPtr jmpAddress) {
        var displacement = jmpAddress.ToInt64() - jmpInstructionAddr.ToInt64() - 5;
        if (displacement is > int.MaxValue or < int.MinValue) {
            throw new OverflowException("displacement cannot be cast to int");
        }

        return (int) displacement;
    }

    private static IntPtr* GetJumpFarAbsoluteIndirectAddress(IntPtr jmpInstructionAddr) {
        // Return the pointer to the address that the jmp instruction dereferences to jump to.
        var modRM = Read<byte>(jmpInstructionAddr + 1);
        if ((modRM & 0b111) != 5) {
            throw new ArgumentException(
                "Expecting ModRM of FF instruction to be 5 (Jump far, absolute indirect)");
        }
        var ptrRelativeOffset = Read<int>(jmpInstructionAddr + 2);
        return (IntPtr*)(jmpInstructionAddr + 6 + ptrRelativeOffset);
    }
}