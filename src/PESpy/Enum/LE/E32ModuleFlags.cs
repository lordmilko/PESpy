using System;

namespace PESpy.LE
{
    [Flags]
    public enum E32ModuleFlags
    {
        /// <summary>
        /// Library Module - used as NENOTP
        /// </summary>
        E32NOTP = 0x8000,

        /// <summary>
        /// Module not Loadable
        /// </summary>
        E32NOLOAD = 0x2000,

        /// <summary>
        /// Uses PM Windowing API
        /// </summary>
        E32PMAPI = 0x0300,

        /// <summary>
        /// Compatible with PM Windowing
        /// </summary>
        E32PMW = 0x0200,

        /// <summary>
        /// Incompatible with PM Windowing
        /// </summary>
        E32NOPMW = 0x0100,

        /// <summary>
        /// NO External Fixups in .EXE
        /// </summary>
        E32NOEXTFIX = 0x0020,

        /// <summary>
        /// NO Internal Fixups in .EXE
        /// </summary>
        E32NOINTFIX = 0x0010,

        /// <summary>
        /// Per-Process Library Initialization
        /// </summary>
        E32LIBINIT = 0x0004,

        //E32APPMASK = 0x0700, //Aplication Type Mask

        /// <summary>
        /// Protected memory library module
        /// </summary>
        E32PROTDLL = 0x10000,

        /// <summary>
        /// Device driver
        /// </summary>
        E32DEVICE = 0x20000,

        /// <summary>
        /// .EXE module
        /// </summary>
        E32MODEXE = 0x00000,

        /// <summary>
        /// .DLL module
        /// </summary>
        E32MODDLL = 0x08000,

        /// <summary>
        /// Protected memory library module
        /// </summary>
        E32MODPROTDLL = 0x18000,

        /// <summary>
        /// Physical device driver
        /// </summary>
        E32MODPDEV = 0x20000,

        /// <summary>
        /// Virtual device driver
        /// </summary>
        E32MODVDEV = 0x28000,

        /// <summary>
        /// Virtual device driver (dynamic)
        /// </summary>
        E32MODVDEVDYN = 0x38000,

        //E32MODMASK = 0x38000 //Module type mask
    }
}
