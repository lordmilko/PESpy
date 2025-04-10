using System.Diagnostics;

namespace PESpy
{
    [DebuggerDisplay("Type = {Type}, Rva = {Rva}, RvaTarget = {RvaTarget}, Extra = {Extra}")]
    public readonly struct XFixupData : IValue
    {
        public short Type { get; }

        public short Extra { get; }

        public int Rva { get; }

        public int RvaTarget { get; }

        public int Offset { get; }

        internal const int StructSize =
            sizeof(short) +
            sizeof(short) +
            sizeof(int) +
            sizeof(int);

        internal XFixupData(IFileReader reader)
        {
            Offset = (int) reader.Position;

            //PEAnatomist thinks that these types correspond with the IMAGE_REL_* type used in ImageRelocation. So if the IMAGE_FILE_MACHINE
            //is I386, use ImageRelI386
            Type = reader.ReadInt16();
            Extra = reader.ReadInt16();
            Rva = reader.ReadInt32();
            RvaTarget = reader.ReadInt32();
        }
    }
}
