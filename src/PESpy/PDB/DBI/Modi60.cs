using System.Collections.Generic;
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
        //Supposedly this field is used to store the "currently open mod", but in version 6.0 I don't think its actually used
        public int pmod
        {
            get => chunk.PeekInt32(0);
            set => chunk.PokeInt32(0, value);
        }

        /// <summary>
        /// this module's first section contribution
        /// </summary>
        public SC sc
        {
            get => chunk.PeekUnmanaged<SC>(4);
            set => chunk.PokeUnmanaged<SC>(4, value);
        }

        public Modi60Flags flags
        {
            get => chunk.PeekUInt16(4 + SC.StructSize);
            set => chunk.PokeUInt16(4 + SC.StructSize, value);
        }

        /// <summary>
        /// SN of module debug info (syms, lines, fpo), or snNil
        /// </summary>
        public SN sn
        {
            get => chunk.PeekUInt16(6 + SC.StructSize);
            set => chunk.PokeUInt16(6 + SC.StructSize, value);
        }

        /// <summary>
        /// size of local symbols debug info in stream sn
        /// </summary>
        public int cbSyms
        {
            get => chunk.PeekInt32(8 + SC.StructSize);
            set => chunk.PokeInt32(8 + SC.StructSize, value);
        }

        /// <summary>
        /// size of line number debug info in stream sn
        /// </summary>
        public int cbLines
        {
            get => chunk.PeekInt32(12 + SC.StructSize);
            set => chunk.PokeInt32(12 + SC.StructSize, value);
        }

        /// <summary>
        /// size of C13 style line number info in stream sn
        /// </summary>
        public int cbC13Lines
        {
            get => chunk.PeekInt32(16 + SC.StructSize);
            set => chunk.PokeInt32(16 + SC.StructSize, value);
        }

        /// <summary>
        /// number of files contributing to this module
        /// </summary>
        public ushort ifileMac
        {
            get => chunk.PeekUInt16(20 + SC.StructSize);
            set => chunk.PokeUInt16(20 + SC.StructSize, value);
        }

        public ushort padding1
        {
            get => chunk.PeekUInt16(22 + SC.StructSize);
            set => chunk.PokeUInt16(22 + SC.StructSize, value);
        }
        public int mpifileichFile
        {
            get => chunk.PeekInt32(24 + SC.StructSize);
            set => chunk.PokeInt32(24 + SC.StructSize, value);
        }
        public ECInfo ecInfo => new ECInfo(chunk.Slice(28 + SC.StructSize));
        public AnsiString szModule { get; }
        public AnsiString szObjFile { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private PDBModuleSymbols? symbols;

        public unsafe PDBModuleSymbols? Symbols => Modi.GetSymbols(ref symbols, this, chunk);

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

                        using var headers = new PooledList<CvDebugSSubsectionHeader>();

                        while (moduleChunk.AbsoluteOffset < end)
                        {
                            var header = new CvDebugSSubsectionHeader(moduleChunk);

                            headers.Add(header);
                            moduleChunk = moduleChunk.Slice(header.Length + 8); //cbLen just covers the data, not the header
                        }

                        c13Lines = headers.ToArray();
                    }
                }

                return c13Lines;
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            sizeof(int) + //pmod
            SC.StructSize + //sc
            sizeof(short) + //flags
            sizeof(short) + //sn
            sizeof(int) + //cbSyms
            sizeof(int) + //cbLines
            sizeof(int) + //cbC13Lines
            sizeof(short) + //iFileMac
            sizeof(short) + //padding1
            sizeof(int) + //mpifileichFile
            ECInfo.StructSize; //ecInfo

        internal int StructSize => (FixedStructSize + szModule.Length + 1 + szObjFile.Length + 1 + 3) & ~3; //32-bit aligned

        private readonly MemoryChunk chunk;

        internal unsafe Modi60(in MemoryChunk chunk, out int read)
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

            //Modules are 32-bit aligned
            read = (read + 3) & ~3;

            symbols = default;
            c13Lines = default;

            if (cbLines > 0)
                Debug.Assert(false, "Reading C11 lines is not implemented"); //microsoft-pdb calls these C11 lines, they're not called C7 lines

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
            writer.NewStruct("MODI_60_Persist", this, ViewKind.Modi60Persist, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(pmod), pmod);
            s.WriteStructField(nameof(sc), sc);

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

            return s.ToArray();
        }

        public override string ToString()
        {
            return szModule.ToString();
        }
    }
}
