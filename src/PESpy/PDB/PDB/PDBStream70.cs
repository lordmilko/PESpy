using System;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    public readonly struct PDBStream70 : IValue, IViewable
    {
        //impv
        public PDBIMPV ImplementationVersion => (PDBIMPV) chunk.PeekUInt32(0);

        //sig. If "z" (reproducible" is specified in the open mode, sig is 1.
        //Otherwise, if a sigInitial was specified to OpenEx2W, that is used. Otherwise,
        //the result of the function time(0) is used
        public int Signature => chunk.PeekInt32(4);

        //age
        public int Age => chunk.PeekInt32(8);

        //sig70. if fRepro ("z") is used in the open mode, this is -1. Otherwise, it's a random GUID
        public Guid Guid => chunk.PeekGuid(12);

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        public const int StructSize =
            sizeof(int) + //ImplementationVersion
            sizeof(int) + //Signature
            sizeof(int) + //Age
            16;           //Guid

        internal PDBStream70(MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(PDBStream70), this, ViewKind.PDBStream70);

            s.WriteField("impv", ImplementationVersion, sizeof(int));
            s.WriteField("sig", Signature);
            s.WriteField("age", Age);
            s.WriteField("sig70", Guid);
        }
    }
}
