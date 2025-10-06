using System.Diagnostics;
using ClrDebug;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="STORAGESIGNATURE"/> type that is pointed to by <see cref="IMAGE_COR20_HEADER.MetaData"/>.
    /// </summary>
    public readonly struct StorageSignature : IValue, IViewable
    {
        public const uint STORAGE_MAGIC_SIG = 0x424A5342; //BSJB

        /// <summary>
        /// Magic signature for physical metadata : 0x424A5342.
        /// </summary>
        public uint Signature => chunk.PeekUInt32(0);

        public short MajorVersion => chunk.PeekInt16(4);

        public short MinorVersion => chunk.PeekInt16(6);

        public int ExtraData => chunk.PeekInt32(8);

        public int VersionStringLength => chunk.PeekInt32(12);

        //The version string is a bit weird. It's really more of a null padded string, but VersionStringLength bytes
        //are allocated for it
        public FixedUtf8String Version => chunk.PeekNullPaddedUtf8(16, VersionStringLength);

        public int Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            sizeof(uint) + //Signature
            sizeof(short) + //MajorVersion
            sizeof(short) + //MinorVersion
            sizeof(int) + //ExtraData
            sizeof(int); //VersionStringLength

        internal int StructSize =>
            FixedStructSize +
            VersionStringLength;

        private readonly MemoryChunk chunk;

        internal StorageSignature(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.STORAGESIGNATURE, this, ViewKind.StorageSignature, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("iSignature", Signature);
            s.WriteField("iMajorVer", MajorVersion);
            s.WriteField("iMinorVer", MinorVersion);
            s.WriteField("iExtraData", ExtraData);
            s.WriteField("iVersionString", VersionStringLength);
            s.WriteUTF8FixedLengthField("pVersion", Version);

            s.Align(4);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
