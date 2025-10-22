using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using PESpy.View;

namespace PESpy.PDB
{
    public static partial class MsfStream
    {
        public class GSI : IValue, IViewable
        {
            public GSIHashHdr? GsiHdr { get; }

            public NativeSpan<HRFile> HashRecords { get; }

            public int[] Buckets { get; }

            public GlobalSymTypeList Symbols { get; }

            public int Offset => chunk.AbsoluteOffset;

            private int iphrHash;

            internal readonly MemoryChunk chunk;

            internal unsafe GSI(in MemoryChunk chunk, int length)
            {
                this.chunk = chunk;

                var pdbFile = chunk.PDBFile();

                //I think the hash header, hash records and buckets total cbSymHash. In GSI, the total number of bytes is just the size of the GSI stream

                if (pdbFile.PDB.Features.Contains(PdbFeature.featMinimalDbgInfo))
                    iphrHash = 0x3FFFF;
                else
                    iphrHash = 4096;

                var gsiHdr = new GSIHashHdr(chunk);
                if (gsiHdr.verSignature == GSIHashHdr.hdrSignature && gsiHdr.verHdr == GSIHashSCImpv.GSIHashSCImpvV70)
                {
                    GsiHdr = gsiHdr;

                    //microsoft-pdb does does some dodgy stuff loading the HRFile records into an array of HR records,
                    //and then I think it fixes things up, since the layout of HR is not the same as HRFile

                    var numItems = gsiHdr.cbHr / HRFile.StructSize;

                    var hashRecords = chunk.PeekNativeSpan<HRFile>(GSIHashHdr.StructSize, gsiHdr.cbHr / HRFile.StructSize);
                    HashRecords = hashRecords;

                    if (hashRecords.Length > 0)
                    {
                        if (!pdbFile.TryGetStreamChunk(pdbFile.DBI!.DbiHdr.snSymRecs, out var symbolChunk))
                            throw new InvalidOperationException("Couldn't retrieve section for DbiHdr.snSymRecs for global symbols");

                        SymbolMemoryTracker.RegisterPDBSymbolMemory(symbolChunk);

                        var symbolsStart = symbolChunk.Pointer;

                        Symbols = new GlobalSymTypeList(hashRecords, symbolsStart);

                        if (gsiHdr.cbBuckets > 0)
                        {
                            /* gsi.cpp contains an #ifdef for SMALLBUCKETS. This define _is_ present in compiled builds of PDB1
                             * (GSI1::ExpandBuckets is present). Even when SMALLBUCKETS is defined, small buckets are only in use
                             * when the two signatures we check above match. Otherwise, we need to use the legacy bucket reading mechanism
                             *
                             * At the beginning of the bucket area is a bitmap describing which buckets in the hashmap have values in them.
                             * Following this bitmap is an array of offsets for each bucket that has a value. In order to know how many entries
                             * there are, we need to know how many bits are set in the bitmap */

                            //GSI1::ExpandBuckets
                            var numBitMapInts = SI.DivideUp(iphrHash + 1, 32);
                            var cbphr = numBitMapInts * sizeof(int);

                            var read = GSIHashHdr.StructSize + gsiHdr.cbHr;

                            //This gives us a bitmap that describes the status of all of the buckets in the hashmap.
                            var bitmap = chunk.PeekNativeSpan<int>(read, numBitMapInts); //Read as int so that we can easily count its bits

                            read += cbphr;

                            int numBitsSet = 0;

                            //Now count how many bits are set
                            for (var i = 0; i < bitmap.Length; i++)
                                numBitsSet += CountBits((uint) bitmap[i]);

                            var bitArray = new BitArray(bitmap.ToArray());

                            var offsets = chunk.PeekNativeSpan<int>(read, numBitsSet);
                            var offsetIndex = 0;

                            var results = new int[iphrHash + 1];

                            for (var i = 0; i <= iphrHash; i++)
                            {
                                /* GSI1::fixHashIn does some insane memory manipulation. On disk, GSI contains a list of HRFile items,
                                 * followed by the bitmap (described above) and then the offsets of each bucket whose value is present.
                                 * What fixHashIn wants to do is
                                 * 1. Convert each HRFile item to an in-memory representation called "HR"
                                 * 2. Make each bucket item point to the in-memory "HR" item that it is associated with
                                 *
                                 * Each HRFile record is 8 bytes, while each HR record is 24 (todo: 24? or 12?) bytes. Each bucket that contains a value
                                 * contains a 12-byte offset. PDB1 takes this 12 byte offset, and uses it to index into the memory area
                                 * that is being used by HRFile items, and reinterprets it as a HR item. As the buffer containing the HRFile
                                 * items had an extra HR's worth of data allocated at the front, this prevents the data we're reinterpreting
                                 * as a HR from overwriting a HRFile that we haven't processed yet. A pointer to this under construction
                                 * HR is then stored in the bucket location that originally contained the 12 byte offset, and then the actual
                                 * HRFile that we're up to is retrieved, and stored in the dodgy HR. Assigning the HRFile to the HR pointer
                                 * causes the HRFile to be properly laid out within the HR's memory region
                                 *
                                 * This is way too nuts. All we really do is divide the offset by 12 (which is the size of HROffsetCalc).
                                 * I think that HROffsetCalc is only really used in 64-bit and is needed due to the fact that they want to store
                                 * a pointer, rather than the raw offset itself. We are just storing offsets so we don't need to worry about
                                 * any of this nonsense
                                 *
                                 * todo: its a 12 bit offset? or its an offset * 12?
                                 */
                                if (bitArray[i])
                                    results[i] = offsets[offsetIndex++] / 12;
                                else
                                    results[i] = -1;
                            }

                            Buckets = results;
                        }
                    }
                    else
                    {
                        Debug.Assert(gsiHdr.cbBuckets == 0); //We assume there's no bucket info to read if we didn't have any records. This may not be true!
                        Symbols = null!;
                    }
                }
                else
                {
                    //There's no header (e.g. it's a PDB2File). First, we have a list of offsets, and then after this we may potentially have some HRFile entries

                    var numBuckets = iphrHash + 1;

                    //How many bytes do the buckets encompass
                    var cbphr = sizeof(int) * numBuckets;

                    //How many bytes do the HRFile records encompass
                    var hrFileLength = length - cbphr;

                    //How many HRFile items are there
                    var numHRFiles = hrFileLength / HRFile.StructSize;

                    //the HRFile's come before the buckets. You can have buckets but have no hash records
                    var hashRecords = chunk.PeekNativeSpan<HRFile>(0, numHRFiles);
                    HashRecords = hashRecords;

                    Debug.Assert(hrFileLength >= 0);
                    Buckets = chunk.PeekNativeSpan<int>(hrFileLength, numBuckets).ToArray();

                    if (hashRecords.Length > 0)
                    {
                        if (!pdbFile.TryGetStreamChunk(pdbFile.DBI!.DbiHdr.snSymRecs, out var symbolChunk))
                            throw new InvalidOperationException("Couldn't retrieve section for DbiHdr.snSymRecs for global symbols");

                        SymbolMemoryTracker.RegisterPDBSymbolMemory(symbolChunk);

                        Symbols = new GlobalSymTypeList(hashRecords, symbolChunk.Pointer);
                    }
                }
            }

            public unsafe bool TryGetSymbol(string name, out SymType symType)
            {
                var bytes = Encoding.UTF8.GetBytes(name);

                fixed (byte* pName = bytes)
                {
                    var hash = Hasher.lhashPbCb(pName, bytes.Length, (uint) iphrHash);

                    var buckets = Buckets;

                    if (hash > buckets.Length)
                    {
                        symType = default;
                        return false;
                    }

                    var offset = buckets[hash];

                    if (offset >= HashRecords.Length)
                    {
                        symType = default;
                        return false;
                    }

                    symType = Symbols[offset];
                    return true;
                }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static int CountBits(uint u)
            {
                const uint _5_32 = 0x55555555;
                const uint _3_32 = 0x33333333;
                const uint _F1_32 = 0x0f0f0f0f;
                const uint _F2_32 = 0x00ff00ff;
                const uint _F4_32 = 0x0000ffff;

                u -= (u >> 1) & _5_32;
                u = ((u >> 2) & _3_32) + (u & _3_32);
                u = ((u >> 4) & _F1_32) + (u & _F1_32);
                u += u >> 8;
                u += u >> 16;
                return (int) (u & 0xff);
            }

            void IViewable.WriteGlobals(ViewWriter writer) => WriteGlobals(writer);

            IView? IViewable.WriteStruct(ViewWriter writer) => null;

            int IViewable.NumChildren() => throw new NotSupportedException();

            void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();

            protected virtual void WriteGlobals(ViewWriter writer)
            {
                if (GsiHdr != null)
                {
                    //If we have a GsiHdr, the following should implicitly be true
                    Debug.Assert(GsiHdr.verSignature == GSIHashHdr.hdrSignature && GsiHdr.verHdr == GSIHashSCImpv.GSIHashSCImpvV70);
                    writer.WritePagedGlobal<HRFile>(chunk.RelativeOffset + GSIHashHdr.StructSize, (PagedMemoryBlock) chunk.block, HashRecords, ViewKind.HRFile);
                }
                else
                {
                    writer.WritePagedGlobal<HRFile>(chunk.RelativeOffset, (PagedMemoryBlock) chunk.block, HashRecords, ViewKind.HRFile);
                    writer.WritePagedGlobal<int>(chunk.RelativeOffset + (HashRecords.Length * HRFile.StructSize), (PagedMemoryBlock) chunk.block, Buckets, ViewKind.HashBuckets);
                }
            }
        }
    }
}
