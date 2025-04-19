using UnsafeCLR.CLR.Shared;

namespace UnsafeCLR.CLR.DotNet9;

internal class Constants : IConstantProvider {
    public StructSize StructSize => new() {
        // Using IDA
        MethodDesc = 0x10,
        DynamicMethodDesc = 0x30
    };

    public FieldOffsets FieldOffsets => new() {
        // Using IDA
        // https://github.com/dotnet/runtime/blob/34e64ad57037093849bbb60a79666b2a3f3746c2/src/coreclr/vm/method.hpp#L1696
        Flags3AndTokenRemainder = 0,
        ChunkIndex = 2,
        Flags2 = 3,             // in net9.0 this is named m_bFlags4
        SlotNumber = 4,
        Flags = 6
    };

    public Classification Classification => new() {
        // In net9.0 this becomes MethodDescFlags
        // https://github.com/dotnet/runtime/blob/34e64ad57037093849bbb60a79666b2a3f3746c2/src/coreclr/vm/method.hpp#L112
        MdcClassification = 0x0007,
        MdcStatic = 0x0080,
        MdcHasNonVtableSlot = 0x0008
    };
}