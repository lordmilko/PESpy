using System;
using System.Diagnostics;

namespace PESpy
{
    [DebuggerDisplay("ClientInterfaces = {ClientInterfaces.Length.ToString(),nq}, ServerInterfaces = {ServerInterfaces.Length.ToString(),nq}")]
    public class RpcInfo
    {
        public static readonly Guid NDRTransferSyntax = new Guid("8A885D04-1CEB-11C9-9FE8-08002B104860");
        public static readonly Guid NDR64TransferSyntax = new Guid("71710533-BEBA-4937-8319-B5DBEF9CCC36");

        private static ReadOnlySpan<byte> NDRTransferSyntaxBytes => new byte[] { 0x04, 0x5D, 0x88, 0x8A, 0xEB, 0x1C, 0xC9, 0x11, 0x9F, 0xE8, 0x08, 0x00, 0x2B, 0x10, 0x48, 0x60 };
        private static ReadOnlySpan<byte> NDR64TransferSyntaxBytes => new byte[] { 0x33, 0x05, 0x71, 0x71, 0xBA, 0xBE, 0x37, 0x49, 0x83, 0x19, 0xB5, 0xDB, 0xEF, 0x9C, 0xCC, 0x36 };

        internal static unsafe RpcInfo? Parse(PEFile peFile)
        {
            var sections = peFile.SectionHeaders;

            using var ptrs = new ValueList<int>();
            using var ptrs64 = new ValueList<int>();

            var clients = new ValueList<RpcClientInterface>();
            var servers = new ValueList<RpcServerInterface>();

            try
            {
                for (var i = 0; i < sections.Length; i++)
                {
                    ref var section = ref sections[i];

                    //MRT.exe has the NDR Transfer Syntax GUID in the .rsrc section, which causes us to read bogus
                    //entities. As such, ignore any entities inside the .rsrc section

                    if (section.Name == ".rsrc"u8)
                        continue;

                    var block = peFile.GetSectionBlock(i, section);
                    var blockLength = block.Length;

                    var totalIndex = 0;

                    while (true)
                    {
                        var index = AppHostSignature.KMPSearch(NDRTransferSyntaxBytes, block.LocalPointer + totalIndex, blockLength - totalIndex);

                        if (index != -1)
                        {
                            totalIndex += index;

                            ptrs.Add(totalIndex);
                            totalIndex += NDRTransferSyntaxBytes.Length;
                        }
                        else
                            break;
                    }

                    totalIndex = 0;

                    while (true)
                    {
                        var index = AppHostSignature.KMPSearch(NDR64TransferSyntaxBytes, block.LocalPointer + totalIndex, blockLength - totalIndex);

                        if (index != -1)
                        {
                            totalIndex += index;

                            ptrs.Add(totalIndex);
                            totalIndex += NDR64TransferSyntaxBytes.Length;
                        }
                        else
                            break;
                    }

                    ParseTransferSyntax(ref clients, ref servers, peFile, block, ptrs.Span);
                    ParseTransferSyntax(ref clients, ref servers, peFile, block, ptrs64.Span);

                    ptrs.Clear();
                    ptrs64.Clear();
                }

                if (clients.Count > 0 || servers.Count > 0)
                    return new RpcInfo(clients.ToArray(), servers.ToArray());

                return null;
            }
            finally
            {
                clients.Dispose();
                servers.Dispose();
            }
        }

        public RpcClientInterface[] ClientInterfaces { get; }

        public RpcServerInterface[] ServerInterfaces { get; }

        private RpcInfo(RpcClientInterface[] clientInterfaces, RpcServerInterface[] serverInterfaces)
        {
            ClientInterfaces = clientInterfaces;
            ServerInterfaces = serverInterfaces;
        }

        private static unsafe void ParseTransferSyntax(
            ref ValueList<RpcClientInterface> clients,
            ref ValueList<RpcServerInterface> servers,
            PEFile peFile,
            MemoryBlock block,
            Span<int> offsets)
        {
            /* Things which contain transfer syntax include:
             * 
             * MIDL_SERVER_INFO
             * MIDL_STUBLESS_PROXY_INFO
             * MIDL_SYNTAX_INFO
             * RPC_MESSAGE
             * RPC_SERVER_INTERFACE
             * RPC_CLIENT_INTERFACE
             * 
             * Some of these may not actually be persisted inside the PEFile
             */

            var expectedSize = peFile.Is32Bit ? 68 : 96;

            foreach (var offset in offsets)
            {
                //RPC_SERVER_INTERFACE and RPC_CLIENT_INTERFACE both have a Length field, which allows for
                //an easy sanity check. Their TransferSyntax starts at offset 24, so if we don't have
                //at least 24 bytes, it's an invalid offset
                if (offset < RpcServerInterface.TransferSyntaxOffset)
                    continue;

                var pTransferSyntax = block.LocalPointer + offset;

                var length = *(uint*) (pTransferSyntax - RpcServerInterface.TransferSyntaxOffset);

                //RPC_SERVER_INTERFACE and RPC_CLIENT_INTERFACE are the same shape;
                //the only difference is that the server has DefaultManagerEpv whereas
                //the client has Reserved instead
                if (length != expectedSize)
                    continue;

                //Skip over the TransferSyntax and check whether we've got a pointer to a dispatch table
                var pDispatchTable = pTransferSyntax + RpcSyntaxIdentifier.StructSize;

                var dispatchTable = peFile.Is32Bit ? *(uint*) pDispatchTable : *(ulong*) pDispatchTable;

                var chunk = new MemoryChunk(block, offset - RpcServerInterface.TransferSyntaxOffset);

                if (dispatchTable == 0)
                {
                    //Sounds like a client!
                    var iface = new RpcClientInterface(chunk);

                    clients.Add(iface);
                }
                else
                {
                    //Sounds like a server!
                    var iface = new RpcServerInterface(chunk);

                    servers.Add(iface);
                }
            }
        }

        internal static VA<RpcProtseqEndpoint> ReadRpcProtseqEndpoint(in MemoryChunk chunk, int fieldOffset)
        {
            var va = (long) chunk.PeekPointer(fieldOffset);

            if (va == 0)
                return default;

            var peFile = chunk.PEFile();

            var rva = (int) (va - peFile.OptionalHeader.ImageBase);

            if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
                return new VA<RpcProtseqEndpoint>(va, chunk.AbsoluteOffset, new PESpy.RpcProtseqEndpoint(valueChunk));

            return new VA<RpcProtseqEndpoint>(va);
        }

        internal static VA<NativeSpan<ushort>> ReadFormatStringOffsets(in MemoryChunk chunk, int offsetsFieldOffset, int formatStringFieldOffset)
        {
            /* There doesn't appear to be any hardcoded count of the number of offsets in the array;
             * rather, the information just appears to be splatted in the PEFile, and then each call
             * knows the index it should specify. This is no good. We will attempt to heuristically
             * parse the list of offsets with the observation that each item appears to ascend from
             * the value before it.
             * 
             * If we encounter a value that is less than the value that came before it, that must mean we've gone
             * past the end of the array. We're only in trouble if there's junk after the last valid item which
             * happens to also be greater in size. We can potentially restrict things further by only considering
             * items that are within a certain distance from the previous item we parsed */

            var va = (long) chunk.PeekPointer(offsetsFieldOffset);

            if (va == 0)
                return default;

            var strOffset = (long) chunk.PeekPointer(formatStringFieldOffset);

            if (strOffset == 0)
                return new VA<NativeSpan<ushort>>(va);

            var peFile = chunk.PEFile();

            var rva = (int) (va - peFile.OptionalHeader.ImageBase);

            if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
            {
                var span = valueChunk.PeekNativeSpan<ushort>(0, (int) valueChunk.Remaining / 2);

                var current = span[0];

                var i = 1;

                for (; i < span.Length; i++)
                {
                    var next = span[i];

                    var diff = next - current;

                    if (next <= current)
                    {
                        break;
                    }

                    //This is an arbitrary number to protect against multiple ranges of offsets being listed after
                    //one another. To do a better job here we need to have xrefs, and then can stop as soon as we encounter
                    //an entity that has an xref straight to it
                    if (diff > 150)
                        break;

                    current = next;
                }

                //Peeking a new span is faster than slicing, as we avoid a bounds check
                return new VA<NativeSpan<ushort>>(va, valueChunk.AbsoluteOffset, valueChunk.PeekNativeSpan<ushort>(0, i));
            }

            return new VA<NativeSpan<ushort>>(va);
        }

        internal static VA<VA<Ndr64ProcFormat>[]> ReadFormatStringOffsets64(in MemoryChunk chunk, int offsetsFieldOffset)
        {
            //Read pointers as long as we're getting values within the bounds of the image

            var va = (long) chunk.PeekPointer(offsetsFieldOffset);

            if (va == 0)
                return default;

            var peFile = chunk.PEFile();

            var rva = (int) (va - peFile.OptionalHeader.ImageBase);

            if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
            {
                var low = peFile.OptionalHeader.ImageBase;
                var high = low + peFile.OptionalHeader.SizeOfImage;

                if (peFile.Is32Bit)
                {
                    var span = valueChunk.PeekNativeSpan<int>(0, (int) valueChunk.Remaining / 4);

                    var i = 0;

                    for (; i < span.Length; i++)
                    {
                        var val = span[i];

                        if (val < low || val >= high)
                            break;
                    }

                    //You can have none
                    if (i == 0)
                        return new VA<VA<Ndr64ProcFormat>[]>(va, valueChunk.AbsoluteOffset, Array.Empty<VA<Ndr64ProcFormat>>());

                    var results = new VA<Ndr64ProcFormat>[i];

                    //The order of the RVAs is random
                    for (var j = 0; j < i; j++)
                        results[j] = RpcFormatString.ReadNdr64ProcFormat(span[j], peFile);

                    return new VA<VA<Ndr64ProcFormat>[]>(va, valueChunk.AbsoluteOffset, results);
                }
                else
                {
                    var span = valueChunk.PeekNativeSpan<long>(0, (int) valueChunk.Remaining / 8);

                    var i = 0;

                    for (; i < span.Length; i++)
                    {
                        var val = span[i];

                        if (val < low || val >= high)
                            break;
                    }

                    //You can have none
                    if (i == 0)
                        return new VA<VA<Ndr64ProcFormat>[]>(va, valueChunk.AbsoluteOffset, Array.Empty<VA<Ndr64ProcFormat>>());

                    var results = new VA<Ndr64ProcFormat>[i];

                    //The order of the RVAs is random
                    for (var j = 0; j < i; j++)
                        results[j] = RpcFormatString.ReadNdr64ProcFormat(span[j], peFile);

                    return new VA<VA<Ndr64ProcFormat>[]>(va, valueChunk.AbsoluteOffset, results);
                }
            }

            return new VA<VA<Ndr64ProcFormat>[]>(va);
        }

        internal static VA<RpcSyntaxIdentifier> ReadTransferSyntax(in MemoryChunk chunk, int fieldOffset)
        {
            var va = (long) chunk.PeekPointer(fieldOffset);

            if (va == 0)
                return default;

            var peFile = chunk.PEFile();

            var rva = (int) (va - peFile.OptionalHeader.ImageBase);

            if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
                return new VA<RpcSyntaxIdentifier>(va, chunk.AbsoluteOffset, new RpcSyntaxIdentifier(valueChunk));

            return new VA<RpcSyntaxIdentifier>(va);
        }

        internal static VA<MidlSyntaxInfo[]> ReadSyntaxInfo(
            in MemoryChunk chunk,
            int fieldOffset,
            long nCount)
        {
            var va = (long) chunk.PeekPointer(fieldOffset);

            if (va == 0)
                return default;

            var peFile = chunk.PEFile();

            var rva = (int) (va - peFile.OptionalHeader.ImageBase);

            if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
            {
                var results = new MidlSyntaxInfo[nCount];

                var structSize = MidlSyntaxInfo.StructSize(peFile.Is32Bit);

                for (var i = 0; i < results.Length; i++)
                    results[i] = new MidlSyntaxInfo(valueChunk.Slice(i * structSize));

                return new VA<MidlSyntaxInfo[]>(va, valueChunk.AbsoluteOffset, results);
            }

            return new VA<MidlSyntaxInfo[]>(va);
        }

        internal static VA<RpcDispatchTable> ReadDispatchTable(in MemoryChunk chunk, int fieldOffset)
        {
            var va = (long) chunk.PeekPointer(fieldOffset);

            if (va == 0)
                return default;

            var peFile = chunk.PEFile();

            var rva = (int) (va - peFile.OptionalHeader.ImageBase);

            if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
                return new VA<RpcDispatchTable>(va, chunk.AbsoluteOffset, new RpcDispatchTable(valueChunk));

            return new VA<RpcDispatchTable>(va);
        }

        internal static VA<long[]> ReadPointers(in MemoryChunk chunk, int fieldOffset, int count)
        {
            var va = (long) chunk.PeekPointer(fieldOffset);

            var peFile = chunk.PEFile();

            var rva = (int) (va - peFile.OptionalHeader.ImageBase);

            if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
            {
                if (chunk.Is32Bit)
                {
                    var raw = valueChunk.PeekNativeSpan<int>(0, count);

                    var result = new long[raw.Length];

                    for (var i = 0; i < result.Length; i++)
                        result[i] = raw[i];

                    return new VA<long[]>(va, valueChunk.AbsoluteOffset, result);
                }
                else
                {
                    var raw = valueChunk.PeekNativeSpan<long>(0, count);

                    return new VA<long[]>(va, valueChunk.AbsoluteOffset, raw.ToArray());
                }
            }

            return new VA<long[]>(va);
        }
    }
}
