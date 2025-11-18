using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;
using static ClrDebug.PDB.CV_ptrmode_e;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfPointer_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfPointer16t : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int utypeOffset = 6;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfPointer_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->u.leaf;

        public TypOrEnumType utype => new TypOrEnumType((byte*) value, value->u.utype);

        public lfPointer_16t.lfPointerAttr_16t attr => value->u.attr;

        public lfPointer_16t.BaseInfo pbase => value->pbase;

        internal LfPointer16t(lfPointer_16t* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfPointer_16t, this, ViewKind.LfPointer16t, typlen + sizeof(short));

        int IViewable.NumChildren() => throw StructWriter.GetEagerLoadOnlyException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            //Way too complicated to do lazy loading
            if (index != -1)
                throw StructWriter.GetEagerLoadOnlyException();

            /* struct lfPointerAttr_16t {
             *     ...
             * }
             * 
             * struct lfPointerBody_16t {
             *     unsigned short      leaf;           // LF_POINTER_16t
             *     lfPointerAttr_16t attr;
             *     CV_typ16_t  utype;          // type index of the underlying type
             * }
             *  
             * 
             * typedef struct lfPointer_16t {
             *     struct lfPointerBody_16t u;
             *     union {
             *         struct {
             *             CV_typ16_t      pmclass;    // index of containing class for pointer to member
             *             unsigned short  pmenum;     // enumeration specifying pm format (CV_pmtype_e)
             *         } pm;
             *         unsigned short      bseg;       // base segment if PTR_BASE_SEG
             *         unsigned char       Sym[1];     // copy of base symbol record (including length)
             *         struct {
             *             CV_typ16_t      index;      // type index if CV_PTR_BASE_TYPE
             *             unsigned char   name[1];    // name of base type
             *         } btype;
             *     } pbase;
             * } lfPointer_16t;
             */

            using var s = structWriter.CreateEagerWriter();

            s.WriteField(nameof(typlen), typlen);

            //We are not currently mirroring the exact complex structure of lfPointer

            #region Body

            s.WriteField(nameof(leaf), leaf, sizeof(ushort));
            s.WriteField(nameof(utype), value->u.utype);

            using (var b = s.WriteBitFields<short>(6))
            {
                b.WriteField(nameof(attr.ptrtype), attr.ptrtype, 5);
                b.WriteField(nameof(attr.ptrmode), attr.ptrmode, 3);
                b.WriteField(nameof(attr.isflat32), attr.isflat32, 1);
                b.WriteField(nameof(attr.isvolatile), attr.isvolatile, 1);
                b.WriteField(nameof(attr.isconst), attr.isconst, 1);
                b.WriteField(nameof(attr.isunaligned), attr.isunaligned, 1);
                b.WriteField(nameof(attr.unused), attr.unused, 4);
            }

            #endregion
            #region pbase

            //What follows is one of pm, bseg, Sym or btype
            //The logic is the same as lfPointer

            if (attr.ptrmode == CV_PTR_MODE_PMEM || attr.ptrmode == CV_PTR_MODE_PMFUNC)
            {
                //PM (per pdbdump.cpp)
                throw new NotImplementedException();
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
                        throw new NotImplementedException();
                }
            }

            #endregion

            structWriter.EagerFields = s.ToArray();
        }
    }
}
