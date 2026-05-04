using System;

namespace PESpy.LE
{
    [Flags]
    public enum ObjectTableFlags
    {
        /// <summary>
        /// Readable Object
        /// </summary>
        OBJREAD = 0x0001,

        /// <summary>
        /// Writeable Object
        /// </summary>
        OBJWRITE = 0x0002,

        /// <summary>
        /// Resource Object
        /// </summary>
        OBJRSRC = 0x0008,

        /// <summary>
        /// Object has invalid pages
        /// </summary>
        OBJINVALID = 0x0080,

        /// <summary>
        /// Object is nonpermanent - should be zero in the .EXE but internally we use 6
        /// </summary>
        OBJNONPERM = 0x0600,

        /// <summary>
        /// Object is permanent and swappable
        /// </summary>
        OBJPERM = 0x0100,

        /// <summary>
        /// Object is permanent and resident
        /// </summary>
        OBJRESIDENT = 0x0200,

        /// <summary>
        /// Object is resident and contiguous
        /// </summary>
        OBJCONTIG = 0x0300,

        /// <summary>
        /// Object is permanent and long locable
        /// </summary>
        OBJDYNAMIC = 0x0400,

        /// <summary>
        /// Object type mask
        /// </summary>
        OBJTYPEMASK = 0x0700,

        /// <summary>
        /// 16:16 alias required (80x86 specific)
        /// </summary>
        OBJALIAS16 = 0x1000,

        /// <summary>
        /// Big/Default bit setting (80x86 specific)
        /// </summary>
        OBJBIGDEF = 0x2000,

        /// <summary>
        /// Object I/O privilege level (80x86 specific)
        /// </summary>
        OBJIOPL = 0x8000,

        /// <summary>
        /// Object is Discardable
        /// </summary>
        OBJDISCARD = 0x0010,

        /// <summary>
        /// Object is Shared
        /// </summary>
        OBJSHARED = 0x0020,

        /// <summary>
        /// Object has preload pages
        /// </summary>
        OBJPRELOAD = 0x0040,

        /// <summary>
        /// Executable Object
        /// </summary>
        OBJEXEC = 0x0004,

        /// <summary>
        /// Object is conforming for code (80x86 specific)
        /// </summary>
        OBJCONFORM = 0x4000
    }
}
