using System;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    public static partial class MsfStream
    {
        public class TpiHash
        {
            private readonly PESpy.PDB.TpiHash info;

            private readonly MemoryChunk chunk;

            internal TpiHash(in MemoryChunk chunk, PESpy.PDB.TpiHash info, TPIImpv impv)
            {
                //The physical order is:
                //1. hash vals
                //2. ti off
                //3. hash adj

                int offset;

                if (impv < TPIImpv.impv80)
                {
                    //The hashes are 16-bit

                    var numHashes = info.offcbHashVals.cb / sizeof(short);

                    var hashes = new ushort[numHashes];

                    offset = info.offcbHashVals.off;

                    for (var i = 0; i < numHashes; i++)
                    {
                        hashes[i] = chunk.PeekUInt16(offset);
                        offset += sizeof(short);
                    }
                }
                else
                {
                    var numHashes = info.offcbHashVals.cb / sizeof(int);

                    var hashes = new uint[numHashes];

                    offset = info.offcbHashVals.off;

                    for (var i = 0; i < numHashes; i++)
                    {
                        hashes[i] = chunk.PeekUInt32(offset);
                        offset += sizeof(int);
                    }
                }

                offset = info.offcbTiOff.off;

                var numTiOffs = info.offcbTiOff.cb / 8;

                var tiOffs = new TI_OFF[numTiOffs];

                for (var i = 0; i < numTiOffs; i++)
                {
                    tiOffs[i] = chunk.PeekUnmanaged<TI_OFF>(offset);
                    offset += 8;
                }

                if (info.offcbHashAdj.cb > 0)
                    throw new NotImplementedException("Handling the hash adjustment is not implemented");

                this.chunk = chunk;
                this.info = info;
            }
        }
    }
}
