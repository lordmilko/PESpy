using System;
using ClrDebug;
using PESpy.View;

namespace PESpy.NativeAOT
{
    public struct ModuleInfoRowV1 : IModuleInfoRow
    {
        private const int SectionIdOffset = 0;
        private const int FlagsOffset = 4;
        private const int StartOffset = 8;
        private int EndOffset => StartOffset + chunk.PointerSize;

        public ReadyToRunSectionType SectionId => (ReadyToRunSectionType) chunk.PeekUInt32(SectionIdOffset);

        public ModuleInfoFlags Flags => (ModuleInfoFlags) chunk.PeekUInt32(FlagsOffset);

        public long Start => (long) chunk.PeekPointer(StartOffset);

        public long End => (long) chunk.PeekPointer(EndOffset);

        int IModuleInfoRow.Length => (int) (End - Start);

        private VA<IValue> data;

        public VA<IValue> Data => GetData(SectionId, ref data, chunk, Start, (int) (End - Start));

        public int Offset => chunk.AbsoluteOffset;

        internal static int StructSize(bool is32Bit) =>
            sizeof(int) + //SectionId
            sizeof(int) + //Flags
            (2 * (is32Bit ? 4 : 8));

        private readonly MemoryChunk chunk;

        internal ModuleInfoRowV1(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        internal static VA<IValue> GetData(
            ReadyToRunSectionType sectionId,
            ref VA<IValue> data,
            in MemoryChunk chunk,
            long start,
            int length)
        {
            if (data.ListedAddress == 0)
            {
                var peFile = chunk.PEFile();

                var rva = (int) (start - peFile.OptionalHeader.ImageBase);

                if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
                {
                    var kind = GetViewKind(sectionId);
                    data = new VA<IValue>(start, rva, new ByteBlob(valueChunk, length, kind));
                }
                else
                    data = new VA<IValue>(start);
            }

            return data;
        }

        void IViewable.WriteGlobals(ViewWriter writer) => WriteGlobals(writer, this);

        internal static void WriteGlobals(
            ViewWriter writer,
            IModuleInfoRow moduleInfoRow)
        {
            var data = moduleInfoRow.Data;

            if (data.IsValid)
            {
                writer.WriteOffsetXRef(moduleInfoRow.Offset, 0, data.ActualOffset);

                var value = data.Value;

                if (value is IViewable v)
                    writer.WriteGlobal(v);
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        internal static ViewKind GetViewKind(ReadyToRunSectionType type)
        {
            var kind = type switch
            {
                ReadyToRunSectionType.CompilerIdentifier                      => ViewKind.ReadyToRunSection_CompilerIdentifier,
                ReadyToRunSectionType.ImportSections                          => ViewKind.ReadyToRunSection_ImportSections,
                ReadyToRunSectionType.RuntimeFunctions                        => ViewKind.ReadyToRunSection_RuntimeFunctions,
                ReadyToRunSectionType.MethodDefEntryPoints                    => ViewKind.ReadyToRunSection_MethodDefEntryPoints,
                ReadyToRunSectionType.ExceptionInfo                           => ViewKind.ReadyToRunSection_ExceptionInfo,
                ReadyToRunSectionType.DebugInfo                               => ViewKind.ReadyToRunSection_DebugInfo,
                ReadyToRunSectionType.DelayLoadMethodCallThunks               => ViewKind.ReadyToRunSection_DelayLoadMethodCallThunks,
                ReadyToRunSectionType.AvailableTypes                          => ViewKind.ReadyToRunSection_AvailableTypes,
                ReadyToRunSectionType.InstanceMethodEntryPoints               => ViewKind.ReadyToRunSection_InstanceMethodEntryPoints,
                ReadyToRunSectionType.InliningInfo                            => ViewKind.ReadyToRunSection_InliningInfo,
                ReadyToRunSectionType.ProfileDataInfo                         => ViewKind.ReadyToRunSection_ProfileDataInfo,
                ReadyToRunSectionType.ManifestMetadata                        => ViewKind.ReadyToRunSection_ManifestMetadata,
                ReadyToRunSectionType.AttributePresence                       => ViewKind.ReadyToRunSection_AttributePresence,
                ReadyToRunSectionType.InliningInfo2                           => ViewKind.ReadyToRunSection_InliningInfo2,
                ReadyToRunSectionType.ComponentAssemblies                     => ViewKind.ReadyToRunSection_ComponentAssemblies,
                ReadyToRunSectionType.OwnerCompositeExecutable                => ViewKind.ReadyToRunSection_OwnerCompositeExecutable,
                ReadyToRunSectionType.PgoInstrumentationData                  => ViewKind.ReadyToRunSection_PgoInstrumentationData,
                ReadyToRunSectionType.ManifestAssemblyMvids                   => ViewKind.ReadyToRunSection_ManifestAssemblyMvids,
                ReadyToRunSectionType.CrossModuleInlineInfo                   => ViewKind.ReadyToRunSection_CrossModuleInlineInfo,
                ReadyToRunSectionType.HotColdMap                              => ViewKind.ReadyToRunSection_HotColdMap,
                ReadyToRunSectionType.MethodIsGenericMap                      => ViewKind.ReadyToRunSection_MethodIsGenericMap,
                ReadyToRunSectionType.EnclosingTypeMap                        => ViewKind.ReadyToRunSection_EnclosingTypeMap,
                ReadyToRunSectionType.TypeGenericInfoMap                      => ViewKind.ReadyToRunSection_TypeGenericInfoMap,
                ReadyToRunSectionType.ExternalTypeMaps                        => ViewKind.ReadyToRunSection_ExternalTypeMaps,
                ReadyToRunSectionType.ProxyTypeMaps                           => ViewKind.ReadyToRunSection_ProxyTypeMaps,
                ReadyToRunSectionType.TypeMapAssemblyTargets                  => ViewKind.ReadyToRunSection_TypeMapAssemblyTargets,
                ReadyToRunSectionType.StringTable                             => ViewKind.ReadyToRunSection_StringTable,
                ReadyToRunSectionType.GCStaticRegion                          => ViewKind.ReadyToRunSection_GCStaticRegion,
                ReadyToRunSectionType.ThreadStaticRegion                      => ViewKind.ReadyToRunSection_ThreadStaticRegion,
                ReadyToRunSectionType.TypeManagerIndirection                  => ViewKind.ReadyToRunSection_TypeManagerIndirection,
                ReadyToRunSectionType.EagerCctor                              => ViewKind.ReadyToRunSection_EagerCctor,
                ReadyToRunSectionType.FrozenObjectRegion                      => ViewKind.ReadyToRunSection_FrozenObjectRegion,
                ReadyToRunSectionType.DehydratedData                          => ViewKind.ReadyToRunSection_DehydratedData,
                ReadyToRunSectionType.ThreadStaticOffsetRegion                => ViewKind.ReadyToRunSection_ThreadStaticOffsetRegion,
                ReadyToRunSectionType.ImportAddressTables                     => ViewKind.ReadyToRunSection_ImportAddressTables,
                ReadyToRunSectionType.ModuleInitializerList                   => ViewKind.ReadyToRunSection_ModuleInitializerList,
                ReadyToRunSectionType.ReadonlyBlobRegionStart                 => ViewKind.ReadyToRunSection_ReadonlyBlobRegionStart,
                ReadyToRunSectionType.TypeMap                                 => ViewKind.ReadyToRunSection_TypeMap,
                ReadyToRunSectionType.ArrayMap                                => ViewKind.ReadyToRunSection_ArrayMap,
                ReadyToRunSectionType.PointerTypeMap                          => ViewKind.ReadyToRunSection_PointerTypeMap,
                //ReadyToRunSectionType.GenericInstanceMap                    => ViewKind.ReadyToRunSection_GenericInstanceMap,
                ReadyToRunSectionType.FunctionPointerTypeMap                  => ViewKind.ReadyToRunSection_FunctionPointerTypeMap,
                //ReadyToRunSectionType.GenericParameterMap                   => ViewKind.ReadyToRunSection_GenericParameterMap,
                ReadyToRunSectionType.BlockReflectionTypeMap                  => ViewKind.ReadyToRunSection_BlockReflectionTypeMap,
                ReadyToRunSectionType.InvokeMap                               => ViewKind.ReadyToRunSection_InvokeMap,
                ReadyToRunSectionType.VirtualInvokeMap                        => ViewKind.ReadyToRunSection_VirtualInvokeMap,
                ReadyToRunSectionType.CommonFixupsTable                       => ViewKind.ReadyToRunSection_CommonFixupsTable,
                ReadyToRunSectionType.FieldAccessMap                          => ViewKind.ReadyToRunSection_FieldAccessMap,
                ReadyToRunSectionType.CCtorContextMap                         => ViewKind.ReadyToRunSection_CCtorContextMap,
                ReadyToRunSectionType.ByRefTypeMap                            => ViewKind.ReadyToRunSection_ByRefTypeMap,
                //ReadyToRunSectionType.DiagGenericInstanceMap                => ViewKind.ReadyToRunSection_DiagGenericInstanceMap,
                ReadyToRunSectionType.DiagGenericParameterMap                 => ViewKind.ReadyToRunSection_DiagGenericParameterMap,
                ReadyToRunSectionType.EmbeddedMetadata                        => ViewKind.ReadyToRunSection_EmbeddedMetadata,
                ReadyToRunSectionType.DefaultConstructorMap                   => ViewKind.ReadyToRunSection_DefaultConstructorMap,
                ReadyToRunSectionType.UnboxingAndInstantiatingStubMap         => ViewKind.ReadyToRunSection_UnboxingAndInstantiatingStubMap,
                ReadyToRunSectionType.StructMarshallingStubMap                => ViewKind.ReadyToRunSection_StructMarshallingStubMap,
                ReadyToRunSectionType.DelegateMarshallingStubMap              => ViewKind.ReadyToRunSection_DelegateMarshallingStubMap,
                ReadyToRunSectionType.GenericVirtualMethodTable               => ViewKind.ReadyToRunSection_GenericVirtualMethodTable,
                ReadyToRunSectionType.InterfaceGenericVirtualMethodTable      => ViewKind.ReadyToRunSection_InterfaceGenericVirtualMethodTable,
                ReadyToRunSectionType.TypeTemplateMap                         => ViewKind.ReadyToRunSection_TypeTemplateMap,
                ReadyToRunSectionType.GenericMethodsTemplateMap               => ViewKind.ReadyToRunSection_GenericMethodsTemplateMap,
                ReadyToRunSectionType.DynamicInvokeTemplateData               => ViewKind.ReadyToRunSection_DynamicInvokeTemplateData,
                ReadyToRunSectionType.BlobIdResourceIndex                     => ViewKind.ReadyToRunSection_BlobIdResourceIndex,
                ReadyToRunSectionType.BlobIdResourceData                      => ViewKind.ReadyToRunSection_BlobIdResourceData,
                ReadyToRunSectionType.BlobIdStackTraceEmbeddedMetadata        => ViewKind.ReadyToRunSection_BlobIdStackTraceEmbeddedMetadata,
                ReadyToRunSectionType.BlobIdStackTraceMethodRvaToTokenMapping => ViewKind.ReadyToRunSection_BlobIdStackTraceMethodRvaToTokenMapping,
                ReadyToRunSectionType.BlobIdStackTraceLineNumbers             => ViewKind.ReadyToRunSection_BlobIdStackTraceLineNumbers,
                ReadyToRunSectionType.BlobIdStackTraceDocuments               => ViewKind.ReadyToRunSection_BlobIdStackTraceDocuments,
                ReadyToRunSectionType.NativeLayoutInfo                        => ViewKind.ReadyToRunSection_NativeLayoutInfo,
                ReadyToRunSectionType.NativeReferences                        => ViewKind.ReadyToRunSection_NativeReferences,
                ReadyToRunSectionType.GenericsHashtable                       => ViewKind.ReadyToRunSection_GenericsHashtable,
                ReadyToRunSectionType.NativeStatics                           => ViewKind.ReadyToRunSection_NativeStatics,
                ReadyToRunSectionType.StaticsInfoHashtable                    => ViewKind.ReadyToRunSection_StaticsInfoHashtable,
                ReadyToRunSectionType.GenericMethodsHashtable                 => ViewKind.ReadyToRunSection_GenericMethodsHashtable,
                ReadyToRunSectionType.ExactMethodInstantiationsHashtable      => ViewKind.ReadyToRunSection_ExactMethodInstantiationsHashtable,
                ReadyToRunSectionType.ReadonlyBlobRegionEnd                   => ViewKind.ReadyToRunSection_ReadonlyBlobRegionEnd,
                _ => ViewKind.UnknownModuleInfoRowData
            };

            return kind;
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.ModuleInfoRowV1, StructSize(((PEViewWriter) writer).Is32Bit));

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(SectionId), SectionIdOffset, SectionId, sizeof(int));
                    break;

                case 1:
                    structWriter.WriteField(nameof(Flags), FlagsOffset, Flags, sizeof(int));
                    break;

                case 2:
                    structWriter.WritePointerField(nameof(Start), StartOffset, Start);
                    break;

                case 3:
                    structWriter.WriteField(nameof(End), EndOffset, End);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return SectionId.ToString();
        }
    }
}
