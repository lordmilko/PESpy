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

        public uint Signature => chunk.PeekUInt32(0);
        
        public uint StrucVersion => chunk.PeekUInt32(4);

        #region FileVersionMS

        public int FileVersionMS => chunk.PeekInt32(8);

        public ushort FileVersionMinor => (ushort) (FileVersionMS & 0xffff);

        public ushort FileVersionMajor => (ushort) (FileVersionMS >> 16);

        #endregion
        #region FileVersionLS

        public int FileVersionLS => chunk.PeekInt32(12);

        public ushort FileVersionRevision => (ushort) (FileVersionLS & 0xffff);

        public ushort FileVersionBuild => (ushort) (FileVersionLS >> 16);

        #endregion
        #region ProductVersionMS

        public int ProductVersionMS => chunk.PeekInt32(16);

        public ushort ProductVersionMinor => (ushort) (ProductVersionMS & 0xffff);

        public ushort ProductVersionMajor => (ushort) (ProductVersionMS >> 16);

        #endregion
        #region ProductVersionLS

        public int ProductVersionLS => chunk.PeekInt32(20);

        public ushort ProductVersionRevision => (ushort) (ProductVersionLS & 0xffff);

        public ushort ProductVersionBuild => (ushort) (ProductVersionLS >> 16);

        #endregion

        public uint FileFlagsMask => chunk.PeekUInt32(24);
        
        public VS_FF FileFlags => (VS_FF) chunk.PeekUInt32(28);
        
        public VOS FileOS => (VOS) chunk.PeekUInt32(32);
        
        public uint FileType => chunk.PeekUInt32(36);
        
        public uint FileSubtype => chunk.PeekUInt32(40);

        public uint FileDateMS => chunk.PeekUInt32(44);

        public uint FileDateLS => chunk.PeekUInt32(48);

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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("dwSignature", Signature);
            s.WriteField("dwStrucVersion", StrucVersion);

            s.WriteField("dwFileVersionMS", FileVersionMS);
            s.WriteField("dwFileVersionLS", FileVersionLS);
            s.WriteField("dwProductVersionMS", ProductVersionMS);
            s.WriteField("dwProductVersionLS", ProductVersionLS);

            s.WriteField("dwFileFlagsMask", FileFlagsMask);
            s.WriteField("dwFileFlags", FileFlags, sizeof(int));
            s.WriteField("dwFileOS", FileOS, sizeof(int));
            s.WriteField("dwFileType", FileType);
            s.WriteField("dwFileSubtype", FileSubtype);
            s.WriteField("dwFileDateMS", FileDateMS);
            s.WriteField("dwFileDateLS", FileDateLS);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
