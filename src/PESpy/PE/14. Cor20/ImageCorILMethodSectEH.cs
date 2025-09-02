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

        internal int StructSize => Sect.DataSize;

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
                using var _ = viewWriter.EnterTag(ViewTag.FatEH);

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
