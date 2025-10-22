using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    [Source(SourceKind.cvexefmt)]
    public struct OMFModule : IValue, IViewable
    {
        private const int ovlNumberOffset = 0;
        private const int iLibOffset = 2;
        private const int cSegOffset = 4;
        private const int StyleOffset = 6;
        private const int SegInfoOffset = 8;
        private int NameOffset => FixedStructSize + (cSeg * OMFSegDesc.StructSize);

        public ushort ovlNumber => chunk.PeekUInt16(ovlNumberOffset);

        public ushort iLib => chunk.PeekUInt16(iLibOffset);

        public ushort cSeg => chunk.PeekUInt16(cSegOffset);

        public FixedAnsiString Style => chunk.PeekAnsiFixedLength(StyleOffset, 2);

        private OMFSegDesc[]? segInfo;

        public OMFSegDesc[] SegInfo
        {
            get
            {
                if (segInfo == null)
                {
                    var results = new OMFSegDesc[cSeg];

                    for (var i = 0; i < cSeg; i++)
                        results[i] = new OMFSegDesc(chunk.Slice(SegInfoOffset + (i * OMFSegDesc.StructSize)));

                    segInfo = results;
                }

                return segInfo;
            }
        }

        public FixedAnsiString Name
        {
            get
            {
                var offset = NameOffset;
                var length = chunk.PeekByte(offset);
                return chunk.PeekAnsiFixedLength(offset + 1, length);
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            sizeof(short) + //ovlNumber
            sizeof(short) + //iLib
            sizeof(short) + //cSeg
            2; //Style

        internal int StructSize
        {
            get
            {
                var size = FixedStructSize + (cSeg * OMFSegDesc.StructSize);
                var strLen = chunk.PeekByte(size);
                size += strLen + sizeof(byte);
                return size;
            }
        }

        private readonly MemoryChunk chunk;

        internal OMFModule(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            segInfo = default;

            //Supposedly the module name is padded to an int word boundary, but I don't think this matters: we don't care what info is directly after us,
            //we read where we're told by OMFDirEntry.lfo
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.OMFModule, this, ViewKind.OMFModule, StructSize);

        int IViewable.NumChildren() => 6;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(ovlNumber), ovlNumberOffset, ovlNumber);
                    break;

                case 1:
                    structWriter.WriteField(nameof(iLib), iLibOffset, iLib);
                    break;

                case 2:
                    structWriter.WriteField(nameof(cSeg), cSegOffset, cSeg);
                    break;

                case 3:
                    structWriter.WriteAnsiFixedLengthField(nameof(Style), StyleOffset, Style);
                    break;

                case 4:
                    structWriter.WriteStructField(nameof(SegInfo), SegInfoOffset, SegInfo);
                    break;

                case 5:
                    structWriter.WriteLengthPrefixedAnsiField(nameof(Name), NameOffset, Name);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
