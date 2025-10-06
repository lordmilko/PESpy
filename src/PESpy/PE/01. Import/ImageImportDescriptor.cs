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
        private const int NameOffset = 12;

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
                    var rva = chunk.PeekInt32(0);

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
        public Timestamp TimeDateStamp => chunk.PeekUInt32(4);

        /// <summary>
        /// The index of the first forwarder reference.
        /// </summary>
        public int ForwarderChain => chunk.PeekInt32(8);

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
                    var rva = chunk.PeekInt32(16);

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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteRVAAnsiNullTerminatedField(Name, ViewKind.ImageImportDescriptor_Name, fieldOffset: NameOffset);

            using var _ = writer.EnterTag(ViewTag.Import);

            if (FirstThunk.IsValid && FirstThunk.ListedOffset != 0)
            {
                using var r = writer.CreateScopedRegion(FirstThunk.ActualOffset, $"[ImportAddressTable] {Name}", ViewKind.ImportAddressTable, ViewKind.ImageThunkData);

                r.WriteUnique(FirstThunk.Value);
            }

            if (OriginalFirstThunk.IsValid && OriginalFirstThunk.ListedOffset != 0)
            {
                using var r = writer.CreateScopedRegion(OriginalFirstThunk.ActualOffset, $"[ImportLookupTable] {Name}", ViewKind.ImportLookupTable, ViewKind.ImageThunkData);

                r.WriteUnique(OriginalFirstThunk.Value);
            }
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_IMPORT_DESCRIPTOR, this, ViewKind.ImageImportDescriptor, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(OriginalFirstThunk), (int) OriginalFirstThunk.ListedOffset);
            s.WriteField(nameof(TimeDateStamp), TimeDateStamp);
            s.WriteField(nameof(ForwarderChain), ForwarderChain);
            s.WriteRVAAnsiNullTerminatedField(nameof(Name), Name);
            s.WriteField(nameof(FirstThunk), (int) FirstThunk.ListedOffset);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
