using System.Diagnostics;

namespace UnsafeCLR.Architecture;

internal unsafe class InstructionPatcherArm64 : IInstructionPatcher {

    // https://developer.arm.com/documentation/ddi0602/2025-03/Base-Instructions/LDR--literal---Load-register--literal--
    // Only supporting the 64-bit variant of the instruction
    private const int Arm64LdrLiteralSignature = 0b01011000;
    
    private const int Arm64AdrSignature =    0b00010000;
    private const int Arm64AdrSignatureXor = 0b01100000;
    
    public IntPtr FindJumpAbsoluteAddress(IntPtr jmpInstruction) {
        var currentInstructionPtr = jmpInstruction;
        SkipAdrInstruction(ref currentInstructionPtr);
        
        var instruction = UnsafeOperations.Read<int>(currentInstructionPtr);
        if (!IsLdrLiteralArm64Instruction(instruction, out var imm19, out _)) {
            throw new ArgumentException("jmpInstruction does not point to a LDR instruction");
        }

        var displacement = imm19 * 0x4;
        var addrPtr = (IntPtr*) IntPtr.Add(currentInstructionPtr, displacement);
        return *addrPtr;
    }

    public void PatchJumpWithAbsoluteAddress(IntPtr jmpInstruction, IntPtr absoluteAddress) {
        var currentInstructionPtr = jmpInstruction;
        SkipAdrInstruction(ref currentInstructionPtr);
        
        var instruction = UnsafeOperations.Read<int>(currentInstructionPtr);
        if (!IsLdrLiteralArm64Instruction(instruction, out var imm19, out _)) {
            throw new ArgumentException("jmpInstruction does not point to a LDR instruction");
        }
        
        var displacement = imm19 * 0x4;
        var addrPtr = (IntPtr*) IntPtr.Add(currentInstructionPtr, displacement);
        Program.Main((ulong) ((IntPtr) addrPtr).ToInt64());
        *addrPtr = absoluteAddress;
    }

    private static void SkipAdrInstruction(ref IntPtr jmpInstructionPtr) {
        // In net6.0, the first instruction is 'adr X(Rd), #0', which will load PC into the register Rd
        //  followed by a ldr and then br (just like net7.0)
        var instruction = UnsafeOperations.Read<int>(jmpInstructionPtr);
        if (!IsAdrArm64Instruction(instruction, out var imm19, out var rd)) {
            return;
        }

        // Some assertions, since I'm not yet sure if its consistent
        // TODO: confirm this by looking at the CoreCLR (precisely JIT code)
        Debug.Assert(imm19 == 0);
        Debug.Assert(rd == 12);
            
        // Skip this instruction
        jmpInstructionPtr = IntPtr.Add(jmpInstructionPtr, 4);
    }
    
    private static bool IsLdrLiteralArm64Instruction(int instruction, out int imm19, out int rt) {
        var signature = instruction >> 24;
        if ((signature ^ Arm64LdrLiteralSignature) != 0) {
            imm19 = 0;
            rt = 0;
            return false;
        }

        imm19 = (instruction & 0xFFFFFF) >> 5;
        rt = instruction & 0b11111;
        return true;
    }

    private static bool IsAdrArm64Instruction(int instruction, out int imm, out int rd) {
        var signature = instruction >> 24;
        if (((signature ^ Arm64AdrSignature) | Arm64AdrSignatureXor) != Arm64AdrSignatureXor) {
            imm = 0;
            rd = 0;
            return false;
        }

        imm = (instruction >> 2) & 0xFFFFFC + (instruction >> 29) & 0b11;
        rd = instruction & 0b11111;
        return true;
    }
}