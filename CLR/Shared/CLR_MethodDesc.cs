using UnsafeCLR.CLR.Shared;

namespace UnsafeCLR.CLR.Method;

internal class CLR_MethodDesc : IMethodDesc {
    private readonly IConstantProvider _constantProvider;
    private readonly IntPtr _methodDescPtr;

    internal CLR_MethodDesc(IConstantProvider constantProvider, IntPtr methodDescPtr) {
        _constantProvider = constantProvider;
        _methodDescPtr = methodDescPtr;
    }
        
    internal ushort flags3AndTokenRemainder => UnsafeOperations.Read<ushort>(_methodDescPtr, _constantProvider.FieldOffsets.Flags3AndTokenRemainder);
    internal byte chunkIndex => UnsafeOperations.Read<byte>(_methodDescPtr, _constantProvider.FieldOffsets.ChunkIndex);
    internal byte flags2 => UnsafeOperations.Read<byte>(_methodDescPtr, _constantProvider.FieldOffsets.Flags2);
    internal ushort slotNumber => UnsafeOperations.Read<ushort>(_methodDescPtr, _constantProvider.FieldOffsets.SlotNumber);
    internal ushort flags => UnsafeOperations.Read<ushort>(_methodDescPtr, _constantProvider.FieldOffsets.Flags);

    public IntPtr GetAddrOfSlot() {
        // https://github.com/dotnet/runtime/blob/34e64ad57037093849bbb60a79666b2a3f3746c2/src/coreclr/vm/method.cpp#L571
        if (!HasNonVtableSlot()) {
            throw new NotImplementedException(
                "GetAddrOfSlot of virtual table slots is not implemented (use CLR_MethodDesc::GetSlotNumber + CLR_MethodTable::GetSlotPtrRaw)");
        }

        var size = GetBaseSize();
        return IntPtr.Add(_methodDescPtr, size);

    }

    private bool HasNonVtableSlot() {
        // https://github.com/dotnet/runtime/blob/34e64ad57037093849bbb60a79666b2a3f3746c2/src/coreclr/vm/method.hpp#L3551
        return (flags & (ushort) _constantProvider.Classification.MdcHasNonVtableSlot) != 0;
    }

    private byte GetBaseSize() {
        // https://github.com/dotnet/runtime/blob/34e64ad57037093849bbb60a79666b2a3f3746c2/src/coreclr/vm/method.hpp#L1802
        var classification = flags & (ushort) _constantProvider.Classification.MdcClassification;
        return classification switch {
            0 => _constantProvider.StructSize.MethodDesc,
            7 => _constantProvider.StructSize.DynamicMethodDesc,
            _ => throw new NotImplementedException(
                $"GetBaseSize of classification {classification} is not implemented.")
        };
    }
}