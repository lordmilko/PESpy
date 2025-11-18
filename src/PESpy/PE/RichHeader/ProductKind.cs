using System;

namespace PESpy
{
    internal static class ProductKindFlags
    {
        //Category (which could be tool or other) is just & ~LanguageMask
        internal const int LanguageMask = 0x1F;
        internal const int ToolMask = 0x1FF00;
        internal const int OtherMask = 0x1FF00000;
    }

    [Flags]
    public enum ProductKind
    {
        None,

        //Languages
        C    = 1,
        CPP  = 2,
        VB   = 4,
        MSIL = 8,
        ASM  = 0x10, //Note that apparently MASM can sometimes be listed in the PRODITEM's even when MASM wasn't actually used. I can't remember where I read that

        //Tools

        //CL stands for "compile and link"

        /// <summary>
        /// C2.DLL, the backend of CL.EXE which contains the Universal Tuple Compiler (UTC)<para/>
        /// https://devblogs.microsoft.com/cppblog/optimizing-c-code-overview/?utm_source=chatgpt.com
        /// </summary>
        C2       = 0x100,

        /// <summary>
        /// LINK.EXE, the Microsoft Linker.
        /// </summary>
        LINK     = 0x200,

        /// <summary>
        /// ML.EXE, the Microsoft Macro Assembler.
        /// </summary>
        MASM     = 0x400,

        CVTOMF   = 0x800,
        CVTRES   = 0x1000,
        ALIASOBJ = 0x2000, //ALIASOBJ.EXE (CRT Tool that builds OLDNAMES.LIB)
        ILASM    = 0x4000,
        CVTPGD   = 0x8000,
        CVTCIL   = 0x10000,

        //Other
        Std        = 0x100000, //Std compiler (compiling for the C standard?)
        Book       = 0x200000, //Book compiler (compiling for K&R?)
        Import     = 0x400000,
        Export     = 0x800000,
        PreRelease = 0x1000000, //The fact the Phoenix prerelease compiler was used?
        POGO_I     = 0x2000000,
        POGO_O     = 0x4000000,
        LTCG       = 0x8000000,
        Resource   = 0x10000000,
    }
}
