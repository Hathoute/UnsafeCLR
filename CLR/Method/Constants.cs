namespace UnsafeCLR.CLR.Method;

internal static class StructSize {
    // Using IDA
    internal const byte MethodDesc = 0x8;
    internal const byte DynamicMethodDesc = 0x28;
}

internal static class FieldOffsets {
    // Using IDA
    // https://github.com/dotnet/runtime/blob/f1dd57165bfd91875761329ac3a8b17f6606ad18/src/coreclr/vm/method.hpp#L1829
    internal const byte Flags3AndTokenRemainder = 0;
    internal const byte ChunkIndex = 2;
    internal const byte Flags2 = 3;
    internal const byte SlotNumber = 4;
    internal const byte Flags = 6;
}

internal static class Constants {
    // https://github.com/dotnet/runtime/blob/f1dd57165bfd91875761329ac3a8b17f6606ad18/src/coreclr/vm/method.hpp#L1857
    internal const ushort PackedSlotLayoutSlotmask = 0x03FF;
}

internal enum Classification {
    // https://github.com/dotnet/runtime/blob/f1dd57165bfd91875761329ac3a8b17f6606ad18/src/coreclr/vm/method.hpp#L126C6-L126C30
    MdcClassification = 0x0007,
    MdcStatic = 0x0080,
    MdcHasNonVtableSlot = 0x0008,
    MdcRequiresFullSlotNumber = 0x8000
}