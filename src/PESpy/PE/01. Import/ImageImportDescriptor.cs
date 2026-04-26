using System;
using System.ComponentModel;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    //https://learn.microsoft.com/en-us/archive/msdn-magazine/2002/march/inside-windows-an-in-depth-look-into-the-win32-portable-executable-file-format-part-2

    /// <summary>
    /// Represents the <see cref="IMAGE_IMPORT_DESCRIPTOR"/> structure.
    /// </summary>
    public class ImageImportDescriptor : IValue, IViewable //A class so that we don't have to keep recreating [Original]FirstThunk depending on which struct copy loaded it
    {
        internal const int OriginalFirstThunkOffset = 0;
        private const int TimeDateStampOffset = 4;
        private const int ForwarderChainOffset = 8;
        internal const int NameOffset = 12;
        internal const int FirstThunkOffset = 16;

        /// <summary>
        /// The RVA of the import lookup table. This table contains a name or ordinal for each import.
        /// </summary>
        private RVA<ImageThunkDataList>? originalFirstThunk;

        public RVA<ImageThunkDataList> OriginalFirstThunk
        {
            get
            {
                if (originalFirstThunk == null)
                {
                    var rva = chunk.PeekInt32(OriginalFirstThunkOffset);

                    if (chunk.PEFile().TryGetValueChunkFromSection(rva, out var valueChunk))
                        originalFirstThunk = ParseThunks(rva, valueChunk, false);
                    else
                        originalFirstThunk = new RVA<ImageThunkDataList>(rva);
                }

                return originalFirstThunk.Value;
            }
        }

        /// <summary>
        /// The stamp that is set to zero until the image is bound. After the image is bound, this field is set to the time/data stamp of the DLL.
        /// </summary>
        public Timestamp TimeDateStamp => chunk.PeekUInt32(TimeDateStampOffset);

        /// <summary>
        /// The index of the first forwarder reference.
        /// </summary>
        public int ForwarderChain => chunk.PeekInt32(ForwarderChainOffset);

        /// <summary>
        /// The address of an ASCII string that contains the name of the DLL. This address is relative to the image base.
        /// </summary>
        private RVA<AnsiString>? name;

        public RVA<AnsiString> Name
        {
            get
            {
                if (name == null)
                {
                    var rva = chunk.PeekInt32(NameOffset);

                    if (chunk.PEFile().TryGetValueChunkFromSection(rva, out var valueChunk))
                    {
                        var str = valueChunk.PeekAnsiNullTerminatedString(0);
                        name = new RVA<AnsiString>(rva, valueChunk.AbsoluteOffset, str);
                    }
                    else
                        name = new RVA<AnsiString>(rva);
                }

                return name.Value;
            }
        }

        /// <summary>
        /// The RVA of the import address table. The contents of this table are identical to the contents of the import lookup table until the image is bound.
        /// </summary>
        private RVA<ImageThunkDataList>? firstThunk;

        public RVA<ImageThunkDataList> FirstThunk
        {
            get
            {
                if (firstThunk == null)
                {
                    var rva = chunk.PeekInt32(FirstThunkOffset);

                    if (chunk.PEFile().TryGetValueChunkFromSection(rva, out var valueChunk))
                        firstThunk = ParseThunks(rva, valueChunk, true);
                    else
                        firstThunk = new RVA<ImageThunkDataList>(rva);
                }

                return firstThunk.Value;
            }
        }

        //"ILT" vs "IAT" are confusing enough; having to decipher "OriginalFirstThunk" and "FirstThunk" makes it even more confusing.
        //Our design goal however is try and mirror the native API definitions; so we define hidden ILT/IAT members that simply redirect
        //to the "true" native members

        /// <summary>
        /// Gets the Import Lookup Table (<see cref="OriginalFirstThunk"/>) representing the original imports as they existed on disk.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public RVA<ImageThunkDataList> ImportLookupTable => OriginalFirstThunk;

        /// <summary>
        /// Gets the Import Address Table (<see cref="FirstThunk"/>) representing the actual imports once they've been loaded into memory.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public RVA<ImageThunkDataList> ImportAddressTable => FirstThunk;

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //OriginalFirstThunk
            sizeof(int) + //TimeDateStamp
            sizeof(int) + //ForwarderChain
            sizeof(int) + //Name
            sizeof(int);  //FirstThunk

        private readonly MemoryChunk chunk;

        internal ImageImportDescriptor(in MemoryChunk chunk)
        {
            this.chunk = chunk;

#if STRESS_TEST
            _ = OriginalFirstThunk;
            _ = Name;
            _ = FirstThunk;
#endif
        }

        internal static RVA<ImageThunkDataList> ParseThunks(int rva, in MemoryChunk valueChunk, bool isIAT)
        {
            return new RVA<ImageThunkDataList>(rva, valueChunk.AbsoluteOffset, new ImageThunkDataList(valueChunk, null, isIAT));
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            var structOffset = Offset;

            using var _ = writer.EnterTag(ViewTag.Import);

            if (OriginalFirstThunk.IsValid && OriginalFirstThunk.ListedOffset != 0)
            {
                using var r = writer.CreateScopedRegion(
                    OriginalFirstThunk.ActualOffset,
                    structOffset,
                    OriginalFirstThunkOffset,
                    $"[ImportLookupTable] {Name}",
                    ViewKind.ImportLookupTable,
                    ViewKind.ImageThunkData
#if DEBUG
                    , OriginalFirstThunk.ListedOffset
#endif
                );

                r.WriteUnique<ImageThunkDataList, ImageThunkDataList.Enumerator, ImageThunkData>(OriginalFirstThunk.Value);
            }

            //This name may also be written by ImageEnclaveImport
            writer.WriteUniqueRVAAnsiNullTerminatedField(Name, ViewKind.ImageImportDescriptor_Name, structOffset, fieldOffset: NameOffset);

            if (FirstThunk.IsValid && FirstThunk.ListedOffset != 0)
            {
                using var r = writer.CreateScopedRegion(
                    FirstThunk.ActualOffset,
                    structOffset,
                    FirstThunkOffset,
                    $"[ImportAddressTable] {Name}",
                    ViewKind.ImportAddressTable,
                    ViewKind.ImageThunkData
#if DEBUG
                    , FirstThunk.ListedOffset
#endif
                );

                r.WriteUnique<ImageThunkDataList, ImageThunkDataList.Enumerator, ImageThunkData>(FirstThunk.Value);
            }
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.ImageImportDescriptor, StructSize);

        int IViewable.NumChildren() => 5;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteRVAField<ImageThunkDataList, ImageThunkDataList.Enumerator, ImageThunkData>(nameof(OriginalFirstThunk), OriginalFirstThunkOffset, OriginalFirstThunk);
                    break;

                case 1:
                    structWriter.WriteField(nameof(TimeDateStamp), TimeDateStampOffset, TimeDateStamp);
                    break;

                case 2:
                    structWriter.WriteField(nameof(ForwarderChain), ForwarderChainOffset, ForwarderChain);
                    break;

                case 3:
                    structWriter.WriteRVAAnsiNullTerminatedField(nameof(Name), NameOffset, Name);
                    break;

                case 4:
                    structWriter.WriteRVAField<ImageThunkDataList, ImageThunkDataList.Enumerator, ImageThunkData>(nameof(FirstThunk), FirstThunkOffset, FirstThunk);
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
