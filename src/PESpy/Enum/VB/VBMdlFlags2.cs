using System;

namespace PESpy.VB
{
    //Name is made up
    [Flags]
    public enum VBMdlFlags2 : uint
    {
        Unsupported1 = 0x1,
        Unsupported2 = 0x2,
        Unsupported3 = 0x4,
        Unsupported4 = 0x8,
        Unsupported5 = 0x10,
        DataQuery = 0x20,
        OLE = 0x40,
        Unsupported6 = 0x80,
        UserControl = 0x100,
        PropertyPage = 0x200,
        Document = 0x400,
        Unsupported7 = 0x800,
    }
}
