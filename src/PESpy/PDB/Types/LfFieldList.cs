using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using ClrDebug.PDB;
using PESpy.View;
using static ClrDebug.PDB.LEAF_ENUM_e;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfFieldList"/> structure.
    /// </summary>
    public readonly unsafe struct LfFieldList : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfFieldList* value;

        //This type is within the range of enum values that are defined as only ever existing inside other types,
        //however this type _can_ exist as a standalone type
        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        //Our length includes the size of our "leaf" field in it
        public LfEasy[] fields => EnumerateFields(null).ToArray();

        public IEnumerable<LfEasy> EnumerateFields(ICodeViewAccessor? codeViewAccessor) => EnumerateFields(typlen - sizeof(ushort), (IntPtr) value->data, null);

        //We share the same logic for LfFIeldList and LfFieldList16t
        internal static IEnumerable<LfEasy> EnumerateFields(int length, IntPtr ptr, ICodeViewAccessor? codeViewAccessor)
        {
            //pdbdump.cpp!strForFieldList only shows how to handle a couple of these; NT 4 shows how to handle all of them

            var pos = 0;

            while (pos < length)
            {
                var item = ProcessField(length, ptr, codeViewAccessor, ref pos);

                yield return item;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static LfEasy ProcessField(int length, IntPtr ptr, ICodeViewAccessor? codeViewAccessor, ref int pos)
        {
            var item = ptr + pos;

            var easy = new LfEasy((lfEasy*) item);

            switch (easy.leaf)
            {
                #region LfBClass

                case LF_BCLASS_16t:
                    var bClass16t = new LfBClass16t((lfBClass_16t*) item);
                    pos += bClass16t.StructSize;
                    break;

                case LF_BCLASS:
                    var bClass = new LfBClass((lfBClass*) item);
                    pos += bClass.StructSize;
                    break;

                #endregion
                #region LfEnumerate

                case LF_ENUMERATE:
                case LF_ENUMERATE_ST:
                    //We need to account for the standard length of an lfEnumerate (4), the size of the value,
                    //and the length of the name, which may or may not be length prefixed
                    var enumerate = new LfEnumerate((lfEnumerate*) item);
                    pos += enumerate.GetStructSize(codeViewAccessor);
                    break;

                #endregion
                #region LfFriendCls

                case LF_FRIENDCLS_16t:
                    throw new NotImplementedException();

                case LF_FRIENDCLS:
                    throw new NotImplementedException();

                #endregion
                #region LfFriendFcn

                case LF_FRIENDFCN_16t:
                    throw new NotImplementedException();

                case LF_FRIENDFCN:
                case LF_FRIENDFCN_ST:
                    throw new NotImplementedException();

                #endregion
                #region LfIndex

                case LF_INDEX_16t:
                    pos += LfIndex16t.StructSize;
                    break;

                case LF_INDEX:
                    pos += LfIndex.StructSize;
                    break;

                #endregion
                #region LfIVBClass

                case LF_IVBCLASS_16t:
                    throw new NotImplementedException();

                case LF_IVBCLASS:
                    throw new NotImplementedException();

                #endregion
                #region LfMember

                case LF_MEMBER_16t:
                    var member16t = new LfMember16t((lfMember_16t*) item);
                    pos += member16t.GetStructSize(codeViewAccessor);
                    break;

                case LF_MEMBER:
                case LF_MEMBER_ST:
                    var member = new LfMember((lfMember*) item);
                    pos += member.GetStructSize(codeViewAccessor);
                    break;

                #endregion
                #region LfMethod

                case LF_METHOD_16t:
                    var method16t = new LfMethod16t((lfMethod_16t*) item);
                    pos += method16t.GetStructSize(codeViewAccessor);
                    break;

                case LF_METHOD:
                case LF_METHOD_ST:
                    var method = new LfMethod((lfMethod*) item);
                    pos += method.GetStructSize(codeViewAccessor);
                    break;

                #endregion
                #region LfNestType

                case LF_NESTTYPE_16t:
                    var nestType16t = new LfNestType16t((lfNestType_16t*) item);
                    pos += nestType16t.GetStructSize(codeViewAccessor);
                    break;

                case LF_NESTTYPE:
                case LF_NESTTYPE_ST:
                    var nestType = new LfNestType((lfNestType*) item);
                    pos += nestType.GetStructSize(codeViewAccessor);
                    break;

                #endregion
                #region LfOneMethod

                case LF_ONEMETHOD_16t:
                    var oneMethod16t = new LfOneMethod16t((lfOneMethod_16t*) item);
                    pos += oneMethod16t.GetStructSize(codeViewAccessor);
                    break;

                case LF_ONEMETHOD:
                case LF_ONEMETHOD_ST:
                    var oneMethod = new LfOneMethod((lfOneMethod*) item);
                    pos += oneMethod.GetStructSize(codeViewAccessor);
                    break;

                #endregion
                #region LfSTMember

                case LF_STMEMBER_16t:
                    var staticMember16t = new LfSTMember((lfSTMember*) item);
                    pos += staticMember16t.GetStructSize(codeViewAccessor);
                    break;

                case LF_STMEMBER:
                case LF_STMEMBER_ST:
                    var staticMember = new LfSTMember((lfSTMember*) item);
                    pos += staticMember.GetStructSize(codeViewAccessor);
                    break;

                #endregion
                #region LFVBClass

                case LF_VBCLASS_16t:
                    var vbClass16t = new LfVBClass16t((lfVBClass_16t*) item);
                    pos += vbClass16t.StructSize;
                    break;

                case LF_VBCLASS:
                    var vbClass = new LfVBClass((lfVBClass*) item);
                    pos += vbClass.StructSize;
                    break;

                #endregion
                #region LFVFuncTab

                case LF_VFUNCTAB_16t:
                    pos += LfVFuncTab16t.StructSize;
                    break;

                case LF_VFUNCTAB:
                    pos += LfVFuncTab.StructSize;
                    break;

                #endregion

                default:
                    throw new NotImplementedException($"Don't know how to handle a sub-leaf of type '{easy.leaf}'");
            }

            //Skip pad bytes
            var val = ((byte*) ptr + pos);

            if (pos < length && (*val & (byte) LF_PAD0) == (byte) LF_PAD0)
            {
                var toSkip = *val & 0xF;
                pos += toSkip;
            }

            return easy;
        }

        internal unsafe int ComputeLength(PDBFile pdbFile)
        {
            var pos = 0;

            var length = typlen - sizeof(ushort);
            var ptr = (IntPtr) value->data;

            var lastMemberEnd = 0;

            while (pos < length)
            {
                var item = ProcessField(length, ptr, pdbFile, ref pos);

                if (item.leaf == LF_MEMBER)
                {
                    var lfMember = (LfMember) item;

                    if (TryGetLengthSafe(lfMember.index, pdbFile, out var memberLength))
                    {
                        //Members are not guaranteed to be listed in order
                        lastMemberEnd = Math.Max(lastMemberEnd, lfMember.offset + memberLength);
                    }
                }
            }

            if (lastMemberEnd == 0)
            {
                //We need to process base types to find their last end
                pos = 0;

                while (pos < length)
                {
                    var item = ProcessField(length, ptr, pdbFile, ref pos);

                    if (item.leaf == LF_BCLASS)
                    {
                        var lfBClass = ((LfBClass) item).index.TypTyp;

                        if (lfBClass != null)
                        {
                            var val = lfBClass.Value;

                            Debug.Assert(val.leaf == LF_CLASS || val.leaf == LF_STRUCTURE);

                            var lfClass = (LfClass) val;

                            var maybeFieldList = lfClass.field.TypTyp;

                            if (maybeFieldList != null)
                            {
                                Debug.Assert(maybeFieldList.Value.leaf == LF_FIELDLIST);

                                var baseClassLength = ((LfFieldList) maybeFieldList.Value).ComputeLength(pdbFile);
                                lastMemberEnd = Math.Max(lastMemberEnd, baseClassLength);
                            }
                        }
                    }
                }
            }

            Debug.Assert(lastMemberEnd != 0);

            return lastMemberEnd;
        }

        private static bool TryGetLengthSafe(
            TypOrEnumType type,
            PDBFile pdbFile,
            out int length)
        {
            if (type.TypTyp != null)
            {
                var typType = type.TypTyp.Value;

                if (typType.IsFwdRef())
                {
                    var name = typType.GetName(pdbFile);

                    if (pdbFile.TPI.TpiHash.TryGetIndexFromName(name, false, out var typeIndex))
                    {
                        //If this fails, not much we can do
                        pdbFile.TPI.TryGetTypTypeFromIndex(typeIndex, out typType);

                        return typType.TryGetLength(out length);
                    }
                }
            }

            return type.TryGetLength(out length);
        }

        internal static void WriteChild(ushort typlen, LEAF_ENUM_e leaf, byte* data, int index, ref StructWriter structWriter)
        {
            if (index != -1)
                throw StructWriter.GetEagerLoadOnlyException();

            using var s = structWriter.CreateEagerWriter();

            s.WriteField(nameof(typlen), typlen);
            s.WriteField(nameof(leaf), leaf, sizeof(ushort));

            //We need to write any padding, so we need to parse this manually here

            var pos = 0;

            //Our length includes the size of our "leaf" field in it
            var length = typlen - sizeof(ushort);

            var codeViewAccessor = structWriter.GetSymbolAccessor();

            while (pos < length)
            {
                var item = data + pos;

                var easy = new LfEasy((lfEasy*) item);

                switch (easy.leaf)
                {
                    #region LfBClass

                    case LF_BCLASS_16t:
                        var bClass16t = new LfBClass16t((lfBClass_16t*) item);
                        pos += bClass16t.StructSize;
                        s.WriteUnmanagedInline(bClass16t);
                        break;

                    case LF_BCLASS:
                        var bClass = new LfBClass((lfBClass*) item);
                        pos += bClass.StructSize;
                        s.WriteUnmanagedInline(bClass);
                        break;

                    #endregion
                    #region LfEnumerate

                    case LF_ENUMERATE:
                    case LF_ENUMERATE_ST:
                        //We need to account for the standard length of an lfEnumerate (4), the size of the value,
                        //and the length of the name, which may or may not be length prefixed
                        var enumerate = new LfEnumerate((lfEnumerate*) item);
                        pos += enumerate.GetStructSize(codeViewAccessor);
                        s.WriteUnmanagedInline(enumerate);
                        break;

                    #endregion
                    #region LfFriendCls

                    case LF_FRIENDCLS_16t:
                        throw new NotImplementedException();

                    case LF_FRIENDCLS:
                        throw new NotImplementedException();

                    #endregion
                    #region LfFriendFcn

                    case LF_FRIENDFCN_16t:
                        throw new NotImplementedException();

                    case LF_FRIENDFCN:
                    case LF_FRIENDFCN_ST:
                        throw new NotImplementedException();

                    #endregion
                    #region LfIndex

                    case LF_INDEX_16t:
                        s.WriteUnmanagedInline(new LfIndex16t((lfIndex_16t*) item));
                        pos += LfIndex16t.StructSize;
                        break;

                    case LF_INDEX:
                        s.WriteUnmanagedInline(new LfIndex((lfIndex*) item));
                        pos += LfIndex.StructSize;
                        break;

                    #endregion
                    #region LfIVBClass

                    case LF_IVBCLASS_16t:
                        throw new NotImplementedException();

                    case LF_IVBCLASS:
                        throw new NotImplementedException();

                    #endregion
                    #region LfMember

                    case LF_MEMBER_16t:
                        var member16t = new LfMember16t((lfMember_16t*) item);
                        pos += member16t.GetStructSize(codeViewAccessor);
                        s.WriteUnmanagedInline(member16t);
                        break;

                    case LF_MEMBER:
                    case LF_MEMBER_ST:
                        var member = new LfMember((lfMember*) item);
                        pos += member.GetStructSize(codeViewAccessor);
                        s.WriteUnmanagedInline(member);
                        break;

                    #endregion
                    #region LfMethod

                    case LF_METHOD_16t:
                        var method16t = new LfMethod16t((lfMethod_16t*) item);
                        pos += method16t.GetStructSize(codeViewAccessor);
                        s.WriteUnmanagedInline(method16t);
                        break;

                    case LF_METHOD:
                    case LF_METHOD_ST:
                        var method = new LfMethod((lfMethod*) item);
                        pos += method.GetStructSize(codeViewAccessor);
                        s.WriteUnmanagedInline(method);
                        break;

                    #endregion
                    #region LfNestType

                    case LF_NESTTYPE_16t:
                        var nestType16t = new LfNestType16t((lfNestType_16t*) item);
                        pos += nestType16t.GetStructSize(codeViewAccessor);
                        s.WriteUnmanagedInline(nestType16t);
                        break;

                    case LF_NESTTYPE:
                    case LF_NESTTYPE_ST:
                        var nestType = new LfNestType((lfNestType*) item);
                        pos += nestType.GetStructSize(codeViewAccessor);
                        s.WriteUnmanagedInline(nestType);
                        break;

                    #endregion
                    #region LfOneMethod

                    case LF_ONEMETHOD_16t:
                        var oneMethod16t = new LfOneMethod16t((lfOneMethod_16t*) item);
                        pos += oneMethod16t.GetStructSize(codeViewAccessor);
                        s.WriteUnmanagedInline(oneMethod16t);
                        break;

                    case LF_ONEMETHOD:
                    case LF_ONEMETHOD_ST:
                        var oneMethod = new LfOneMethod((lfOneMethod*) item);
                        pos += oneMethod.GetStructSize(codeViewAccessor);
                        s.WriteUnmanagedInline(oneMethod);
                        break;

                    #endregion
                    #region LfSTMember

                    case LF_STMEMBER_16t:
                        var staticMember16t = new LfSTMember16t((lfSTMember_16t*) item);
                        pos += staticMember16t.GetStructSize(codeViewAccessor);
                        s.WriteUnmanagedInline(staticMember16t);
                        break;

                    case LF_STMEMBER:
                    case LF_STMEMBER_ST:
                        var staticMember = new LfSTMember((lfSTMember*) item);
                        pos += staticMember.GetStructSize(codeViewAccessor);
                        s.WriteUnmanagedInline(staticMember);
                        break;

                    #endregion
                    #region LFVBClass

                    case LF_VBCLASS_16t:
                        throw new NotImplementedException();

                    case LF_VBCLASS:
                        throw new NotImplementedException();

                    #endregion
                    #region LFVFuncTab

                    case LF_VFUNCTAB_16t:
                        s.WriteUnmanagedInline(new LfVFuncTab16t((lfVFuncTab_16t*) item));
                        pos += LfVFuncTab16t.StructSize;
                        break;

                    case LF_VFUNCTAB:
                        s.WriteUnmanagedInline(new LfVFuncTab((lfVFuncTab*) item));
                        pos += LfVFuncTab.StructSize;
                        break;

                    #endregion

                    default:
                        throw new System.NotImplementedException();
                }

                //Skip pad bytes
                var val = (data + pos);

                //If you have LF_PAD2, what you'll actually have is LF_PAD2, LF_PAD1
                while (pos < length && (*val & (byte) LF_PAD0) == (byte) LF_PAD0)
                {
                    s.WriteValue((LEAF_ENUM_e) (*val), sizeof(byte));
                    pos++;
                    val++;
                }
            }

            structWriter.EagerFields = s.ToArray();
        }

        internal const int FixedStructSize =
            sizeof(ushort);  //leaf

        internal LfFieldList(lfFieldList* value)
        {
            this.value = value;
        }

        public static implicit operator LfEasy(LfFieldList easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfFieldList, this, ViewKind.LfFieldList, typlen + sizeof(short));

        int IViewable.NumChildren() => throw StructWriter.GetEagerLoadOnlyException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => WriteChild(typlen, leaf, value->data, index, ref structWriter);
    }
}
