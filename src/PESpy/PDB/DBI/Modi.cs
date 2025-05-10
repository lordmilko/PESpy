using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.PDB
{
    public class Modi : IModi, IValue //Will always be boxed
    {
        /// <summary>
        /// currently open mod
        /// </summary>
        public uint pmod => chunk.PeekUInt32(0);

        private SC40? scField;

        /// <summary>
        /// this module's first section contribution
        /// </summary>
        public SC40 sc => chunk.PeekUnmanaged<SC40>(4);

        public ModiFlags flags => chunk.PeekUInt16(4 + SC40.StructSize);

        /// <summary>
        /// SN of module debug info (syms, lines, fpo), or snNil
        /// </summary>
        public SN sn => chunk.PeekUInt16(6 + SC40.StructSize);

        /// <summary>
        /// size of local symbols debug info in stream sn
        /// </summary>
        public int cbSyms => chunk.PeekInt32(8 + SC40.StructSize);

        /// <summary>
        /// size of line number debug info in stream sn
        /// </summary>
        public int cbLines => chunk.PeekInt32(12 + SC40.StructSize);

        /// <summary>
        /// size of frame pointer opt debug info in stream sn
        /// </summary>
        public int cbFpo => chunk.PeekInt32(16 + SC40.StructSize);

        /// <summary>
        /// number of files contributing to this module
        /// </summary>
        public ushort iFileMac => chunk.PeekUInt16(20 + SC40.StructSize);

        //Bytes 22-23: Padding

        /// <summary>
        /// array [0..ifileMac) of offsets into dbi.bufFilenames
        /// </summary>
        public int mpifileichFile => chunk.PeekInt32(24 + SC40.StructSize); //This is defined as an ICH* meaning it's ostensibly an array, but I don't feel like it's necessarily an array on disk

        public AnsiString szModule { get; }
        public AnsiString szObjFile { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private PDBModuleSymbols? symbols;

        public PDBModuleSymbols? Symbols => GetSymbols(ref symbols, this, chunk);

        internal static unsafe PDBModuleSymbols? GetSymbols(ref PDBModuleSymbols? field, IModi modi, in MemoryChunk chunk)
        {
            if (field == null && modi.cbSyms > 0)
            {
                var pdbFile = chunk.PDBFile();

                //Note that sn could potentially be snNil
                if (pdbFile.TryGetStreamChunk(modi.sn, out var symbolsChunk))
                {
                    SymbolMemoryTracker.RegisterPDBSymbolMemory(symbolsChunk);

                    var signature = (CV_SIGNATURE) symbolsChunk.PeekInt32(0);

                    var ptr = symbolsChunk.Pointer;
                    Debug.Assert(symbolsChunk.RelativeOffset == 0);

                    var results = new SymTypeList(ptr + 4, modi.cbSyms - 4);

                    field = new PDBModuleSymbols(symbolsChunk, signature, results);
                }
            }

            return field;
        }

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal Modi(in MemoryChunk chunk, out int read)
        {
            this.chunk = chunk;

            var szModuleStart = 28 + SC40.StructSize;
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

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("MODI", this, ViewKind.Modi);

            s.WriteField(nameof(pmod), pmod);
            s.WriteStructField(nameof(sc), sc);

            writer.WriteGlobal(Symbols);

            using (var bitField = s.WriteBitFields<ushort>())
            {
                bitField.WriteField("fWritten", flags.fWritten, 1);
                bitField.WriteField("unused", flags.unused, 15);
            }

            s.WriteField(nameof(sn), sn);
            s.WriteField(nameof(cbSyms), cbSyms);
            s.WriteField(nameof(cbLines), cbLines);
            s.WriteField(nameof(cbFpo), cbFpo);
            s.WriteField(nameof(iFileMac), iFileMac);
            s.Align(4);
            s.WriteField(nameof(mpifileichFile), mpifileichFile);
            s.WriteAnsiNullTerminatedField(nameof(szModule), szModule);
            s.WriteAnsiNullTerminatedField(nameof(szObjFile), szObjFile);
        }

        public override string ToString()
        {
            return szModule.ToString();
        }
    }
}
