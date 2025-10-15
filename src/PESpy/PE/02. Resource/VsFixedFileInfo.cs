using System;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="VS_FIXEDFILEINFO"/> structure.
    /// </summary>
    [DebuggerDisplay("File = {FileVersion.ToString(),nq}, Product = {ProductVersion.ToString(),nq}")]
    public class VsFixedFileInfo : IValue, IViewable //This is a class so that it can be null without needing to use Nullable<T>
    {
        public const uint FixedFileInfoSignature = 0xFEEF04BD;
        private const int SignatureOffset = 0;
        private const int StrucVersionOffset = 4;
        private const int FileVersionMSOffset = 8;
        private const int FileVersionLSOffset = 12;
        private const int ProductVersionMSOffset = 16;
        private const int ProductVersionLSOffset = 20;
        private const int FileFlagsMaskOffset = 24;
        private const int FileFlagsOffset = 28;
        private const int FileOSOffset = 32;
        private const int FileTypeOffset = 36;
        private const int FileSubtypeOffset = 40;
        private const int FileDateMSOffset = 44;
        private const int FileDateLSOffset = 48;

        public uint Signature => chunk.PeekUInt32(SignatureOffset);

        public uint StrucVersion => chunk.PeekUInt32(StrucVersionOffset);

        #region FileVersionMS

        public int FileVersionMS => chunk.PeekInt32(FileVersionMSOffset);

        public ushort FileVersionMinor => (ushort) (FileVersionMS & 0xffff);

        public ushort FileVersionMajor => (ushort) (FileVersionMS >> 16);

        #endregion
        #region FileVersionLS

        public int FileVersionLS => chunk.PeekInt32(FileVersionLSOffset);

        public ushort FileVersionRevision => (ushort) (FileVersionLS & 0xffff);

        public ushort FileVersionBuild => (ushort) (FileVersionLS >> 16);

        #endregion
        #region ProductVersionMS

        public int ProductVersionMS => chunk.PeekInt32(ProductVersionMSOffset);

        public ushort ProductVersionMinor => (ushort) (ProductVersionMS & 0xffff);

        public ushort ProductVersionMajor => (ushort) (ProductVersionMS >> 16);

        #endregion
        #region ProductVersionLS

        public int ProductVersionLS => chunk.PeekInt32(ProductVersionLSOffset);

        public ushort ProductVersionRevision => (ushort) (ProductVersionLS & 0xffff);

        public ushort ProductVersionBuild => (ushort) (ProductVersionLS >> 16);

        #endregion

        public uint FileFlagsMask => chunk.PeekUInt32(FileFlagsMaskOffset);

        public VS_FF FileFlags => (VS_FF) chunk.PeekUInt32(FileFlagsOffset);

        public VOS FileOS => (VOS) chunk.PeekUInt32(FileOSOffset);

        public uint FileType => chunk.PeekUInt32(FileTypeOffset);

        public uint FileSubtype => chunk.PeekUInt32(FileSubtypeOffset);

        public uint FileDateMS => chunk.PeekUInt32(FileDateMSOffset);

        public uint FileDateLS => chunk.PeekUInt32(FileDateLSOffset);

        public Version FileVersion => new Version(FileVersionMajor, FileVersionMinor, FileVersionBuild, FileVersionRevision);

        public Version ProductVersion => new Version(ProductVersionMajor, ProductVersionMinor, ProductVersionBuild, ProductVersionRevision);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) +    //Signature
            sizeof(int) +    //StrucVersion
            sizeof(ushort) + //FileVersionMinor
            sizeof(ushort) + //FileVersionMajor
            sizeof(ushort) + //FileVersionRevision
            sizeof(ushort) + //FileVersionBuild
            sizeof(ushort) + //ProductVersionMinor
            sizeof(ushort) + //ProductVersionMajor
            sizeof(ushort) + //ProductVersionRevision
            sizeof(ushort) + //ProductVersionBuild
            sizeof(int) +    //FileFlagsMask
            sizeof(int) +    //FileFlags
            sizeof(int) +    //FileOS
            sizeof(int) +    //FileType
            sizeof(int) +    //FileSubtype
            sizeof(int) +    //FileDateMS
            sizeof(int);     //FileDateLS

        private readonly MemoryChunk chunk;

        internal VsFixedFileInfo(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.VS_FIXEDFILEINFO, this, ViewKind.VsFixedFileInfo, StructSize);

        int IViewable.NumChildren => 13;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField("dwSignature", SignatureOffset, Signature);
                    break;

                case 1:
                    structWriter.WriteField("dwStrucVersion", StrucVersionOffset, StrucVersion);
                    break;

                case 2:
                    structWriter.WriteField("dwFileVersionMS", FileVersionMSOffset, FileVersionMS);
                    break;

                case 3:
                    structWriter.WriteField("dwFileVersionLS", FileVersionLSOffset, FileVersionLS);
                    break;

                case 4:
                    structWriter.WriteField("dwProductVersionMS", ProductVersionMSOffset, ProductVersionMS);
                    break;

                case 5:
                    structWriter.WriteField("dwProductVersionLS", ProductVersionLSOffset, ProductVersionLS);
                    break;

                case 6:
                    structWriter.WriteField("dwFileFlagsMask", FileFlagsMaskOffset, FileFlagsMask);
                    break;

                case 7:
                    structWriter.WriteField("dwFileFlags", FileFlagsOffset, FileFlags, sizeof(int));
                    break;

                case 8:
                    structWriter.WriteField("dwFileOS", FileOSOffset, FileOS, sizeof(int));
                    break;

                case 9:
                    structWriter.WriteField("dwFileType", FileTypeOffset, FileType);
                    break;

                case 10:
                    structWriter.WriteField("dwFileSubtype", FileSubtypeOffset, FileSubtype);
                    break;

                case 11:
                    structWriter.WriteField("dwFileDateMS", FileDateMSOffset, FileDateMS);
                    break;

                case 12:
                    structWriter.WriteField("dwFileDateLS", FileDateLSOffset, FileDateLS);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
