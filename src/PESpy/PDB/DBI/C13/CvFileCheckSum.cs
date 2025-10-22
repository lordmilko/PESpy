using System;
using System.Diagnostics;
using ClrDebug.DIA;
using PESpy.View;

namespace PESpy.PDB
{
    //CV_FileCheckSum (from Roslyn)
    public readonly struct CvFileCheckSum : IValue, IViewable
    {
        private const int nameOffset = 0;
        private const int lenOffset = 4;
        private const int typeOffset = 5;
        private const int hashOffset = 6;

        //An index into /names
        public NI name => chunk.PeekInt32(nameOffset);

        public byte len => chunk.PeekByte(lenOffset);

        public CV_SourceChksum_t type => (CV_SourceChksum_t) chunk.PeekByte(typeOffset);

        public NativeSpan<byte> hash => chunk.PeekNativeSpan<byte>(hashOffset, len);

        public int Offset => chunk.AbsoluteOffset;

        internal int StructSize =>
            sizeof(int) + //name
            sizeof(byte) + //len
            sizeof(byte) + //type
            len;

        private readonly MemoryChunk chunk;

        internal CvFileCheckSum(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.CV_FileCheckSum, this, ViewKind.CvFileCheckSum, StructSize);

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(name), nameOffset, name);
                    break;

                case 1:
                    structWriter.WriteField(nameof(len), lenOffset, len);
                    break;

                case 2:
                    structWriter.WriteField(nameof(type), typeOffset, type, sizeof(byte));
                    break;

                case 3:
                    structWriter.WriteField(nameof(hash), hashOffset, hash);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            var nameMap = chunk.PDBFile().NameMap;

            if (nameMap == null)
                return $"/names[{name}]";

            return nameMap.GetStringFromNI(name).ToString();
        }
    }
}
