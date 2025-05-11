using System;

namespace PESpy.NE
{
    //Name is made up
    [Flags]
    public enum NewExeFlags : ushort
    {
        /// <summary>
        /// Not a process
        /// </summary>
        NENOTP = 0x8000, //Means it's a library module? http://benoit.papillault.free.fr/c/disc2/exefmt.txt

        /// <summary>
        /// Private Library
        /// </summary>
        NEPRIVLIB,

        /// <summary>
        /// Non-conforming program
        /// </summary>
        NENONC = 0x4000,

        /// <summary>
        /// Errors in image
        /// </summary>
        NEIERR = 0x2000,

        /// <summary>
        /// Uses PM API. For binary compat
        /// </summary>
        NEWINAPI = 0x0300,

        /// <summary>
        /// Library GA above EMS line
        /// </summary>
        NEEMSLIB = 0x0040,

        /// <summary>
        /// LIM 32 expanded memory
        /// </summary>
        NELIM32 = 0x0010,

        /// <summary>
        /// Runs in protected mode
        /// </summary>
        NEPROT = 0x0008,

        /// <summary>
        /// Runs in real mode
        /// </summary>
        NEREAL = 0x0004,

        /// <summary>
        /// Instance data
        /// </summary>
        NEINST = 0x0002, //People seem to think that this means "multiple" data

        /// <summary>
        /// Solo data
        /// </summary>
        NESOLO = 0x0001
    }
}
