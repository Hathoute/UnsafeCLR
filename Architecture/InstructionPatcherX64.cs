namespace UnsafeCLR.Architecture;

internal unsafe class InstructionPatcherX64 : IInstructionPatcher {
    
    public IntPtr FindJumpAbsoluteAddress(IntPtr jmpInstruction) {
        var instruction = UnsafeOperations.Read<byte>(jmpInstruction);
        switch (instruction) {
            case 0xE9:  // jump relative
                var relativeOffset = UnsafeOperations.Read<int>(jmpInstruction + 1);
                return IntPtr.Add(jmpInstruction, relativeOffset + 5);
            case 0xFF:  // jump far
                var addressPtr = GetJumpFarAbsoluteIndirectAddress(jmpInstruction);
                return *addressPtr;
            default:
                throw new ArgumentException("jmpInstructionAddr does not point to a JMP instruction");
        }
    }

    public void PatchJumpWithAbsoluteAddress(IntPtr jmpInstruction, IntPtr absoluteAddress) {
        var instruction = UnsafeOperations.Read<byte>(jmpInstruction);
        switch (instruction) {
            case 0xE9:
                var displacementFromJmpToAddr = GetJmpRelativeAddress(jmpInstruction, absoluteAddress);
                var displacementPtr = (int*) IntPtr.Add(jmpInstruction, 1);
                *displacementPtr = displacementFromJmpToAddr;
                break;
            case 0xFF:
                var addressPtr = GetJumpFarAbsoluteIndirectAddress(jmpInstruction);
                *addressPtr = absoluteAddress;
                break;
            default:
                throw new ArgumentException("jmpInstructionAddr does not point to a JMP instruction");
        }
    }
    
    private static IntPtr* GetJumpFarAbsoluteIndirectAddress(IntPtr jmpInstructionAddr) {
        // Return the pointer to the address that the jmp instruction dereferences to jump to.
        var modRM = UnsafeOperations.Read<byte>(jmpInstructionAddr + 1);
        if ((modRM & 0b111) != 5) {
            throw new ArgumentException(
                "Expecting ModRM of FF instruction to be 5 (Jump far, absolute indirect)");
        }
        var ptrRelativeOffset = UnsafeOperations.Read<int>(jmpInstructionAddr + 2);
        return (IntPtr*)(jmpInstructionAddr + 6 + ptrRelativeOffset);
    }
    
    private static int GetJmpRelativeAddress(IntPtr jmpInstructionAddr, IntPtr jmpAddress) {
        var displacement = jmpAddress.ToInt64() - jmpInstructionAddr.ToInt64() - 5;
        if (displacement is > int.MaxValue or < int.MinValue) {
            throw new OverflowException("displacement cannot be cast to int");
        }

        return (int) displacement;
    }
}