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
                numItems = Sect.DataSize / 24;

                Reserved = 0;
            }
            else
            {
                //Ostensibly, DataSize is n*12+4, but given what we saw with the isFat scenario, we can posit that the same issue could occur for thin modules as well
                numItems = Sect.DataSize / 12;

                Reserved = chunk.PeekInt16(read);
                read += 2;
            }

            var clauses = new ImageCorILMethodSectEHClause[numItems];

            for (var i = 0; i < numItems; i++)
                clauses[i] = new ImageCorILMethodSectEHClause(chunk, isFat, ref read);

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
                isFat ? Strings.IMAGE_COR_ILMETHOD_SECT_EH_FAT : Strings.IMAGE_COR_ILMETHOD_SECT_EH_SMALL,
                this,
                ViewKind.ImageCorILMethodSectEH,
                StructSize
            );
        }

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            var isFat = (Sect.Kind & CorILMethodSect.FatFormat) != 0;

            using var s = viewWriter.CreateStruct(parent);

            if (isFat)
            {
                s.WriteInline(Sect);
                s.WriteInline(Clauses);
            }
            else
            {
                s.WriteInline(Sect);
                s.WriteField(nameof(Reserved), Reserved);
                s.WriteInline(Clauses);
            }

            return s.ToArray();
        }
    }
}
