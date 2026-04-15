using System;
using System.Reflection;
using ClrDebug;
using PESpy.View;
using AssemblyHashAlgorithm = System.Configuration.Assemblies.AssemblyHashAlgorithm;

namespace PESpy.Ecma335
{
    /// <summary>
    /// Encapsulates all data in the compressed model heap (#~).<para/>
    /// Neither this type, or any types referenced from this type, have a native equivalent.
    /// </summary>
    public class CompressedModelHeap : IValue, IViewable
    {
        internal const int EnumEnded = 1 << 24;

        public CompressedModelHeader Header { get; }

        #region ECMA-335

        public ModuleTable? ModuleTable { get; }

        public TypeRefTable? TypeRefTable { get; }

        public TypeDefTable? TypeDefTable { get; }

        public FieldPtrTable? FieldPtrTable { get; }

        public FieldTable? FieldTable { get; }

        public MethodPtrTable? MethodPtrTable { get; }

        public MethodDefTable? MethodDefTable { get; }

        public ParamPtrTable? ParamPtrTable { get; }

        public ParamTable? ParamTable { get; }

        public InterfaceImplTable? InterfaceImplTable { get; }

        public MemberRefTable? MemberRefTable { get; }

        public ConstantTable? ConstantTable { get; }

        public CustomAttributeTable? CustomAttributeTable { get; }

        public FieldMarshalTable? FieldMarshalTable { get; }

        public DeclSecurityTable? DeclSecurityTable { get; }

        public ClassLayoutTable? ClassLayoutTable { get; }

        public FieldLayoutTable? FieldLayoutTable { get; }

        public StandAloneSigTable? StandAloneSigTable { get; }

        public EventMapTable? EventMapTable { get; }

        public EventPtrTable? EventPtrTable { get; }

        public EventTable? EventTable { get; }

        public PropertyMapTable? PropertyMapTable { get; }

        public PropertyPtrTable? PropertyPtrTable { get; }

        public PropertyTable? PropertyTable { get; }

        public MethodSemanticsTable? MethodSemanticsTable { get; }

        public MethodImplTable? MethodImplTable { get; }

        public ModuleRefTable? ModuleRefTable { get; }

        public TypeSpecTable? TypeSpecTable { get; }

        public ImplMapTable? ImplMapTable { get; }

        public FieldRvaTable? FieldRvaTable { get; }

        public EncLogTable? EncLogTable { get; }

        public EncMapTable? EncMapTable { get; }

        public AssemblyTable? AssemblyTable { get; }

        public AssemblyProcessorTable? AssemblyProcessorTable { get; }

        public AssemblyOSTable? AssemblyOSTable { get; }

        public AssemblyRefTable? AssemblyRefTable { get; }

        public AssemblyRefProcessorTable? AssemblyRefProcessorTable { get; }

        public AssemblyRefOSTable? AssemblyRefOSTable { get; }

        public FileTable? FileTable { get; }

        public ExportedTypeTable? ExportedTypeTable { get; }

        public ManifestResourceTable? ManifestResourceTable { get; }

        public NestedClassTable? NestedClassTable { get; }

        public GenericParamTable? GenericParamTable { get; }

        public MethodSpecTable? MethodSpecTable { get; }

        public GenericParamConstraintTable? GenericParamConstraintTable { get; }

        #endregion
        #region Portable PDB

        public DocumentTable? DocumentTable { get; }

        public MethodDebugInformationTable? MethodDebugInformationTable { get; }

        public LocalScopeTable? LocalScopeTable { get; }

        public LocalVariableTable? LocalVariableTable { get; }

        public LocalConstantTable? LocalConstantTable { get; }

        public ImportScopeTable? ImportScopeTable { get; }

        public StateMachineMethodTable? StateMachineMethodTable { get; }

        public CustomDebugInformationTable? CustomDebugInformationTable { get; }

        #endregion

        public int Size { get; }

        public int Offset => chunk.AbsoluteOffset;

        private readonly MetadataSizes sizes;

        public MetadataSizes Sizes => sizes;

        internal IFile File() => chunk.File();

        private readonly MemoryChunk chunk;

        internal CompressedModelHeap(in MemoryChunk chunk, int size)
        {
            this.chunk = chunk;
            Size = size;

            Header = new CompressedModelHeader(chunk, out var rowCounts);

            var offset = CompressedModelHeader.FixedStructSize + (Header.RowCounts.Length * 4);

            var peFile = chunk.PEFile();

            EcmaMetadata ecmaMetadata;

            if (peFile != null!)
                ecmaMetadata = peFile.EcmaMetadata!;
            else
            {
                //It's a Portable PDB
                ecmaMetadata = chunk.PortablePDBFile().EcmaMetadata;
            }

            var isMinimalDelta = false;

            var streamHeaders = ecmaMetadata.Header.StreamHeaders;

            for (var i = 0; i < streamHeaders.Length; i++)
            {
                ref var header = ref streamHeaders[i];

                //If we have a #JTD stream, metadata references are always 4 bytes
                if (header.Name == "#JTD")
                    isMinimalDelta = true;
            }

            //When constructing the CompressedModelHeap, the other heaps may not have been constructed yet, so these need to be lazily evaluated
            Func<StringHeap?> stringHeap = () => ecmaMetadata.StringHeap;
            Func<BlobHeap?> blobHeap = () => ecmaMetadata.BlobHeap;
            Func<GuidHeap?> guidHeap = () => ecmaMetadata.GuidHeap;

            var sizes = new MetadataSizes(Header.HeapSizes, isMinimalDelta, rowCounts);
            this.sizes = sizes;
            var stringIndexSize = sizes.StringIndexSize;

            int numRows;

            #region ECMA-335
            #region ModuleTable

            if ((numRows = rowCounts[(int) TableKind.Module]) > 0)
            {
                ModuleTable = new ModuleTable(
                    numRows,
                    stringIndexSize,
                    sizes.GuidIndexSize,
                    this,
                    stringHeap,
                    guidHeap,
                    chunk.Slice(offset)
                );
                offset += numRows * ModuleTable.RowSize;
            }

            #endregion
            #region TypeRefTable

            if ((numRows = rowCounts[(int) TableKind.TypeRef]) > 0)
            {
                TypeRefTable = new TypeRefTable(
                    numRows,
                    sizes.ResolutionScopeSize,
                    stringIndexSize,
                    this,
                    stringHeap,
                    chunk.Slice(offset)
                );
                offset += numRows * TypeRefTable.RowSize;
            }

            #endregion
            #region TypeDefTable

            if ((numRows = rowCounts[(int) TableKind.TypeDef]) > 0)
            {
                TypeDefTable = new TypeDefTable(
                    numRows,
                    stringIndexSize,
                    sizes.TypeDefOrRefSize,
                    sizes.GetSimpleIndexSize(TableKind.Field),
                    sizes.GetSimpleIndexSize(TableKind.MethodDef),
                    this,
                    stringHeap,
                    chunk.Slice(offset)
                );
                offset += numRows * TypeDefTable.RowSize;
            }

            #endregion
            #region FieldPtrTable

            if ((numRows = rowCounts[(int) TableKind.FieldPtr]) > 0)
            {
                FieldPtrTable = new FieldPtrTable(
                    numRows,
                    sizes.GetSimpleIndexSize(TableKind.Field),
                    this,
                    chunk.Slice(offset)
                );
                offset += numRows * FieldPtrTable.RowSize;
            }

            #endregion
            #region FieldTable

            if ((numRows = rowCounts[(int) TableKind.Field]) > 0)
            {
                FieldTable = new FieldTable(
                    numRows,
                    stringIndexSize,
                    sizes.BlobIndexSize,
                    this,
                    stringHeap,
                    blobHeap,
                    chunk.Slice(offset)
                );
                offset += numRows * FieldTable.RowSize;
            }

            #endregion
            #region MethodPtrTable

            if ((numRows = rowCounts[(int) TableKind.MethodPtr]) > 0)
            {
                MethodPtrTable = new MethodPtrTable(
                    numRows,
                    sizes.GetSimpleIndexSize(TableKind.MethodDef),
                    this,
                    chunk.Slice(offset)
                );
                offset += numRows * MethodPtrTable.RowSize;
            }

            #endregion
            #region MethodDefTable

            if ((numRows = rowCounts[(int) TableKind.MethodDef]) > 0)
            {
                MethodDefTable = new MethodDefTable(
                    numRows,
                    stringIndexSize,
                    sizes.BlobIndexSize,
                    sizes.GetSimpleIndexSize(TableKind.Param),
                    this,
                    stringHeap,
                    blobHeap,
                    chunk.Slice(offset)
                );
                offset += numRows * MethodDefTable.RowSize;
            }

            #endregion
            #region ParamPtrTable

            if ((numRows = rowCounts[(int) TableKind.ParamPtr]) > 0)
            {
                ParamPtrTable = new ParamPtrTable(
                    numRows,
                    sizes.GetSimpleIndexSize(TableKind.Param),
                    this,
                    chunk.Slice(offset)
                );
                offset += numRows * ParamPtrTable.RowSize;
            }

            #endregion
            #region ParamTable

            if ((numRows = rowCounts[(int) TableKind.Param]) > 0)
            {
                ParamTable = new ParamTable(
                    numRows,
                    stringIndexSize,
                    this,
                    stringHeap,
                    chunk.Slice(offset)
                );
                offset += numRows * ParamTable.RowSize;
            }

            #endregion
            #region InterfaceImplTable

            if ((numRows = rowCounts[(int) TableKind.InterfaceImpl]) > 0)
            {
                InterfaceImplTable = new InterfaceImplTable(
                    numRows,
                    sizes.GetSimpleIndexSize(TableKind.TypeDef),
                    sizes.TypeDefOrRefSize,
                    this,
                    chunk.Slice(offset)
                );
                offset += numRows * InterfaceImplTable.RowSize;
            }

            #endregion
            #region MemberRefTable

            if ((numRows = rowCounts[(int) TableKind.MemberRef]) > 0)
            {
                MemberRefTable = new MemberRefTable(
                    numRows,
                    sizes.MemberRefParentSize,
                    stringIndexSize,
                    sizes.BlobIndexSize,
                    this,
                    stringHeap,
                    blobHeap,
                    chunk.Slice(offset)
                );
                offset += numRows * MemberRefTable.RowSize;
            }

            #endregion
            #region ConstantTable

            if ((numRows = rowCounts[(int) TableKind.Constant]) > 0)
            {
                ConstantTable = new ConstantTable(
                    numRows,
                    sizes.HasConstantSize,
                    sizes.BlobIndexSize,
                    this,
                    blobHeap,
                    chunk.Slice(offset)
                );
                offset += numRows * ConstantTable.RowSize;
            }

            #endregion
            #region CustomAttributeTable

            if ((numRows = rowCounts[(int) TableKind.CustomAttribute]) > 0)
            {
                CustomAttributeTable = new CustomAttributeTable(
                    numRows,
                    (Header.Sorted & TableMask.CustomAttribute) != 0,
                    sizes.HasCustomAttributeSize,
                    sizes.CustomAttributeTypeSize,
                    sizes.BlobIndexSize,
                    this,
                    blobHeap,
                    chunk.Slice(offset)
                );
                offset += numRows * CustomAttributeTable.RowSize;
            }

            #endregion
            #region FieldMarshalTable

            if ((numRows = rowCounts[(int) TableKind.FieldMarshal]) > 0)
            {
                FieldMarshalTable = new FieldMarshalTable(
                    numRows,
                    sizes.HasFieldMarshalSize,
                    sizes.BlobIndexSize,
                    this,
                    blobHeap,
                    chunk.Slice(offset)
                );
                offset += numRows * FieldMarshalTable.RowSize;
            }

            #endregion
            #region DeclSecurityTable

            if ((numRows = rowCounts[(int) TableKind.DeclSecurity]) > 0)
            {
                DeclSecurityTable = new DeclSecurityTable(
                    numRows,
                    sizes.HasDeclSecuritySize,
                    sizes.BlobIndexSize,
                    this,
                    blobHeap,
                    chunk.Slice(offset)
                );
                offset += numRows * DeclSecurityTable.RowSize;
            }

            #endregion
            #region ClassLayoutTable

            if ((numRows = rowCounts[(int) TableKind.ClassLayout]) > 0)
            {
                ClassLayoutTable = new ClassLayoutTable(
                    numRows,
                    sizes.GetSimpleIndexSize(TableKind.TypeDef),
                    this,
                    chunk.Slice(offset)
                );
                offset += numRows * ClassLayoutTable.RowSize;
            }

            #endregion
            #region FieldLayoutTable

            if ((numRows = rowCounts[(int) TableKind.FieldLayout]) > 0)
            {
                FieldLayoutTable = new FieldLayoutTable(
                    numRows,
                    sizes.GetSimpleIndexSize(TableKind.Field),
                    this,
                    chunk.Slice(offset)
                );
                offset += numRows * FieldLayoutTable.RowSize;
            }

            #endregion
            #region StandAloneSigTable

            if ((numRows = rowCounts[(int) TableKind.StandAloneSig]) > 0)
            {
                StandAloneSigTable = new StandAloneSigTable(
                    numRows,
                    sizes.BlobIndexSize,
                    this,
                    blobHeap,
                    chunk.Slice(offset)
                );
                offset += numRows * StandAloneSigTable.RowSize;
            }

            #endregion
            #region EventMapTable

            if ((numRows = rowCounts[(int) TableKind.EventMap]) > 0)
            {
                EventMapTable = new EventMapTable(
                    numRows,
                    sizes.GetSimpleIndexSize(TableKind.TypeDef),
                    sizes.GetSimpleIndexSize(TableKind.Event),
                    this,
                    chunk.Slice(offset)
                );
                offset += numRows * EventMapTable.RowSize;
            }

            #endregion
            #region EventPtrTable

            if ((numRows = rowCounts[(int) TableKind.EventPtr]) > 0)
            {
                EventPtrTable = new EventPtrTable(
                    numRows,
                    sizes.GetSimpleIndexSize(TableKind.Event),
                    this,
                    chunk.Slice(offset)
                );
                offset += numRows * EventPtrTable.RowSize;
            }

            #endregion
            #region EventTable

            if ((numRows = rowCounts[(int) TableKind.Event]) > 0)
            {
                EventTable = new EventTable(
                    numRows,
                    stringIndexSize,
                    sizes.TypeDefOrRefSize,
                    this,
                    stringHeap,
                    chunk.Slice(offset)
                );
                offset += numRows * EventTable.RowSize;
            }

            #endregion
            #region PropertyMapTable

            if ((numRows = rowCounts[(int) TableKind.PropertyMap]) > 0)
            {
                PropertyMapTable = new PropertyMapTable(
                    numRows,
                    sizes.GetSimpleIndexSize(TableKind.TypeDef),
                    sizes.GetSimpleIndexSize(TableKind.Property),
                    this,
                    chunk.Slice(offset)
                );
                offset += numRows * PropertyMapTable.RowSize;
            }

            #endregion
            #region PropertyPtrTable

            if ((numRows = rowCounts[(int) TableKind.PropertyPtr]) > 0)
            {
                PropertyPtrTable = new PropertyPtrTable(
                    numRows,
                    sizes.GetSimpleIndexSize(TableKind.Property),
                    this,
                    chunk.Slice(offset)
                );
                offset += numRows * PropertyPtrTable.RowSize;
            }

            #endregion
            #region PropertyTable

            if ((numRows = rowCounts[(int) TableKind.Property]) > 0)
            {
                PropertyTable = new PropertyTable(
                    numRows,
                    stringIndexSize,
                    sizes.BlobIndexSize,
                    this,
                    stringHeap,
                    blobHeap,
                    chunk.Slice(offset)
                );
                offset += numRows * PropertyTable.RowSize;
            }

            #endregion
            #region MethodSemanticsTable

            if ((numRows = rowCounts[(int) TableKind.MethodSemantics]) > 0)
            {
                MethodSemanticsTable = new MethodSemanticsTable(
                    numRows,
                    sizes.GetSimpleIndexSize(TableKind.MethodDef),
                    sizes.HasSemanticsSize,
                    this,
                    chunk.Slice(offset)
                );
                offset += numRows * MethodSemanticsTable.RowSize;
            }

            #endregion
            #region MethodImplTable

            if ((numRows = rowCounts[(int) TableKind.MethodImpl]) > 0)
            {
                MethodImplTable = new MethodImplTable(
                    numRows,
                    sizes.GetSimpleIndexSize(TableKind.TypeDef),
                    sizes.MethodDefOrRefSize,
                    this,
                    chunk.Slice(offset)
                );
                offset += numRows * MethodImplTable.RowSize;
            }

            #endregion
            #region ModuleRefTable

            if ((numRows = rowCounts[(int) TableKind.ModuleRef]) > 0)
            {
                ModuleRefTable = new ModuleRefTable(
                    numRows,
                    stringIndexSize,
                    this,
                    stringHeap,
                    chunk.Slice(offset)
                );
                offset += numRows * ModuleRefTable.RowSize;
            }

            #endregion
            #region TypeSpecTable

            if ((numRows = rowCounts[(int) TableKind.TypeSpec]) > 0)
            {
                TypeSpecTable = new TypeSpecTable(
                    numRows,
                    sizes.BlobIndexSize,
                    this,
                    blobHeap,
                    chunk.Slice(offset)
                );
                offset += numRows * TypeSpecTable.RowSize;
            }

            #endregion
            #region ImplMapTable

            if ((numRows = rowCounts[(int) TableKind.ImplMap]) > 0)
            {
                ImplMapTable = new ImplMapTable(
                    numRows,
                    sizes.MemberForwardedSize,
                    stringIndexSize,
                    sizes.GetSimpleIndexSize(TableKind.ModuleRef),
                    this,
                    stringHeap,
                    chunk.Slice(offset)
                );
                offset += numRows * ImplMapTable.RowSize;
            }

            #endregion
            #region FieldRvaTable

            if ((numRows = rowCounts[(int) TableKind.FieldRva]) > 0)
            {
                FieldRvaTable = new FieldRvaTable(
                    numRows,
                    sizes.GetSimpleIndexSize(TableKind.Field),
                    this,
                    chunk.Slice(offset)
                );
                offset += numRows * FieldRvaTable.RowSize;
            }

            #endregion
            #region EncLogTable

            if ((numRows = rowCounts[(int) TableKind.EncLog]) > 0)
            {
                EncLogTable = new EncLogTable(
                    numRows,
                    this,
                    chunk.Slice(offset)
                );
                offset += numRows * EncLogTable.RowSize;
            }

            #endregion
            #region EncMapTable

            if ((numRows = rowCounts[(int) TableKind.EncMap]) > 0)
            {
                EncMapTable = new EncMapTable(
                    numRows,
                    this,
                    chunk.Slice(offset)
                );
                offset += numRows * EncMapTable.RowSize;
            }

            #endregion
            #region AssemblyTable

            if ((numRows = rowCounts[(int) TableKind.Assembly]) > 0)
            {
                AssemblyTable = new AssemblyTable(
                    numRows,
                    sizes.BlobIndexSize,
                    stringIndexSize,
                    this,
                    stringHeap,
                    blobHeap,
                    chunk.Slice(offset)
                );
                offset += numRows * AssemblyTable.RowSize;
            }

            #endregion
            #region AssemblyProcessorTable

            if ((numRows = rowCounts[(int) TableKind.AssemblyProcessor]) > 0)
            {
                AssemblyProcessorTable = new AssemblyProcessorTable(
                    numRows,
                    chunk.Slice(offset)
                );
                offset += numRows * AssemblyProcessorTable.RowSize;
            }

            #endregion
            #region AssemblyOSTable

            if ((numRows = rowCounts[(int) TableKind.AssemblyOS]) > 0)
            {
                AssemblyOSTable = new AssemblyOSTable(
                    numRows,
                    chunk.Slice(offset)
                );
                offset += numRows * AssemblyOSTable.RowSize;
            }

            #endregion
            #region AssemblyRefTable

            if ((numRows = rowCounts[(int) TableKind.AssemblyRef]) > 0)
            {
                AssemblyRefTable = new AssemblyRefTable(
                    numRows,
                    sizes.BlobIndexSize,
                    stringIndexSize,
                    this,
                    stringHeap,
                    blobHeap,
                    chunk.Slice(offset)
                );
                offset += numRows * AssemblyRefTable.RowSize;
            }

            #endregion
            #region AssemblyRefProcessorTable

            if ((numRows = rowCounts[(int) TableKind.AssemblyRefProcessor]) > 0)
            {
                AssemblyRefProcessorTable = new AssemblyRefProcessorTable(
                    numRows,
                    sizes.GetSimpleIndexSize(TableKind.AssemblyRef),
                    this,
                    chunk.Slice(offset)
                );
                offset += numRows * AssemblyRefProcessorTable.RowSize;
            }

            #endregion
            #region AssemblyRefOSTable

            if ((numRows = rowCounts[(int) TableKind.AssemblyRefOS]) > 0)
            {
                AssemblyRefOSTable = new AssemblyRefOSTable(
                    numRows,
                    sizes.GetSimpleIndexSize(TableKind.AssemblyRef),
                    this,
                    chunk.Slice(offset)
                );
                offset += numRows * AssemblyRefOSTable.RowSize;
            }

            #endregion
            #region FileTable

            if ((numRows = rowCounts[(int) TableKind.File]) > 0)
            {
                FileTable = new FileTable(
                    numRows,
                    stringIndexSize,
                    sizes.BlobIndexSize,
                    this,
                    stringHeap,
                    blobHeap,
                    chunk.Slice(offset)
                );
                offset += numRows * FileTable.RowSize;
            }

            #endregion
            #region ExportedTypeTable

            if ((numRows = rowCounts[(int) TableKind.ExportedType]) > 0)
            {
                ExportedTypeTable = new ExportedTypeTable(
                    numRows,
                    stringIndexSize,
                    sizes.ImplementationSize,
                    this,
                    stringHeap,
                    chunk.Slice(offset)
                );
                offset += numRows * ExportedTypeTable.RowSize;
            }

            #endregion
            #region ManifestResourceTable

            if ((numRows = rowCounts[(int) TableKind.ManifestResource]) > 0)
            {
                ManifestResourceTable = new ManifestResourceTable(
                    numRows,
                    stringIndexSize,
                    sizes.ImplementationSize,
                    this,
                    stringHeap,
                    chunk.Slice(offset)
                );
                offset += numRows * ManifestResourceTable.RowSize;
            }

            #endregion
            #region NestedClassTable

            if ((numRows = rowCounts[(int) TableKind.NestedClass]) > 0)
            {
                NestedClassTable = new NestedClassTable(
                    numRows,
                    sizes.GetSimpleIndexSize(TableKind.TypeDef),
                    this,
                    chunk.Slice(offset)
                );
                offset += numRows * NestedClassTable.RowSize;
            }

            #endregion
            #region GenericParamTable

            if ((numRows = rowCounts[(int) TableKind.GenericParam]) > 0)
            {
                GenericParamTable = new GenericParamTable(
                    numRows,
                    sizes.TypeOrMethodDefSize,
                    stringIndexSize,
                    this,
                    stringHeap,
                    chunk.Slice(offset)
                );
                offset += numRows * GenericParamTable.RowSize;
            }

            #endregion
            #region MethodSpecTable

            if ((numRows = rowCounts[(int) TableKind.MethodSpec]) > 0)
            {
                MethodSpecTable = new MethodSpecTable(
                    numRows,
                    sizes.MethodDefOrRefSize,
                    sizes.BlobIndexSize,
                    this,
                    blobHeap,
                    chunk.Slice(offset)
                );
                offset += numRows * MethodSpecTable.RowSize;
            }

            #endregion
            #region GenericParamConstraintTable

            if ((numRows = rowCounts[(int) TableKind.GenericParamConstraint]) > 0)
            {
                GenericParamConstraintTable = new GenericParamConstraintTable(
                    numRows,
                    sizes.GetSimpleIndexSize(TableKind.GenericParam),
                    sizes.TypeDefOrRefSize,
                    this,
                    chunk.Slice(offset)
                );
                offset += numRows * GenericParamConstraintTable.RowSize;
            }

            #endregion
            #endregion
            #region Portable PDB

            //https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md

            #region DocumentTable

            if ((numRows = rowCounts[(int) TableKind.Document]) > 0)
            {
                DocumentTable = new DocumentTable(
                    numRows,
                    sizes.BlobIndexSize,
                    sizes.GuidIndexSize,
                    blobHeap,
                    guidHeap,
                    chunk.Slice(offset)
                );
                offset += numRows * DocumentTable.RowSize;
            }

            #endregion
            #region MethodDebugInformationTable

            if ((numRows = rowCounts[(int) TableKind.MethodDebugInformation]) > 0)
            {
                MethodDebugInformationTable = new MethodDebugInformationTable(
                    numRows,
                    sizes.BlobIndexSize,
                    sizes.GetSimpleIndexSize(TableKind.Document),
                    this,
                    blobHeap,
                    chunk.Slice(offset)
                );
                offset += numRows * MethodDebugInformationTable.RowSize;
            }

            #endregion
            #region LocalScopeTable

            if ((numRows = rowCounts[(int) TableKind.LocalScope]) > 0)
            {
                LocalScopeTable = new LocalScopeTable(
                    numRows,
                    sizes.GetSimpleIndexSize(TableKind.MethodDebugInformation),
                    sizes.GetSimpleIndexSize(TableKind.ImportScope),
                    sizes.GetSimpleIndexSize(TableKind.LocalVariable),
                    sizes.GetSimpleIndexSize(TableKind.LocalConstant),
                    this,
                    chunk.Slice(offset)
                );
                offset += numRows * LocalScopeTable.RowSize;
            }

            #endregion
            #region LocalVariableTable

            if ((numRows = rowCounts[(int) TableKind.LocalVariable]) > 0)
            {
                LocalVariableTable = new LocalVariableTable(
                    numRows,
                    stringIndexSize,
                    stringHeap,
                    this,
                    chunk.Slice(offset)
                );
                offset += numRows * LocalVariableTable.RowSize;
            }

            #endregion
            #region LocalConstantTable

            if ((numRows = rowCounts[(int) TableKind.LocalConstant]) > 0)
            {
                LocalConstantTable = new LocalConstantTable(
                    numRows,
                    stringIndexSize,
                    sizes.BlobIndexSize,
                    stringHeap,
                    blobHeap,
                    this,
                    chunk.Slice(offset)
                );
                offset += numRows * LocalConstantTable.RowSize;
            }

            #endregion
            #region ImportScopeTable

            if ((numRows = rowCounts[(int) TableKind.ImportScope]) > 0)
            {
                ImportScopeTable = new ImportScopeTable(
                    numRows,
                    sizes.BlobIndexSize,
                    sizes.GetSimpleIndexSize(TableKind.ImportScope),
                    blobHeap,
                    chunk.Slice(offset)
                );
                offset += numRows * ImportScopeTable.RowSize;
            }

            #endregion
            #region StateMachineMethodTable

            if ((numRows = rowCounts[(int) TableKind.StateMachineMethod]) > 0)
            {
                StateMachineMethodTable = new StateMachineMethodTable(
                    numRows,
                    sizes.GetSimpleIndexSize(TableKind.MethodDebugInformation),
                    this,
                    chunk.Slice(offset)
                );
                offset += numRows * StateMachineMethodTable.RowSize;
            }

            #endregion
            #region CustomDebugInformationTable

            if ((numRows = rowCounts[(int) TableKind.CustomDebugInformation]) > 0)
            {
                CustomDebugInformationTable = new CustomDebugInformationTable(
                    numRows,
                    sizes.HasCustomDebugInformationSize,
                    sizes.GuidIndexSize,
                    sizes.BlobIndexSize,
                    this,
                    blobHeap,
                    guidHeap,
                    chunk.Slice(offset)
                );
                offset += numRows * CustomDebugInformationTable.RowSize;
            }

            #endregion
            #endregion
        }

        //internal int BinarySearch(string[] asciiKeys, int offset)
        //BinarySearchForSlot
        //BinarySearchReference(int rowCount)
        //BinarySearchReference(int[] ptrTable)
        //BinarySearchReferenceRange(int rowCount)
        //BinarySearchReferenceRange(int[] ptrTable)

        //e.g. for TypeDef.FieldList, TypeDef.MethodList
        //BinarySearchForSlot
        internal static int BinarySearchEcmaIndexList(
            in MemoryChunk tableChunk,
            int rowCount,
            int rowSize,
            int fieldListOffset,
            uint targetValue,
            bool isIndexBig)
        {
            var lo = 0; //Start row number
            var hi = rowCount - 1; //End row number

            var startValue = tableChunk.PeekEcmaIndex(lo * rowSize + fieldListOffset, isIndexBig);
            var endValue = tableChunk.PeekEcmaIndex(hi * rowSize + fieldListOffset, isIndexBig);

            if (hi == 1)
            {
                if (targetValue >= endValue)
                    return hi;

                return lo;
            }

            while (hi - lo > 1)
            {
                if (targetValue <= startValue)
                    return targetValue == startValue ? lo : lo - 1;

                if (targetValue >= endValue)
                    return targetValue == endValue ? hi : hi + 1;

                var mid = (lo + hi) / 2;

                var midValue = tableChunk.PeekEcmaIndex(mid * rowSize + fieldListOffset, isIndexBig);

                if (targetValue > midValue)
                {
                    lo = mid;
                    startValue = midValue;
                }
                else if (targetValue < midValue)
                {
                    hi = mid;
                    endValue = midValue;
                }
                else
                    return mid;
            }

            return lo;
        }

        //BinarySearchReference
        internal static int BinarySearchEcmaIndex(
            in MemoryChunk tableChunk,
            int rowCount,
            int rowSize,
            int fieldOffset,
            uint targetValue,
            bool isIndexBig)
        {
            var lo = 0;
            var hi = rowCount - 1;

            while (lo <= hi)
            {
                var mid = (lo + hi) / 2;

                var value = tableChunk.PeekEcmaIndex(mid * rowSize + fieldOffset, isIndexBig);

                if (targetValue > value)
                    lo = mid + 1;
                else if (targetValue < value)
                    hi = mid - 1;
                else
                    return mid;
            }

            return -1;
        }

        //BinarySearchReferenceRange
        internal static void BinarySearchEcmaIndexRange(
            in MemoryChunk tableChunk,
            int rowCount,
            int rowSize,
            int fieldOffset,
            uint targetValue,
            bool isIndexBig,
            out int startRowNumber,
            out int endRowNumber)
        {
            var rowNumber = BinarySearchEcmaIndex(
                tableChunk,
                rowCount,
                rowSize,
                fieldOffset,
                targetValue,
                isIndexBig
            );

            if (rowNumber == -1)
            {
                startRowNumber = -1;
                endRowNumber = -1;
                return;
            }

            //We've found a random location that matches our target value.
            //Trace backwards and forwards to find the range of items where our
            //target value is found

            startRowNumber = rowNumber;

            while (startRowNumber >= 0 && tableChunk.PeekEcmaIndex((startRowNumber - 1) * rowSize + fieldOffset, isIndexBig) == targetValue)
                startRowNumber--;

            endRowNumber = rowNumber;

            while (endRowNumber + 1 < rowCount && tableChunk.PeekEcmaIndex((endRowNumber + 1) * rowSize + fieldOffset, isIndexBig) == targetValue)
                endRowNumber++;
        }

        //LinearSearchReference
        internal static int LinearSearchEcmaIndex(
            in MemoryChunk tableChunk,
            int rowCount,
            int rowSize,
            int targetOffset,
            uint targetValue,
            bool isIndexBig)
        {
            var currentOffset = targetOffset;

            //System.Reflection.Metadata has a bunch of "memory blocks" and evidently
            //the size of each block may be the size of a table? Not sure, but that doesn't
            //work in our world, so we'll just compute the end of the table
            var totalSize = rowSize * rowCount;

            while (currentOffset < totalSize)
            {
                var item = tableChunk.PeekEcmaIndex(currentOffset, isIndexBig);

                if (item == targetValue)
                    return currentOffset / rowSize;

                currentOffset += rowSize;
            }

            return -1;
        }

        internal TypeDefRow? GetDeclaringType(MethodDefIndex methodDefIndex)
        {
            if (MethodPtrTable?.Count > 0)
                throw new NotImplementedException("Getting the declaring type from a method pointer table is not implemented");

            return TypeDefTable.FindTypeContainingMethod(methodDefIndex.RowId, MethodDefTable.Count);
        }

        internal TypeDefRow? GetDeclaringType(FieldIndex fieldDefIndex)
        {
            if (FieldPtrTable?.Count > 0)
                throw new NotImplementedException("Getting the declaring type from a field pointer table is not implemented");

            return TypeDefTable.FindTypeContainingField(fieldDefIndex.RowId, FieldTable.Count);
        }

        internal TypeDefRow? GetDeclaringType(EventIndex eventDefIndex)
        {
            if (EventPtrTable?.Count > 0)
                throw new NotImplementedException("Getting the declaring type from an event pointer table is not implemented");

            return EventMapTable.FindTypeContainingEvent(eventDefIndex.RowId, EventTable.Count);
        }

        internal TypeDefRow? GetDeclaringType(PropertyIndex propertyDefIndex)
        {
            if (PropertyPtrTable?.Count > 0)
                throw new NotImplementedException("Getting the declaring type from a property pointer table is not implemented");

            return PropertyMapTable.FindTypeContainingProperty(propertyDefIndex.RowId, PropertyTable.Count);
        }

        internal static string FormatType(StringIndex typeNamespace, StringIndex typeName)
        {
            var ns = typeNamespace.GetString();

            if (ns.Length == 0)
                return typeName.GetString().ToString();

            using var builder = new Utf8StringBuilder();

            builder.Append(ns);
            builder.Append('.');
            builder.Append(typeName.GetString());

            return builder.ToString();
        }

        internal static AssemblyName GetAssemblyName(
            StringIndex nameIndex,
            Version version,
            StringIndex cultureIndex,
            BlobIndex publicKeyOrTokenIndex,
            AssemblyHashAlgorithm assemblyHashAlgorithm,
            CorAssemblyFlags flags)
        {
            var publicKeyOrToken = publicKeyOrTokenIndex.IsNil ? Array.Empty<byte>() : publicKeyOrTokenIndex.GetBlob().Value.ToArray();

            var contentType = (AssemblyContentType) (((int) flags & (int) CorAssemblyFlags.afContentType_Mask) >> 9);

            AssemblyNameFlags assemblyNameFlags = AssemblyNameFlags.None;

            if ((flags & CorAssemblyFlags.afPublicKey) != 0)
                assemblyNameFlags |= AssemblyNameFlags.PublicKey;

            if ((flags & CorAssemblyFlags.afRetargetable) != 0)
                assemblyNameFlags |= AssemblyNameFlags.Retargetable;

            if ((flags & CorAssemblyFlags.afEnableJITcompileTracking) != 0)
                assemblyNameFlags |= AssemblyNameFlags.EnableJITcompileTracking;

            if ((flags & CorAssemblyFlags.afDisableJITcompileOptimizer) != 0)
                assemblyNameFlags |= AssemblyNameFlags.EnableJITcompileOptimizer;

            var assemblyName = new AssemblyName
            {
                Name = nameIndex.GetString().ToString(),
                Version = version,
                CultureName = cultureIndex.IsNil ? string.Empty : cultureIndex.GetString().ToString(),
#pragma warning disable SYSLIB0037
                HashAlgorithm = assemblyHashAlgorithm,
#pragma warning restore
                Flags = assemblyNameFlags,
                ContentType = contentType
            };

            if ((flags & CorAssemblyFlags.afPublicKey) != 0)
                assemblyName.SetPublicKey(publicKeyOrToken);
            else
                assemblyName.SetPublicKeyToken(publicKeyOrToken);

            return assemblyName;
        }

        public object GetRow(mdToken token)
        {
            return token.Type switch
            {
                CorTokenType.mdtModule                 => ModuleTable.FromToken(token),
                CorTokenType.mdtTypeRef                => TypeRefTable.FromToken(token),
                CorTokenType.mdtTypeDef                => TypeDefTable.FromToken(token),
                CorTokenType.mdtFieldDef               => FieldTable.FromToken(token),
                CorTokenType.mdtMethodDef              => MethodDefTable.FromToken(token),
                CorTokenType.mdtParamDef               => ParamTable.FromToken(token),
                CorTokenType.mdtInterfaceImpl          => InterfaceImplTable.FromToken(token),
                CorTokenType.mdtMemberRef              => MemberRefTable.FromToken(token),
                CorTokenType.mdtCustomAttribute        => CustomAttributeTable.FromToken(token),
                CorTokenType.mdtPermission             => DeclSecurityTable.FromToken(token),
                CorTokenType.mdtSignature              => StandAloneSigTable.FromToken(token),
                CorTokenType.mdtEvent                  => EventTable.FromToken(token),
                CorTokenType.mdtProperty               => PropertyTable.FromToken(token),
                CorTokenType.mdtMethodImpl             => MethodImplTable.FromToken(token),
                CorTokenType.mdtModuleRef              => ModuleRefTable.FromToken(token),
                CorTokenType.mdtTypeSpec               => TypeSpecTable.FromToken(token),
                CorTokenType.mdtAssembly               => AssemblyTable.FromToken(token),
                CorTokenType.mdtAssemblyRef            => AssemblyRefTable.FromToken(token),
                CorTokenType.mdtFile                   => FileTable.FromToken(token),
                CorTokenType.mdtExportedType           => ExportedTypeTable.FromToken(token),
                CorTokenType.mdtManifestResource       => ManifestResourceTable.FromToken(token),
                CorTokenType.mdtGenericParam           => GenericParamTable.FromToken(token),
                CorTokenType.mdtMethodSpec             => MethodSpecTable.FromToken(token),
                CorTokenType.mdtGenericParamConstraint => GenericParamConstraintTable.FromToken(token),
                CorTokenType.mdtString                 => throw new NotImplementedException(),
                CorTokenType.mdtName                   => throw new NotImplementedException(),
                CorTokenType.mdtBaseType               => throw new NotImplementedException(),
            };
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteGlobal(Header);

            static void WriteTable<T>(ViewWriter writer, string tableName, Table<T>? table) where T : IValue, IViewable
            {
                if (table != null && table.Count > 0)
                {
                    var first = table[0];

                    using (var r = writer.CreateRegion(first.Offset, tableName, ViewKind.MetadataTable, false))
                    {
                        r.WriteValue(first);

                        for (var i = 1; i < table.Count; i++)
                            r.WriteValue(table[i]);
                    }
                }
            }

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

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        int IViewable.NumChildren() => throw new NotSupportedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();
    }
}
