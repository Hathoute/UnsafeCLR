namespace UnsafeCLR.CLR.Shared;

internal interface IConstantProvider {
    StructSize StructSize { get; }
    FieldOffsets FieldOffsets { get; }
    Classification Classification { get; }
}

internal struct StructSize {
    internal byte MethodDesc;
    internal byte DynamicMethodDesc;
}

internal struct FieldOffsets {
    internal byte Flags3AndTokenRemainder;
    internal byte ChunkIndex;
    internal byte Flags2;
    internal byte SlotNumber;
    internal byte Flags;
}

internal struct Classification {
    internal int MdcClassification;
    internal int MdcStatic;
    internal int MdcHasNonVtableSlot;
}