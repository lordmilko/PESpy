using System.Diagnostics;
using ClrDebug.DIA;
using PESpy.View;

namespace PESpy.PDB
{
    //CV_FileCheckSum (from Roslyn)
    [DebuggerDisplay("{DebuggerDisplay()}")]
    public readonly struct CvFileCheckSum : IValue, IViewable
    {
        private string DebuggerDisplay()
        {
            var nameMap = chunk.PDBFile().NameMap;

            if (nameMap == null)
                return $"/names[{name}]";

            return nameMap.GetStringFromNI(name).ToString();
        }

        //An index into /names
        public NI name => chunk.PeekInt32(0);

        public byte len => chunk.PeekByte(4);

        public CV_SourceChksum_t type => (CV_SourceChksum_t) chunk.PeekByte(5);

        public NativeSpan<byte> hash => chunk.PeekNativeSpan<byte>(6, len);

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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(name), name);
            s.WriteField(nameof(len), len);
            s.WriteField(nameof(type), type, sizeof(byte));
            s.WriteField(nameof(hash), hash);

            return s.ToArray();
        }
    }
}
