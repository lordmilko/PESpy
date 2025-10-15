using System;
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
        private const int SignatureOffset = 0;
        private const int MajorVersionOffset = 4;
        private const int MinorVersionOffset = 6;
        private const int ExtraDataOffset = 8;
        private const int VersionStringLengthOffset = 12;
        private const int VersionOffset = 16;

        /// <summary>
        /// Magic signature for physical metadata : 0x424A5342.
        /// </summary>
        public uint Signature => chunk.PeekUInt32(SignatureOffset);

        public short MajorVersion => chunk.PeekInt16(MajorVersionOffset);

        public short MinorVersion => chunk.PeekInt16(MinorVersionOffset);

        public int ExtraData => chunk.PeekInt32(ExtraDataOffset);

        public int VersionStringLength => chunk.PeekInt32(VersionStringLengthOffset);

        //The version string is a bit weird. It's really more of a null padded string, but VersionStringLength bytes
        //are allocated for it
        public FixedUtf8String Version => chunk.PeekNullPaddedUtf8(VersionOffset, VersionStringLength);

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

        int IViewable.NumChildren => StructWriter.GetNumChildrenAlign4(6, StructSize);

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField("iSignature", SignatureOffset, Signature);
                    break;

                case 1:
                    structWriter.WriteField("iMajorVer", MajorVersionOffset, MajorVersion);
                    break;

                case 2:
                    structWriter.WriteField("iMinorVer", MinorVersionOffset, MinorVersion);
                    break;

                case 3:
                    structWriter.WriteField("iExtraData", ExtraDataOffset, ExtraData);
                    break;

                case 4:
                    structWriter.WriteField("iVersionString", VersionStringLengthOffset, VersionStringLength);
                    break;

                case 5:
                    structWriter.WriteUtf8FixedLengthField("pVersion", VersionOffset, Version);
                    break;

                case 6:
                    structWriter.AlignOrThrow(StructSize);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
