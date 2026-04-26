using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;
using static ClrDebug.PDB.CV_ptrmode_e;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfPointer"/> structure.
    /// </summary>
    public readonly unsafe struct LfPointer : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int utypeOffset = 4;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfPointer* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        #region lfPointerBody

        public LEAF_ENUM_e leaf => value->u.leaf;

        public TypOrEnumType utype => new TypOrEnumType((byte*) value, value->u.utype);

        public lfPointer.lfPointerAttr attr => value->u.attr;

        #endregion

        public lfPointer.BaseInfo pbase => value->pbase;

        internal LfPointer(lfPointer* value)
        {
            this.value = value;
        }

        public static implicit operator LfEasy(LfPointer easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.LfPointer, typlen + sizeof(short));

        int IViewable.NumChildren() => throw StructWriter.GetEagerLoadOnlyException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            //Way too complicated to do lazy loading
            if (index != -1)
                throw StructWriter.GetEagerLoadOnlyException();

            /* The definition of LfPointer in cvinfo.h is a huge mess if ifdef's to enable a C++ style definition
             * using inheritance, but once you cut all that the definition is as follows
             * 
             * struct lfPointerAttr {
             *     ...
             * }
             * 
             * struct lfPointerBody {
             *     unsigned short      leaf;
             *     CV_typ_t            utype;
             *     lfPointerAttr attr;
             * }
             * 
             * typedef struct lfPointer {
             *     lfPointerBody u;
             *     union {
             *         struct {
             *             CV_typ_t        pmclass;
             *             unsigned short  pmenum;
             *         } pm;
             *         unsigned short      bseg;
             *         unsigned char       Sym[1];
             *         struct {
             *             CV_typ_t        index;
             *             unsigned char   name[1];
             *         } btype;
             *     } pbase;
             * } lfPointer;
             * 
             * The hardest part is the pbase union, which can be one of four different values
             */

            using var s = structWriter.CreateEagerWriter();

            s.WriteField(nameof(typlen), typlen);

            //We are not currently mirroring the exact complex structure of lfPointer

            #region Body

            s.WriteField(nameof(leaf),leaf, sizeof(ushort));
            s.WriteField(nameof(utype), value->u.utype);

            using (var b = s.WriteBitFields<int>(12))
            {
                b.WriteField(nameof(attr.ptrtype), attr.ptrtype, 5);
                b.WriteField(nameof(attr.ptrmode), attr.ptrmode, 3);
                b.WriteField(nameof(attr.isflat32), attr.isflat32, 1);
                b.WriteField(nameof(attr.isvolatile), attr.isvolatile, 1);
                b.WriteField(nameof(attr.isconst), attr.isconst, 1);
                b.WriteField(nameof(attr.isunaligned), attr.isunaligned, 1);
                b.WriteField(nameof(attr.isrestrict), attr.isrestrict, 1);
                b.WriteField(nameof(attr.size), attr.size, 6);
                b.WriteField(nameof(attr.ismocom), attr.ismocom, 1);
                b.WriteField(nameof(attr.islref), attr.islref, 1);
                b.WriteField(nameof(attr.isrref), attr.isrref, 1);
                b.WriteField(nameof(attr.unused), attr.unused, 10);
            }

            #endregion
            #region pbase

            //What follows is one of pm, bseg, Sym or btype

            if (attr.ptrmode == CV_PTR_MODE_PMEM || attr.ptrmode == CV_PTR_MODE_PMFUNC)
            {
                //PM (per pdbdump.cpp)
                s.WriteField(nameof(pbase.pm.pmclass), pbase.pm.pmclass);
                s.WriteField(nameof(pbase.pm.pmenum), pbase.pm.pmenum, sizeof(short));
            }
            else
            {
                switch (attr.ptrtype)
                {
                    case CV_ptrtype_e.CV_PTR_BASE_SEG:
                        //bseg
                        throw new NotImplementedException();

                    case CV_ptrtype_e.CV_PTR_BASE_TYPE:
                        //btype
                        throw new NotImplementedException();

                    //From NT 4
                    case CV_ptrtype_e.CV_PTR_BASE_VAL:
                    case CV_ptrtype_e.CV_PTR_BASE_SEGVAL:
                    case CV_ptrtype_e.CV_PTR_BASE_ADDR:
                    case CV_ptrtype_e.CV_PTR_BASE_SEGADDR:
                        //Sym (which is apparently a SYMTYPE*)
                        throw new NotImplementedException();

                    default:
                        //The length of the object should be such that we've now written 12 bytes worth;
                        //the stated length is 10 + the leaf = 12
                        break;
                }
            }

            #endregion

            var expectedSize = typlen + sizeof(short);

            while (s.Size < expectedSize)
            {
                var val = (LEAF_ENUM_e) (*((byte*) value + s.Size - sizeof(short)));
                Debug.Assert(val >= LEAF_ENUM_e.LF_PAD0 && val <= LEAF_ENUM_e.LF_PAD15);
                s.WriteValue(val, 1);
            }

            structWriter.EagerFields = s.ToArray();
        }

        public override string ToString()
        {
            return $"{utype}*";
        }
    }
}
