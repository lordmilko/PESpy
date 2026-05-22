using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ClrDebug;
using PESpy.Ecma335;

namespace PESpy.Mstat
{
    /* Mstat files are a bit funny, in that _everything_ is considered a "reference". TypeDefTable
     * and MethodDefTable are reserved for describing the encoded entities contained within the file.
     * The actual entities are then contained in TypeRefTable, MemberRefTable, and the other normal
     * tables
     * 
     * This makes things a bit tricky for us, because in order to do something like, say, iterate the members
     * of a type, you can't; a TypeRef doesn't maintain a list of its members in the MemberRef table.
     * 
     * sizoscope solves this problem in a clever way: it maintains a cache of nodes for each entity, and
     * as the type hierarchy is built up, the relationships between nodes are stored via a linked list.
     * The craziness is really dialed up to 11 when a general purpose allocation free Enumerator is
     * implemented, that use an extreme amount of generics to reuse that enumerator for different types
     * of scenarios
     * 
     * See sizoscope license in ThirdPartyNotices.txt
     */

    //Based on the design of ModelHeap
    public class MstatHeap
    {
        public MstatAssemblyTable AssemblyTable { get; }

        public MstatTypeDefTable TypeDefTable { get; }

        public MstatTypeSpecTable TypeSpecTable { get; }

        public MstatMemberTable MemberTable { get; }

        public MstatMethodSpecTable MethodSpecTable { get; }

        public MstatManifestResourceTable ManifestResourceTable { get; }

        public MstatFrozenObjectTable FrozenObjectTable { get; }

        internal MstatHeap(ModelHeap modelHeap, MstatInfo info)
        {
            if (modelHeap.AssemblyRefTable != null)
                AssemblyTable = new MstatAssemblyTable(modelHeap.AssemblyRefTable, this);

            if (modelHeap.TypeRefTable != null)
                TypeDefTable = new MstatTypeDefTable(modelHeap.TypeRefTable, this);

            if (modelHeap.TypeSpecTable != null)
                TypeSpecTable = new MstatTypeSpecTable(modelHeap.TypeSpecTable, this);

            if (modelHeap.MemberRefTable != null)
                MemberTable = new MstatMemberTable(modelHeap.MemberRefTable, this);

            if (modelHeap.MethodSpecTable != null)
                MethodSpecTable = new MstatMethodSpecTable(modelHeap.MethodSpecTable, this);

            var builder = new MstatHeapBuilder(this, modelHeap);

            ProcessTypes(info, ref builder);
            ProcessMethods(info, ref builder);
            ProcessFields(info, ref builder);

            ManifestResourceTable = ProcessManifestResources(info, ref builder);
            FrozenObjectTable = ProcessFrozenObjects(info, ref builder);

            //Deduplicated methods not yet implemented
        }

        private void ProcessTypes(MstatInfo info, ref MstatHeapBuilder builder)
        {
            var totalRefSize = 0;
            var totalSpecSize = 0;

            foreach (var type in info.Types)
            {
                switch (type.Token.Type)
                {
                    case CorTokenType.mdtTypeRef:
                        totalRefSize += type.Size;
                        ref var typeDefBuilder = ref TypeDefTable.GetRowBuilder((mdTypeRef) type.Token, ref builder);
                        typeDefBuilder.Size = type.Size;
                        typeDefBuilder.PropagateSize((mdTypeRef) type.Token, type.Size, ref builder);
                        break;

                    case CorTokenType.mdtTypeSpec:
                        totalSpecSize += type.Size;
                        ref var typeSpecBuilder = ref TypeSpecTable.GetRowBuilder((mdTypeSpec) type.Token, ref builder);
                        typeSpecBuilder.Size = type.Size;
                        typeSpecBuilder.PropagateSize((mdTypeSpec) type.Token, type.Size, ref builder);
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            TypeDefTable.Size = totalRefSize;
            TypeSpecTable.Size = totalSpecSize;
        }

        private void ProcessMethods(MstatInfo info, ref MstatHeapBuilder builder)
        {
            var totalMemberSize = 0;
            var totalSpecSize = 0;

            foreach (var method in info.Methods)
            {
                var size = method.Size + method.GcInfoSize + method.MethodEhInfoSize;

                switch (method.Token.Type)
                {
                    case CorTokenType.mdtMemberRef:
                        totalMemberSize += size;
                        ref var memberBuilder = ref MemberTable.GetRowBuilder((mdMemberRef) method.Token, ref builder);
                        memberBuilder.Size = size;
                        memberBuilder.PropagateSize((mdMemberRef) method.Token, size, ref builder);
                        break;

                    case CorTokenType.mdtMethodSpec:
                        totalSpecSize += size;
                        ref var methodSpecBuilder = ref MethodSpecTable.GetRowBuilder((mdMethodSpec) method.Token, ref builder);
                        methodSpecBuilder.Size = size;
                        methodSpecBuilder.PropagateSize((mdMethodSpec) method.Token, size, ref builder);
                        break;

                    default:
                        throw new NotImplementedException();

                }
            }

            MemberTable.Size = totalMemberSize;
            MethodSpecTable.Size = totalSpecSize;
        }

        private void ProcessFields(MstatInfo info, ref MstatHeapBuilder builder)
        {
            var totalSize = 0;

            foreach (var field in info.Fields)
            {
                ref var memberBuilder = ref MemberTable.GetRowBuilder((mdMemberRef) field.Token, ref builder);
                memberBuilder.Size = field.Size;
                memberBuilder.PropagateSize((mdMemberRef) field.Token, field.Size, ref builder);

                totalSize += field.Size;
            }

            MemberTable.Size = totalSize;
        }

        private MstatManifestResourceTable ProcessManifestResources(MstatInfo info, ref MstatHeapBuilder builder)
        {
            var rows = new MstatManifestResourceRow[info.ManifestResources.Length];

            var totalSize = 0;

            for (var i = 0; i < info.ManifestResources.Length; i++)
            {
                var manifestResource = info.ManifestResources[i];

                ref var assemblyBuilder = ref AssemblyTable.GetRowBuilder((mdAssemblyRef) manifestResource.Token, ref builder);
                assemblyBuilder.PropagateSize(manifestResource.Size);

                var row = new MstatManifestResourceRow(manifestResource, this)
                {
                    NextManifestResource = assemblyBuilder.FirstManifestResource
                };

                rows[i] = row;

                //While there is mdManifestResource, we don't use this to index into the real ECMA-335 metadata
                assemblyBuilder.FirstManifestResource = i;

                totalSize += manifestResource.Size;
            }

            return new MstatManifestResourceTable(rows, totalSize);
        }

        private MstatFrozenObjectTable ProcessFrozenObjects(MstatInfo info, ref MstatHeapBuilder builder)
        {
            var ownedFrozenObjectSize = 0;
            var unownedFrozenObjectSize = 0;

            var firstUnownedFrozenObject = 0;

            var rows = new MstatFrozenObjectRow[info.FrozenObjects.Length];

            for (var i = 0; i < info.FrozenObjects.Length; i++)
            {
                var frozenObject = info.FrozenObjects[i];

                var row = new MstatFrozenObjectRow(frozenObject, this);

                if (!frozenObject.OwningTypeToken.IsNil)
                {
                    if (frozenObject.OwningTypeToken.Type == CorTokenType.mdtTypeRef)
                    {
                        ref var typeBuilder = ref TypeDefTable.GetRowBuilder((mdTypeRef) frozenObject.OwningTypeToken, ref builder);
                        typeBuilder.PropagateSize((mdTypeRef) frozenObject.OwningTypeToken, frozenObject.Size, ref builder);

                        row.NextFrozenObject = typeBuilder.FirstFrozenObject;
                        typeBuilder.FirstFrozenObject = i + 1;
                    }
                    else
                    {
                        ref var typeBuilder = ref TypeSpecTable.GetRowBuilder((mdTypeSpec) frozenObject.OwningTypeToken, ref builder);
                        typeBuilder.PropagateSize((mdTypeSpec) frozenObject.OwningTypeToken, frozenObject.Size, ref builder);

                        row.NextFrozenObject = typeBuilder.FirstFrozenObject;
                        typeBuilder.FirstFrozenObject = i + 1;
                    }

                    ownedFrozenObjectSize += frozenObject.Size;
                }
                else
                {
                    unownedFrozenObjectSize += frozenObject.Size;

                    row.NextFrozenObject = firstUnownedFrozenObject;
                    firstUnownedFrozenObject = i;
                }

                rows[i] = row;
            }

            return new MstatFrozenObjectTable(rows, ownedFrozenObjectSize, unownedFrozenObjectSize, firstUnownedFrozenObject);
        }

        public interface IMstatIterator<TTable, TRow>
        {
            TRow GetCurrent(TTable table, int currentRowId);

            int MoveNext(TTable table, int currentRowId);
        }

        public struct FrozenObjectIterator : IMstatIterator<MstatFrozenObjectTable, MstatFrozenObjectRow>
        {
            public MstatFrozenObjectRow GetCurrent(MstatFrozenObjectTable table, int currentRowId) =>
                table[currentRowId - 1];

            public int MoveNext(MstatFrozenObjectTable table, int currentRowId) => table[currentRowId - 1].NextFrozenObject;
        }

        public struct TypeMemberIterator : IMstatIterator<MstatMemberTable, MstatMemberRow>
        {
            public MstatMemberRow GetCurrent(MstatMemberTable table, int currentRowId) =>
                new MstatMemberRow(table, currentRowId);

            public int MoveNext(MstatMemberTable table, int currentRowId) => table.GetMstatRow(currentRowId).NextMemberRef.Rid;
        }

        internal class EnumeratorDebugView<TTable, TRow, TIterator>
            where TIterator : struct, IMstatIterator<TTable, TRow>
        {
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public TRow[] Items { get; }

            public EnumeratorDebugView(Enumerator<TTable, TRow, TIterator> enumerator)
            {
                Items = enumerator.ToArray();
            }
        }

        [DebuggerTypeProxy(typeof(EnumeratorDebugView<,,>))]
        public struct Enumerator<TTable, TRow, TIterator> : IEnumerator<TRow>, IEnumerable<TRow>
            where TIterator : struct, IMstatIterator<TTable, TRow>
        {
            private readonly int _firstRowId;
            private readonly TTable _table;
            private int _currentRowId;

            internal Enumerator(int firstRowId, TTable table)
            {
                _firstRowId = firstRowId;
                _table = table;
            }

            //Since TIterator is a struct, we can new them up for free without
            //needing to hold a reference
            public TRow Current => new TIterator().GetCurrent(_table, _currentRowId);

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_currentRowId == 0)
                {
                    if (_firstRowId == 0)
                        return false;

                    _currentRowId = _firstRowId;
                }
                else
                {
                    var nextRowId = new TIterator().MoveNext(_table, _currentRowId);

                    if (nextRowId == 0)
                        return false;

                    _currentRowId = nextRowId;
                }

                return true;
            }

            public Enumerator<TTable, TRow, TIterator> GetEnumerator() => this;

            IEnumerator<TRow> IEnumerable<TRow>.GetEnumerator() => this;

            IEnumerator IEnumerable.GetEnumerator() => this;

            public void Reset()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
