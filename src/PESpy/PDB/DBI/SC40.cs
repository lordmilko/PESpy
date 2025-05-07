using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.PDB
{
    [DebuggerDisplay("isect = {isect}, off = 0x{off.ToString(\"X\"),nq}, cb = {cb}, imod = {imod}")]
    public class SC40 : IValue, IViewable
    {
        public ISECT isect => chunk.PeekUInt16(0);

        public ushort padding1 => chunk.PeekUInt16(2);

        public int off => chunk.PeekInt32(4);

        public int cb => chunk.PeekInt32(8);

        public IMAGE_SCN dwCharacteristics => (IMAGE_SCN) chunk.PeekUInt32(12);

        //I believe this value is 0 based
        public IMOD imod => chunk.PeekUInt16(16);

        public ushort padding2 => chunk.PeekUInt16(18);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(ushort) + //isect
            sizeof(ushort) + //padding1
            sizeof(int) + //off
            sizeof(int) + //cb
            sizeof(int) + //dwCharacteristics
            sizeof(ushort) + //imod
            sizeof(ushort); //padding2

        internal readonly MemoryChunk chunk;

        internal SC40(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteView(ViewWriter writer) => WriteView(writer);

        protected virtual void WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(SC40), this, ViewKind.SC);

            s.WriteField(nameof(isect), isect);
            s.WriteField(nameof(padding1), padding1);
            s.WriteField(nameof(off), off);
            s.WriteField(nameof(cb), cb);
            s.WriteField(nameof(dwCharacteristics), dwCharacteristics, sizeof(int));
            s.WriteField(nameof(imod), imod);
            s.WriteField(nameof(padding2), padding2);
        }
    }
}
