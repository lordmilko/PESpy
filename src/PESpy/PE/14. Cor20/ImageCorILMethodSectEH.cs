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

        internal ImageCorILMethodSectEH(CorILMethodSect kind, IFileReader reader)
        {
            //Already read kind byte
            Offset = (int) reader.Position - 1;

            Sect = new ImageCorILMethodSect(kind, reader);

            int numItems;
            var isFat = kind.HasFlag(CorILMethodSect.FatFormat);

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

                Reserved = reader.ReadInt16();
            }

            var clauses = new ImageCorILMethodSectEHClause[numItems];

            for (var i = 0; i < numItems; i++)
                clauses[i] = new ImageCorILMethodSectEHClause(reader, isFat);

            Clauses = clauses;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            var isFat = Sect.Kind.HasFlag(CorILMethodSect.FatFormat);

            using var s = writer.CreateStruct(
                isFat ? nameof(IMAGE_COR_ILMETHOD_SECT_EH_FAT) : nameof(IMAGE_COR_ILMETHOD_SECT_EH_SMALL),
                this,
                ViewKind.ImageCorILMethodSectEH
            );

            if (isFat)
            {
                using var _ = writer.EnterTag(ViewTag.FatEH);

                s.WriteInline(Sect);
                s.WriteInline(Clauses);
            }
            else
            {
                s.WriteInline(Sect);
                s.WriteField(nameof(Reserved), Reserved);
                s.WriteInline(Clauses);
            }
        }
    }
}
