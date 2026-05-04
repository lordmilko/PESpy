using System;
using System.Diagnostics;
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
    public class Modi60 : IModi, IValue, IViewable //Will always be boxed
    {
        private const int pmodOffset = 0;
        private const int scOffset = 4;
        private const int flagsOffset = 4 + SC.StructSize;
        private const int snOffset = 6 + SC.StructSize;
        private const int cbSymsOffset = 8 + SC.StructSize;
        private const int cbLinesOffset = 12 + SC.StructSize;
        private const int cbC13LinesOffset = 16 + SC.StructSize;
        private const int ifileMacOffset = 20 + SC.StructSize;
        private const int padding1Offset = 22 + SC.StructSize;
        private const int mpifileichFileOffset = 24 + SC.StructSize;
        private const int ecInfoOffset = 28 + SC.StructSize;
        private const int szModuleOffset = 28 + SC.StructSize + ECInfo.StructSize;
        private int szObjFileOffset => szModuleOffset + szModule.Length + 1;

        //Supposedly this field is used to store the "currently open mod", but in version 6.0 I don't think its actually used
        public int pmod
        {
            get => chunk.PeekInt32(pmodOffset);
            set => chunk.PokeInt32(pmodOffset, value);
        }

        /// <summary>
        /// this module's first section contribution
        /// </summary>
        public SC sc
        {
            get => chunk.PeekUnmanaged<SC>(scOffset);
            set => chunk.PokeUnmanaged<SC>(scOffset, value);
        }

        public Modi60Flags flags
        {
            get => chunk.PeekUInt16(flagsOffset);
            set => chunk.PokeUInt16(flagsOffset, value);
        }

        /// <summary>
        /// SN of module debug info (syms, lines, fpo), or snNil
        /// </summary>
        public SN sn
        {
            get => chunk.PeekUInt16(snOffset);
            set => chunk.PokeUInt16(snOffset, value);
        }

        /// <summary>
        /// size of local symbols debug info in stream sn
        /// </summary>
        public int cbSyms
        {
            get => chunk.PeekInt32(cbSymsOffset);
            set => chunk.PokeInt32(cbSymsOffset, value);
        }

        /// <summary>
        /// size of line number debug info in stream sn
        /// </summary>
        public int cbLines
        {
            get => chunk.PeekInt32(cbLinesOffset);
            set => chunk.PokeInt32(cbLinesOffset, value);
        }

        /// <summary>
        /// size of C13 style line number info in stream sn
        /// </summary>
        public int cbC13Lines
        {
            get => chunk.PeekInt32(cbC13LinesOffset);
            set => chunk.PokeInt32(cbC13LinesOffset, value);
        }

        /// <summary>
        /// number of files contributing to this module
        /// </summary>
        public ushort ifileMac
        {
            get => chunk.PeekUInt16(ifileMacOffset);
            set => chunk.PokeUInt16(ifileMacOffset, value);
        }

        public int mpifileichFile
        {
            get => chunk.PeekInt32(mpifileichFileOffset);
            set => chunk.PokeInt32(mpifileichFileOffset, value);
        }
        public ECInfo ecInfo => new ECInfo(chunk.Slice(ecInfoOffset));
        public AnsiString szModule { get; }
        public AnsiString szObjFile { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private PDBModuleSymbols? symbols;

        public unsafe PDBModuleSymbols? Symbols => Modi.GetSymbols(ref symbols, this, imod, chunk);

        #region C11Lines

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private C11Lines? c11Lines;

        //VC60 has C11 lines
        public C11Lines? C11Lines
        {
            get
            {
                if (cbLines > 0 && c11Lines == null)
                {
                    var pdbFile = chunk.PDBFile();

                    if (pdbFile.TryGetStreamChunk(sn, out var moduleChunk))
                        c11Lines = new C11Lines(moduleChunk.Slice(cbSyms));
                }

                return c11Lines;
            }
        }

        #endregion
        #region C13Lines

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private CvDebugSSubsectionHeader[]? c13Lines;

        public CvDebugSSubsectionHeader[]? C13Lines
        {
            get
            {
                if (c13Lines == null && cbC13Lines > 0)
                {
                    var pdbFile = chunk.PDBFile();

                    if (pdbFile.TryGetStreamChunk(sn, out var moduleChunk))
                    {
                        if (cbSyms > 0)
                            moduleChunk = moduleChunk.Slice(cbSyms);

                        var end = moduleChunk.AbsoluteOffset + cbC13Lines;

                        Debug.Assert(cbC13Lines <= moduleChunk.Remaining);

                        using var headers = new PooledList<CvDebugSSubsectionHeader>();

                        var read = 0;

                        while (read < cbC13Lines)
                        {
                            var header = new CvDebugSSubsectionHeader(moduleChunk.Slice(read));

                            headers.Add(header);

                            //Watch out, these need to be aligned!
                            read += (header.StructSize + 3) & ~3; ;
                        }

                        Debug.Assert(read == cbC13Lines);

                        c13Lines = headers.ToArray();
                    }
                }

                return c13Lines;
            }
        }

        #endregion

        public long Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            sizeof(int) + //pmod
            SC.StructSize + //sc
            sizeof(short) + //flags
            sizeof(short) + //sn
            sizeof(int) + //cbSyms
            sizeof(int) + //cbLines
            sizeof(int) + //cbC13Lines
            sizeof(short) + //iFileMac
            sizeof(short) + //padding
            sizeof(int) + //mpifileichFile
            ECInfo.StructSize; //ecInfo

        internal int StructSize => (FixedStructSize + szModule.Length + 1 + szObjFile.Length + 1 + 3) & ~3; //32-bit aligned

        private int BytesUsed() => FixedStructSize + szModule.Length + 1 + szObjFile.Length + 1;

        private readonly MemoryChunk chunk;
        private readonly IMOD imod; //I don't want to expose this because it's not part of the MODI type; it's just for us internally

        internal unsafe Modi60(in MemoryChunk chunk, IMOD imod, out int read)
        {
            this.chunk = chunk;
            this.imod = imod;

            //For Imports: szModule is Import:foo.dll, szObjFile is the lib file
            //For CRT: szModule is C:\path\to\foo.obj, szObjFile is the lib file
            //For local: both szModule and szObjFile are the obj file

            var szModuleStart = 28 + SC.StructSize + ECInfo.StructSize;
            szModule = chunk.PeekAnsiNullTerminatedString(szModuleStart);

            var szObjFileStart = szModuleStart + szModule.Length + 1;
            szObjFile = chunk.PeekAnsiNullTerminatedString(szObjFileStart);

            read = szObjFileStart + szObjFile.Length + 1;

            //Modules are 32-bit aligned
            read = (read + 3) & ~3;

            symbols = default;
            c13Lines = default;

#if STRESS_TEST
            _ = Symbols;

            _ = C13Lines;
#endif
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteGlobal(Symbols);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.Modi60Persist, StructSize);

        //Note that it doesn't seem to be possible for a MODI to have a DEBUG_S_SYMBOLS C13 item;
        //I feel like you can only get DEBUG_S_SYMBOLS in an OBJ file. On that basis,
        //we don't need to have a mechanism to lookup which ICodeViewAccessor is associated
        //with a given SymType; it's always going to be the PDBModuleSymbols

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(16, BytesUsed());

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(pmod), pmodOffset, pmod);
                    break;

                case 1:
                    structWriter.WriteStructField(nameof(sc), scOffset, sc);
                    break;

                case 2:
                    structWriter.WriteBitField("fWritten", flagsOffset, flags.fWritten, sizeof(ushort), 1);
                    break;

                case 3:
                    structWriter.WriteBitField("fECEnabled", flagsOffset, flags.fECEnabled, sizeof(ushort), 1);
                    break;

                case 4:
                    structWriter.WriteBitField("unused", flagsOffset, flags.unused, sizeof(ushort), 6);
                    break;

                case 5:
                    structWriter.WriteBitField("iTSM", flagsOffset, flags.iTSM, sizeof(ushort), 8);
                    break;

                case 6:
                    structWriter.WriteField(nameof(sn), snOffset, sn);
                    break;

                case 7:
                    structWriter.WriteField(nameof(cbSyms), cbSymsOffset, cbSyms);
                    break;

                case 8:
                    structWriter.WriteField(nameof(cbLines), cbLinesOffset, cbLines);
                    break;

                case 9:
                    structWriter.WriteField(nameof(cbC13Lines), cbC13LinesOffset, cbC13Lines);
                    break;

                case 10:
                    structWriter.WriteField(nameof(ifileMac), ifileMacOffset, ifileMac);
                    break;

                case 11:
                    structWriter.WriteByteBlob(padding1Offset, sizeof(short));
                    break;

                case 12:
                    structWriter.WriteField(nameof(mpifileichFile), mpifileichFileOffset, mpifileichFile);
                    break;

                case 13:
                    structWriter.WriteStructField(nameof(ecInfo), ecInfo);
                    break;

                case 14:
                    structWriter.WriteAnsiNullTerminatedField(nameof(szModule), szModuleOffset, szModule);
                    break;

                case 15:
                    structWriter.WriteAnsiNullTerminatedField(nameof(szObjFile), szObjFileOffset, szObjFile);
                    break;

                case 16:
                    structWriter.AlignOrThrow(BytesUsed());
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return szModule.ToString();
        }
    }
}
