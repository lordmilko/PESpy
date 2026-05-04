using System;
using PESpy.View;

namespace PESpy.PDB
{
    //Type is made up
    public readonly struct MergedAssemblyInfo : IViewableValue
    {
        private const int TimestampOffset = 0;
        private const int IndexOffset = 4;
        private const int VersionInfoOffset = 8;
        private int NameOffset => 8 + VersionLength;

        public Timestamp Timestamp { get; }

        public int Index { get; }

        public bool IsPDB { get; }

        public VsVersionInfo VersionInfo { get; }

        public NativeSpan<byte> VersionBytes { get; }

        public unsafe int VersionLength => *(ushort*) (byte*) VersionBytes;

        //Don't know whether it's actually ANSI or UTF-8
        public Utf8String Name { get; }

        //Note that the struct size needs to be 32-bit aligned
        internal int StructSize =>
            (sizeof(int) + //Timestamp
            sizeof(int) + //Index
            VersionLength +
            Name.Length + 1 + 3) & ~3;

        public long Offset { get; }

        //VsVersionInfo allocates, so just eagerly read everything

        internal MergedAssemblyInfo(in MemoryChunk chunk)
        {
            Offset = chunk.AbsoluteOffset;

            Timestamp = chunk.PeekUInt32(TimestampOffset);
            var index = chunk.PeekUInt32(IndexOffset);

            //If the high bit is set, it's a PDB, which means you should clear the index in order to use it
            if ((index >> 31) != 0)
            {
                IsPDB = true;
                index &= 0x7fffffff; //Clear the high bit
            }
            else
                IsPDB = false;

            Index = (int) index;

            var versionChunk = chunk.Slice(VersionInfoOffset);

            //The only value that is guaranteed to be present in the version is the length; which means if the length
            //is merely "2", there won't be an actual VS_VERSION_INFO!

            var length = versionChunk.PeekUInt16(0);

            //Check whether the length is large enough to contain "VS_VERSION_INFO"
            if (length >= 38 && versionChunk.PeekUtf16FixedLength(VsVersionInfo.KeyOffset, 15) == "VS_VERSION_INFO")
                VersionInfo = new VsVersionInfo(versionChunk);

            VersionBytes = versionChunk.PeekNativeSpan<byte>(0, length);
            Name = chunk.PeekUtf8NullTerminatedString(NameOffset);
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.MergedAssemblyInfo, StructSize);

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Timestamp), TimestampOffset, Timestamp);
                    break;

                case 1:
                    structWriter.WriteField(nameof(Index), IndexOffset, (uint) (IsPDB ? 0x80000000 | Index : Index));
                    break;

                case 2:
                    if (VersionInfo != null)
                        structWriter.WriteInline(VersionInfo);
                    else
                        structWriter.WriteField("Version", VersionInfoOffset, VersionBytes);
                    break;

                case 3:
                    structWriter.WriteUtf8NullTerminatedField(nameof(Name), NameOffset, Name);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
