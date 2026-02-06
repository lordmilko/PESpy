using System;
using System.Diagnostics;
using System.IO;
using PInvoke;

namespace PESpy
{
    /// <summary>
    /// Provides fast access to bitmaps contained in the Cor20 Manifest Resources stream.
    /// </summary>
    internal static class Resource
    {
        unsafe static Resource()
        {
            RTL_OSVERSIONINFOW version;
            Ntdll.RtlGetVersion(&version);
            var isWin7 = version.dwPlatformId == PlatformID.Win32NT && version.dwMajorVersion == 6 && version.dwMinorVersion == 1;
            nuint token;
            var input = new GdiplusStartupInput
            {
                GdiplusVersion = isWin7 ? 1 : 2,
                SuppressBackgroundThread = false,
                SuppressExternalCodecs = false
            };
            GdiPlus.GdiplusStartup(&token, &input, default).ThrowIfFailed();

            //Note that the stream doesn't own the memory, so we don't have to worry about holding onto it.
            //This is not necessarily the case with our ResourceFile type; the caller could pass in a custom
            //UnmanagedResourceStream type
            var stream = (UnmanagedMemoryStream) typeof(Resource).Assembly.GetManifestResourceStream("PESpy.Resource.resources")!;

            var block = new GlobalMemoryBlock(stream.PositionPointer, (int) stream.Length, null!);
            var chunk = new MemoryChunk(block, 0);

            //Resource Manager Header

            Debug.Assert(chunk.PeekUInt32(0) == ResourceFile.MagicNumber);
            Debug.Assert(chunk.PeekInt32(4) == 1); //Version

            //I think in V2 it might be implied you always use DeserializingResourceReader,
            //hence why they utilize the bytes to skip to skip over the types
            var read = chunk.PeekInt32(8) + 12; //We've now read 12 bytes

            //Runtime Resource Header
            Debug.Assert(chunk.PeekInt32(read) == 2);

            var numResources = chunk.PeekInt32(read + 4);
            Debug.Assert(chunk.PeekInt32(read + 8) == 1); //There should only be one type: the bitmap type
            read += 12;

            //Skip over the type name
            var typeNameLength = chunk.Peek7BitEncodedInt32(read, out var bytesRead);
            read += typeNameLength + bytesRead;

            //Position should be 8 byte aligned
            read = (read + 7) & ~7;

            //The hashes are sorted by their interpretations as _signed_ integers, which means
            //negative hashes will be first
            var nameHashes = chunk.PeekNativeSpan<int>(read, numResources);
            read += numResources * sizeof(int);

            var namePositions = chunk.PeekNativeSpan<int>(read, numResources);
            read += numResources * sizeof(int);

            var dataSectionOffset = chunk.PeekInt32(read);
            read += sizeof(int);

            //Runtime Resource Reader Name Section

            //Contains the name of the resource and the relative offset of it into the data section
            var nameEntries = new (FixedUtf16String name, int offset)[numResources];

            for (var i = 0; i < nameEntries.Length; i++)
            {
                var offset = namePositions[i] + read;

                var name = chunk.Peek7BitEncodedUtf16(offset, out bytesRead);
                var off = chunk.PeekInt32(offset + bytesRead);

                nameEntries[i] = (name, off);
            }

            var bitmaps = new NativeBitmap[numResources];

            //Eagerly read all data. We already checked above that we're data version 2
            for (var i = 0; i < nameEntries.Length; i++)
            {
                var entry = nameEntries[i];

                read = dataSectionOffset + entry.offset;
                var typeCode = (ResourceTypeCode) chunk.Peek7BitEncodedInt32(read, out bytesRead);
                read += bytesRead;

                Debug.Assert(typeCode >= ResourceTypeCode.StartOfUserTypes);
                var typeIndex = typeCode - ResourceTypeCode.StartOfUserTypes;

                var format = (SerializationFormat) chunk.Peek7BitEncodedInt32(read, out bytesRead);
                read += bytesRead;

                Debug.Assert(format == SerializationFormat.ActivatorStream);
                var length = chunk.Peek7BitEncodedInt32(read, out bytesRead);
                read += bytesRead;

                bitmaps[i] = new NativeBitmap(chunk.Pointer + read, length);
            }

            _nameHashes = nameHashes;
            _nameEntries = nameEntries;
            _bitmaps = bitmaps;
        }

        private static NativeSpan<int> _nameHashes;
        private static (FixedUtf16String name, int offset)[] _nameEntries;
        private static NativeBitmap[] _bitmaps;

        public static NativeBitmap block => GetBitmap(nameof(block));

        public static NativeBitmap box => GetBitmap(nameof(box));

        public static NativeBitmap database => GetBitmap(nameof(database));

        public static NativeBitmap document_binary => GetBitmap("document-binary");

        public static NativeBitmap documents_stack => GetBitmap("documents-stack");

        public static NativeBitmap edit_alignment => GetBitmap("edit-alignment");

        public static NativeBitmap edit_style => GetBitmap("edit-style");

        public static NativeBitmap FieldPublic_16_16 => GetBitmap("FieldPublic.16.16");

        public static NativeBitmap folder_open_document_text => GetBitmap("folder-open-document-text");

        public static NativeBitmap folder_struct => GetBitmap("folder-struct");

        public static NativeBitmap folders => GetBitmap(nameof(folders));

        public static NativeBitmap layers_stack => GetBitmap("layers-stack");

        public static NativeBitmap magnifier => GetBitmap(nameof(magnifier));

        public static NativeBitmap MethodPublic_16_16 => GetBitmap("MethodPublic.16.16");

        public static NativeBitmap money_coin => GetBitmap("money-coin");

        public static NativeBitmap rocket_fly => GetBitmap("rocket-fly");

        public static NativeBitmap script_text => GetBitmap("script-text");

        public static NativeBitmap struct_named => GetBitmap("struct-named");

        public static NativeBitmap struct_stack => GetBitmap("struct-stack");

        public static NativeBitmap StructurePublic_16_16 => GetBitmap("StructurePublic.16.16");

        private static NativeBitmap GetBitmap(string name)
        {
            //Hash the name
            uint hash = 5381;
            for (int i = 0; i < name.Length; i++)
                hash = ((hash << 5) + hash) ^ name[i];

            var signedHash = (int) hash;

            var nameHashes = _nameHashes;

            var lo = 0;
            var hi = nameHashes.Length - 1;
            var index = -1;

            while (lo <= hi)
            {
                index = (lo + hi) >> 1;

                var currentHash = nameHashes[index];

                if (signedHash == currentHash)
                {
                    //Success; get the upper and lower bound of all adjacent hashes
                    //that share the same value as us (this works because we're sorted)
                    
                    if (lo != index)
                    {
                        lo = index;

                        while (lo > 0 && nameHashes[lo - 1] == signedHash)
                            lo--;
                    }
                    if (hi != index)
                    {
                        hi = index;

                        while (hi < nameHashes.Length - 1 && nameHashes[hi + 1] == signedHash)
                            hi++;
                    }

                    var nameEntries = _nameEntries;

                    //For all matching hashes, check the name to find the one that matches
                    for (var i = lo; i <= hi; i++)
                    {
                        var entry = nameEntries[i];

                        if (entry.name == name)
                            return _bitmaps[i];
                    }
                }
                else if (currentHash < signedHash)
                    lo = index + 1;
                else
                    hi = index - 1;
            }

            //Should not be reachable
            Debug.Assert(false);
            throw new InvalidOperationException($"Could not find resource '{name}'");
        }
    }
}
