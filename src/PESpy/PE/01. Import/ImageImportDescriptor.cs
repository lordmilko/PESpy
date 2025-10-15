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
        private RVA<ImageThunkData[]>? originalFirstThunk;

        public RVA<ImageThunkData[]> OriginalFirstThunk
        {
            get
            {
                if (originalFirstThunk == null)
                {
                    var rva = chunk.PeekInt32(OriginalFirstThunkOffset);

                    if (chunk.PEFile().TryGetValueChunkFromSection(rva, out var valueChunk))
                        originalFirstThunk = ParseThunks(rva, valueChunk, false);
                    else
                        originalFirstThunk = new RVA<ImageThunkData[]>(rva);
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
        private RVA<ImageThunkData[]>? firstThunk;

        public RVA<ImageThunkData[]> FirstThunk
        {
            get
            {
                if (firstThunk == null)
                {
                    var rva = chunk.PeekInt32(FirstThunkOffset);

                    if (chunk.PEFile().TryGetValueChunkFromSection(rva, out var valueChunk))
                        firstThunk = ParseThunks(rva, valueChunk, true);
                    else
                        firstThunk = new RVA<ImageThunkData[]>(rva);
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
        public RVA<ImageThunkData[]> ImportLookupTable => OriginalFirstThunk;

        /// <summary>
        /// Gets the Import Address Table (<see cref="FirstThunk"/>) representing the actual imports once they've been loaded into memory.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public RVA<ImageThunkData[]> ImportAddressTable => FirstThunk;

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

        internal static RVA<ImageThunkData[]> ParseThunks(int rva, in MemoryChunk valueChunk, bool isIAT)
        {
            using var results = new PooledList<ImageThunkData>();

            var size = valueChunk.PointerSize;

            ImageThunkData thunk;

            var read = 0;

            do
            {
                thunk = new ImageThunkData(valueChunk.Slice(read), isIAT);
                results.Add(thunk);
                read += size;
            } while (thunk.Value != 0); //Needs to be a value that works for both loaded and unloaded thunks

            return new RVA<ImageThunkData[]>(rva, valueChunk.AbsoluteOffset, results.ToArray());
        }

        internal static ImageThunkData[] ParseIATThunks(in MemoryChunk valueChunk, int directorySize)
        {
            //Unlike when parsing thunks for a particular import descriptor, when parsing thunks for the whole IAT,
            //we don't stop when a null thunk is hitl we stop when we reach the end

            var ptrSize = valueChunk.PointerSize;

            var results = new ImageThunkData[directorySize / ptrSize];

            for (var i = 0; i < results.Length; i++)
                results[i] = new ImageThunkData(valueChunk.Slice(i * ptrSize), true);

            return results;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            using var _ = writer.EnterTag(ViewTag.Import);

            if (OriginalFirstThunk.IsValid && OriginalFirstThunk.ListedOffset != 0)
            {
                using var r = writer.CreateScopedRegion(
                    OriginalFirstThunk.ActualOffset,
                    OriginalFirstThunkOffset,
                    $"[ImportLookupTable] {Name}",
                    ViewKind.ImportLookupTable,
                    ViewKind.ImageThunkData
#if DEBUG
                    , OriginalFirstThunk.ListedOffset
#endif
                );

                r.WriteUnique(OriginalFirstThunk.Value);
            }

            //This name may also be written by ImageEnclaveImport
            writer.WriteUniqueRVAAnsiNullTerminatedField(Name, ViewKind.ImportName, fieldOffset: NameOffset);

            if (FirstThunk.IsValid && FirstThunk.ListedOffset != 0)
            {
                using var r = writer.CreateScopedRegion(
                    FirstThunk.ActualOffset,
                    FirstThunkOffset,
                    $"[ImportAddressTable] {Name}",
                    ViewKind.ImportAddressTable,
                    ViewKind.ImageThunkData
#if DEBUG
                    , FirstThunk.ListedOffset
#endif
                );

                r.WriteUnique(FirstThunk.Value);
            }
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_IMPORT_DESCRIPTOR, this, ViewKind.ImageImportDescriptor, StructSize);

        int IViewable.NumChildren => 5;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteRVAField(nameof(OriginalFirstThunk), OriginalFirstThunkOffset, OriginalFirstThunk);
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
                    structWriter.WriteRVAField(nameof(FirstThunk), FirstThunkOffset, FirstThunk);
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
