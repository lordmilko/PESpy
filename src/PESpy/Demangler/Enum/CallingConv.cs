namespace PESpy
{
    public static partial class Demangler
    {
        public enum CallingConv : byte
        {
            None,
            Cdecl,
            Pascal,
            Thiscall,
            Stdcall,
            Fastcall,
            Clrcall,
            Eabi,
            Vectorcall,
            Regcall,
            //Swift,      // Clang-only
            //SwiftAsync, // Clang-only
        }
    }
}
