using System;

namespace PESpy
{
    [Flags]
    public enum BCD
    {
        BCD_NOTVISIBLE			= 0x00000001,
        BCD_AMBIGUOUS			= 0x00000002,
        BCD_PRIVORPROTBASE		= 0x00000004,
        BCD_PRIVORPROTINCOMPOBJ	= 0x00000008,
        BCD_VBOFCONTOBJ			= 0x00000010,
        BCD_NONPOLYMORPHIC		= 0x00000020,
        BCD_HASPCHD				= 0x00000040			// pClassDescriptor field is present
    }
}
