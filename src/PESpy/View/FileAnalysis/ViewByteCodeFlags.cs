using System;

namespace PESpy.View
{
    //Flags for a ViewByte when its Kind is Code
    [Flags]
    public enum ViewByteCodeFlags
    {
        None = 0,

        /// <summary>
        /// Whether this byte represents the start of a function
        /// </summary>
        Function = 0x10,

        NoReturn = 0x20,

        IsIL = 0x40,

        /* Whether the current instruction can be flowed to from the previous instruction. e.g.
         *     mov rax,1
         *     mov rbx,1
         * mov rbx,1 has flow (from mov rax,1)
         * 
         *     jmp rax
         *     mov rbx,1
         * 
         * mov rbx,1 does not have flow from jmp rax
         */

        //
        //mov
        HasFlow = 0x80

        //Because this is flags, we can only store 4 values
    }
}
