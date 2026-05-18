using System;
using System.Collections.Generic;
using System.Diagnostics;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class TypeDefTable : Table<TypeDefRow>
    {
        internal readonly int FlagsOffset;
        internal readonly int TypeNameOffset;
        internal readonly int TypeNamespaceOffset;
        internal readonly int ExtendsOffset;
        internal readonly int FieldListOffset;
        internal readonly int MethodListOffset;

        private readonly bool isBigStringIndex;
        private readonly bool isBigTypeDefOrRefIndex;
        private readonly bool isBigFieldIndex;
        private readonly bool isBigMethodIndex;

        internal readonly ModelHeap ModelHeap;
        private readonly Func<StringHeap?> stringHeap;

        private Dictionary<TypeDefIndex, TypeDefIndex[]>? _lazyNestedTypesMap;

        internal TypeDefTable(
            int numRows,
            int stringIndexSize,
            int typeDefOrRefIndexSize,
            int fieldIndexSize,
            int methodIndexSize,
            ModelHeap modelHeap,
            Func<StringHeap?> stringHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            //II.22.37

            ModelHeap = modelHeap;
            this.stringHeap = stringHeap;

            isBigStringIndex = stringIndexSize == 4;
            isBigTypeDefOrRefIndex = typeDefOrRefIndexSize == 4;
            isBigFieldIndex = fieldIndexSize == 4;
            isBigMethodIndex = methodIndexSize == 4;

            FlagsOffset = 0;
            TypeNameOffset = FlagsOffset + sizeof(int);
            TypeNamespaceOffset = TypeNameOffset + stringIndexSize;
            ExtendsOffset = TypeNamespaceOffset + stringIndexSize;
            FieldListOffset = ExtendsOffset + typeDefOrRefIndexSize;
            MethodListOffset = FieldListOffset + fieldIndexSize;
            RowSize = MethodListOffset + methodIndexSize;
        }

        public CorTypeAttr GetFlags(TypeDefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (CorTypeAttr) tableChunk.PeekUInt32(rowOffset + FlagsOffset);
        }

        public StringIndex GetTypeName(TypeDefIndex index)
        {
            Debug.Assert(index.RowId >= 0);
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + TypeNameOffset, isBigStringIndex), stringHeap);
        }

        public StringIndex GetTypeNamespace(TypeDefIndex index)
        {
            Debug.Assert(index.RowId >= 0);
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + TypeNamespaceOffset, isBigStringIndex), stringHeap);
        }

        public CodedIndex GetExtends(TypeDefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekCodedIndex(rowOffset + ExtendsOffset, isBigTypeDefOrRefIndex, CodedIndexType.TypeDefOrRef);
        }

        public FieldIndex GetFieldList(TypeDefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (FieldIndex) tableChunk.PeekEcmaIndex(rowOffset + FieldListOffset, isBigFieldIndex);
        }

        public MethodDefIndex GetMethodList(TypeDefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (MethodDefIndex) tableChunk.PeekEcmaIndex(rowOffset + MethodListOffset, isBigMethodIndex);
        }

        //Note: doesn't support nested types
        public TypeDefRow this[string topLevelName]
        {
            get
            {
                var dot = topLevelName.LastIndexOf('.');

                if (dot == -1)
                {
                    foreach (var item in this)
                    {
                        if (item.TypeName.GetString() == topLevelName)
                            return item;
                    }
                }
                else
                {
                    var ns = topLevelName.AsSpan(0, dot);
                    var name = topLevelName.AsSpan(dot + 1);

                    foreach (var item in this)
                    {
                        if (item.TypeNamespace.GetString() == ns && item.TypeName.GetString() == name)
                            return item;
                    }
                }

                throw new InvalidOperationException($"Failed to find a type named '{topLevelName}'");
            }
        }

        internal TypeDefRow? FindTypeContainingMethod(int methodRowId, int numberOfMethods)
        {
            var row = ModelHeap.BinarySearchEcmaIndexList(
                tableChunk,
                Count,
                RowSize,
                MethodListOffset,
                (uint) methodRowId,
                isBigMethodIndex
            ) + 1;

            if (row == 0)
                return default;

            if (row > Count)
            {
                if (methodRowId <= numberOfMethods)
                    return this[(TypeDefIndex) Count]; //It's the last type

                return default;
            }

            var methodList = GetMethodList((TypeDefIndex) row);

            if (methodList.RowId == methodRowId)
            {
                //See the comment in FindTypeContainingField() for why this is necessary

                while (row < Count)
                {
                    var nextRow = row + 1;

                    var nextMethodList = GetMethodList((TypeDefIndex) nextRow);

                    if (nextMethodList.RowId == methodRowId)
                        row = nextRow;
                    else
                        break;
                }

                return this[(TypeDefIndex) row];
            }

            return this[(TypeDefIndex) row];
        }

        internal TypeDefRow? FindTypeContainingField(int fieldRowId, int numberOfFields)
        {
            var row = ModelHeap.BinarySearchEcmaIndexList(
                tableChunk,
                Count,
                RowSize,
                FieldListOffset,
                (uint) fieldRowId,
                isBigFieldIndex
            ) + 1;

            if (row == 0)
                return default;

            if (row > Count)
            {
                if (fieldRowId <= numberOfFields)
                    return this[(TypeDefIndex) Count]; //It's the last type

                return default;
            }

            var fieldList = GetFieldList((TypeDefIndex) row);

            if (fieldList.RowId == fieldRowId)
            {
                //If multiple TypeDefs in a row say their FieldList is "3", what this really means is that the next type to have a field
                //has that field start at 3, however every type but the last type doesn't actually own any fields, since the range of
                //values between their field (3) and the next type's fields (3) is 0. Thus, we need to skip ahead to the last type
                //that says they own field 3, which will give us the _actual_ owner of the field

                while (row < Count)
                {
                    var nextRow = row + 1;

                    var nextFieldList = GetFieldList((TypeDefIndex) nextRow);

                    if (nextFieldList.RowId == fieldRowId)
                        row = nextRow;
                    else
                        break;
                }
            }

            return this[(TypeDefIndex) row];
        }

        private void InitializeNestedTypesMap()
        {
            var groupedNestedTypes = new Dictionary<TypeDefIndex, List<TypeDefIndex>>();

            var nestedClassTable = ModelHeap.NestedClassTable;

            int numberOfNestedTypes = nestedClassTable.Count;
            List<TypeDefIndex>? builder = null;
            TypeDefIndex previousEnclosingClass = default;

            for (int i = 1; i <= numberOfNestedTypes; i++)
            {
                var enclosingClass = nestedClassTable.GetEnclosingClass((NestedClassIndex) i);

                Debug.Assert(!enclosingClass.IsNil);

                if (enclosingClass != previousEnclosingClass)
                {
                    if (!groupedNestedTypes.TryGetValue(enclosingClass, out builder))
                    {
                        builder = new List<TypeDefIndex>();
                        groupedNestedTypes.Add(enclosingClass, builder);
                    }

                    previousEnclosingClass = enclosingClass;
                }
                else
                {
                    Debug.Assert(builder == groupedNestedTypes[enclosingClass]);
                }

                builder.Add(nestedClassTable.GetNestedClass((NestedClassIndex) i));
            }

            var nestedTypesMap = new Dictionary<TypeDefIndex, TypeDefIndex[]>();
            foreach (var group in groupedNestedTypes)
            {
                nestedTypesMap.Add(group.Key, group.Value.ToArray());
            }

            _lazyNestedTypesMap = nestedTypesMap;
        }

        public TypeDefIndex[] GetNestedTypes(TypeDefIndex index)
        {
            if (_lazyNestedTypesMap == null)
            {
                InitializeNestedTypesMap();
                Debug.Assert(_lazyNestedTypesMap != null);
            }

            if (_lazyNestedTypesMap.TryGetValue(index, out var nestedTypes))
            {
                return nestedTypes;
            }

            return Array.Empty<TypeDefIndex>();
        }

        public CustomAttributeList GetCustomAttributes(TypeDefIndex index) =>
            new CustomAttributeList(ModelHeap, HasCustomAttributeTag.CreateIndex((int) index, TableKind.TypeDef));

        public DeclSecurityAttributeList GetDeclSecurityAttributes(TypeDefIndex index) =>
            new DeclSecurityAttributeList(ModelHeap, HasDeclSecurityTag.CreateIndex((int) index, TableKind.TypeDef));

        public long GetRowOffset(TypeDefIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public TypeDefRow this[TypeDefIndex index] => GetRowSafe((int) index);

        protected override TypeDefRow GetRow(int index) => new TypeDefRow((TypeDefIndex) index, this);
    }
}
