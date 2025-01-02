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
    public readonly struct RSDSI : ICodeView, IViewable
    {
        public const int RSDSSignature = 0x53445352; //RSDS

        public int Signature { get; }

        /// <summary>
        /// GUID (Globally Unique Identifier) of the associated PDB.
        /// </summary>
        public Guid Guid { get; init; }

        /// <summary>
        /// Iteration of the PDB. The first iteration is 1. The iteration is incremented each time the PDB content is augmented.
        /// </summary>
        public int Age { get; init; }

        /// <summary>
        /// Path to the .pdb file containing debug information for the PE/COFF file.
        /// </summary>
        public string Path { get; init; } //The _max length_ is MAX_PATH * 3

        public RawOffset Offset { get; }

        internal const int FixedStructSize =
            sizeof(int) +     //Signature
            sizeof(int) * 4 + //Guid
            sizeof(int);      //Age

        internal RSDSI(IFileReader reader, int signature)
        {
            //Signature has already been read
            Offset = (RawOffset) reader.Position - 4;

            Signature = signature;

            if (Signature != RSDSSignature)
                throw new BadImageFormatException("Unexpected CodeView data signature value.");

            reader.FillBuffer(FixedStructSize - sizeof(int));

            Guid = reader.ReadGuid();
            Age = reader.ReadInt32();
            Path = reader.ReadAnsiNullTerminatedString();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(RSDSI), this, ViewKind.RSDSI);

            s.WriteField("dwSig", Signature);
            s.WriteField("guidSig", Guid);
            s.WriteField("age", Age);
            s.WriteAnsiNullTerminatedField("szPdb", Path);
        }

        public override string ToString()
        {
            return Path;
        }
    }
}
