using System;
using PESpy.View;

namespace PESpy
{
    public class NB05Data : ICodeViewData, IViewable
    {
        public int LfoDir { get; }

        public CodeViewSig Signature { get; }

        public int LfoBase { get; }

        private readonly OMFDirHeader dirHeader;

        public ref readonly OMFDirHeader DirHeader => ref dirHeader;

        public OMFDirEntry[] DirEntries { get; }

        public int Offset => chunk.AbsoluteOffset;

        private ICodeViewAccessor codeViewAccessor;

        public ICodeViewAccessor GetCodeViewAccessor() => codeViewAccessor;

        private readonly MemoryChunk chunk;

        internal NB05Data(in MemoryChunk chunk, CodeViewSig sig, int lfoBase, int lfoDir, in OMFDirHeader dirHeader, OMFDirEntry[] dirEntries, ICodeViewAccessor codeViewAccessor)
        {
            this.chunk = chunk;
            LfoDir = lfoDir;
            Signature = sig;
            LfoBase = lfoBase;
            this.dirHeader = dirHeader;
            DirEntries = dirEntries;
            this.codeViewAccessor = codeViewAccessor;
        }

        public bool TryGetSegName(int offset, out AnsiString str)
        {
            var dirEntries = DirEntries;

            for (var i = dirEntries.Length - 1; i >= 0; i--)
            {
                ref var entry = ref dirEntries[i];

                if (entry.iMod != ushort.MaxValue)
                {
                    str = default;
                    return false;
                }

                if (entry.SubSection == ClrDebug.OMF.SST.sstSegName)
                {
                    str = chunk.PeekAnsiNullTerminatedString(entry.lfo + offset);
                    return true;
                }
            }

            str = default;
            return false;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //Merger.TryCreateOMFRegion will wrap this all up in a region

            writer.WriteGlobal(Offset, Signature, sizeof(int), ViewKind.CodeViewSig);
            writer.WriteGlobalField(Offset + 4, "lfoDir", LfoDir, sizeof(int));

            //Data comes before the headers

            for (var i = 0; i < DirEntries.Length; i++)
            {
                ref var entry = ref DirEntries[i];

                var data = entry.Data;

                if (data is IValue v)
                    writer.WriteGlobal((IViewable) v);
                else if (data is OMFHashedSymbols s)
                {
                    writer.WriteGlobal(s.Hash);
                    writer.WriteGlobal(s.Hash.Offset + OMFSymHash.StructSize, s.Symbols);
                }
                else
                {
                    if (data != null)
                        throw new NotImplementedException($"Don't know how to write a global of type '{data.GetType().Name}'");
                }
            }

            writer.WriteGlobal(DirHeader);
            writer.WriteGlobal(DirEntries);

            writer.WriteGlobalField(Offset + LfoBase - 8, "lfoBase", LfoBase, sizeof(int));
            writer.WriteGlobal(Offset + LfoBase - 4, Signature, sizeof(int), ViewKind.CodeViewSig);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        int IViewable.NumChildren() => throw new NotSupportedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();
    }
}
