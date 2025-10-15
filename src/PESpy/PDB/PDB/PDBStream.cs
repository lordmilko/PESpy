using System;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    public class PDBStream : IValue, IViewable
    {
        protected const int ImplementationVersionOffset = 0;
        protected const int SignatureOffset = 4;
        protected const int AgeOffset = 8;

        //impv
        public PDBIMPV ImplementationVersion
        {
            get => (PDBIMPV) chunk.PeekUInt32(ImplementationVersionOffset);
            set => chunk.PokeUInt32(ImplementationVersionOffset, (uint) value);
        }

        //sig. If "z" (reproducible" is specified in the open mode, sig is 1.
        //Otherwise, if a sigInitial was specified to OpenEx2W, that is used. Otherwise,
        //the result of the function time(0) is used
        public uint Signature //By default this comes from the C time() function, so we need to make unsigned in case the high bit is set
        {
            get => chunk.PeekUInt32(SignatureOffset);
            set => chunk.PokeUInt32(SignatureOffset, value);
        }

        //age
        public int Age
        {
            get => chunk.PeekInt32(AgeOffset);
            set => chunk.PokeInt32(AgeOffset, value);
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

        int IViewable.NumChildren => NumChildren;

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => WriteChild(index, ref structWriter);

        protected virtual IView? WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.PDBStream, this, ViewKind.PDBStream, StructSize);

        protected virtual int NumChildren => 3;

        protected virtual void WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField("impv", ImplementationVersionOffset, ImplementationVersion, sizeof(int));
                    break;

                case 1:
                    structWriter.WriteField("sig", SignatureOffset, Signature);
                    break;

                case 2:
                    structWriter.WriteField("age", AgeOffset, Age);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
