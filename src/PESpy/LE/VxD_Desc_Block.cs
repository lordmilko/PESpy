namespace PESpy
{
    //Device Descriptor Block
    public readonly struct VxD_Desc_Block
    {
        private const int DDB_NextOffset = 0;
        private const int DDB_SDK_VersionOffset = 4;
        private const int DDB_Req_Device_NumberOffset = 6;
        private const int DDB_Dev_Major_VersionOffset = 8;
        private const int DDB_Dev_Minor_VersionOffset = 9;
        private const int DDB_FlagsOffset = 10;
        private const int DDB_NameOffset = 12;
        private const int DDB_Init_OrderOffset = 20;
        private const int DDB_Control_ProcOffset = 24;
        private const int DDB_V86_API_ProcOffset = 28;
        private const int DDB_PM_API_ProcOffset = 32;
        private const int DDB_V86_API_CSIPOffset = 36;
        private const int DDB_PM_API_CSIPOffset = 40;
        private const int DDB_Reference_DataOffset = 44;
        private const int DDB_Service_Table_PtrOffset = 48;
        private const int DDB_Service_Table_SizeOffset = 52;
        private const int DDB_Win32_Service_TableOffset = 56;
        private const int DDB_PrevOffset = 60;
        private const int DDB_Reserved0Offset = 64;
        private const int DDB_Reserved1Offset = 68;
        private const int DDB_Reserved2Offset = 72;
        private const int DDB_Reserved3Offset = 76;

        /// <summary>
        /// VMM RESERVED FIELD
        /// </summary>
        public int DDB_Next => chunk.PeekInt32(DDB_NextOffset);

        /// <summary>
        /// INIT &lt;DDK_VERSION7gt; RESERVED FIELD
        /// </summary>
        public short DDB_SDK_Version => chunk.PeekInt16(DDB_SDK_VersionOffset);

        /// <summary>
        /// INIT &lt;UNDEFINED_DEVICE_ID&gt;
        /// </summary>
        public short DDB_Req_Device_Number => chunk.PeekInt16(DDB_Req_Device_NumberOffset);

        /// <summary>
        /// INIT &lt;0&gt; Major device number
        /// </summary>
        public byte DDB_Dev_Major_Version => chunk.PeekByte(DDB_Dev_Major_VersionOffset);

        /// <summary>
        /// INIT &lt;0&gt; Minor device number
        /// </summary>
        public byte DDB_Dev_Minor_Version => chunk.PeekByte(DDB_Dev_Minor_VersionOffset);

        /// <summary>
        /// INIT &lt;0&gt; for init calls complete
        /// </summary>
        public short DDB_Flags => chunk.PeekInt16(DDB_FlagsOffset);

        /// <summary>
        /// AINIT &lt;"        "&gt; Device name
        /// </summary>
        public FixedAnsiString DDB_Name => chunk.PeekAnsiFixedLength(DDB_NameOffset, 8);

        /// <summary>
        /// INIT &lt;UNDEFINED_INIT_ORDER>
        /// </summary>
        public int DDB_Init_Order => chunk.PeekInt32(DDB_Init_OrderOffset);

        /// <summary>
        /// Offset of control procedure
        /// </summary>
        public int DDB_Control_Proc => chunk.PeekInt32(DDB_Control_ProcOffset);

        /// <summary>
        /// INIT &lt;0&gt; Offset of API procedure
        /// </summary>
        public int DDB_V86_API_Proc => chunk.PeekInt32(DDB_V86_API_ProcOffset);

        /// <summary>
        /// INIT &lt;0&gt; Offset of API procedure
        /// </summary>
        public int DDB_PM_API_Proc => chunk.PeekInt32(DDB_PM_API_ProcOffset);

        /// <summary>
        /// INIT &lt;0&gt; CS:IP of API entry point
        /// </summary>
        public int DDB_V86_API_CSIP => chunk.PeekInt32(DDB_V86_API_CSIPOffset);

        /// <summary>
        /// INIT &lt;0&gt; CS:IP of API entry point
        /// </summary>
        public int DDB_PM_API_CSIP => chunk.PeekInt32(DDB_PM_API_CSIPOffset);

        /// <summary>
        /// Reference data from real mode
        /// </summary>
        public int DDB_Reference_Data => chunk.PeekInt32(DDB_Reference_DataOffset);

        /// <summary>
        /// INIT &lt;0&gt; Pointer to service table
        /// </summary>
        public int DDB_Service_Table_Ptr => chunk.PeekInt32(DDB_Service_Table_PtrOffset);

        /// <summary>
        /// INIT &lt;0&gt; Number of services
        /// </summary>
        public int DDB_Service_Table_Size => chunk.PeekInt32(DDB_Service_Table_SizeOffset);

        /// <summary>
        /// INIT &lt;0&gt; Pointer to Win32 services
        /// </summary>
        public int DDB_Win32_Service_Table => chunk.PeekInt32(DDB_Win32_Service_TableOffset);

        /// <summary>
        /// INIT &lt;'Prev'&gt; Ptr to prev 4.0 DDB
        /// </summary>
        public int DDB_Prev => chunk.PeekInt32(DDB_PrevOffset);

        /// <summary>
        /// INIT &lt;0&gt; Reserved
        /// </summary>
        public int DDB_Reserved0 => chunk.PeekInt32(DDB_Reserved0Offset); //The size of the VxD_Desc_Block; should be 80

        /// <summary>
        /// INIT &lt;'Rsv1'&gt; Reserved
        /// </summary>
        public int DDB_Reserved1 => chunk.PeekInt32(DDB_Reserved1Offset);

        /// <summary>
        /// INIT &lt;'Rsv2'&gt; Reserved
        /// </summary>
        public int DDB_Reserved2 => chunk.PeekInt32(DDB_Reserved2Offset);

        /// <summary>
        /// INIT &lt;'Rsv3'&gt; Reserved
        /// </summary>
        public int DDB_Reserved3 => chunk.PeekInt32(DDB_Reserved3Offset);

        private readonly MemoryChunk chunk;

        internal VxD_Desc_Block(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
