using System.Diagnostics;
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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => WriteStruct(writer);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter) => GetChildren(parent, viewWriter);

        protected virtual IView? WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.PDBStream, this, ViewKind.PDBStream, StructSize);

        protected virtual IView[] GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("impv", ImplementationVersion, sizeof(int));
            s.WriteField("sig", Signature);
            s.WriteField("age", Age);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
