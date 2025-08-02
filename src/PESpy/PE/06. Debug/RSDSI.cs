using System;
using PESpy.View;

namespace PESpy
{

    /// <summary>
    /// Represents the <see cref="Native.RSDSI"/> structure.
    /// </summary>
    public class RSDSI : ICodeViewPDB, IViewable //It's going to be boxed
    {
        public CodeViewSig Signature => (CodeViewSig) chunk.PeekUInt32(0);

        /// <summary>
        /// GUID (Globally Unique Identifier) of the associated PDB.
        /// </summary>
        public Guid Guid => chunk.PeekGuid(4);

        /// <summary>
        /// Iteration of the PDB. The first iteration is 1. The iteration is incremented each time the PDB content is augmented.
        /// </summary>
        public int Age => chunk.PeekInt32(20);

        /// <summary>
        /// Path to the .pdb file containing debug information for the PE/COFF file.
        /// </summary>
        public AnsiString Path => chunk.PeekAnsiNullTerminatedString(24); //microsoft-pdb's LOCATOR says to use UTF 8 when it's RSDS, but our ICodeView interface wants an AnsiString

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
            writer.NewStruct(Strings.RSDSI, this, ViewKind.RSDSI, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("dwSig", Signature, sizeof(uint));
            s.WriteField("guidSig", Guid);
            s.WriteField("age", Age);
            s.WriteAnsiNullTerminatedField("szPdb", Path);

            return s.ToArray();
        }

        public override string ToString()
        {
            return Path.ToString();
        }
    }
}
