namespace UnsafeCLR.CLR.Type;

internal static class StructSize {
    // Using IDA
    internal const byte MethodTable = 0x40;
    internal const byte VTableIndir = 0x8;     // typedef PlainPointer<DPTR(VTableIndir2_t)> VTableIndir_t
}

internal static class FieldOffsets {
    // Using IDA
    // https://github.com/dotnet/runtime/blob/f1dd57165bfd91875761329ac3a8b17f6606ad18/src/coreclr/vm/methodtable.h#L3871
    internal const byte Flags = 0;
    internal const byte BaseSize = 4;
    internal const byte Flags2 = 8;
    internal const byte NumVirtuals = 0xC;
    internal const byte NumInterfaces = 0xE;
}

internal static class Constants {
    // https://github.com/dotnet/runtime/blob/f1dd57165bfd91875761329ac3a8b17f6606ad18/src/coreclr/vm/methodtable.h#L1484
    internal const ulong VtableSlotsPerChunk = 8;
}

internal enum Flags2 {
    // https://github.com/dotnet/runtime/blob/f1dd57165bfd91875761329ac3a8b17f6606ad18/src/coreclr/vm/methodtable.h#L3771
    HasNonVirtualSlots = 0x0008,
    HasSingleNonVirtualSlot = 0x4000
}