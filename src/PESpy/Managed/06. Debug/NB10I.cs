using System;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="Native.NB10I"/> structure.
    /// </summary>
    public readonly struct NB10I : ICodeView, IViewable
    {
        public const int NB10Signature = 0x3031424E; //NB10

        public int Signature { get; }

        public int dwOffset { get; }

        public int PdbSignature { get; }

        public int Age { get; }

        public string Path { get; } //It's szPdb[MAX_PATH] but I don't think it's actually going to occupy 260 bytes if not needed

        public RawOffset Offset { get; }

        internal const int FixedStructSize =
            sizeof(int) + //Signature
            sizeof(int) + //Offset
            sizeof(int) + //PdbSignature
            sizeof(int);  //Age

        internal NB10I(IFileReader reader, int signature)
        {
            //Signature has already been read
            Offset = (RawOffset) reader.Position - 4;

            Signature = signature;

            if (Signature != NB10Signature)
                throw new BadImageFormatException("Unexpected CodeView data signature value.");

            reader.FillBuffer(FixedStructSize - sizeof(int));

            dwOffset = reader.ReadInt32();
            PdbSignature = reader.ReadInt32();
            Age = reader.ReadInt32();
            Path = reader.ReadAnsiNullTerminatedString();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(NB10I), this, ViewKind.NB10I);

            s.WriteField("dwSig", Signature);
            s.WriteField("dwOffset", dwOffset);
            s.WriteField("sig", PdbSignature);
            s.WriteField("age", Age);
            s.WriteAnsiNullTerminatedField("szPdb", Path);
        }

        public override string ToString()
        {
            return Path;
        }
    }
}
