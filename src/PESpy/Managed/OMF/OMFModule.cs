namespace PESpy
{
    public struct OMFModule : IValue
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
                var offset = 8 + (cSeg * OMFSegDesc.StructSize);
                var length = chunk.PeekByte(offset);
                return chunk.PeekAnsiFixedLength(offset + 1, length);
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal OMFModule(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            segInfo = default;
            _ = Name;

            //Supposedly the module name is padded to an int word boundary, but I don't think this matters: we don't care what info is directly after us,
            //we read where we're told by OMFDirEntry.lfo
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
