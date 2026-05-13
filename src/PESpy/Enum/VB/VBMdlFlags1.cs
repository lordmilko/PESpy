using System;

namespace PESpy.VB
{
    //Name is made up
    [Flags]
    public enum VBMdlFlags1 : uint
    {
        PictureBox = 0x1,
        Label = 0x2,
        TextBox = 0x4,
        Frame = 0x8,
        CommandButton = 0x10,
        CheckBox = 0x20,
        OptionButton = 0x40,
        ComboBox = 0x80,
        ListBox = 0x100,
        HScrollBar = 0x200,
        VScrollBar = 0x400,
        Timer = 0x800,
        Print = 0x1000,
        Form = 0x2000,
        Screen = 0x4000,
        Clipboard = 0x8000,
        Drive = 0x10000,
        Dir = 0x20000,
        FileListBox = 0x40000,
        Menu = 0x80000,
        MDIForm = 0x100000,
        App = 0x200000,
        Shape = 0x400000,
        Line = 0x800000,
        Image = 0x1000000,
        Unsupported1 = 0x2000000,
        Unsupported2 = 0x4000000,
        Unsupported3 = 0x8000000,
        Unsupported4 = 0x10000000,
        Unsupported5 = 0x20000000,
        Unsupported6 = 0x40000000,
        Unsupported7 = 0x80000000,
    }
}
