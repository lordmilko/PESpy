using System;
using PESpy.View;

namespace PESpy
{
    [Source(SourceKind.cvexefmt_h)]
    public struct OMFSourceModule : IValue, IViewable
    {
        private const int cFileOffset = 0;
        private const int cSegOffset = 2;
        private const int baseSrcFileOffset = 4;

        public ushort cFile => chunk.PeekUInt16(cFileOffset);

        public ushort cSeg => chunk.PeekUInt16(cSegOffset);

        //baseSrcFile points to an array of offsets to OMFSourceFile items
        private OMFSourceFile[]? rawBaseSrcFile;

        public OMFSourceFile[] baseSrcFile
        {
            get
            {
                if (rawBaseSrcFile == null)
                {
                    var offsets = chunk.PeekSpan<int>(baseSrcFileOffset, cFile);

                    var results = new OMFSourceFile[offsets.Length];

                    for (var i = 0; i < results.Length; i++)
                        results[i] = new OMFSourceFile(chunk.Slice(offsets[i]), chunk.RelativeOffset); //offsets contains an array of offsets relative to the start of the OMFSourceModule

                    rawBaseSrcFile = results;
                }

                return rawBaseSrcFile;
            }
        }

        public long Offset => chunk.AbsoluteOffset;

        internal int StructSize =>
            sizeof(short) + //cFile
            sizeof(short) + //cSeg
            (cFile * sizeof(int)); //baseSrcFile

        private readonly MemoryChunk chunk;

        internal OMFSourceModule(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            rawBaseSrcFile = default;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //baseSrcFile points to an array of offsets to OMFSourceFile items,
            //so technically speaking each baseSrcFile element is an xref

            var offsets = chunk.PeekSpan<int>(baseSrcFileOffset, cFile);

            var structOffset = Offset;

            for (var i = 0; i < offsets.Length; i++)
            {
                var sourceFile = new OMFSourceFile(chunk.Slice(offsets[i]), chunk.RelativeOffset);
                writer.WriteGlobal(sourceFile);

                //Offsets are relative to the start of the OMFSourceModule
                writer.WriteOffsetXRef(structOffset, baseSrcFileOffset + (i * sizeof(int)), sourceFile.Offset);
            }
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.OMFSourceModule, StructSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(cFile), cFileOffset, cFile);
                    break;

                case 1:
                    structWriter.WriteField(nameof(cSeg), cSegOffset, cSeg);
                    break;

                case 2:
                    structWriter.WriteField(nameof(baseSrcFile), baseSrcFileOffset, chunk.PeekNativeSpan<int>(baseSrcFileOffset, cFile));
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
