using UnsafeCLR.CLR.Shared;

namespace UnsafeCLR.CLR.DotNet8;

internal class Constants : IConstantProvider {
    // These constants are valid for net6.0, net7.0 and net8.0
    // some naming changes might have occured between the versions.
    
    public StructSize StructSize => new() {
        // Using IDA
        MethodDesc = 0x8,
        DynamicMethodDesc = 0x28
    };

    public FieldOffsets FieldOffsets => new() {
        // Using IDA
        // https://github.com/dotnet/runtime/blob/f1dd57165bfd91875761329ac3a8b17f6606ad18/src/coreclr/vm/method.hpp#L1829
        Flags3AndTokenRemainder = 0,
        ChunkIndex = 2,
        Flags2 = 3,
        SlotNumber = 4,
        Flags = 6
    };

    public Classification Classification => new() {
        // https://github.com/dotnet/runtime/blob/f1dd57165bfd91875761329ac3a8b17f6606ad18/src/coreclr/vm/method.hpp#L126C6-L126C30
        MdcClassification = 0x0007,
        MdcStatic = 0x0080,
        MdcHasNonVtableSlot = 0x0008
    };
}