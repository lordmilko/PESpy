using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    public class PDBStream : IValue, IViewable
    {
        //impv
        public PDBIMPV ImplementationVersion
        {
            get => (PDBIMPV) chunk.PeekUInt32(0);
            set => chunk.PokeUInt32(0, (uint) value);
        }

        //sig. If "z" (reproducible" is specified in the open mode, sig is 1.
        //Otherwise, if a sigInitial was specified to OpenEx2W, that is used. Otherwise,
        //the result of the function time(0) is used
        public uint Signature //By default this comes from the C time() function, so we need to make unsigned in case the high bit is set
        {
            get => chunk.PeekUInt32(4);
            set => chunk.PokeUInt32(4, value);
        }

        //age
        public int Age
        {
            get => chunk.PeekInt32(8);
            set => chunk.PokeInt32(8, value);
        }

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
