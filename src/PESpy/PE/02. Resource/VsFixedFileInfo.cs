using System;
using System.ComponentModel;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="VS_FIXEDFILEINFO"/> structure.
    /// </summary>
    [DebuggerDisplay("File = {FileVersion.ToString(),nq}, Product = {ProductVersion.ToString(),nq}")]
    public class VsFixedFileInfo : IValue, IViewable //This is a class so that it can be null without needing to use Nullable<T>
    {
        public const uint FixedFileInfoSignature = 0xFEEF04BD;

#if PEFAST
        public uint Signature => chunk.PeekUInt32(0);
#else
        public uint Signature { get; init; }          // e.g. 0xfeef04bd
#endif
        
#if PEFAST
        public uint StrucVersion => chunk.PeekUInt32(4);
#else
        public uint StrucVersion { get; init; }       // e.g. 0x00000042 = "0.42"
#endif

        #region FileVersionMS

#if PEFAST
        public int FileVersionMS => chunk.PeekInt32(8);
#else
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int FileVersionMS { get; init; }
#endif

        public ushort FileVersionMinor => (ushort) (FileVersionMS & 0xffff);

        public ushort FileVersionMajor => (ushort) (FileVersionMS >> 16);

        #endregion
        #region FileVersionLS

#if PEFAST
        public int FileVersionLS => chunk.PeekInt32(12);
#else
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int FileVersionLS { get; init; }
#endif

        public ushort FileVersionRevision => (ushort) (FileVersionLS & 0xffff);

        public ushort FileVersionBuild => (ushort) (FileVersionLS >> 16);

        #endregion
        #region ProductVersionMS

#if PEFAST
        public int ProductVersionMS => chunk.PeekInt32(16);
#else
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int ProductVersionMS { get; init; }
#endif

        public ushort ProductVersionMinor => (ushort) (ProductVersionMS & 0xffff);

        public ushort ProductVersionMajor => (ushort) (ProductVersionMS >> 16);

        #endregion
        #region ProductVersionLS

#if PEFAST
        public int ProductVersionLS => chunk.PeekInt32(20);
#else
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int ProductVersionLS { get; init; }
#endif

        public ushort ProductVersionRevision => (ushort) (ProductVersionLS & 0xffff);

        public ushort ProductVersionBuild => (ushort) (ProductVersionLS >> 16);

        #endregion

#if PEFAST
        public uint FileFlagsMask => chunk.PeekUInt32(24);
#else
        public uint FileFlagsMask { get; init; }      // = 0x3F for version "0.42"
#endif
        
#if PEFAST
        public VS_FF FileFlags => (VS_FF) chunk.PeekUInt32(28);
#else
        public VS_FF FileFlags { get; init; }
#endif
        
#if PEFAST
        public VOS FileOS => (VOS) chunk.PeekUInt32(32);
#else
        public VOS FileOS { get; init; }             // e.g. VOS_DOS_WINDOWS16
#endif
        
#if PEFAST
        public uint FileType => chunk.PeekUInt32(36);
#else
        public uint FileType { get; init; }           // e.g. VFT_DRIVER
#endif
        
#if PEFAST
        public uint FileSubtype => chunk.PeekUInt32(40);
#else
        public uint FileSubtype { get; init; }        // e.g. VFT2_DRV_KEYBOARD
#endif

#if PEFAST
        public uint FileDateMS => chunk.PeekUInt32(44);
#else
        public uint FileDateMS { get; init; }         // e.g. 0
#endif

#if PEFAST
        public uint FileDateLS => chunk.PeekUInt32(48);
#else
        public uint FileDateLS { get; init; }         // e.g. 0
#endif

        public Version FileVersion => new Version(FileVersionMajor, FileVersionMinor, FileVersionBuild, FileVersionRevision);

        public Version ProductVersion => new Version(ProductVersionMajor, ProductVersionMinor, ProductVersionBuild, ProductVersionRevision);

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

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

#if PEFAST
        private readonly MemoryChunk chunk;

        internal VsFixedFileInfo(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
#else
        internal VsFixedFileInfo(IFileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            reader.FillBuffer(StructSize);

            Signature = reader.ReadUInt32();
            StrucVersion = reader.ReadUInt32();

            FileVersionMS = reader.ReadInt32();
            FileVersionLS = reader.ReadInt32();
            ProductVersionMS = reader.ReadInt32();
            ProductVersionLS = reader.ReadInt32();

            FileFlagsMask = reader.ReadUInt32();
            FileFlags = (VS_FF) reader.ReadUInt32();
            FileOS = (VOS) reader.ReadUInt32();
            FileType = reader.ReadUInt32();
            FileSubtype = reader.ReadUInt32();
            FileDateMS = reader.ReadUInt32();
            FileDateLS = reader.ReadUInt32();
        }
#endif

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

            return s.ToArray();
        }
    }
}
