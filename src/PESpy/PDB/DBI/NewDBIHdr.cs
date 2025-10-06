using System.Diagnostics;
using ClrDebug;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    public class NewDBIHdr : IDBIHdr, IValue, IViewable
    {
        //In DBIHdr the first member is snGSSyms. I imagine that DBI will always
        //create a snGSSyms, which would mean that it would never be snNil. Therefore,
        //I think the expectation is that when verSignature is hdrSignature (-1) this means
        //we _must_ be NewDBIHdr
        public const int hdrSignature = -1;

        //verSignature. Value is always hdrSignature (-1)
        public int verSignature
        {
            get => chunk.PeekInt32(0);
            set => chunk.PokeInt32(0, value);
        }

        //verHdr
        public DBIImpv verHdr
        {
            get => (DBIImpv) chunk.PeekUInt32(4);
            set => chunk.PokeUInt32(4, (uint) value);
        }

        //age
        public int age
        {
            get => chunk.PeekInt32(8);
            set => chunk.PokeInt32(8, value);
        }

        //snGSSyms
        public SN snGSSyms
        {
            get => chunk.PeekUInt16(12);
            set => chunk.PokeUInt16(12, value);
        }

        //usVerAll
        public DbiHdrVersion usVerAll
        {
            get => chunk.PeekUInt16(14);
            set => chunk.PokeUInt16(14, value);
        }

        //snPSSyms
        public SN snPSSyms
        {
            get => chunk.PeekUInt16(16);
            set => chunk.PokeUInt16(16, value);
        }

        /// <summary>
        /// build version of the pdb dll that built this pdb last.
        /// </summary>
        public ushort usVerPdbDllBuild
        {
            get => chunk.PeekUInt16(18);
            set => chunk.PokeUInt16(18, value);
        }

        public SN snSymRecs
        {
            get => chunk.PeekUInt16(20);
            set => chunk.PokeUInt16(20, value);
        }

        /// <summary>
        /// rbld version of the pdb dll that built this pdb last.
        /// </summary>
        public ushort usVerPdbDllRBld
        {
            get => chunk.PeekUInt16(22);
            set => chunk.PokeUInt16(22, value);
        }

        /// <summary>
        /// size of rgmodi substream
        /// </summary>
        public int cbGpModi
        {
            get => chunk.PeekInt32(24);
            set => chunk.PokeInt32(24, value);
        }

        /// <summary>
        /// size of Section Contribution substream
        /// </summary>
        public int cbSC
        {
            get => chunk.PeekInt32(28);
            set => chunk.PokeInt32(28, value);
        }

        public int cbSecMap
        {
            get => chunk.PeekInt32(32);
            set => chunk.PokeInt32(32, value);
        }

        public int cbFileInfo
        {
            get => chunk.PeekInt32(36);
            set => chunk.PokeInt32(36, value);
        }

        /// <summary>
        /// size of the Type Server Map substream
        /// </summary>
        public int cbTSMap
        {
            get => chunk.PeekInt32(40);
            set => chunk.PokeInt32(40, value);
        }

        /// <summary>
        /// index of MFC type server
        /// </summary>
        public int iMFC
        {
            get => chunk.PeekInt32(44);
            set => chunk.PokeInt32(44, value);
        }

        /// <summary>
        /// size of optional DbgHdr info appended to the end of the stream
        /// </summary>
        public int cbDbgHdr
        {
            get => chunk.PeekInt32(48);
            set => chunk.PokeInt32(48, value);
        }

        /// <summary>
        /// number of bytes in EC substream, or 0 if EC no EC enabled Mods
        /// </summary>
        public int cbECInfo
        {
            get => chunk.PeekInt32(52);
            set => chunk.PokeInt32(52, value);
        }

        public DbiHdrFlags flags
        {
            get => chunk.PeekUInt16(56);
            set => chunk.PokeUInt16(56, value);
        }

        /// <summary>
        /// machine type
        /// </summary>
        public IMAGE_FILE_MACHINE wMachine
        {
            get => (IMAGE_FILE_MACHINE) chunk.PeekUInt16(58);
            set => chunk.PokeUInt16(58, (ushort) value);
        }

        /// <summary>
        /// pad out to 64 bytes for future growth.
        /// </summary>
        public int rgulReserved
        {
            get => chunk.PeekInt32(60);
            set => chunk.PokeInt32(60, value);
        }

        int IDBIHdr.StructSize => StructSize;

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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.NewDBIHdr, this, ViewKind.NewDbiHdr, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

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

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
