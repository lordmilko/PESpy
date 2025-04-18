using PESpy.View;

namespace PESpy.PDB
{
    /* There are three MODI related structs
     * - MODI50
     * - MODI_60_PERSIST
     * - MODI
     *
     * PMODI60 is a typedef for MODI_60_PERSIST, and PMODI is a typedef for MODI
     *
     * As MODI60 is the same thing as MODI_60_PERSIST, and is a more sensible name, I have opted to use that name for our managed type definition */
    public readonly struct Modi60 : IValue, IViewable
    {
        //Supposedly this field is used to store the "currently open mod", but in version 6.0 I don't think its actually used
        public int pmod => chunk.PeekInt32(0);

        /// <summary>
        /// this module's first section contribution
        /// </summary>
        public SC sc => new SC(chunk.Slice(4));
        public Modi60Flags flags => chunk.PeekUInt16(4 + SC.StructSize);
        public SN sn => chunk.PeekUInt16(6 + SC.StructSize);
        public int cbSyms => chunk.PeekInt32(8 + SC.StructSize);
        public int cbLines => chunk.PeekInt32(12 + SC.StructSize);
        public int cbC13Lines => chunk.PeekInt32(16 + SC.StructSize);
        public ushort ifileMac => chunk.PeekUInt16(20 + SC.StructSize);
        public ushort padding1 => chunk.PeekUInt16(22 + SC.StructSize);
        public int mpifileichFile => chunk.PeekInt32(24 + SC.StructSize);
        public ECInfo ecInfo => new ECInfo(chunk.Slice(28 + SC.StructSize));
        public AnsiString szModule { get; }
        public AnsiString szObjFile { get; }

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal Modi60(in MemoryChunk chunk, out int read)
        {
            this.chunk = chunk;

            //For Imports: szModule is Import:foo.dll, szObjFile is the lib file
            //For CRT: szModule is C:\path\to\foo.obj, szObjFile is the lib file
            //For local: both szModule and szObjFile are the obj file

            var szModuleStart = 28 + SC.StructSize + ECInfo.StructSize;
            szModule = chunk.PeekAnsiNullTerminatedString(szModuleStart);

            var szObjFileStart = szModuleStart + szModule.Length + 1;
            szObjFile = chunk.PeekAnsiNullTerminatedString(szObjFileStart);

            read = szObjFileStart + szObjFile.Length + 1;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("MODI_60_Persist", this, ViewKind.Modi60Persist);

            s.WriteField(nameof(pmod), pmod);
            s.WriteInline(sc);

            using (var bitField = s.WriteBitFields<ushort>())
            {
                bitField.WriteField("fWritten", flags.fWritten, 1);
                bitField.WriteField("fECEnabled", flags.fECEnabled, 1);
                bitField.WriteField("unused", flags.unused, 6);
                bitField.WriteField("iTSM", flags.iTSM, 8);
            }

            s.WriteField(nameof(sn), sn);
            s.WriteField(nameof(cbSyms), cbSyms);
            s.WriteField(nameof(cbLines), cbLines);
            s.WriteField(nameof(cbC13Lines), cbC13Lines);
            s.WriteField(nameof(ifileMac), ifileMac);
            s.WriteField(nameof(padding1), padding1);
            s.WriteField(nameof(mpifileichFile), mpifileichFile);
            s.WriteInline(ecInfo);
            s.WriteAnsiNullTerminatedField(nameof(szModule), szModule);
            s.WriteAnsiNullTerminatedField(nameof(szObjFile), szObjFile);
        }
    }
}
