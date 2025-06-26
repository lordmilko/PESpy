using System;
using ClrDebug.PDB;
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
            public PDBStream PDBHeader { get; }

            private NMTNI streamNameTable;

            public ref readonly NMTNI StreamNameTable => ref streamNameTable;

            //The first entry can be 0 and that's normal
            public NativeSpan<PdbFeature> Features { get; }

            public int Offset => chunk.AbsoluteOffset;

            private readonly MemoryChunk chunk;

            internal PDB(in MemoryChunk chunk)
            {
                this.chunk = chunk;

                //Check if the size of exactly PDBStream, and if so if its impvVC2

                var impv = (PDBIMPV) chunk.PeekUInt32(0);

                if (chunk.Remaining == PDBStream.StructSize || impv == PDBIMPV.PDBImpvVC2)
                    throw new NotImplementedException(); //microsoft-pdb does not support these anymore

                //microsoft-pdb only parses the header if impv >= impvVC4 and <= impvVC140. However, we don't know what future
                //PDBIMPV values Microsoft will add, so we can't be doing such checks

                var headerSize = 0;

                //it's only PDBStream70 if impv > impvVC70Dep
                if (impv >= PDBIMPV.PDBImpvVC70Dep)
                {
                    PDBHeader = new PDBStream70(chunk);
                    headerSize = PDBStream70.StructSize;
                }
                else
                {
                    PDBHeader = new PDBStream(chunk);
                    headerSize = PDBStream.StructSize;
                }

                streamNameTable = new NMTNI(chunk.Slice(headerSize));

                var remainingChunk = chunk.Slice(headerSize + streamNameTable.StructSize);

                /* Following the end of the stream name table there may be one or more "feature codes"
                 * - impvVC110 (m_fContainIDStream)
                 * - impvVC140 (m_fContainIDStream)
                 * - featNoTypeMerge (m_fNoTypeMerge)
                 * - featMinimalDbgInfo (m_fMinimalDbgInfo)
                 */

                if (remainingChunk.Remaining >= 4)
                    Features = remainingChunk.PeekNativeSpan<PdbFeature>(0, remainingChunk.Remaining / 4);
                else
                    Features = default;
            }

            internal bool HasIPI
            {
                get
                {
                    /* Per PDB1::savePdbStream, additional features are only set when m_fContainIDStream is true.
                     * However, in reverse m_fContainIDStream is set under the following circumstances:
                     * 
                     * 1. You're creating a new PDB
                     * 2. the features contain impvVC110
                     * 3. The features contain impvVC140
                     * PDB1::savePdbStream will automatically use impvVC140 as the feature unless PdbOpenMode.pdbVC120 was specified, in which case impvVC110 is used.
                     * 
                     * But either way, these still denote that the ID stream is present.
                     *
                     * It's possible that the versions used could change in the future; as such, we will say that IPI is present as long as any features are defined */

                    return Features.Length > 0;
                }
            }

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                writer.WriteGlobal(PDBHeader);
                writer.WriteGlobal(StreamNameTable);

                if (Features.Length > 0)
                {
                    var featuresStart = chunk.RelativeOffset + PDBStream70.StructSize + streamNameTable.StructSize;

                    using var p = writer.CreatePagedWriter(featuresStart, (PagedMemoryBlock) chunk.block, global: true);

                    foreach (var feature in Features)
                        p.WriteValue(feature, sizeof(int), ViewKind.Value);
                }
            }

            IView? IViewable.WriteStruct(ViewWriter writer) => null;

            IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter) => throw new NotSupportedException();
        }
    }
}
