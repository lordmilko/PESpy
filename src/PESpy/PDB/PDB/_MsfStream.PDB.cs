using System;
using PESpy.View;

namespace PESpy.PDB
{
    public static partial class MsfStream
    {
        /// <summary>
        /// Encapsulates the contents of the <see cref="SN.PDB"/> (1) PDB Stream.
        /// </summary>
        public class PDB : IValue, IViewable
        {
            private PDBStream70 pdbHeader;

            public ref readonly PDBStream70 PDBHeader => ref pdbHeader;

            private NMTNI streamNameTable;

            public ref readonly NMTNI StreamNameTable => ref streamNameTable;

            //The first entry can be 0 and that's normal
            public PdbFeature[] Features { get; }

            public int Offset => pdbHeader.Offset;

            internal PDB(in MemoryChunk chunk)
            {
                pdbHeader = new PDBStream70(chunk);

                streamNameTable = new NMTNI(chunk.Slice(PDBStream70.StructSize));

                var remainingChunk = chunk.Slice(PDBStream70.StructSize + streamNameTable.StructSize);

                /* Following the end of the stream name table there may be one or more "feature codes"
                 * - impvVC110 (m_fContainIDStream)
                 * - impvVC140 (m_fContainIDStream)
                 * - featNoTypeMerge (m_fNoTypeMerge)
                 * - featMinimalDbgInfo (m_fMinimalDbgInfo)
                 */

                if (remainingChunk.Remaining >= 4)
                    Features = remainingChunk.PeekSpan<PdbFeature>(0, remainingChunk.Remaining / 4).ToArray();
                else
                    Features = Array.Empty<PdbFeature>();
            }

            void IViewable.WriteView(ViewWriter writer)
            {
                writer.WriteGlobal(PDBHeader);
                writer.WriteGlobal(StreamNameTable);

                if (Features.Length > 0)
                {
                    var featuresStart = Offset + PDBStream70.StructSize + streamNameTable.StructSize;

                    foreach (var feature in Features)
                    {
                        writer.WriteGlobal(featuresStart, feature, 4, ViewKind.Value);
                        featuresStart += 4;
                    }
                }
            }
        }
    }
}
