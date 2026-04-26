using System;
using PESpy.View;

namespace PESpy.PDB
{
    public class PDBStream70 : PDBStream, IValue, IViewable //Header could either be PDBStream or PDBStream70, so must be a class
    {
        private const int GuidOffset = 12;

        //sig70. if fRepro ("z") is used in the open mode, this is -1. Otherwise, it's a random GUID
        public Guid Guid
        {
            get => chunk.PeekGuid(GuidOffset);
            set => chunk.PokeGuid(GuidOffset, value);
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
            writer.NewStruct(this, ViewKind.PDBStream70, StructSize);

        protected override int NumChildren => 4;

        protected override void WriteChild(int index, ref StructWriter structWriter)
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

                case 3:
                    structWriter.WriteField("sig70", GuidOffset, Guid);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
