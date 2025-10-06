using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    public class PDBStream70 : PDBStream, IValue, IViewable //Header could either be PDBStream or PDBStream70, so must be a class
    {
        //sig70. if fRepro ("z") is used in the open mode, this is -1. Otherwise, it's a random GUID
        public Guid Guid
        {
            get => chunk.PeekGuid(12);
            set => chunk.PokeGuid(12, value);
        }

        internal new const int StructSize =
            sizeof(int) + //ImplementationVersion
            sizeof(int) + //Signature
            sizeof(int) + //Age
            16;           //Guid

        internal PDBStream70(in MemoryChunk chunk) : base(chunk)
        {
        }

        protected override IView? WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.PDBStream70, this, ViewKind.PDBStream70, StructSize);

        protected override IView[] GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("impv", ImplementationVersion, sizeof(int));
            s.WriteField("sig", Signature);
            s.WriteField("age", Age);
            s.WriteField("sig70", Guid);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
