using ClrDebug;
using PESpy.View;

namespace PESpy
{
    //Top level structure that encapsulates all EH related structures
    public readonly struct ImageCorILMethodSectEH : IValue, IViewable
    {
        public ImageCorILMethodSect Sect { get; }

        public short Reserved { get; }

        public ImageCorILMethodSectEHClause[] Clauses { get; }

        public int Offset { get; }

        internal ImageCorILMethodSectEH(CorILMethodSect kind, in MemoryChunk chunk)
        {
            //We need the Sect to know what data comes next, so we need to eagerly read

            Offset = chunk.AbsoluteOffset;

            Sect = new ImageCorILMethodSect(kind, chunk, out var read);

            int numItems;
            var isFat = (kind & CorILMethodSect.FatFormat) != 0;

            //ECMA 335 II.25.4.5
            if (isFat)
            {
                //DataSize is n*24+4
                numItems = (Sect.DataSize - 4) / 24;

                Reserved = 0;
            }
            else
            {
                //DataSize is n*12+4
                numItems = (Sect.DataSize - 4) / 12;

                Reserved = chunk.PeekInt16(read);
                read += 2;
            }

            var clauses = new ImageCorILMethodSectEHClause[numItems];

            for (var i = 0; i < numItems; i++)
            {
                clauses[i] = new ImageCorILMethodSectEHClause(chunk, isFat, ref read);
            }

            Clauses = clauses;
        }
    }
}
