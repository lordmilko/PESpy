using System;
using PESpy.Ecma335;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Encapsulates all data in the compressed model heap (#~).<para/>
    /// Neither this type, or any types referenced from this type, have a native equivalent.
    /// </summary>
    public class CompressedModelHeap : IValue, IViewable
    {
        public CompressedModelHeader Header { get; }

        #region ECMA-335

        public Table<ModuleRow>? ModuleTable { get; }

        public Table<TypeRefRow>? TypeRefTable { get; }

        public Table<TypeDefRow>? TypeDefTable { get; }

        public Table<FieldPtrRow>? FieldPtrTable { get; }

        public Table<FieldRow>? FieldTable { get; }

        public Table<MethodPtrRow>? MethodPtrTable { get; }

        public Table<MethodDefRow>? MethodDefTable { get; }

        public Table<ParamPtrRow>? ParamPtrTable { get; }

        public Table<ParamRow>? ParamTable { get; }

        public Table<InterfaceImplRow>? InterfaceImplTable { get; }

        public Table<MemberRefRow>? MemberRefTable { get; }

        public Table<ConstantRow>? ConstantTable { get; }

        public Table<CustomAttributeRow>? CustomAttributeTable { get; }

        public Table<FieldMarshalRow>? FieldMarshalTable { get; }

        public Table<DeclSecurityRow>? DeclSecurityTable { get; }

        public Table<ClassLayoutRow>? ClassLayoutTable { get; }

        public Table<FieldLayoutRow>? FieldLayoutTable { get; }

        public Table<StandAloneSigRow>? StandAloneSigTable { get; }

        public Table<EventMapRow>? EventMapTable { get; }

        public Table<EventPtrRow>? EventPtrTable { get; }

        public Table<EventRow>? EventTable { get; }

        public Table<PropertyMapRow>? PropertyMapTable { get; }

        public Table<PropertyPtrRow>? PropertyPtrTable { get; }

        public Table<PropertyRow>? PropertyTable { get; }

        public Table<MethodSemanticsRow>? MethodSemanticsTable { get; }

        public Table<MethodImplRow>? MethodImplTable { get; }

        public Table<ModuleRefRow>? ModuleRefTable { get; }

        public Table<TypeSpecRow>? TypeSpecTable { get; }

        public Table<ImplMapRow>? ImplMapTable { get; }

        public Table<FieldRvaRow>? FieldRvaTable { get; }

        public Table<EncLogRow>? EncLogTable { get; }

        public Table<EncMapRow>? EncMapTable { get; }

        public Table<AssemblyRow>? AssemblyTable { get; }

        public Table<AssemblyProcessorRow>? AssemblyProcessorTable { get; }

        public Table<AssemblyOSRow>? AssemblyOSTable { get; }

        public Table<AssemblyRefRow>? AssemblyRefTable { get; }

        public Table<AssemblyRefProcessorRow>? AssemblyRefProcessorTable { get; }

        public Table<AssemblyRefOSRow>? AssemblyRefOSTable { get; }

        public Table<FileRow>? FileTable { get; }

        public Table<ExportedTypeRow>? ExportedTypeTable { get; }

        public Table<ManifestResourceRow>? ManifestResourceTable { get; }

        public Table<NestedClassRow>? NestedClassTable { get; }

        public Table<GenericParamRow>? GenericParamTable { get; }

        public Table<MethodSpecRow>? MethodSpecTable { get; }

        public Table<GenericParamConstraintRow>? GenericParamConstraintTable { get; }

        #endregion
        #region Portable PDB

        public Table<DocumentRow>? DocumentTable { get; }

        public Table<MethodDebugInformationRow>? MethodDebugInformationTable { get; }

        public Table<LocalScopeRow>? LocalScopeTable { get; }

        public Table<LocalVariableRow>? LocalVariableTable { get; }

        public Table<LocalConstantRow>? LocalConstantTable { get; }

        public Table<ImportScopeRow>? ImportScopeTable { get; }

        public Table<StateMachineMethodRow>? StateMachineMethodTable { get; }

        public Table<CustomDebugInformationRow>? CustomDebugInformationTable { get; }

        #endregion

        private int Size { get; }

        public RawOffset Offset { get; }

        internal MetadataReader MetadataReader { get; }

        internal CompressedModelHeap(IFileReader reader, int size)
        {
            Offset = (RawOffset) reader.Position;

            Size = size;

            //Reader is filled by parent

            Header = new CompressedModelHeader(reader, out var rowCounts);

            var metadataReader = new MetadataReader(reader, Header.HeapSizes, rowCounts);

            #region ECMA-335

            var offset = (int) reader.Position;

            ModuleTable                 = CreateTable(ref offset, metadataReader, TableKind.Module,                 ModuleRow.GetRowSize,                 ModuleRow.New);
            TypeRefTable                = CreateTable(ref offset, metadataReader, TableKind.TypeRef,                TypeRefRow.GetRowSize,                TypeRefRow.New);
            TypeDefTable                = CreateTable(ref offset, metadataReader, TableKind.TypeDef,                TypeDefRow.GetRowSize,                TypeDefRow.New);
            FieldPtrTable               = CreateTable(ref offset, metadataReader, TableKind.FieldPtr,               FieldPtrRow.GetRowSize,               FieldPtrRow.New);
            FieldTable                  = CreateTable(ref offset, metadataReader, TableKind.Field,                  FieldRow.GetRowSize,                  FieldRow.New);
            MethodPtrTable              = CreateTable(ref offset, metadataReader, TableKind.MethodPtr,              MethodPtrRow.GetRowSize,              MethodPtrRow.New);
            MethodDefTable              = CreateTable(ref offset, metadataReader, TableKind.MethodDef,              MethodDefRow.GetRowSize,              MethodDefRow.New);
            ParamPtrTable               = CreateTable(ref offset, metadataReader, TableKind.ParamPtr,               ParamPtrRow.GetRowSize,               ParamPtrRow.New);
            ParamTable                  = CreateTable(ref offset, metadataReader, TableKind.Param,                  ParamRow.GetRowSize,                  ParamRow.New);
            InterfaceImplTable          = CreateTable(ref offset, metadataReader, TableKind.InterfaceImpl,          InterfaceImplRow.GetRowSize,          InterfaceImplRow.New);
            MemberRefTable              = CreateTable(ref offset, metadataReader, TableKind.MemberRef,              MemberRefRow.GetRowSize,              MemberRefRow.New);
            ConstantTable               = CreateTable(ref offset, metadataReader, TableKind.Constant,               ConstantRow.GetRowSize,               ConstantRow.New);
            CustomAttributeTable        = CreateTable(ref offset, metadataReader, TableKind.CustomAttribute,        CustomAttributeRow.GetRowSize,        CustomAttributeRow.New);
            FieldMarshalTable           = CreateTable(ref offset, metadataReader, TableKind.FieldMarshal,           FieldMarshalRow.GetRowSize,           FieldMarshalRow.New);
            DeclSecurityTable           = CreateTable(ref offset, metadataReader, TableKind.DeclSecurity,           DeclSecurityRow.GetRowSize,           DeclSecurityRow.New);
            ClassLayoutTable            = CreateTable(ref offset, metadataReader, TableKind.ClassLayout,            ClassLayoutRow.GetRowSize,            ClassLayoutRow.New);
            FieldLayoutTable            = CreateTable(ref offset, metadataReader, TableKind.FieldLayout,            FieldLayoutRow.GetRowSize,            FieldLayoutRow.New);
            StandAloneSigTable          = CreateTable(ref offset, metadataReader, TableKind.StandAloneSig,          StandAloneSigRow.GetRowSize,          StandAloneSigRow.New);
            EventMapTable               = CreateTable(ref offset, metadataReader, TableKind.EventMap,               EventMapRow.GetRowSize,               EventMapRow.New);
            EventPtrTable               = CreateTable(ref offset, metadataReader, TableKind.EventPtr,               EventPtrRow.GetRowSize,               EventPtrRow.New);
            EventTable                  = CreateTable(ref offset, metadataReader, TableKind.Event,                  EventRow.GetRowSize,                  EventRow.New);
            PropertyMapTable            = CreateTable(ref offset, metadataReader, TableKind.PropertyMap,            PropertyMapRow.GetRowSize,            PropertyMapRow.New);
            PropertyPtrTable            = CreateTable(ref offset, metadataReader, TableKind.PropertyPtr,            PropertyPtrRow.GetRowSize,            PropertyPtrRow.New);
            PropertyTable               = CreateTable(ref offset, metadataReader, TableKind.Property,               PropertyRow.GetRowSize,               PropertyRow.New);
            MethodSemanticsTable        = CreateTable(ref offset, metadataReader, TableKind.MethodSemantics,        MethodSemanticsRow.GetRowSize,        MethodSemanticsRow.New);
            MethodImplTable             = CreateTable(ref offset, metadataReader, TableKind.MethodImpl,             MethodImplRow.GetRowSize,             MethodImplRow.New);
            ModuleRefTable              = CreateTable(ref offset, metadataReader, TableKind.ModuleRef,              ModuleRefRow.GetRowSize,              ModuleRefRow.New);
            TypeSpecTable               = CreateTable(ref offset, metadataReader, TableKind.TypeSpec,               TypeSpecRow.GetRowSize,               TypeSpecRow.New);
            ImplMapTable                = CreateTable(ref offset, metadataReader, TableKind.ImplMap,                ImplMapRow.GetRowSize,                ImplMapRow.New);
            FieldRvaTable               = CreateTable(ref offset, metadataReader, TableKind.FieldRva,               FieldRvaRow.GetRowSize,               FieldRvaRow.New);
            EncLogTable                 = CreateTable(ref offset, metadataReader, TableKind.EncLog,                 EncLogRow.GetRowSize,                 EncLogRow.New);
            EncMapTable                 = CreateTable(ref offset, metadataReader, TableKind.EncMap,                 EncMapRow.GetRowSize,                 EncMapRow.New);
            AssemblyTable               = CreateTable(ref offset, metadataReader, TableKind.Assembly,               AssemblyRow.GetRowSize,               AssemblyRow.New);
            AssemblyProcessorTable      = CreateTable(ref offset, metadataReader, TableKind.AssemblyProcessor,      AssemblyProcessorRow.GetRowSize,      AssemblyProcessorRow.New);
            AssemblyOSTable             = CreateTable(ref offset, metadataReader, TableKind.AssemblyOS,             AssemblyOSRow.GetRowSize,             AssemblyOSRow.New);
            AssemblyRefTable            = CreateTable(ref offset, metadataReader, TableKind.AssemblyRef,            AssemblyRefRow.GetRowSize,            AssemblyRefRow.New);
            AssemblyRefProcessorTable   = CreateTable(ref offset, metadataReader, TableKind.AssemblyRefProcessor,   AssemblyRefProcessorRow.GetRowSize,   AssemblyRefProcessorRow.New);
            AssemblyRefOSTable          = CreateTable(ref offset, metadataReader, TableKind.AssemblyRefOS,          AssemblyRefOSRow.GetRowSize,          AssemblyRefOSRow.New);
            FileTable                   = CreateTable(ref offset, metadataReader, TableKind.File,                   FileRow.GetRowSize,                   FileRow.New);
            ExportedTypeTable           = CreateTable(ref offset, metadataReader, TableKind.ExportedType,           ExportedTypeRow.GetRowSize,           ExportedTypeRow.New);
            ManifestResourceTable       = CreateTable(ref offset, metadataReader, TableKind.ManifestResource,       ManifestResourceRow.GetRowSize,       ManifestResourceRow.New);
            NestedClassTable            = CreateTable(ref offset, metadataReader, TableKind.NestedClass,            NestedClassRow.GetRowSize,            NestedClassRow.New);
            GenericParamTable           = CreateTable(ref offset, metadataReader, TableKind.GenericParam,           GenericParamRow.GetRowSize,           GenericParamRow.New);
            MethodSpecTable             = CreateTable(ref offset, metadataReader, TableKind.MethodSpec,             MethodSpecRow.GetRowSize,             MethodSpecRow.New);
            GenericParamConstraintTable = CreateTable(ref offset, metadataReader, TableKind.GenericParamConstraint, GenericParamConstraintRow.GetRowSize, GenericParamConstraintRow.New);

            #endregion

            //Portable PDB Tables
            //https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md
            DocumentTable               = CreateTable(ref offset, metadataReader, TableKind.Document,               DocumentRow.GetRowSize,               DocumentRow.New);
            MethodDebugInformationTable = CreateTable(ref offset, metadataReader, TableKind.MethodDebugInformation, MethodDebugInformationRow.GetRowSize, MethodDebugInformationRow.New);
            LocalScopeTable             = CreateTable(ref offset, metadataReader, TableKind.LocalScope,             LocalScopeRow.GetRowSize,             LocalScopeRow.New);
            LocalVariableTable          = CreateTable(ref offset, metadataReader, TableKind.LocalVariable,          LocalVariableRow.GetRowSize,          LocalVariableRow.New);
            LocalConstantTable          = CreateTable(ref offset, metadataReader, TableKind.LocalConstant,          LocalConstantRow.GetRowSize,          LocalConstantRow.New);
            ImportScopeTable            = CreateTable(ref offset, metadataReader, TableKind.ImportScope,            ImportScopeRow.GetRowSize,            ImportScopeRow.New);
            StateMachineMethodTable     = CreateTable(ref offset, metadataReader, TableKind.StateMachineMethod,     StateMachineMethodRow.GetRowSize,     StateMachineMethodRow.New);
            CustomDebugInformationTable = CreateTable(ref offset, metadataReader, TableKind.CustomDebugInformation, CustomDebugInformationRow.GetRowSize, CustomDebugInformationRow.New);

            MetadataReader = metadataReader;
        }

        private static Table<T>? CreateTable<T>(ref int offset, MetadataReader metadataReader, TableKind tableKind, Func<MetadataReader, int> getRowSize, Func<MetadataReader, T> createRow)
        {
            var numRows = metadataReader.GetRowCount(tableKind);

            if (numRows == 0)
                return null;

            var rowSize = getRowSize(metadataReader);

            var table = new Table<T>(offset, metadataReader, numRows, rowSize, createRow);

            offset += numRows * rowSize;

            return table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            static void WriteTable<T>(ViewWriter writer, string tableName, Table<T>? table) where T : IValue, IViewable
            {
                if (table != null && table.Count > 0)
                {
                    var first = table[0];
                    using var r = writer.CreateRegion(first.Offset, tableName, ViewKind.MetadataTable);

                    r.WriteValue(first);

                    for (var i = 1; i < table.Count; i++)
                        r.WriteValue(table[i]);
                }
            }

            writer.WriteGlobal(Header);

            WriteTable(writer, "Modules", ModuleTable);
            WriteTable(writer, "TypeRefs", TypeRefTable);
            WriteTable(writer, "TypeDefs", TypeDefTable);
            WriteTable(writer, "FieldPtrs", FieldPtrTable);
            WriteTable(writer, "Fields", FieldTable);
            WriteTable(writer, "MethodPtrs", MethodPtrTable);
            WriteTable(writer, "MethodDefs", MethodDefTable);
            WriteTable(writer, "ParamPtrs", ParamPtrTable);
            WriteTable(writer, "Params", ParamTable);
            WriteTable(writer, "InterfaceImpls", InterfaceImplTable);
            WriteTable(writer, "MemberRefs", MemberRefTable);
            WriteTable(writer, "Constants", ConstantTable);
            WriteTable(writer, "CustomAttributes", CustomAttributeTable);
            WriteTable(writer, "FieldMarshals", FieldMarshalTable);
            WriteTable(writer, "DeclSecuritys", DeclSecurityTable);
            WriteTable(writer, "ClassLayouts", ClassLayoutTable);
            WriteTable(writer, "FieldLayouts", FieldLayoutTable);
            WriteTable(writer, "StandAloneSigs", StandAloneSigTable);
            WriteTable(writer, "EventMaps", EventMapTable);
            WriteTable(writer, "EventPtrs", EventPtrTable);
            WriteTable(writer, "Events", EventTable);
            WriteTable(writer, "PropertyMaps", PropertyMapTable);
            WriteTable(writer, "PropertyPtrs", PropertyPtrTable);
            WriteTable(writer, "Propertys", PropertyTable);
            WriteTable(writer, "MethodSemanticss", MethodSemanticsTable);
            WriteTable(writer, "MethodImpls", MethodImplTable);
            WriteTable(writer, "ModuleRefs", ModuleRefTable);
            WriteTable(writer, "TypeSpecs", TypeSpecTable);
            WriteTable(writer, "ImplMaps", ImplMapTable);
            WriteTable(writer, "FieldRvas", FieldRvaTable);
            WriteTable(writer, "EncLogs", EncLogTable);
            WriteTable(writer, "EncMaps", EncMapTable);
            WriteTable(writer, "Assemblys", AssemblyTable);
            WriteTable(writer, "AssemblyProcessors", AssemblyProcessorTable);
            WriteTable(writer, "AssemblyOSs", AssemblyOSTable);
            WriteTable(writer, "AssemblyRefs", AssemblyRefTable);
            WriteTable(writer, "AssemblyRefProcessors", AssemblyRefProcessorTable);
            WriteTable(writer, "AssemblyRefOSs", AssemblyRefOSTable);
            WriteTable(writer, "Files", FileTable);
            WriteTable(writer, "ExportedTypes", ExportedTypeTable);
            WriteTable(writer, "ManifestResources", ManifestResourceTable);
            WriteTable(writer, "NestedClasss", NestedClassTable);
            WriteTable(writer, "GenericParams", GenericParamTable);
            WriteTable(writer, "MethodSpecs", MethodSpecTable);
            WriteTable(writer, "GenericParamConstraints", GenericParamConstraintTable);

            //Portable PDB Tables
            WriteTable(writer, "Documents", DocumentTable);
            WriteTable(writer, "MethodDebugInformation", MethodDebugInformationTable);
            WriteTable(writer, "LocalScopes", LocalScopeTable);
            WriteTable(writer, "LocalVariables", LocalVariableTable);
            WriteTable(writer, "LocalConstants", LocalConstantTable);
            WriteTable(writer, "ImportScopes", ImportScopeTable);
            WriteTable(writer, "StateMachineMethods", StateMachineMethodTable);
            WriteTable(writer, "CustomDebugInformation", CustomDebugInformationTable);
        }
    }
}
