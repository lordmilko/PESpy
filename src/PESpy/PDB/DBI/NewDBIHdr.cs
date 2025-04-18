using ClrDebug;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    public readonly struct NewDBIHdr : IValue, IViewable
    {
        //verSignature. Value is always hdrSignature (-1)
        public int verSignature => chunk.PeekInt32(0);

        //verHdr
        public DBIImpv verHdr => (DBIImpv) chunk.PeekUInt32(4);

        //age
        public int age => chunk.PeekInt32(8);

        //snGSSyms
        public SN snGSSyms => (SN) chunk.PeekUInt16(12);

        //usVerAll
        public DbiHdrVersion usVerAll => chunk.PeekUInt16(14);

        //snPSSyms
        public SN snPSSyms => (SN) chunk.PeekUInt16(16);

        /// <summary>
        /// build version of the pdb dll that built this pdb last.
        /// </summary>
        public ushort usVerPdbDllBuild => chunk.PeekUInt16(18);

        public SN snSymRecs => (SN) chunk.PeekUInt16(20);

        /// <summary>
        /// rbld version of the pdb dll that built this pdb last.
        /// </summary>
        public ushort usVerPdbDllRBld => chunk.PeekUInt16(22);

        /// <summary>
        /// size of rgmodi substream
        /// </summary>
        public int cbGpModi => chunk.PeekInt32(24);

        /// <summary>
        /// size of Section Contribution substream
        /// </summary>
        public int cbSC => chunk.PeekInt32(28);

        public int cbSecMap => chunk.PeekInt32(32);

        public int cbFileInfo => chunk.PeekInt32(36);

        /// <summary>
        /// size of the Type Server Map substream
        /// </summary>
        public int cbTSMap => chunk.PeekInt32(40);

        /// <summary>
        /// index of MFC type server
        /// </summary>
        public int iMFC => chunk.PeekInt32(44);

        /// <summary>
        /// size of optional DbgHdr info appended to the end of the stream
        /// </summary>
        public int cbDbgHdr => chunk.PeekInt32(48);

        /// <summary>
        /// number of bytes in EC substream, or 0 if EC no EC enabled Mods
        /// </summary>
        public int cbECInfo => chunk.PeekInt32(52);

        public DbiHdrFlags flags => chunk.PeekUInt16(56);

        /// <summary>
        /// machine type
        /// </summary>
        public IMAGE_FILE_MACHINE wMachine => (IMAGE_FILE_MACHINE) chunk.PeekUInt16(58);

        /// <summary>
        /// pad out to 64 bytes for future growth.
        /// </summary>
        public int rgulReserved => chunk.PeekInt32(60);

        public int Offset => chunk.AbsoluteOffset;

        public const int StructSize =
            sizeof(int) + //verSignature
            sizeof(int) + //verHdr
            sizeof(int) + //age
            sizeof(ushort) + //snGSSyms
            sizeof(ushort) + //usVerAll
            sizeof(ushort) + //snPSSyms
            sizeof(ushort) + //usVerPdbDllBuild
            sizeof(ushort) + //snSymRecs
            sizeof(ushort) + //usVerPdbDllRBld
            sizeof(int) + //cbGpModi
            sizeof(int) + //cbSC
            sizeof(int) + //cbSecMap
            sizeof(int) + //cbFileInfo
            sizeof(int) + //cbTSMap
            sizeof(int) + //iMFC
            sizeof(int) + //cbDbgHdr
            sizeof(int) + //cbECInfo
            sizeof(ushort) + //flags
            sizeof(ushort) + //wMachine
            sizeof(int); //rgulReserved

        private readonly MemoryChunk chunk;

        internal NewDBIHdr(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(NewDBIHdr), this, ViewKind.NewDbiHdr);

            s.WriteField(nameof(verSignature), verSignature);
            s.WriteField(nameof(verHdr), verHdr, sizeof(int));
            s.WriteField(nameof(age), age);
            s.WriteField(nameof(snGSSyms), snGSSyms);

            using (var bitField = s.WriteBitFields<ushort>())
            {
                if (usVerAll.vernew.fNewVerFmt)
                {
                    bitField.WriteField("usVerPdbDllMin", usVerAll.vernew.usVerPdbDllMin, 8);
                    bitField.WriteField("usVerPdbDllMaj", usVerAll.vernew.usVerPdbDllMaj, 7);
                    bitField.WriteField("fNewVerFmt", usVerAll.vernew.fNewVerFmt, 1);
                }
                else
                {
                    //Assume it's just old and not really screwed up
                    bitField.WriteField("usVerPdbDllRbld", usVerAll.verold.usVerPdbDllRBld, 4);
                    bitField.WriteField("usVerPdbDllMin", usVerAll.verold.usVerPdbDllMin, 7);
                    bitField.WriteField("usVerPdbDllMaj", usVerAll.verold.usVerPdbDllMaj, 5);
                }
            }

            s.WriteField(nameof(snPSSyms), snPSSyms);
            s.WriteField(nameof(usVerPdbDllBuild), usVerPdbDllBuild);
            s.WriteField(nameof(snSymRecs), snSymRecs);
            s.WriteField(nameof(usVerPdbDllRBld), usVerPdbDllRBld);
            s.WriteField(nameof(cbGpModi), cbGpModi);
            s.WriteField(nameof(cbSC), cbSC);
            s.WriteField(nameof(cbSecMap), cbSecMap);
            s.WriteField(nameof(cbFileInfo), cbFileInfo);
            s.WriteField(nameof(cbTSMap), cbTSMap);
            s.WriteField(nameof(iMFC), iMFC);
            s.WriteField(nameof(cbDbgHdr), cbDbgHdr);
            s.WriteField(nameof(cbECInfo), cbECInfo);

            using (var bitField = s.WriteBitFields<ushort>())
            {
                bitField.WriteField("fIncLink", flags.fIncLink, 1);
                bitField.WriteField("fStripped", flags.fStripped, 1);
                bitField.WriteField("fCTypes", flags.fCTypes, 1);
                bitField.WriteField("unused", flags.unused, 13);
            }

            s.WriteField(nameof(wMachine), wMachine, sizeof(short));
            s.WriteField(nameof(rgulReserved), rgulReserved);
        }
    }
}
