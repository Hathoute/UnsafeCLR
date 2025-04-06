namespace UnsafeCLR.CLR.Method;

internal class CLR_MethodDesc {
    private readonly IntPtr _methodDescPtr;

    private CLR_MethodDesc(IntPtr methodDescPtr) {
        _methodDescPtr = methodDescPtr;
    }

    internal static CLR_MethodDesc ofMethod(RuntimeMethodHandle methodHandle) {
        return new CLR_MethodDesc(methodHandle.Value);
    }
        
    internal ushort flags3AndTokenRemainder => UnsafeOperations.Read<ushort>(_methodDescPtr, FieldOffsets.Flags3AndTokenRemainder);
    internal byte chunkIndex => UnsafeOperations.Read<byte>(_methodDescPtr, FieldOffsets.ChunkIndex);
    internal byte flags2 => UnsafeOperations.Read<byte>(_methodDescPtr, FieldOffsets.Flags2);
    internal ushort slotNumber => UnsafeOperations.Read<ushort>(_methodDescPtr, FieldOffsets.SlotNumber);
    internal ushort flags => UnsafeOperations.Read<ushort>(_methodDescPtr, FieldOffsets.Flags);

    internal IntPtr GetAddrOfSlot() {
        if (!HasNonVtableSlot()) {
            throw new NotImplementedException(
                "GetAddrOfSlot of virtual table slots is not implemented (use CLR_MethodDesc::GetSlotNumber + CLR_MethodTable::GetSlotPtrRaw)");
        }

        var size = GetBaseSize();
        return IntPtr.Add(_methodDescPtr, size);

    }
    
    internal ushort GetSlotNumber() {
        if (RequiresFullSlotNumber()) {
            return (ushort)(slotNumber & Constants.PackedSlotLayoutSlotmask);
        }

        return slotNumber;
    }

    private bool RequiresFullSlotNumber() {
        return (flags & (ushort) Classification.MdcRequiresFullSlotNumber) != 0;
    }

    private bool HasNonVtableSlot() {
        return (flags & (ushort) Classification.MdcHasNonVtableSlot) != 0;
    }

    private byte GetBaseSize() {
        var classification = flags & (ushort) Classification.MdcClassification;
        return classification switch {
            0 => StructSize.MethodDesc,
            7 => StructSize.DynamicMethodDesc,
            _ => throw new NotImplementedException(
                $"GetBaseSize of classification {classification} is not implemented.")
        };
    }
}