using System;
using ClrDebug;
using PESpy.View;

namespace PESpy
{
    //Top level structure that encapsulates all EH related structures
    public readonly struct ImageCorILMethodSectEH : IViewableValue
    {
        private const int SectOffset = 0;
        private const int ReservedOffset = ImageCorILMethodSect.TinySize;

        public ImageCorILMethodSect Sect { get; }

        public short Reserved { get; }

        public ImageCorILMethodSectEHClause[] Clauses { get; }

        public long Offset { get; }

        internal int StructSize
        {
            get
            {
                //Ordinarily, DataSize should be the true size, but as described below sometimes it doesn't account for the header. Both fat and thin are divisible by 12, so we can use this
                //check to see whether we're new style or old style
                var remainder = Sect.DataSize % 12;

                //If we have a remainder, we're new style
                if (remainder != 0)
                    return Sect.DataSize;

                //DataSize was something like 12 or 24. Actual size is 16 or 28
                return Sect.DataSize + 4;
            }
        }

        internal ImageCorILMethodSectEH(in MemoryChunk chunk, bool isFat) : this(isFat ? CorILMethodSect.FatFormat : default, chunk)
        {
        }

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
                //Ordinarily, DataSize should be n*24+4. However, in older assemblies DataSize can just be n*12. We can handle both
                //scenarios by evaluating DataSize / 24
                numItems = Sect.DataSize / ImageCorILMethodSectEHClause.FatSize;

                Reserved = 0;
            }
            else
            {
                //Ostensibly, DataSize is n*12+4, but given what we saw with the isFat scenario, we can posit that the same issue could occur for thin modules as well
                numItems = Sect.DataSize / ImageCorILMethodSectEHClause.TinySize;

                Reserved = chunk.PeekInt16(read);
                read += 2;
            }

            var clauses = new ImageCorILMethodSectEHClause[numItems];

            for (var i = 0; i < numItems; i++)
            {
                clauses[i] = new ImageCorILMethodSectEHClause(chunk.Slice(read), isFat);
                read += isFat ? ImageCorILMethodSectEHClause.FatSize : ImageCorILMethodSectEHClause.TinySize;
            }

            Clauses = clauses;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer)
        {
            var isFat = (Sect.Kind & CorILMethodSect.FatFormat) != 0;

            return writer.NewStruct(
                this,
                isFat ? ViewKind.ImageCorILMethodSectEHFat : ViewKind.ImageCorILMethodSectEHSmall,
                StructSize
            );
        }

        int IViewable.NumChildren() => (Sect.Kind & CorILMethodSect.FatFormat) != 0 ? 2 : 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            var isFat = (Sect.Kind & CorILMethodSect.FatFormat) != 0;

            switch (index)
            {
                case 0:
                    if (isFat)
                        structWriter.WriteStructField("SectFat", Sect);
                    else
                        structWriter.WriteStructField("SectSmall", Sect);

                    break;

                case 1:
                    if (isFat)
                        structWriter.WriteStructField("Clauses", Clauses);
                    else
                        structWriter.WriteField(nameof(Reserved), ReservedOffset, Reserved);

                    break;

                case 2:
                    if (isFat)
                        throw new IndexOutOfRangeException();
                    else
                        structWriter.WriteStructField("Clauses", Clauses);

                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
