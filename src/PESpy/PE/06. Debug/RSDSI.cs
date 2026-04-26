using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{

    /// <summary>
    /// Represents the <see cref="Native.RSDSI"/> structure.
    /// </summary>
    public class RSDSI : ICodeViewPDB, IViewable //It's going to be boxed
    {
        private const int SignatureOffset = 0;
        private const int GuidOffset = 4;
        private const int AgeOffset = 20;
        private const int PathOffset = 24;

        public CodeViewSig Signature => (CodeViewSig) chunk.PeekUInt32(SignatureOffset);

        /// <summary>
        /// GUID (Globally Unique Identifier) of the associated PDB.
        /// </summary>
        public Guid Guid => chunk.PeekGuid(GuidOffset);

        /// <summary>
        /// Iteration of the PDB. The first iteration is 1. The iteration is incremented each time the PDB content is augmented.
        /// </summary>
        public int Age => chunk.PeekInt32(AgeOffset);

        /// <summary>
        /// Path to the .pdb file containing debug information for the PE/COFF file.
        /// </summary>
        public AnsiString Path => chunk.PeekAnsiNullTerminatedString(PathOffset); //microsoft-pdb's LOCATOR says to use UTF 8 when it's RSDS, but our ICodeView interface wants an AnsiString

        public int Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            sizeof(int) +     //Signature
            sizeof(int) * 4 + //Guid
            sizeof(int);      //Age

        internal int StructSize =>
            FixedStructSize + Path.Length + 1;

        private readonly MemoryChunk chunk;

        internal RSDSI(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.RSDSI, StructSize);

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField("dwSig", SignatureOffset, (int) Signature, FieldViewFlags.HexString);
                    break;

                case 1:
                    structWriter.WriteField("guidSig", GuidOffset, Guid);
                    break;

                case 2:
                    structWriter.WriteField("age", AgeOffset, Age);
                    break;

                case 3:
                    structWriter.WriteAnsiNullTerminatedField("szPdb", PathOffset, Path);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return Path.ToString();
        }
    }
}
