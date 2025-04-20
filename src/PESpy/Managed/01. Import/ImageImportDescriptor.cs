using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
using RVA = System.Int32;
#endif

namespace PESpy
{
    //https://learn.microsoft.com/en-us/archive/msdn-magazine/2002/march/inside-windows-an-in-depth-look-into-the-win32-portable-executable-file-format-part-2

    /// <summary>
    /// Represents the <see cref="IMAGE_IMPORT_DESCRIPTOR"/> structure.
    /// </summary>
    public class ImageImportDescriptor : IValue, IViewable //A class so that we don't have to keep recreating [Original]FirstThunk depending on which struct copy loaded it
    {
        /// <summary>
        /// The RVA of the import lookup table. This table contains a name or ordinal for each import.
        /// </summary>
#if PEFAST
        private RVA<ImageThunkData[]>? originalFirstThunk;

        public RVA<ImageThunkData[]> OriginalFirstThunk
        {
            get
            {
                if (originalFirstThunk == null)
                {
                    //chunk.PeekInt32(0);
                }

                throw new NotImplementedException();
            }
        }
#else
        public RVA<ImageThunkData[]> OriginalFirstThunk { get; init; }
#endif

        /// <summary>
        /// The stamp that is set to zero until the image is bound. After the image is bound, this field is set to the time/data stamp of the DLL.
        /// </summary>
#if PEFAST
        public uint TimeDateStamp => chunk.PeekUInt32(4);
#else
        public uint TimeDateStamp { get; init; }
#endif

        /// <summary>
        /// The index of the first forwarder reference.
        /// </summary>
#if PEFAST
        public int ForwarderChain => chunk.PeekInt32(8);
#else
        public int ForwarderChain { get; init; }
#endif

        /// <summary>
        /// The address of an ASCII string that contains the name of the DLL. This address is relative to the image base.
        /// </summary>
#if PEFAST
        public RVA<AnsiString> Name
        {
            get
            {
                //chunk.PeekInt32(12);
                throw new NotImplementedException();
            }
        }
#else
        public RVA<string> Name { get; init; }
#endif

        /// <summary>
        /// The RVA of the import address table. The contents of this table are identical to the contents of the import lookup table until the image is bound.
        /// </summary>
#if PEFAST
        private RVA<ImageThunkData[]>? firstThunk;

        public RVA<ImageThunkData[]> FirstThunk
        {
            get
            {
                //chunk.PeekInt32(16);
                throw new NotImplementedException();
            }
        }
#else
        public RVA<ImageThunkData[]> FirstThunk { get; init; }
#endif

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

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

        internal const int StructSize =
            sizeof(int) + //OriginalFirstThunk
            sizeof(int) + //TimeDateStamp
            sizeof(int) + //ForwarderChain
            sizeof(int) + //Name
            sizeof(int);  //FirstThunk

#if PEFAST
        private readonly MemoryChunk chunk;

        internal ImageImportDescriptor(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
#else
        internal ImageImportDescriptor(IFileReader reader, PEFile peFile, ImageThunkData[]? importAddressTable)
        {
            Offset = (RawOffset) reader.Position;

            reader.FillBuffer(StructSize);

            var originalFirstThunk = (RVA) reader.ReadInt32();
            TimeDateStamp = reader.ReadUInt32();
            ForwarderChain = reader.ReadInt32();
            var name = (RVA) reader.ReadInt32();
            var firstThunk = (RVA) reader.ReadInt32();

            if (originalFirstThunk == 0 && name == 0 && firstThunk == 0)
            {
                OriginalFirstThunk = default;
                Name = default;
                FirstThunk = default;
                return;
            }

            OriginalFirstThunk = ParseThunks(originalFirstThunk, reader, peFile, null, false);

            if (peFile.TryGetOffset(name, out var offset))
            {
                reader.Seek(offset);
                var str = reader.ReadAnsiNullTerminatedString();
                Name = new RVA<string>(name, offset, str);
            }
            else
                Name = new RVA<string>(name);

            Dictionary<RawOffset, ImageThunkData>? iatCache = null;

            if (importAddressTable != null)
            {
                iatCache = new Dictionary<RawOffset, ImageThunkData>();

                for (var i = 0; i < importAddressTable.Length; i++)
                {
                    var item = importAddressTable[i];

                    iatCache.Add(item.Offset, item);
                }
            }

            FirstThunk = ParseThunks(firstThunk, reader, peFile, iatCache, true);
        }

        internal static RVA<ImageThunkData[]> ParseThunks(RVA rva, IFileReader reader, PEFile peFile, Dictionary<RawOffset, ImageThunkData>? iatCache, bool isIAT)
        {
            if (!peFile.TryGetOffset(rva, out var offset))
                return new RVA<ImageThunkData[]>(rva);

            var is32Bit = peFile.OptionalHeader.Magic == PEMagic.PE32;
            var thunkDataSize = is32Bit ? 4 : 8;

            var results = new List<ImageThunkData>();

            var read = 0;

            while (true)
            {
                var itemOffset = offset + read;

                if (iatCache == null || !iatCache.TryGetValue(itemOffset, out var thunk))
                {
                    reader.Seek(itemOffset);

                    thunk = new ImageThunkData(reader, peFile, is32Bit, isIAT);
                }

                results.Add(thunk);

                if (thunk.Value == 0)
                    break;

                read += thunkDataSize;
            }

            return new RVA<ImageThunkData[]>(rva, offset, results.ToArray());
        }
#endif

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct($"{nameof(IMAGE_IMPORT_DESCRIPTOR)} {Name}", this, ViewKind.ImageImportDescriptor);

            using var _ = writer.EnterTag(ViewTag.Import);

            s.WriteField(nameof(OriginalFirstThunk), (int) OriginalFirstThunk.ListedOffset);
            s.WriteField(nameof(TimeDateStamp), TimeDateStamp);
            s.WriteField(nameof(ForwarderChain), ForwarderChain);
            s.WriteRVAAnsiNullTerminatedField(nameof(Name), Name);

            s.WriteField(nameof(FirstThunk), (int) FirstThunk.ListedOffset);

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

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
