using System;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{

    /// <summary>
    /// Represents the <see cref="Native.RSDSI"/> structure.
    /// </summary>
    public class RSDSI : ICodeViewPDB, IViewable //It's going to be boxed
    {
#if PEFAST
        public CodeViewSig Signature => (CodeViewSig) chunk.PeekUInt32(0);
#else
        public CodeViewSig Signature { get; }
#endif

        /// <summary>
        /// GUID (Globally Unique Identifier) of the associated PDB.
        /// </summary>
#if PEFAST
        public Guid Guid => chunk.PeekGuid(4);
#else
        public Guid Guid { get; init; }
#endif

        /// <summary>
        /// Iteration of the PDB. The first iteration is 1. The iteration is incremented each time the PDB content is augmented.
        /// </summary>
#if PEFAST
        public int Age => chunk.PeekInt32(20);
#else
        public int Age { get; init; }
#endif

        /// <summary>
        /// Path to the .pdb file containing debug information for the PE/COFF file.
        /// </summary>
#if PEFAST
        public AnsiString Path => chunk.PeekAnsiNullTerminatedString(24); //microsoft-pdb's LOCATOR says to use UTF 8 when it's RSDS, but our ICodeView interface wants an AnsiString
#else
        public string Path { get; init; } //The _max length_ is MAX_PATH * 3
#endif

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

        internal const int FixedStructSize =
            sizeof(int) +     //Signature
            sizeof(int) * 4 + //Guid
            sizeof(int);      //Age

        internal int StructSize =>
            FixedStructSize + Path.Length + 1;

#if PEFAST
        private readonly MemoryChunk chunk;

        internal RSDSI(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
#else
        internal RSDSI(IFileReader reader, CodeViewSig signature)
        {
            //Signature has already been read
            Offset = (RawOffset) reader.Position - 4;

            Signature = signature;

            if (Signature != CodeViewSig.RSDS)
                throw new BadImageFormatException("Unexpected CodeView data signature value.");

            reader.FillBuffer(FixedStructSize - sizeof(int));

            Guid = reader.ReadGuid();
            Age = reader.ReadInt32();
            Path = reader.ReadAnsiNullTerminatedString();
        }
#endif

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
