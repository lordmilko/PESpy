namespace PESpy.UI
{
    public enum WellKnownCommand : short
    {
        OpenFile = 1001,
        CloseFile = 1002,
        Exit = 1003,

        //Ctrl+C
        Copy = 1004,

        //Ctrl+G, Ctrl+T
        Goto = 1005,
    }
}
