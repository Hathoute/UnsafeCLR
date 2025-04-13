namespace UnsafeCLR.Architecture;

internal unsafe class InstructionPatcherArm64 : IInstructionPatcher {

    // https://developer.arm.com/documentation/ddi0602/2025-03/Base-Instructions/LDR--literal---Load-register--literal--
    // Only supporting the 64-bit variant of the instruction
    private const int Arm64LdrLiteralSignature = 0b01011000 << 24;
    
    public IntPtr FindJumpAbsoluteAddress(IntPtr jmpInstruction) {
        var instruction = UnsafeOperations.Read<int>(jmpInstruction);
        if (!IsLdrLiteralArm64Instruction(instruction, out var imm19, out _)) {
            throw new ArgumentException("jmpInstruction does not point to a LDR instruction");
        }

        var displacement = imm19 * 0x4;
        var addrPtr = (IntPtr*) IntPtr.Add(jmpInstruction, displacement);
        return *addrPtr;
    }

    public void PatchJumpWithAbsoluteAddress(IntPtr jmpInstruction, IntPtr absoluteAddress) {
        var instruction = UnsafeOperations.Read<int>(jmpInstruction);
        if (!IsLdrLiteralArm64Instruction(instruction, out var imm19, out _)) {
            throw new ArgumentException("jmpInstruction does not point to a LDR instruction");
        }
        
        var displacement = imm19 * 0x4;
        var addrPtr = (IntPtr*) IntPtr.Add(jmpInstruction, displacement);
        *addrPtr = absoluteAddress;
    }
    
    private static bool IsLdrLiteralArm64Instruction(int instruction, out int imm19, out int rt) {
        if (!HasSameSignature(instruction, Arm64LdrLiteralSignature, 0xFF << 24)) {
            imm19 = 0;
            rt = 0;
            return false;
        }

        imm19 = (instruction & 0xFFFFFF) >> 5;
        rt = instruction & 0b11111;
        return true;
    }
    
    private static bool HasSameSignature(int instruction, int signature, int mask) {
        return ((instruction ^ signature) & mask) == 0;
    }
}