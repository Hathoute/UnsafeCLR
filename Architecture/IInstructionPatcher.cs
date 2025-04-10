namespace UnsafeCLR.Architecture;

internal interface IInstructionPatcher {
    IntPtr FindJumpAbsoluteAddress(IntPtr jmpInstruction);
    void PatchJumpWithAbsoluteAddress(IntPtr jmpInstruction, IntPtr absoluteAddress);
}