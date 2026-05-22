using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ClrDebug;
using PESpy.Ecma335;

namespace PESpy.Mstat
{
    internal class MstatTypeDefTableDebugView
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public MstatTypeDefRow[] Items { get; }

        internal MstatTypeDefTableDebugView(MstatTypeDefTable table)
        {
            Items = table.ToArray();
        }
    }

    /// <summary>
    /// Represents a pseudo-metadata table that encapsulates <see cref="TypeRefRow"/> entries
    /// from a mstat <see cref="PEFile"/> and presents them as if they were a <see cref="TypeDefRow"/>
    /// </summary>
    [DebuggerTypeProxy(typeof(MstatTypeDefTableDebugView))]
    public class MstatTypeDefTable : IEnumerable<MstatTypeDefRow>
    {
        public int Size { get; internal set; }

        private readonly TypeRefTable _refTable;
        internal readonly MstatHeap _mstatHeap;

        private RowBuilder[] _rowBuilders;

        internal MstatTypeDefTable(TypeRefTable refTable, MstatHeap mstatHeap)
        {
            _refTable = refTable;
            _mstatHeap = mstatHeap;
            _rowBuilders = new RowBuilder[refTable.Count];
        }

        public MstatTypeDefRow this[int index] => new MstatTypeDefRow(this, index + 1);

        internal ref RowBuilder GetRowBuilder(mdTypeRef token, ref MstatHeapBuilder builder)
        {
            ref var rowBuilder = ref _rowBuilders[token.Rid - 1]; //RID is 1-based

            if (!rowBuilder.IsInitialized)
                rowBuilder.Initialize(token, ref builder);

            return ref rowBuilder;
        }

        internal ref RowBuilder GetMstatRow(int rowId) => ref _rowBuilders[rowId - 1];

        internal TypeRefRow GetRefRow(int rowId) => _refTable[rowId - 1];

        internal struct RowBuilder
        {
            //The next type ref in the linked list; we set this to the previous
            //type ref stored on the assembly that we overwrite with ourselves
            public mdTypeRef NextAssemblyTypeRef;

            //If a nested type is encountered, they will write themselves to this field,
            //and steal the previous value to store in NextNestedTypeRef
            public mdTypeRef FirstNestedTypeRef;
            public mdTypeRef NextNestedTypeRef;

            //If a type specialization is encountered, I think we'll have a linked list going
            //to the generic type definition?
            public mdTypeSpec FirstTypeSpec;

            public mdMemberRef FirstMemberRef;

            public int FirstFrozenObject;

            public int Size;
            public int TotalSize;

            internal bool IsInitialized;

            internal void Initialize(mdTypeRef token, ref MstatHeapBuilder builder)
            {
                var typeRef = builder.ModelHeap.TypeRefTable.FromToken(token);

                //The scope of the typeRef is either an assemblyref, or another typeref

                if (typeRef.ResolutionScope.TableKind == TableKind.AssemblyRef)
                {
                    //Updated the linked list of types; we become the head of the linked list,
                    //and the previous head is appended on to us
                    ref var assemblyBuilder = ref builder.MstatHeap.AssemblyTable.GetRowBuilder((mdAssemblyRef) typeRef.ResolutionScope, ref builder);

                    NextAssemblyTypeRef = assemblyBuilder.FirstTypeRef;
                    assemblyBuilder.FirstTypeRef = token;
                }
                else
                {
                    //Update the linked list of nested types; we become the head of the linked list,
                    //and the previous head is appended on to us
                    ref var parentBuilder = ref builder.MstatHeap.TypeDefTable.GetRowBuilder((mdTypeRef) typeRef.ResolutionScope, ref builder);

                    NextNestedTypeRef = parentBuilder.FirstNestedTypeRef;
                    parentBuilder.FirstNestedTypeRef = token;
                }

                IsInitialized = true;
            }

            internal void PropagateSize(mdTypeRef token, int size, ref MstatHeapBuilder builder)
            {
                var typeRef = builder.ModelHeap.TypeRefTable.FromToken(token);

                TotalSize += size;

                if (typeRef.ResolutionScope.TableKind == TableKind.AssemblyRef)
                    builder.MstatHeap.AssemblyTable.GetRowBuilder((mdAssemblyRef) typeRef.ResolutionScope, ref builder).PropagateSize(size);
                else
                    builder.MstatHeap.TypeDefTable.GetRowBuilder((mdTypeRef) typeRef.ResolutionScope, ref builder).PropagateSize((mdTypeRef) typeRef.ResolutionScope, size, ref builder);
            }
        }

        public struct AssemblyTypeRefIterator : MstatHeap.IMstatIterator<MstatTypeDefTable, MstatTypeDefRow>
        {
            public MstatTypeDefRow GetCurrent(MstatTypeDefTable table, int currentRowId) =>
                new MstatTypeDefRow(table, currentRowId);

            public int MoveNext(MstatTypeDefTable table, int currentRowId) => table.GetMstatRow(currentRowId).NextAssemblyTypeRef.Rid;
        }

        public struct TypeDefIterator : MstatHeap.IMstatIterator<MstatTypeDefTable, MstatTypeDefRow>
        {
            public MstatTypeDefRow GetCurrent(MstatTypeDefTable table, int currentRowId) =>
                new MstatTypeDefRow(table, currentRowId);

            public int MoveNext(MstatTypeDefTable table, int currentRowId)
            {
                if (currentRowId < table._rowBuilders.Length)
                    return currentRowId + 1;

                //We're the last RID, so there's no further token
                return default;
            }
        }

        public struct NestedTypeIterator : MstatHeap.IMstatIterator<MstatTypeDefTable, MstatTypeDefRow>
        {
            public MstatTypeDefRow GetCurrent(MstatTypeDefTable table, int currentRowId) =>
                new MstatTypeDefRow(table, currentRowId);

            public int MoveNext(MstatTypeDefTable table, int currentRowId) => table.GetMstatRow(currentRowId).NextNestedTypeRef.Rid;
        }

        public struct TypeSpecIterator : MstatHeap.IMstatIterator<MstatTypeSpecTable, MstatTypeSpecRow>
        {
            public MstatTypeSpecRow GetCurrent(MstatTypeSpecTable table, int currentRowId) =>
                new MstatTypeSpecRow(table, currentRowId);

            public int MoveNext(MstatTypeSpecTable table, int currentRowId) => table.GetMstatRow(currentRowId).NextTypeSpec.Rid;
        }

        public MstatHeap.Enumerator<MstatTypeDefTable, MstatTypeDefRow, TypeDefIterator> GetEnumerator() => new(1, this);

        IEnumerator<MstatTypeDefRow> IEnumerable<MstatTypeDefRow>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
