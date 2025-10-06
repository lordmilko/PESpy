using System;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    
                    var numHashes = info.offcbHashVals.cb / sizeof(int);

                    var hashes = new int[numHashes];

                    offset = info.offcbHashVals.off;

                    for (var i = 0; i < numHashes; i++)
                    {
                        hashes[i] = chunk.PeekInt32(offset);
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
                this.chunk = chunk;
                this.info = info;
            }
        }
    }
}
