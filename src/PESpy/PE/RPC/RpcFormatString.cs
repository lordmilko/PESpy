using System;
using System.Diagnostics;

namespace PESpy
{
    //Type is made up
    public class RpcFormatString
    {
        public long ListedAddress { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public RpcFormatStringItem[]? Entries { get; } //Null if the ListedAddress was invalid or the VA to offsets was invalid

        internal static RpcFormatString New(
            in MemoryChunk chunk,
            int fieldOffset,
            VA<NativeSpan<ushort>> offsets)
        {
            var va = (long) chunk.PeekPointer(fieldOffset);

            if (va == 0)
                return null;

            return new RpcFormatString(chunk, fieldOffset, offsets, va);
        }

        internal static VA<Ndr64ProcFormat> ReadNdr64ProcFormat(long va, PEFile peFile)
        {
            if (peFile.TryGetValueChunkFromVA(va, out var valueChunk))
            {
                var desc = new Ndr64ProcFormat(valueChunk);
                return new VA<Ndr64ProcFormat>(va, valueChunk.AbsoluteOffset, desc);
            }

            return new VA<Ndr64ProcFormat>(va);
        }

        private unsafe RpcFormatString(
            in MemoryChunk chunk,
            int fieldOffset,
            VA<NativeSpan<ushort>> offsets,
            long va)
        {
            //NdrClientCall3 / MIDL_STUBLESS_PROXY_INFO format. But also seems to be NdrClientCall2
            //format as well?

            ListedAddress = va;

            if (!offsets.IsValid)
                return;

            //MulNdrpInitializeContextFromProc only supports NDR; you would think NDR64
            //should be NDR64_PROC_FORMAT, but that doesn't seem to be the case. The shape
            //does seem to be the same as NDR. Furthermore, I've found that the format string
            //seems to be valid even if the transfer syntax is empty. It seems you only have
            //NDR64_PROC_FORMAT when it's a proc string pointed to by a MIDL_SYNTAX_INFO item

            var peFile = chunk.PEFile();

            if (peFile.TryGetValueChunkFromVA(va, out var valueChunk))
            {
                var results = new RpcFormatStringItem[offsets.Value.Length];

                var i = 0;

                foreach (var offset in offsets.Value)
                {
                    var ptr = valueChunk.Pointer + offset;

                    var handleType = *(FORMAT_CHARACTER*) ptr++; //Could be 0, could be FC_AUTO_HANDLE

                    var interpreterFlags = *(INTERPRETER_FLAGS*) ptr++;

                    RPC_FLAGS rpcFlags = 0;

                    if (interpreterFlags.HasRpcFlags)
                    {
                        rpcFlags = *(RPC_FLAGS*) ptr;
                        ptr += sizeof(int);
                    }

                    var procNum = *(ushort*) ptr;
                    ptr += sizeof(short);

                    var stackSize = *(ushort*) ptr;
                    ptr += sizeof(short);

                    FORMAT_CHARACTER explicitHandleType = default;
                    NativeSpan<byte> explicitHandle = default;

                    if (handleType == 0)
                    {
                        var fc = *(FORMAT_CHARACTER*) ptr;
                        explicitHandleType = fc;

                        if (fc == FORMAT_CHARACTER.FC_BIND_PRIMITIVE)
                        {
                            explicitHandle = new NativeSpan<byte>(ptr + 1, 3);
                            ptr += 4;
                        }
                        else
                        {
                            explicitHandle = new NativeSpan<byte>(ptr + 1, 5);
                            ptr += 6;
                        }
                    }

                    var relativeOffset = (int) (ptr - valueChunk.Pointer);

                    var desc = new NdrProcDesc(valueChunk.Slice(relativeOffset));

                    results[i] = new RpcFormatStringItem(
                        handleType,
                        interpreterFlags,
                        rpcFlags,
                        procNum,
                        stackSize,
                        explicitHandleType,
                        explicitHandle,
                        desc
                    );

                    i++;
                }

                Entries = results;
            }
            else
            {
                //Not much we can do; but we do need to report the listed address
                return;
            }
        }
    }
}
