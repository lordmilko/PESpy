using System;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    public static partial class MsfStream
    {
        public class TPI : IValue, IViewable, IDisposable
        {
            internal const uint cchnV7 = 0x1000; // for v7 and previous, we have 4k buckets
            internal const uint cchnV8 = 0x3ffff; // default to 256k - 1buckets
            internal const uint cprecInit = 0x1000; // start with 4k prec pointers
            internal const uint cchnMax = 0x40000; // deliberate maximum bucket count == 256k

            public IHDR Hdr { get; } //We have to use interfaces because there's just way too much variability as to when different structures may appear

            public TypTypeList Types { get; }

            private MsfStream.TpiHash? tpiHash;

            /// <summary>
            /// Gets the stream pointed to by the <see cref="HDR.tpihash"/>.sn field.<para/>
            /// Not to be confused with the <see cref="PESpy.PDB.TpiHash"/> data structure contained
            /// in the <see cref="HDR.tpihash"/> field itself.
            /// </summary>
            public MsfStream.TpiHash? TpiHash
            {
                get
                {
                    if (tpiHash == null && hashSN != SN.Nil)
                    {
                        var pdbFile = chunk.PDBFile();

                        if (pdbFile.TryGetStreamChunk(hashSN, out var hashChunk))
                        {
                            switch (Hdr.vers)
                            {
                                case TPIImpv.impv80:
                                    tpiHash = new MsfStream.TpiHash(hashChunk, (HDR) Hdr, Hdr.vers, Types, hasher, sizeof(int));
                                    break;

                                //v70 and v50 have a TpiHash struct, but the hashes are 16-bit
                                case TPIImpv.impv70:
                                case TPIImpv.impv50:
                                    tpiHash = new MsfStream.TpiHash(hashChunk, (HDR) Hdr, Hdr.vers, Types, hasher, sizeof(short));
                                    break;

                                case TPIImpv.impv50Interim:
                                    var hdr50 = (HDR_VC50Interim) Hdr;
                                    tpiHash = new MsfStream.TpiHash(hashChunk, hdr50.snHash, hdr50.tiMac, hdr50.tiMin, Hdr.vers, Types, hasher);
                                    break;

                                case TPIImpv.impv41:
                                case TPIImpv.impv40:
                                case TPIImpv.intvVC2:
                                    var hdr16 = (HDR_16t) Hdr;
                                    tpiHash = new MsfStream.TpiHash(hashChunk, hdr16.snHash, hdr16.tiMac, hdr16.tiMin, Hdr.vers, Types, hasher);
                                    break;

                                default:
                                    throw new NotImplementedException($"Don't know how to handle {nameof(TPIImpv)} '{Hdr.vers}'");
                            }
                        }
                    }

                    return tpiHash;
                }
            }

            public int Offset => chunk.AbsoluteOffset;

            private readonly MemoryChunk chunk;
            private readonly SN hashSN;
            private readonly MsfStream.TpiHash.HashDelegate hasher;
            private MsfStream.TpiHash effectiveHashStream;

            internal unsafe TPI(in MemoryChunk chunk)
            {
                //tpi.cpp!fLoad + acslValidateHdr

                this.chunk = chunk;

                var impv = (TPIImpv) chunk.PeekUInt32(0);

                var headerSize = 0;

                HDR hdr;

                switch (impv)
                {
                    case TPIImpv.impv80: //curImpv
                        hdr = new HDR(chunk);
                        Hdr = hdr;
                        headerSize = HDR.StructSize;
                        hashSN = hdr.tpihash.sn;

                        if (hdr.tpihash.cbHashKey == sizeof(int) && hdr.tpihash.cHashBuckets >= cchnV7 && hdr.tpihash.cHashBuckets <= cchnMax)
                        {
                            //Use TPI1::hashBufv8
                            //throw new NotImplementedException();
                            hasher = MsfStream.TpiHash.hashBufv8;
                        }
                        else
                        {
                            throw new BadImageFormatException();
                        }
                        break;

                    case TPIImpv.impv70:
                    case TPIImpv.impv50:
                        hdr = new HDR(chunk);
                        Hdr = hdr;
                        headerSize += HDR.StructSize;
                        hashSN = hdr.tpihash.sn;

                        if (hdr.tpihash.cbHashKey == sizeof(short) && hdr.tpihash.cHashBuckets == cchnV7)
                        {
                            //TPI1::hashBuf
                            hasher = MsfStream.TpiHash.hashBuf;
                        }
                        else
                        {
                            throw new BadImageFormatException();
                        }

                        break;

                    case TPIImpv.impv50Interim:
                        var hdr50 = new HDR_VC50Interim(chunk);
                        Hdr = hdr50;
                        headerSize = HDR_VC50Interim.StructSize;
                        hashSN = hdr50.snHash;

                        //TPi1::hashBuf
                        hasher = MsfStream.TpiHash.hashBuf;
                        break;

                    case TPIImpv.impv41:
                    case TPIImpv.impv40:
                    case TPIImpv.intvVC2:
                        var hdr16 = new HDR_16t(chunk);
                        Hdr = hdr16;
                        headerSize = HDR_16t.StructSize;
                        hashSN = hdr16.snHash;
                        hasher = MsfStream.TpiHash.hashBuf;
                        break;

                    default:
                        throw new NotImplementedException($"Don't know how to handle {nameof(TPIImpv)} '{Hdr.vers}'");
                }

                var ptr = chunk.Pointer + headerSize;

                SymbolMemoryTracker.RegisterPDBSymbolMemory(chunk);

                //TPI1::fInitTiToPrecMap shows that following the header is cbGprec bytes of type records
                Types = new TypTypeList(ptr, Hdr.cbGprec);
            }

            internal TypType GetTypTypeFromIndex(CV_typ_t typeIndex)
            {
                if (!TryGetTypTypeFromIndex(typeIndex, out var typType))
                    throw new InvalidOperationException($"Failed to resolve type index '{typeIndex}'");

                return typType;
            }

            internal bool TryGetTypTypeFromIndex(CV_typ_t typeIndex, out TypType typType)
            {
                //The plan: use the TPI Hash stream (if we have one), otherwise synthesize a fake internal only one to use for type lookups

                if (effectiveHashStream == null)
                {
                    effectiveHashStream = TpiHash ?? new MsfStream.TpiHash(chunk.PDBFile(), Hdr.tiMac, Hdr.tiMin, Hdr.vers, Types, hasher);
                }

                return effectiveHashStream.TryGetTypTypeFromIndex(typeIndex, out typType);
            }

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                writer.WriteGlobal(Hdr);

                writer.WritePagedGlobal(chunk.RelativeOffset + Hdr.StructSize, (PagedMemoryBlock) chunk.block, Types);
            }

            IView? IViewable.WriteStruct(ViewWriter writer) => null;

            int IViewable.NumChildren() => throw new NotSupportedException();

            void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();

            public void Dispose()
            {
                tpiHash?.Dispose();
            }
        }
    }
}
