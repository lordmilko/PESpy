using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ClrDebug;
using PESpy.Ecma335;

namespace PESpy.Mstat
{
    internal class MstatTypeSpecTableDebugView
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public MstatTypeSpecRow[] Items { get; }

        internal MstatTypeSpecTableDebugView(MstatTypeSpecTable table)
        {
            Items = table.ToArray();
        }
    }

    [DebuggerTypeProxy(typeof(MstatTypeSpecTableDebugView))]
    public class MstatTypeSpecTable : IEnumerable<MstatTypeSpecRow>
    {
        public int Size { get; internal set; }

        private readonly TypeSpecTable _refTable;
        internal readonly MstatHeap _mstatHeap;

        private RowBuilder[] _rowBuilders;

        internal MstatTypeSpecTable(TypeSpecTable refTable, MstatHeap mstatHeap)
        {
            _refTable = refTable;
            _mstatHeap = mstatHeap;
            _rowBuilders = new RowBuilder[refTable.Count];
        }

        public MstatTypeSpecRow this[int index] => new MstatTypeSpecRow(this, index + 1);

        internal ref RowBuilder GetRowBuilder(mdTypeSpec token, ref MstatHeapBuilder builder)
        {
            ref var rowBuilder = ref _rowBuilders[token.Rid - 1]; //RID is 1-based

            if (!rowBuilder.IsInitialized)
                rowBuilder.Initialize(token, ref builder);

            return ref rowBuilder;
        }

        internal ref RowBuilder GetMstatRow(int rowId) => ref _rowBuilders[rowId - 1];

        internal TypeSpecRow GetRefRow(int rowId) => _refTable[rowId - 1];

        internal struct RowBuilder
        {
            public mdTypeSpec NextTypeSpec;

            public mdMemberRef FirstMemberRef;

            public int FirstFrozenObject;

            public int Size;
            public int TotalSize;

            internal bool IsInitialized;

            internal void Initialize(mdTypeSpec token, ref MstatHeapBuilder builder)
            {
                var typeSpec = builder.ModelHeap.TypeSpecTable.FromToken(token);

                var declaringTypeToken = GetDeclaringTypeRef(typeSpec.Signature.GetReader(), ref builder);

                ref var declaringTypeBuilder = ref builder.MstatHeap.TypeDefTable.GetRowBuilder(declaringTypeToken, ref builder);
                NextTypeSpec = declaringTypeBuilder.FirstTypeSpec;
                declaringTypeBuilder.FirstTypeSpec = token;

                IsInitialized = true;
            }

            internal void PropagateSize(mdTypeSpec token, int size, ref MstatHeapBuilder builder)
            {
                var typeSpec = builder.ModelHeap.TypeSpecTable.FromToken(token);

                TotalSize += size;

                var declaringTypeToken = GetDeclaringTypeRef(typeSpec.Signature.GetReader(), ref builder);

                ref var declaringTypeBuilder = ref builder.MstatHeap.TypeDefTable.GetRowBuilder(declaringTypeToken, ref builder);

                declaringTypeBuilder.PropagateSize(declaringTypeToken, size, ref builder);
            }

            private mdTypeRef GetDeclaringTypeRef(ByteReader byteReader, ref MstatHeapBuilder builder)
            {
                var corElementType = byteReader.ReadCorElementType();

                switch (corElementType)
                {
                    case CorElementType.GenericInst:
                        return GetDeclaringTypeRef(byteReader, ref builder);

                    case CorElementType.SZArray:
                    case CorElementType.Array:
                    case CorElementType.Ptr:
                    case CorElementType.ByRef:
                        return GetDeclaringTypeRef(byteReader, ref builder);

                    case CorElementType.Class:
                    case CorElementType.ValueType:
                        return (mdTypeRef) byteReader.ReadToken();

                    case CorElementType.Void:
                    case CorElementType.Boolean:
                    case CorElementType.Char:
                    case CorElementType.I1:
                    case CorElementType.U1:
                    case CorElementType.I2:
                    case CorElementType.U2:
                    case CorElementType.I4:
                    case CorElementType.U4:
                    case CorElementType.I8:
                    case CorElementType.U8:
                    case CorElementType.R4:
                    case CorElementType.R8:
                    case CorElementType.String:
                    case CorElementType.Object:
                    case CorElementType.TypedByRef:
                    case CorElementType.I:
                    case CorElementType.U:
                        return builder.GetSystemTypeRef(corElementType);

                    case CorElementType.FnPtr:
                        var callingConv = (CorCallingConvention) byteReader.ReadByte();

                        if ((callingConv & CorCallingConvention.GENERIC) != 0)
                            byteReader.ReadCompressedInteger(); //Skip genericParameterCount

                        //Skip parameterCount
                        byteReader.ReadCompressedInteger();

                        //Effective type will be return type
                        return GetDeclaringTypeRef(byteReader, ref builder);

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public struct TypeSpecIterator : MstatHeap.IMstatIterator<MstatTypeSpecTable, MstatTypeSpecRow>
        {
            public MstatTypeSpecRow GetCurrent(MstatTypeSpecTable table, int currentRowId) =>
                new MstatTypeSpecRow(table, currentRowId);

            public int MoveNext(MstatTypeSpecTable table, int currentRowId)
            {
                if (currentRowId < table._rowBuilders.Length)
                    return currentRowId + 1;

                //We're the last RID, so there's no further token
                return default;
            }
        }

        public MstatHeap.Enumerator<MstatTypeSpecTable, MstatTypeSpecRow, TypeSpecIterator> GetEnumerator() => new(1, this);

        IEnumerator<MstatTypeSpecRow> IEnumerable<MstatTypeSpecRow>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
