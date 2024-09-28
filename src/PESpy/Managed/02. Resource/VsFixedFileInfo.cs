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

        public uint Signature { get; init; }          // e.g. 0xfeef04bd
        
        public uint StrucVersion { get; init; }       // e.g. 0x00000042 = "0.42"

        #region FileVersionMS

        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int FileVersionMS { get; init; }

        public ushort FileVersionMinor => (ushort) (FileVersionMS & 0xffff);

        public ushort FileVersionMajor => (ushort) (FileVersionMS >> 16);

        #endregion
        #region FileVersionLS

        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int FileVersionLS { get; init; }

        public ushort FileVersionRevision => (ushort) (FileVersionLS & 0xffff);

        public ushort FileVersionBuild => (ushort) (FileVersionLS >> 16);

        #endregion
        #region ProductVersionMS

        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int ProductVersionMS { get; init; }

        public ushort ProductVersionMinor => (ushort) (ProductVersionMS & 0xffff);

        public ushort ProductVersionMajor => (ushort) (ProductVersionMS >> 16);

        #endregion
        #region ProductVersionLS

        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int ProductVersionLS { get; init; }

        public ushort ProductVersionRevision => (ushort) (ProductVersionLS & 0xffff);

        public ushort ProductVersionBuild => (ushort) (ProductVersionLS >> 16);

        #endregion

        public uint FileFlagsMask { get; init; }      // = 0x3F for version "0.42"
        
        public VS_FF FileFlags { get; init; }
        
        public VOS FileOS { get; init; }             // e.g. VOS_DOS_WINDOWS16
        
        public uint FileType { get; init; }           // e.g. VFT_DRIVER
        
        public uint FileSubtype { get; init; }        // e.g. VFT2_DRV_KEYBOARD

        public uint FileDateMS { get; init; }         // e.g. 0

        public uint FileDateLS { get; init; }         // e.g. 0

        public Version FileVersion => new Version(FileVersionMajor, FileVersionMinor, FileVersionBuild, FileVersionRevision);

        public Version ProductVersion => new Version(ProductVersionMajor, ProductVersionMinor, ProductVersionBuild, ProductVersionRevision);

        public RawOffset Offset { get; }

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

        internal VsFixedFileInfo(ref FileReader reader)
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

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(VS_FIXEDFILEINFO), this, ViewKind.VsFixedFileInfo);

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
        }
    }
}
