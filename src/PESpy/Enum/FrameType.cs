namespace PESpy
{
    //These values appear to be synonymous with ClrDebug.Dia.StackFrameTypeEnum

    //https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-fpo_data

    public enum FrameType : byte
    {
        /// <summary>
        /// FPO frame
        /// </summary>
        FPO = 0,

        /// <summary>
        /// Trap frame
        /// </summary>
        Trap = 1,

        /// <summary>
        /// TSS frame
        /// </summary>
        TSS = 2,

        /// <summary>
        /// Non-FPO frame
        /// </summary>
        NonFPO = 3
    }
}