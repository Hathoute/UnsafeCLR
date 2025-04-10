namespace UnsafeCLR;

internal unsafe class UnsafeOperations {
    
    internal static T Read<T>(IntPtr address, int offset = 0) where T : struct {
        return *(T*)(address + offset);
    }
}