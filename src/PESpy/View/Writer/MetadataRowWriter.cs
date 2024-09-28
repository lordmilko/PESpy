using System;
using System.Collections.Generic;
using PESpy.Ecma335;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.View
{
    public partial class ViewWriter
    {
        internal ref struct MetadataRowWriter
        {
            private string structName;
            private ViewKind kind;
            private RawOffset startOffset;
            private RawOffset currentOffset;
            private PEViewWriter viewWriter;
            private List<IView> fields;
            private bool shouldAdd;

            public RawOffset Size => currentOffset - startOffset;

            internal MetadataRowWriter(string name, RawOffset startOffset, ViewKind kind, PEViewWriter viewWriter, bool shouldAdd)
            {
                structName = name;
                this.startOffset = startOffset;
                this.kind = kind;
                currentOffset = startOffset;
                this.viewWriter = viewWriter;
                fields = viewWriter.RentList();
                this.shouldAdd = shouldAdd;
            }

            internal void WriteValue(string name, byte value) =>
                WriteFieldInternal(name, value, sizeof(byte));

            internal void WriteValue(string name, short value) =>
                WriteFieldInternal(name, value, sizeof(short));

            internal void WriteValue(string name, ushort value) =>
                WriteFieldInternal(name, value, sizeof(short));

            internal void WriteValue(string name, int value) =>
                WriteFieldInternal(name, value, sizeof(int));

            internal void WriteValue(string name, uint value) =>
                WriteFieldInternal(name, value, sizeof(int));

            internal void WriteValue<T>(string name, T value, int size) where T : Enum =>
                WriteFieldInternal(name, value, size);

            #region Heap

            internal void WriteStringHeapIndex(string name, int index)
            {
                if (viewWriter.MetadataReader.StringIndexSize == 4)
                    WriteValue(name, index);
                else
                    WriteValue(name, (ushort) index);
            }

            internal void WriteBlobHeapIndex(string name, int index)
            {
                if (viewWriter.MetadataReader.BlobIndexSize == 4)
                    WriteValue(name, index);
                else
                    WriteValue(name, (ushort) index);
            }

            internal void WriteGuidHeapIndex(string name, int index)
            {
                if (viewWriter.MetadataReader.GuidIndexSize == 4)
                    WriteValue(name, index);
                else
                    WriteValue(name, (ushort) index);
            }

            #endregion
            #region Coded

            internal void WriteTypeDefOrRefIndex(string name, int value) =>
                WriteIndex(name, value, viewWriter.MetadataReader.TypeDefOrRefSize);

            internal void WriteHasConstantIndex(string name, int value) =>
                WriteIndex(name, value, viewWriter.MetadataReader.HasConstantSize);

            internal void WriteHasCustomAttributeIndex(string name, int value) =>
                WriteIndex(name, value, viewWriter.MetadataReader.HasCustomAttributeSize);

            internal void WriteHasFieldMarshalIndex(string name, int value) =>
                WriteIndex(name, value, viewWriter.MetadataReader.HasFieldMarshalSize);

            internal void WriteHasDeclSecurityIndex(string name, int value) =>
                WriteIndex(name, value, viewWriter.MetadataReader.HasDeclSecuritySize);

            internal void WriteMemberRefParentIndex(string name, int value) =>
                WriteIndex(name, value, viewWriter.MetadataReader.MemberRefParentSize);

            internal void WriteHasSemanticsIndex(string name, int value) =>
                WriteIndex(name, value, viewWriter.MetadataReader.HasSemanticsSize);

            internal void WriteMethodDefOrRefIndex(string name, int value) =>
                WriteIndex(name, value, viewWriter.MetadataReader.MethodDefOrRefSize);

            internal void WriteMemberForwardedIndex(string name, int value) =>
                WriteIndex(name, value, viewWriter.MetadataReader.MemberForwardedSize);

            internal void WriteImplementationIndex(string name, int value) =>
                WriteIndex(name, value, viewWriter.MetadataReader.ImplementationSize);

            internal void WriteCustomAttributeTypeIndex(string name, int value) =>
                WriteIndex(name, value, viewWriter.MetadataReader.CustomAttributeTypeSize);

            internal void WriteResolutionScopeIndex(string name, int value) =>
                WriteIndex(name, value, viewWriter.MetadataReader.ResolutionScopeSize);

            internal void WriteTypeOrMethodDefIndex(string name, int value) =>
                WriteIndex(name, value, viewWriter.MetadataReader.TypeOrMethodDefSize);

            //Portable PDB

            internal void WriteHasCustomDebugInformationIndex(string name, int value) =>
                WriteIndex(name, value, viewWriter.MetadataReader.HasCustomDebugInformationSize);

            private void WriteIndex(string name, int index, int indexSize)
            {
                if (indexSize == 4)
                    WriteValue(name, index);
                else
                    WriteValue(name, (ushort) index);
            }

            #endregion

            internal void WriteSimpleIndex(string name, int value, TableKind kind)
            {
                var size = viewWriter.MetadataReader.GetSimpleIndexSize(kind);

                WriteIndex(name, value, size);
            }

            private void WriteFieldInternal<T>(string name, T value, int size)
            {
                //We will be writing a lot of primative values (Int16's, Int32's, etc). We do not want each value to be boxed,
                //as that will cause a large number of (duplicated) allocations. The CLR does not know that the number "2" has been
                //boxed before, so you'll have a lot of wasted memory for boxes storing the same value.

                fields.Add(new FieldView<T>(currentOffset, name, value, size));
                currentOffset += size;
            }

            public void Dispose()
            {
                if (shouldAdd)
                {
                    var structView = new StructView(startOffset, structName, fields.ToArray(), Size, kind);

                    viewWriter.AddView(structView);    
                }
                
                viewWriter.ReturnList(fields);
            }
        }
    }
}
