using PESpy.View;

namespace PESpy
{
    public struct OMFModule : IValue, IViewable
    {
        public ushort ovlNumber => chunk.PeekUInt16(0);

        public ushort iLib => chunk.PeekUInt16(2);

        public ushort cSeg => chunk.PeekUInt16(4);

        public FixedAnsiString Style => chunk.PeekAnsiFixedLength(6, 2);

        private OMFSegDesc[]? segInfo;

        public OMFSegDesc[] SegInfo
        {
            get
            {
                if (segInfo == null)
                {
                    var results = new OMFSegDesc[cSeg];

                    for (var i = 0; i < cSeg; i++)
                        results[i] = new OMFSegDesc(chunk.Slice(8 + (i * OMFSegDesc.StructSize)));

                    segInfo = results;
                }

                return segInfo;
            }
        }

        public FixedAnsiString Name
        {
            get
            {
                var offset = FixedStructSize + (cSeg * OMFSegDesc.StructSize);
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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(ovlNumber), ovlNumber);
            s.WriteField(nameof(iLib), iLib);
            s.WriteField(nameof(cSeg), cSeg);
            s.WriteStructField(nameof(SegInfo), SegInfo);
            s.WriteAnsiFixedLengthField(nameof(Name), Name);

            return s.ToArray();
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
