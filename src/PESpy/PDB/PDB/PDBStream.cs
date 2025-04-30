using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    public class PDBStream : IValue, IViewable
    {
        //impv
        public PDBIMPV ImplementationVersion => (PDBIMPV) chunk.PeekUInt32(0);

        //sig. If "z" (reproducible" is specified in the open mode, sig is 1.
        //Otherwise, if a sigInitial was specified to OpenEx2W, that is used. Otherwise,
        //the result of the function time(0) is used
        public int Signature => chunk.PeekInt32(4);

        //age
        public int Age => chunk.PeekInt32(8);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //ImplementationVersion
            sizeof(int) + //Signature
            sizeof(int); //Age

        internal readonly MemoryChunk chunk;

        internal PDBStream(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteView(ViewWriter writer) => WriteView(writer);

        protected virtual void WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(PDBStream), this, ViewKind.PDBStream70);

            s.WriteField("impv", ImplementationVersion, sizeof(int));
            s.WriteField("sig", Signature);
            s.WriteField("age", Age);
        }
    }
}
