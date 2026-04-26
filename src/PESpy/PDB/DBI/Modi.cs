using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    //Represents a v4 MODI (which uses SC40). The v2 MODI used SC20
    public class Modi : IModi, IValue //Will always be boxed
    {
        private const int pmodOffset = 0;
        private const int scOffset = 4;
        private const int flagsOffset = 4 + SC40.StructSize;
        private const int snOffset = 6 + SC40.StructSize;
        private const int cbSymsOffset = 8 + SC40.StructSize;
        private const int cbLinesOffset = 12 + SC40.StructSize;
        private const int cbFpoOffset = 16 + SC40.StructSize;
        private const int iFileMacOffset = 20 + SC40.StructSize;
        private const int mpifileichFileOffset = 24 + SC40.StructSize;
        private const int szModuleOffset = 28 + SC40.StructSize;
        private int szObjFileOffset => szModuleOffset + szModule.Length + 1;

        /// <summary>
        /// currently open mod
        /// </summary>
        public uint pmod => chunk.PeekUInt32(pmodOffset);

        /// <summary>
        /// this module's first section contribution
        /// </summary>
        public SC40 sc => chunk.PeekUnmanaged<SC40>(scOffset);

        public ModiFlags flags => chunk.PeekUInt16(flagsOffset);

        /// <summary>
        /// SN of module debug info (syms, lines, fpo), or snNil
        /// </summary>
        public SN sn => chunk.PeekUInt16(snOffset);

        /// <summary>
        /// size of local symbols debug info in stream sn
        /// </summary>
        public int cbSyms => chunk.PeekInt32(cbSymsOffset);

        /// <summary>
        /// size of line number debug info in stream sn
        /// </summary>
        public int cbLines => chunk.PeekInt32(cbLinesOffset);

        /// <summary>
        /// size of frame pointer opt debug info in stream sn
        /// </summary>
        public int cbFpo => chunk.PeekInt32(cbFpoOffset);

        /// <summary>
        /// number of files contributing to this module
        /// </summary>
        public ushort iFileMac => chunk.PeekUInt16(iFileMacOffset);

        //Bytes 22-23: Padding

        /// <summary>
        /// array [0..ifileMac) of offsets into dbi.bufFilenames
        /// </summary>
        public int mpifileichFile => chunk.PeekInt32(mpifileichFileOffset); //This is defined as an ICH* meaning it's ostensibly an array, but I don't feel like it's necessarily an array on disk

        public AnsiString szModule { get; }
        public AnsiString szObjFile { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private PDBModuleSymbols? symbols;

        public PDBModuleSymbols? Symbols => GetSymbols(ref symbols, this, imod, chunk);

        #region C11Lines

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private C11Lines? c11Lines;

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

        CvDebugSSubsectionHeader[]? IModi.C13Lines => null;

        internal static unsafe PDBModuleSymbols? GetSymbols(
            ref PDBModuleSymbols? field,
            IModi modi,
            IMOD imod,
            in MemoryChunk chunk)
        {
            if (field == null && modi.cbSyms > 0)
            {
                var pdbFile = chunk.PDBFile();

                //Note that sn could potentially be snNil
                if (pdbFile.TryGetStreamChunk(modi.sn, out var symbolsChunk))
                {
                    var signature = (CV_SIGNATURE) symbolsChunk.PeekInt32(0);

                    var ptr = symbolsChunk.Pointer;
                    Debug.Assert(symbolsChunk.RelativeOffset == 0);

                    var results = new SymTypeList(ptr, sizeof(int), modi.cbSyms, pdbFile);

                    //ctor registers symbol memory
                    field = new PDBModuleSymbols(symbolsChunk, imod, signature, results);
                }
            }

            return field;
        }

        public int Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            sizeof(int) + //pmod
            SC40.StructSize + //sc
            sizeof(short) + //flags
            sizeof(short) + //sn
            sizeof(int) + //cbSyms
            sizeof(int) + //cbLines
            sizeof(int) + //cbFpo
            sizeof(short) + //iFileMac
            sizeof(short) + //Padding
            sizeof(int); //mpifileichFile

        internal int StructSize => (FixedStructSize + szModule.Length + 1 + szObjFile.Length + 1 + 3) & ~3; //32-bit aligned

        private int BytesUsed() => FixedStructSize + szModule.Length + 1 + szObjFile.Length + 1;

        private readonly MemoryChunk chunk;
        private readonly IMOD imod; //I don't want to expose this because it's not part of the MODI type; it's just for us internally

        //e.g. VC40
        internal Modi(in MemoryChunk chunk, IMOD imod, out int read)
        {
            this.chunk = chunk;
            this.imod = imod;

            var szModuleStart = FixedStructSize;
            szModule = chunk.PeekAnsiNullTerminatedString(szModuleStart);

            var szObjFileStart = szModuleStart + szModule.Length + 1;
            szObjFile = chunk.PeekAnsiNullTerminatedString(szObjFileStart);

            read = szObjFileStart + szObjFile.Length + 1;

            //Modules are 32-bit aligned
            read = (read + 3) & ~3;

#if STRESS_TEST
            _ = Symbols;
#endif
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteGlobal(Symbols);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.Modiv4, StructSize);

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(13, BytesUsed());

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
                    structWriter.WriteBitField("unused", flagsOffset, flags.unused, sizeof(ushort), 15);
                    break;

                case 4:
                    structWriter.WriteField(nameof(sn), snOffset, sn);
                    break;

                case 5:
                    structWriter.WriteField(nameof(cbSyms), cbSymsOffset, cbSyms);
                    break;

                case 6:
                    structWriter.WriteField(nameof(cbLines), cbLinesOffset, cbLines);
                    break;

                case 7:
                    structWriter.WriteField(nameof(cbFpo), cbFpoOffset, cbFpo);
                    break;

                case 8:
                    structWriter.WriteField(nameof(iFileMac), iFileMacOffset, iFileMac);
                    break;

                case 9:
                    structWriter.WriteByteBlob(iFileMacOffset + 2, sizeof(short));
                    break;

                case 10:
                    structWriter.WriteField(nameof(mpifileichFile), mpifileichFileOffset, mpifileichFile);
                    break;

                case 11:
                    structWriter.WriteAnsiNullTerminatedField(nameof(szModule), szModuleOffset, szModule);
                    break;

                case 12:
                    structWriter.WriteAnsiNullTerminatedField(nameof(szObjFile), szObjFileOffset, szObjFile);
                    break;

                case 13:
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
