namespace UnsafeCLR.CLR.Type;

public class CLR_MethodTable {
    
    private readonly IntPtr _methodTablePtr;

    private CLR_MethodTable(IntPtr methodTablePtr) {
        _methodTablePtr = methodTablePtr;
    }

    internal static CLR_MethodTable ofType(RuntimeTypeHandle typeHandle) {
        return new CLR_MethodTable(typeHandle.Value);
    }

    internal uint flags => UnsafeOperations.Read<uint>(_methodTablePtr, FieldOffsets.Flags);
    internal uint baseSize => UnsafeOperations.Read<uint>(_methodTablePtr, FieldOffsets.BaseSize);
    internal ushort flags2 => UnsafeOperations.Read<ushort>(_methodTablePtr, FieldOffsets.Flags2);
    internal ushort numVirtuals => UnsafeOperations.Read<ushort>(_methodTablePtr, FieldOffsets.NumVirtuals);
    internal ushort numInterfaces => UnsafeOperations.Read<ushort>(_methodTablePtr, FieldOffsets.NumInterfaces);

    internal CLR_MethodTable? ParentMethodTable {
        get {
            var parentPtr = UnsafeOperations.Read<IntPtr>(_methodTablePtr, 0x10);
            return parentPtr == IntPtr.Zero ? null : new CLR_MethodTable(parentPtr);
        }
    }

    internal unsafe IntPtr GetSlotPtrRaw(ushort slotNum) {
        if (slotNum < numVirtuals) {
            throw new NotImplementedException("GetSlotPtrRaw not yet implemented for virtual slots");
        }

        var nonVirtualSlotsPtr = GetNonVirtualSlotsPtr();
        if (HasSingleNonVirtualSlot()) {
            return nonVirtualSlotsPtr;
        }
        
        // NonVirtualSlotsPtr points to an array of non-virtual slots
        var nonVirtualSlotsArray = *(IntPtr*) nonVirtualSlotsPtr;
        var relativeSlotNum = slotNum - numVirtuals;
        return IntPtr.Add(nonVirtualSlotsArray, relativeSlotNum);
    }

    private IntPtr GetNonVirtualSlotsPtr() {
        return GetMultipurposeSlotPtr(Flags2.HasNonVirtualSlots, GetNonVirtualSlotOffset);
    }

    private IntPtr GetMultipurposeSlotPtr(Flags2 flag, Func<ushort, ushort> offsets) {
        var flagMask = GetFlag((ushort)((ushort)flag - 1));
        var offset = offsets.Invoke(flagMask);

        if (offset >= StructSize.MethodTable) {
            offset += (ushort) (GetNumVtableIdirections() * StructSize.VTableIndir);
        }
        
        return _methodTablePtr + offset;
    }

    private ushort GetNumVtableIdirections() {
        return (ushort) (numVirtuals / Constants.VtableSlotsPerChunk);
    }
    
    private bool HasSingleNonVirtualSlot() {
        return GetFlag((ushort) Flags2.HasSingleNonVirtualSlot) != 0;
    }

    private static unsafe ushort GetNonVirtualSlotOffset(ushort pFlags) {
        return (ushort) (StructSize.MethodTable + (pFlags - 2) * sizeof(IntPtr));
    }

    private ushort GetFlag(ushort flag) {
        return (ushort)(flags2 & flag);
    }
}