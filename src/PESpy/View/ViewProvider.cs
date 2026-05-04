using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ClrDebug.OMF;
using ClrDebug.PDB;
using PESpy.LIB;
using PESpy.PDB;
using PESpy.View;

namespace PESpy
{
    internal partial class ViewProvider
    {
        public static unsafe IView CreateStructView(ViewKind kind, int length, in MemoryChunk chunk, ViewWriter viewWriter, bool isSplit = false)
        {
            return kind switch
            {
                ViewKind.Data                                        => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.Padding                                     => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.CC                                          => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ImageArchivePad                             => GetBytes(chunk, viewWriter, length, kind),

                #region Headers

                ViewKind.ImageDosHeader                              => Write(new ImageDosHeader(chunk),                              viewWriter),
                ViewKind.DosStub                                     => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.RichHeader                                  => GetRichHeader(chunk, viewWriter),
                ViewKind.ImageNtHeaders                              => Write(new ImageNtHeaders(chunk),                              viewWriter),
                ViewKind.ImageFileHeader                             => Write(new ImageFileHeader(chunk),                             viewWriter),
                ViewKind.ImageOptionalHeader                         => Write(new ImageOptionalHeader(chunk),                         viewWriter),
                ViewKind.ImageDataDirectory                          => Write(new ImageDataDirectory(chunk),                          viewWriter),
                ViewKind.ImageSectionHeader                          => Write(new ImageSectionHeader(chunk),                          viewWriter),
                ViewKind.ImageRelocation                             => Write(new ImageRelocation(chunk),                             viewWriter),
                ViewKind.CoffSymbolTable                             => GetCoffSymbolTable(chunk, viewWriter),
                ViewKind.ImageSymbol                                 => GetImageSymbol(chunk, viewWriter),
                ViewKind.ImageAuxSymbol                              => Write(new ImageAuxSymbol(chunk),                              viewWriter),
                ViewKind.ImageLineNumber                             => Write(new ImageLineNumber(chunk),                             viewWriter),
                ViewKind.AnonObjectHeader                            => Write(new AnonObjectHeader(chunk),                            viewWriter),

                #endregion
                #region Exports Table (0)

                ViewKind.ImageExportDirectory                        => Write(new ImageExportDirectory(chunk),                        viewWriter),
                ViewKind.ImageExportDirectory_Name                   => WriteAnsiNullTerminated(chunk, viewWriter, kind),
                ViewKind.ImageExportDirectory_ForwarderName          => WriteAnsiNullTerminated(chunk, viewWriter, kind),
                ViewKind.ImageExportDirectory_AddressOfNameOrdinals_Entry => viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekInt32(0), sizeof(int), kind),
                ViewKind.ImageExportDirectory_AddressOfNames_Entry   => viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekInt32(0), sizeof(int), kind),
                ViewKind.ImageExportDirectory_AddressOfNames_Name    => WriteAnsiNullTerminated(chunk, viewWriter, kind),
                ViewKind.ImageExportDirectory_AddressOfFunctions_Entry => viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekInt32(0), sizeof(int), kind),

                #endregion
                #region Import Table (1)

                ViewKind.ImageImportDescriptor                       => Write(new ImageImportDescriptor(chunk),                       viewWriter),
                ViewKind.ImageImportDescriptor_Name                  => WriteAnsiNullTerminated(chunk, viewWriter, kind),
                ViewKind.ImageEnclaveImport_ImportName               => WriteAnsiNullTerminated(chunk, viewWriter, kind),
                ViewKind.ImageThunkData                              => GetImageThunkData(chunk, viewWriter),
                ViewKind.ImageImportByName                           => Write(new ImageImportByName(chunk),                           viewWriter),

                #endregion
                #region Resource Directory (2)

                ViewKind.ImageResourceDirectory                      => GetImageResourceDirectory(chunk, viewWriter),
                ViewKind.ImageResourceDirectoryEntry                 => GetImageResourceDirectoryEntry(chunk, viewWriter),
                ViewKind.ImageResourceDataEntry                      => GetImageResourceDataEntry(chunk, viewWriter),
                ViewKind.ImageResourceDirStringU                     => Write(new ImageResourceDirStringU(chunk),                     viewWriter),
                ViewKind.VsVersionInfo                               => Write(new VsVersionInfo(chunk),                               viewWriter),
                ViewKind.VsFixedFileInfo                             => Write(new VsFixedFileInfo(chunk),                             viewWriter),
                ViewKind.StringFileInfo                              => Write(new VsVersionInfo.StringFileInfo(chunk),                viewWriter),
                ViewKind.StringTable                                 => Write(new VsVersionInfo.StringTable(chunk),                   viewWriter),
                ViewKind.VarFileInfo                                 => Write(new VsVersionInfo.VarFileInfo(chunk),                   viewWriter),
                ViewKind.ClrDebugResource                            => Write(new ClrDebugResource(chunk),                            viewWriter),
                ViewKind.MessageResourceData                         => Write(new MessageResourceData(chunk),                         viewWriter),
                //ViewKind.MessageResourceBlock                        => Write(new MessageResourceBlock(chunk),                        viewWriter),
                ViewKind.MessageResourceEntry                        => Write(new MessageResourceEntry(chunk),                        viewWriter),
                ViewKind.Manifest                                    => WriteFixedUtf8String(chunk, viewWriter, length, kind),
                ViewKind.UnknownResource                             => GetBytes(chunk, viewWriter, length, kind),

                #endregion
                #region Exception Table (3)

                ViewKind.RuntimeFunction                             => Write(new RuntimeFunction(chunk),                             viewWriter),
                ViewKind.UnwindInfo                                  => Write(new UnwindInfo(chunk),                                  viewWriter),
                ViewKind.GsHandlerData                               => Write(new GsHandlerData(chunk),                               viewWriter),
                ViewKind.ScopeTable                                  => Write(new ScopeTable(chunk),                                  viewWriter),
                ViewKind.ScopeRecord                                 => Write(new ScopeTable.ScopeRecord(chunk),                      viewWriter),

                #region FuncInfo

                ViewKind.FuncInfo                                    => Write(new FuncInfo(chunk),                                    viewWriter),
                ViewKind.HandlerType                                 => Write(new HandlerType(chunk),                                 viewWriter),
                ViewKind.IptoStateMapEntry                           => Write(new IptoStateMapEntry(chunk),                           viewWriter),
                ViewKind.TryBlockMapEntry                            => Write(new TryBlockMapEntry(chunk),                            viewWriter),
                ViewKind.TypeDescriptor                              => Write(new TypeDescriptor(chunk),                              viewWriter),
                ViewKind.UnwindMapEntry                              => Write(new UnwindMapEntry(chunk),                              viewWriter),

                #endregion
                #region FuncInfo4

                ViewKind.FuncInfo4                                   => Write(new FuncInfo4(chunk),                                   viewWriter),
                //ViewKind.FuncInfoHeader                              => Write(new FuncInfoHeader(chunk),                              viewWriter),
                ViewKind.HandlerMap4                                 => Write(new HandlerMap4(chunk),                                 viewWriter),
                ViewKind.IPtoStateMap4                               => Write(new IPtoStateMap4(chunk),                               viewWriter),
                ViewKind.SepIPtoStateMap4                            => Write(new SepIPtoStateMap4(chunk),                            viewWriter),
                ViewKind.TryBlockMap4                                => Write(new TryBlockMap4(chunk),                                viewWriter),
                ViewKind.UWMap4                                      => Write(new UWMap4(chunk),                                      viewWriter),
                //ViewKind.HandlerType4                                => Write(new HandlerType4(chunk),                                viewWriter),
                //ViewKind.HandlerTypeHeader                           => Write(new HandlerTypeHeader(chunk),                           viewWriter),
                //ViewKind.IPtoStateMapEntry4                          => Write(new IPtoStateMapEntry4(chunk),                          viewWriter),
                //ViewKind.SepIPtoStateMapEntry4                       => Write(new SepIPtoStateMapEntry4(chunk),                       viewWriter),
                //ViewKind.TryBlockMapEntry4                           => Write(new TryBlockMapEntry4(chunk),                           viewWriter),
                //ViewKind.UnwindMapEntry4                             => Write(new UnwindMapEntry4(chunk),                             viewWriter),

                #endregion

                #endregion
                #region Security Table (4)

                ViewKind.WinCertificate                              => Write(new WinCertificate(chunk),                              viewWriter),

                #endregion
                #region Base Relocation Table (5)

                ViewKind.ImageBaseRelocation                         => Write(new ImageBaseRelocation(chunk),                         viewWriter),

                #endregion
                #region Debug Table (6)

                ViewKind.ImageDebugDirectory                         => Write(new ImageDebugDirectory(chunk),                         viewWriter),
                ViewKind.NB10I                                       => Write(new NB10I(chunk),                                       viewWriter),
                ViewKind.RSDSI                                       => Write(new RSDSI(chunk),                                       viewWriter),
                ViewKind.FpoData                                     => Write(new FpoData(chunk),                                     viewWriter),
                ViewKind.XFixupData                                  => Write(new XFixupData(chunk),                                  viewWriter),
                ViewKind.ImageDebugMisc                              => Write(new ImageDebugMisc(chunk),                              viewWriter),
                ViewKind.ImageCoffSymbolsHeader                      => Write(new ImageCoffSymbolsHeader(chunk),                      viewWriter),
                ViewKind.BBT                                         => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.VCFeature                                   => Write(new VCFeature(chunk),                                   viewWriter),
                ViewKind.PogoData                                    => GetPogoData(chunk, viewWriter),
                ViewKind.PogoItem                                    => Write(new PogoItem(chunk),                                    viewWriter),
                ViewKind.Reproducible                                => Write(new Reproducible(chunk),                                viewWriter),
                ViewKind.EmbeddedPortablePdb                         => GetEmbeddedPortablePdb(chunk, viewWriter),
                ViewKind.PdbChecksum                                 => GetPdbChecksum(chunk, viewWriter),
                ViewKind.ExDllCharacteristics                        => viewWriter.NewValue(chunk.AbsoluteOffset, (IMAGE_DLLCHARACTERISTICS_EX) chunk.PeekUInt32(0), sizeof(int), kind),

                #endregion

                #region Thread Local Storage Table (9)

                ViewKind.ImageTlsDirectory                           => Write(new ImageTlsDirectory(chunk),                           viewWriter),

                #endregion
                #region Load Config Table (10)

                ViewKind.ImageLoadConfigDirectory                    => Write(new ImageLoadConfigDirectory(chunk),                    viewWriter),
                ViewKind.ImageLoadConfigCodeIntegrity                => Write(new ImageLoadConfigCodeIntegrity(chunk),                viewWriter),
                ViewKind.ImageEnclaveConfig                          => Write(new ImageEnclaveConfig(chunk),                          viewWriter),
                ViewKind.ImageEnclaveImport                          => Write(new ImageEnclaveImport(chunk),                          viewWriter),
                ViewKind.GuardAddressTakenIatEntryTable              => Write(chunk.PEFile().LoadConfigTable.GuardAddressTakenIatEntryTable.Value, viewWriter),
                ViewKind.GuardCFFunctionTable                        => Write(chunk.PEFile().LoadConfigTable.GuardCFFunctionTable.Value, viewWriter),
                ViewKind.GuardEHContinuationTable                    => Write(chunk.PEFile().LoadConfigTable.GuardEHContinuationTable.Value, viewWriter),
                ViewKind.GuardLongJumpTargetTable                    => Write(chunk.PEFile().LoadConfigTable.GuardLongJumpTargetTable.Value, viewWriter),
                ViewKind.XFG                                         => viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekUInt64(0), sizeof(long), kind),
                ViewKind.LockPrefixTable                             => viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekNativeSpan<int>(0, length / 4), length, kind),
                ViewKind.SecurityCookie                              => viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekPointer(0), chunk.PointerSize, kind),
                ViewKind.SEHandlerTable                              => viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekNativeSpan<int>(0, length / 4), length, kind),
                ViewKind.GuardCFCheckFunctionPointer                 => viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekPointer(0), chunk.PointerSize, kind),
                ViewKind.GuardCFDispatchFunctionPointer              => viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekPointer(0), chunk.PointerSize, kind),
                ViewKind.GuardRFFailureRoutineFunctionPointer        => viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekPointer(0), chunk.PointerSize, kind),
                ViewKind.GuardRFVerifyStackPointerFunctionPointer    => viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekPointer(0), chunk.PointerSize, kind),
                ViewKind.GuardXFGCheckFunctionPointer                => viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekPointer(0), chunk.PointerSize, kind),
                ViewKind.GuardXFGDispatchFunctionPointer             => viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekPointer(0), chunk.PointerSize, kind),
                ViewKind.GuardXFGTableDispatchFunctionPointer        => viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekPointer(0), chunk.PointerSize, kind),
                ViewKind.CastGuardOsDeterminedFailureMode            => viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekPointer(0), chunk.PointerSize, kind),
                ViewKind.GuardMemcpyFunctionPointer                  => viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekPointer(0), chunk.PointerSize, kind),
                ViewKind.UmaFunctionPointers                         => viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekPointer(0), chunk.PointerSize, kind),
                ViewKind.ImageDynamicRelocationTable                 => Write(new ImageDynamicRelocationTable(chunk),                 viewWriter),
                ViewKind.ImageDynamicRelocation                      => Write(new ImageDynamicRelocation(chunk),                      viewWriter),
                //ViewKind.ImageDynamicRelocationV2                    => Write(new ImageDynamicRelocationV2(chunk),                    viewWriter),
                //ViewKind.ImageFunctionOverrideHeader                 => Write(new ImageFunctionOverrideHeader(chunk),                 viewWriter),
                ViewKind.ImagePrologueDynamicRelocationHeader        => Write(new ImagePrologueDynamicRelocationHeader(chunk),        viewWriter),
                //ViewKind.ImageEpilogueDynamicRelocationHeader        => Write(new ImageEpilogueDynamicRelocationHeader(chunk),        viewWriter),
                ViewKind.ImageImportControlTransferDynamicRelocation => Write(new ImageImportControlTransferDynamicRelocation(chunk), viewWriter),
                ViewKind.ImageIndirControlTransferDynamicRelocation  => Write(new ImageIndirControlTransferDynamicRelocation(chunk),  viewWriter),
                ViewKind.ImageSwitchTableBranchDynamicRelocation     => Write(new ImageSwitchTableBranchDynamicRelocation(chunk),     viewWriter),
                ViewKind.ImageFunctionOverrideDynamicRelocation      => Write(new ImageFunctionOverrideDynamicRelocation(chunk),      viewWriter),
                ViewKind.ImageBDDInfo                                => Write(new ImageBDDInfo(chunk),                                viewWriter),
                ViewKind.ImageBDDDynamicRelocation                   => Write(new ImageBDDDynamicRelocation(chunk),                   viewWriter),

                #endregion
                #region Bound Import Table (11)

                ViewKind.ImageBoundImportDescriptor                  => Write(new ImageBoundImportDescriptor(chunk),                  viewWriter),
                ViewKind.ImageBoundImportName                        => WriteAnsiNullTerminated(chunk, viewWriter, kind),
                ViewKind.ImageBoundForwarderRef                      => Write(new ImageBoundForwarderRef(chunk),                      viewWriter),

                #endregion
                #region Delay Import Table (13)

                ViewKind.ImageDelayLoadDescriptor                    => Write(new ImageDelayLoadDescriptor(chunk),                    viewWriter),
                ViewKind.ImageDelayLoadDescriptor_DllNameRVA         => WriteAnsiNullTerminated(chunk, viewWriter, kind),
                ViewKind.ImageDelayLoadDescriptor_ModuleHandleRVA    => viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekPointer(0), chunk.PointerSize, kind),

                #endregion
                #region CorHeader Directory (14)

                ViewKind.ImageCor20Header                            => Write(new ImageCor20Header(chunk),                            viewWriter),
                ViewKind.ImageCorILMethodTiny                        => Write(new ImageCorILMethod(chunk),                            viewWriter),
                ViewKind.ImageCorILMethodFat                         => Write(new ImageCorILMethod(chunk),                            viewWriter),
                ViewKind.ImageCorILMethodSectEHFat                   => Write(new ImageCorILMethodSectEH(chunk, isFat: true),         viewWriter),
                ViewKind.ImageCorILMethodSectEHSmall                 => Write(new ImageCorILMethodSectEH(chunk, isFat: false),        viewWriter),
                ViewKind.ImageCorILMethodSectFat                     => Write(new ImageCorILMethodSect(chunk, isFat: true),           viewWriter),
                ViewKind.ImageCorILMethodSectSmall                   => Write(new ImageCorILMethodSect(chunk, isFat: false),          viewWriter),
                ViewKind.ImageCorILMethodSectEHClauseFat             => Write(new ImageCorILMethodSectEHClause(chunk, isFat: true),   viewWriter),
                ViewKind.ImageCorILMethodSectEHClauseSmall           => Write(new ImageCorILMethodSectEHClause(chunk, isFat: false),  viewWriter),
                ViewKind.StorageSignature                            => Write(new StorageSignature(chunk),                            viewWriter),
                ViewKind.StorageHeader                               => GetStorageHeader(chunk, viewWriter),
                //ViewKind.StorageStream                               => Write(new StorageStream(chunk),                               viewWriter),
                //ViewKind.CompressedModelHeap                         => Write(new Ecma335.CompressedModelHeap(chunk),                 viewWriter),
                //ViewKind.StringPoolHeap                              => Write(new Ecma335.StringHeap(chunk),                          viewWriter),
                //ViewKind.USBlobPoolHeap                              => Write(new Ecma335.UserStringHeap(chunk),                      viewWriter),
                //ViewKind.BlobPoolHeap                                => Write(new Ecma335.BlobHeap(chunk),                            viewWriter),
                //ViewKind.GuidPoolHeap                                => Write(new Ecma335.GuidHeap(chunk),                            viewWriter),
                ViewKind.MetadataHeader                              => Write(new Ecma335.CompressedModelHeader(chunk),               viewWriter),

                #region Metadata Rows

                ViewKind.Metadata_String                             => WriteFixedUtf8String(chunk, viewWriter, length, kind),
                ViewKind.Metadata_UserString                         => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_Blob                               => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_Guid                               => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_ModuleRow                          => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_TypeRefRow                         => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_TypeDefRow                         => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_FieldPtrRow                        => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_FieldRow                           => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_MethodPtrRow                       => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_MethodDefRow                       => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_ParamPtrRow                        => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_ParamRow                           => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_InterfaceImplRow                   => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_MemberRefRow                       => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_ConstantRow                        => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_CustomAttributeRow                 => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_FieldMarshalRow                    => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_DeclSecurityRow                    => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_ClassLayoutRow                     => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_FieldLayoutRow                     => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_StandAloneSigRow                   => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_EventMapRow                        => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_EventPtrRow                        => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_EventRow                           => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_PropertyMapRow                     => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_PropertyPtrRow                     => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_PropertyRow                        => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_MethodSemanticsRow                 => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_MethodImplRow                      => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_ModuleRefRow                       => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_TypeSpecRow                        => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_ImplMapRow                         => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_FieldRvaRow                        => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_EncLogRow                          => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_EncMapRow                          => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_AssemblyRow                        => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_AssemblyProcessorRow               => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_AssemblyOSRow                      => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_AssemblyRefRow                     => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_AssemblyRefProcessorRow            => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_AssemblyRefOSRow                   => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_FileRow                            => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_ExportedTypeRow                    => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_ManifestResourceRow                => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_NestedClassRow                     => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_GenericParamRow                    => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_MethodSpecRow                      => WriteRow(chunk, viewWriter, kind),
                ViewKind.Metadata_GenericParamConstraintRow          => WriteRow(chunk, viewWriter, kind),
                ViewKind.PortablePdb_DocumentRow                     => WriteRow(chunk, viewWriter, kind),
                ViewKind.PortablePdb_MethodDebugInformationRow       => WriteRow(chunk, viewWriter, kind),
                ViewKind.PortablePdb_LocalScopeRow                   => WriteRow(chunk, viewWriter, kind),
                ViewKind.PortablePdb_LocalVariableRow                => WriteRow(chunk, viewWriter, kind),
                ViewKind.PortablePdb_LocalConstantRow                => WriteRow(chunk, viewWriter, kind),
                ViewKind.PortablePdb_ImportScopeRow                  => WriteRow(chunk, viewWriter, kind),
                ViewKind.PortablePdb_StateMachineMethodRow           => WriteRow(chunk, viewWriter, kind),
                ViewKind.PortablePdb_CustomDebugInformationRow       => WriteRow(chunk, viewWriter, kind),

                #endregion

                #endregion
                #region CLR

                ViewKind.RuntimeInfo                                 => Write(new RuntimeInfo(chunk),                                 viewWriter),
                ViewKind.ClrEngineMetrics                            => Write(new ClrEngineMetrics(chunk),                            viewWriter),
                ViewKind.ImageCorVTableFixup                         => Write(new ImageCorVTableFixup(chunk),                         viewWriter),

                #region R2R

                ViewKind.ReadyToRunHeader                            => Write(new R2R.ReadyToRunHeader(chunk),                        viewWriter),
                ViewKind.ReadyToRunCoreHeader                        => Write(new R2R.ReadyToRunCoreHeader(chunk),                    viewWriter),
                ViewKind.ReadyToRunSection                           => Write(new R2R.ReadyToRunSection(chunk),                       viewWriter),

                #region ReadyToRunSection Bytes

                ViewKind.ReadyToRunSection_ImportSections            => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_RuntimeFunctions          => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_MethodDefEntryPoints      => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_ExceptionInfo             => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_DebugInfo                 => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_DelayLoadMethodCallThunks => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_AvailableTypes            => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_InstanceMethodEntryPoints => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_InliningInfo              => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_ProfileDataInfo           => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_ManifestMetadata          => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_AttributePresence         => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_InliningInfo2             => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_ComponentAssemblies       => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_OwnerCompositeExecutable  => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_PgoInstrumentationData    => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_ManifestAssemblyMvids     => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_CrossModuleInlineInfo     => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_HotColdMap                => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_MethodIsGenericMap        => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_EnclosingTypeMap          => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_TypeGenericInfoMap        => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_ExternalTypeMaps          => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_ProxyTypeMaps             => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_TypeMapAssemblyTargets    => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_StringTable               => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_GCStaticRegion            => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_ThreadStaticRegion        => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_TypeManagerIndirection    => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_EagerCctor                => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_FrozenObjectRegion        => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_DehydratedData            => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_ThreadStaticOffsetRegion  => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_ImportAddressTables       => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_ModuleInitializerList     => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_ReadonlyBlobRegionStart   => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_TypeMap                   => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_ArrayMap                  => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_PointerTypeMap            => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_GenericInstanceMap        => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_FunctionPointerTypeMap    => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_GenericParameterMap       => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_BlockReflectionTypeMap    => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_InvokeMap                 => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_VirtualInvokeMap          => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_CommonFixupsTable         => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_FieldAccessMap            => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_CCtorContextMap           => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_ByRefTypeMap              => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_DiagGenericInstanceMap    => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_DiagGenericParameterMap   => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_EmbeddedMetadata          => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_DefaultConstructorMap     => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_UnboxingAndInstantiatingStubMap => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_StructMarshallingStubMap  => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_DelegateMarshallingStubMap => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_GenericVirtualMethodTable => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_InterfaceGenericVirtualMethodTable => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_TypeTemplateMap           => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_GenericMethodsTemplateMap => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_DynamicInvokeTemplateData => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_BlobIdResourceIndex       => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_BlobIdResourceData        => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_BlobIdStackTraceEmbeddedMetadata => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_BlobIdStackTraceMethodRvaToTokenMapping => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_BlobIdStackTraceLineNumbers => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_BlobIdStackTraceDocuments => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_NativeLayoutInfo          => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_NativeReferences          => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_GenericsHashtable         => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_NativeStatics             => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_StaticsInfoHashtable      => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_GenericMethodsHashtable   => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_ExactMethodInstantiationsHashtable => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.ReadyToRunSection_ReadonlyBlobRegionEnd     => GetBytes(chunk, viewWriter, length, kind),

                #endregion

                ViewKind.ReadyToRunImportSection                     => Write(new R2R.ReadyToRunImportSection(chunk),                 viewWriter),

                #endregion

                ViewKind.AppHostSignature                            => Write(chunk.PEFile().AppHostSignature, viewWriter),
                ViewKind.BundleHeaderFixed                           => Write(new Bundle.HeaderFixed(chunk),                          viewWriter),
                ViewKind.BundleHeaderFixedV2                         => Write(new Bundle.HeaderFixedV2(chunk),                        viewWriter),
                ViewKind.BundleFileEntry                             => GetBundleFileEntry(chunk, viewWriter),
                ViewKind.BundleFileEntryFixed                        => GetBundleFileEntryFixed(chunk, viewWriter),
                ViewKind.BundleLocation                              => Write(new Bundle.Location(chunk),                             viewWriter),
                ViewKind.BundleEncodedString                         => Write(new BundleEncodedString(chunk),                         viewWriter),
                ViewKind.DepsJson                                    => WriteFixedUtf8String(chunk, viewWriter, length, kind),
                ViewKind.RuntimeConfigJson                           => WriteFixedUtf8String(chunk, viewWriter, length, kind),
                ViewKind.DotNetRuntimeDebugHeader                    => GetDotNetRuntimeDebugHeader(chunk, viewWriter),
                ViewKind.NativeAOTModulesA                           => viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekPointer(0), chunk.PointerSize, kind),
                ViewKind.NativeAOTModuleAddress                      => viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekPointer(0), chunk.PointerSize, kind),
                ViewKind.NativeAOTModulesZ                           => viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekPointer(0), chunk.PointerSize, kind),
                ViewKind.NativeAOTReadyToRunHeader                   => Write(new NativeAOT.ReadyToRunHeader(chunk),                  viewWriter),
                ViewKind.DebugTypeEntry                              => Write(new NativeAOT.DebugTypeEntry(chunk),                    viewWriter),
                ViewKind.GlobalValueEntry                            => Write(new NativeAOT.GlobalValueEntry(chunk),                  viewWriter),

                #endregion
                #region RTTI

                //ViewKind.RTTIBaseClassArray                          => Write(new RTTIBaseClassArray(chunk),                          viewWriter),
                //ViewKind.RTTIClassHierarchyDescriptor                => Write(new RTTIClassHierarchyDescriptor(chunk),                viewWriter),
                //ViewKind.RTTICompleteObjectLocator                   => Write(new RTTICompleteObjectLocator(chunk),                   viewWriter),

                #endregion

                ViewKind.ImageVXDHeader                              => Write(new ImageVXDHeader(chunk),                              viewWriter),
                ViewKind.PN                                          => viewWriter.NewValue(chunk.AbsoluteOffset, (PN) (length == 2 ? chunk.PeekUInt16(0) : chunk.PeekUInt32(0)), length, kind),
                ViewKind.MsfHdr                                      => Write(new PDB.MsfHdr(chunk),                                  viewWriter),
                ViewKind.BigMsfHdr                                   => Write(new PDB.BigMsfHdr(chunk),                               viewWriter),
                ViewKind.StreamTable                                 => GetStreamTable(chunk, viewWriter),
                ViewKind.PDBStream                                   => Write((PDB.PDBStream) chunk.PDBFile().PDB.PDBHeader, viewWriter),
                ViewKind.PDBStream70                                 => Write((PDB.PDBStream70) chunk.PDBFile().PDB.PDBHeader, viewWriter),
                ViewKind.StreamNameTable                             => Write(chunk.PDBFile().PDB.StreamNameTable, viewWriter),
                ViewKind.Hdr                                         => Write(new PDB.HDR(chunk),                                     viewWriter),
                ViewKind.Hdr_16t                                     => Write(new PDB.HDR_16t(chunk),                                 viewWriter),
                ViewKind.DbiHdr                                      => Write(new PDB.DBIHdr(chunk),                                  viewWriter),
                ViewKind.NewDbiHdr                                   => Write(new PDB.NewDBIHdr(chunk),                               viewWriter),
                ViewKind.Modi60Persist                               => GetModi60Persist(chunk, viewWriter),
                ViewKind.ECInfo                                      => Write(new PDB.ECInfo(chunk),                                  viewWriter),
                //ViewKind.SC20                                        => Write(new PDB.SC20(chunk),                                    viewWriter),
                //ViewKind.SC40                                        => Write(new PDB.SC40(chunk),                                    viewWriter),
                //ViewKind.SC                                          => Write(new PDB.SC(chunk),                                      viewWriter),
                //ViewKind.SC2                                         => Write(new PDB.SC2(chunk),                                     viewWriter),
                ViewKind.SectionContribsV20                          => Write((PDB.SectionContribsV20) chunk.PDBFile().DBI.SectionContribs, viewWriter),
                ViewKind.SectionContribsV40                          => Write((PDB.SectionContribsV40) chunk.PDBFile().DBI.SectionContribs, viewWriter),
                ViewKind.SectionContribsV60                          => Write((PDB.SectionContribsV60) chunk.PDBFile().DBI.SectionContribs, viewWriter),
                ViewKind.SectionContribs2                            => Write((PDB.SectionContribs2) chunk.PDBFile().DBI.SectionContribs, viewWriter),
                ViewKind.OMFSegMap                                   => Write(new OMFSegMap(chunk),                                   viewWriter),
                ViewKind.OMFFileIndex                                => GetOMFFileIndex(chunk, viewWriter),
                ViewKind.NameTable                                   => Write(new PDB.NMT(chunk),                                     viewWriter),
                ViewKind.DbgDataHdr                                  => Write(chunk.PDBFile().DBI.DbgHdr, viewWriter),
                ViewKind.PdbFeature                                  => viewWriter.NewValue(chunk.AbsoluteOffset, (PdbFeature) chunk.PeekUInt32(0), sizeof(int), kind),
                ViewKind.CvSignature                                 => viewWriter.NewValue(chunk.AbsoluteOffset, (CV_SIGNATURE) chunk.PeekUInt32(0), sizeof(int), kind),
                ViewKind.HRFile                                      => WriteUnmanaged<HRFile>(chunk, viewWriter, kind),
                ViewKind.HashBucketsBitmap                           => viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekNativeSpan<int>(0, length / 4), length, kind),
                ViewKind.HashBuckets                                 => viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekNativeSpan<int>(0, length / 4), length, kind),
                ViewKind.CvDebugSSubsectionHeader                    => Write(new PDB.CvDebugSSubsectionHeader(chunk),                viewWriter),
                ViewKind.CvFileCheckSum                              => Write(new PDB.CvFileCheckSum(chunk),                          viewWriter),
                ViewKind.FrameData                                   => Write(new PDB.FrameData(chunk),                               viewWriter),
                ViewKind.CvLine                                      => Write(new PDB.CvLine(chunk),                                  viewWriter),
                ViewKind.InlineeSourceLine                           => Write(new PDB.InlineeSourceLine(chunk),                       viewWriter),
                ViewKind.InlineeSourceLineEx                         => Write(new PDB.InlineeSourceLineEx(chunk),                     viewWriter),

                #region Symbols

                ViewKind.SymType                                     => WriteSymbol(chunk, viewWriter),
                ViewKind.AlignSym                                    => WriteSymbol(chunk, viewWriter),
                ViewKind.AnnotationSym                               => WriteSymbol(chunk, viewWriter),
                ViewKind.ArmSwitchTable                              => WriteSymbol(chunk, viewWriter),
                ViewKind.AttrManyRegSym2                             => WriteSymbol(chunk, viewWriter),
                ViewKind.AttrRegRel                                  => WriteSymbol(chunk, viewWriter),
                ViewKind.AttrRegSym                                  => WriteSymbol(chunk, viewWriter),
                ViewKind.AttrSlotSym                                 => WriteSymbol(chunk, viewWriter),
                ViewKind.BlockSym16                                  => WriteSymbol(chunk, viewWriter),
                ViewKind.BlockSym32                                  => WriteSymbol(chunk, viewWriter),
                ViewKind.BPRelSym16                                  => WriteSymbol(chunk, viewWriter),
                ViewKind.BPRelSym32                                  => WriteSymbol(chunk, viewWriter),
                ViewKind.BPRelSym3216t                               => WriteSymbol(chunk, viewWriter),
                ViewKind.BuildInfoSym                                => WriteSymbol(chunk, viewWriter),
                ViewKind.CallSiteInfo                                => WriteSymbol(chunk, viewWriter),
                ViewKind.CExMSym16                                   => WriteSymbol(chunk, viewWriter),
                ViewKind.CExMSym32                                   => WriteSymbol(chunk, viewWriter),
                ViewKind.CFlagSym                                    => WriteSymbol(chunk, viewWriter),
                ViewKind.CoffGroupSym                                => WriteSymbol(chunk, viewWriter),
                ViewKind.CompileSym                                  => WriteSymbol(chunk, viewWriter),
                ViewKind.CompileSym3                                 => WriteSymbol(chunk, viewWriter),
                ViewKind.ConstSym                                    => WriteSymbol(chunk, viewWriter),
                ViewKind.ConstSym16t                                 => WriteSymbol(chunk, viewWriter),
                ViewKind.DataSym16                                   => WriteSymbol(chunk, viewWriter),
                ViewKind.DataSym32                                   => WriteSymbol(chunk, viewWriter),
                ViewKind.DataSym3216t                                => WriteSymbol(chunk, viewWriter),
                ViewKind.DataSymHLSL                                 => WriteSymbol(chunk, viewWriter),
                ViewKind.DataSymHLSL32                               => WriteSymbol(chunk, viewWriter),
                ViewKind.DataSymHLSL32Ex                             => WriteSymbol(chunk, viewWriter),
                ViewKind.DefRangeSym                                 => WriteSymbol(chunk, viewWriter),
                ViewKind.DefRangeSymFramePointerRel                  => WriteSymbol(chunk, viewWriter),
                ViewKind.DefRangeSymFramePointerRelFullScope         => WriteSymbol(chunk, viewWriter),
                ViewKind.DefRangeSymHLSL                             => WriteSymbol(chunk, viewWriter),
                ViewKind.DefRangeSymRegister                         => WriteSymbol(chunk, viewWriter),
                ViewKind.DefRangeSymRegisterRel                      => WriteSymbol(chunk, viewWriter),
                ViewKind.DefRangeSymSubField                         => WriteSymbol(chunk, viewWriter),
                ViewKind.DefRangeSymSubfieldRegister                 => WriteSymbol(chunk, viewWriter),
                ViewKind.DiscardedSym                                => WriteSymbol(chunk, viewWriter),
                ViewKind.DPCSymTagMap                                => WriteSymbol(chunk, viewWriter),
                ViewKind.EntryThisSym                                => WriteSymbol(chunk, viewWriter),
                ViewKind.EnvBlockSym                                 => WriteSymbol(chunk, viewWriter),
                ViewKind.ExportSym                                   => WriteSymbol(chunk, viewWriter),
                ViewKind.FileStaticSym                               => WriteSymbol(chunk, viewWriter),
                ViewKind.FrameCookie                                 => WriteSymbol(chunk, viewWriter),
                ViewKind.FrameProcSym                                => WriteSymbol(chunk, viewWriter),
                ViewKind.FrameRelSym                                 => WriteSymbol(chunk, viewWriter),
                ViewKind.FunctionList                                => WriteSymbol(chunk, viewWriter),
                ViewKind.HeapAllocSite                               => WriteSymbol(chunk, viewWriter),
                ViewKind.InlineSiteSym                               => WriteSymbol(chunk, viewWriter),
                ViewKind.InlineSiteSym2                              => WriteSymbol(chunk, viewWriter),
                ViewKind.LabelSym16                                  => WriteSymbol(chunk, viewWriter),
                ViewKind.LabelSym32                                  => WriteSymbol(chunk, viewWriter),
                ViewKind.LocalDPCGroupSharedSym                      => WriteSymbol(chunk, viewWriter),
                ViewKind.LocalSym                                    => WriteSymbol(chunk, viewWriter),
                ViewKind.ManProcSym                                  => WriteSymbol(chunk, viewWriter),
                ViewKind.ManTypRef                                   => WriteSymbol(chunk, viewWriter),
                ViewKind.ManyRegSym                                  => WriteSymbol(chunk, viewWriter),
                ViewKind.ManyRegSym16t                               => WriteSymbol(chunk, viewWriter),
                ViewKind.ManyRegSym2                                 => WriteSymbol(chunk, viewWriter),
                ViewKind.ModTypeRef                                  => WriteSymbol(chunk, viewWriter),
                ViewKind.ObjNameSym                                  => WriteSymbol(chunk, viewWriter),
                ViewKind.OemSymbol                                   => WriteSymbol(chunk, viewWriter),
                ViewKind.PdbMap                                      => WriteSymbol(chunk, viewWriter),
                ViewKind.PogoInfo                                    => WriteSymbol(chunk, viewWriter),
                ViewKind.ProcSym16                                   => WriteSymbol(chunk, viewWriter),
                ViewKind.ProcSym32                                   => WriteSymbol(chunk, viewWriter),
                ViewKind.ProcSym3216t                                => WriteSymbol(chunk, viewWriter),
                ViewKind.ProcSymIA64                                 => WriteSymbol(chunk, viewWriter),
                ViewKind.ProcSymMips                                 => WriteSymbol(chunk, viewWriter),
                ViewKind.ProcSymMips16t                              => WriteSymbol(chunk, viewWriter),
                ViewKind.PubSym32                                    => WriteSymbol(chunk, viewWriter),
                ViewKind.RefMiniPdb                                  => WriteSymbol(chunk, viewWriter),
                ViewKind.RefSym                                      => WriteSymbol(chunk, viewWriter),
                ViewKind.RefSym2                                     => WriteSymbol(chunk, viewWriter),
                ViewKind.RegRel16                                    => WriteSymbol(chunk, viewWriter),
                ViewKind.RegRel32                                    => WriteSymbol(chunk, viewWriter),
                ViewKind.RegRel3216t                                 => WriteSymbol(chunk, viewWriter),
                ViewKind.RegSym                                      => WriteSymbol(chunk, viewWriter),
                ViewKind.RegSym16t                                   => WriteSymbol(chunk, viewWriter),
                ViewKind.ReturnSym                                   => WriteSymbol(chunk, viewWriter),
                ViewKind.SearchSym                                   => WriteSymbol(chunk, viewWriter),
                ViewKind.SectionSym                                  => WriteSymbol(chunk, viewWriter),
                ViewKind.SepCodeSym                                  => WriteSymbol(chunk, viewWriter),
                ViewKind.SLink32                                     => WriteSymbol(chunk, viewWriter),
                ViewKind.SlotSym32                                   => WriteSymbol(chunk, viewWriter),
                ViewKind.ThunkSym16                                  => WriteSymbol(chunk, viewWriter),
                ViewKind.ThunkSym32                                  => WriteSymbol(chunk, viewWriter),
                ViewKind.TrampolineSym                               => WriteSymbol(chunk, viewWriter),
                ViewKind.UdtSym                                      => WriteSymbol(chunk, viewWriter),
                ViewKind.UdtSym16t                                   => WriteSymbol(chunk, viewWriter),
                ViewKind.UNameSpace                                  => WriteSymbol(chunk, viewWriter),

                #endregion
                #region Types

                ViewKind.LfAlias                                     => WriteType(chunk, viewWriter),
                ViewKind.LfArgList                                   => WriteType(chunk, viewWriter),
                ViewKind.LfArgList16t                                => WriteType(chunk, viewWriter),
                ViewKind.LfArray                                     => WriteType(chunk, viewWriter),
                ViewKind.LfArray16t                                  => WriteType(chunk, viewWriter),
                ViewKind.LfBArray                                    => WriteType(chunk, viewWriter),
                ViewKind.LfBArray16t                                 => WriteType(chunk, viewWriter),
                ViewKind.LfBClass                                    => WriteType(chunk, viewWriter),
                ViewKind.LfBClass16t                                 => WriteType(chunk, viewWriter),
                ViewKind.LfBitfield                                  => WriteType(chunk, viewWriter),
                ViewKind.LfBitfield16t                               => WriteType(chunk, viewWriter),
                ViewKind.LfBuildInfo                                 => WriteType(chunk, viewWriter),
                ViewKind.LfChar                                      => WriteType(chunk, viewWriter),
                ViewKind.LfClass                                     => WriteType(chunk, viewWriter),
                ViewKind.LfClass16t                                  => WriteType(chunk, viewWriter),
                ViewKind.LfCmplx128                                  => WriteType(chunk, viewWriter),
                ViewKind.LfCmplx32                                   => WriteType(chunk, viewWriter),
                ViewKind.LfCmplx64                                   => WriteType(chunk, viewWriter),
                ViewKind.LfCmplx80                                   => WriteType(chunk, viewWriter),
                ViewKind.LfCobol0                                    => WriteType(chunk, viewWriter),
                ViewKind.LfCobol016t                                 => WriteType(chunk, viewWriter),
                ViewKind.LfCobol1                                    => WriteType(chunk, viewWriter),
                ViewKind.LfDefArg                                    => WriteType(chunk, viewWriter),
                ViewKind.LfDefArg16t                                 => WriteType(chunk, viewWriter),
                ViewKind.LfDerived                                   => WriteType(chunk, viewWriter),
                ViewKind.LfDerived16t                                => WriteType(chunk, viewWriter),
                ViewKind.LfDimArray                                  => WriteType(chunk, viewWriter),
                ViewKind.LfDimArray16t                               => WriteType(chunk, viewWriter),
                ViewKind.LfDimCon                                    => WriteType(chunk, viewWriter),
                ViewKind.LfDimCon16t                                 => WriteType(chunk, viewWriter),
                ViewKind.LfDimVar                                    => WriteType(chunk, viewWriter),
                ViewKind.LfDimVar16t                                 => WriteType(chunk, viewWriter),
                ViewKind.LfEasy                                      => WriteType(chunk, viewWriter),
                ViewKind.LfEndPreComp                                => WriteType(chunk, viewWriter),
                ViewKind.LfEnum                                      => WriteType(chunk, viewWriter),
                ViewKind.LfEnum16t                                   => WriteType(chunk, viewWriter),
                ViewKind.LfEnumerate                                 => WriteType(chunk, viewWriter),
                ViewKind.LfFieldList                                 => WriteType(chunk, viewWriter),
                ViewKind.LfFieldList16t                              => WriteType(chunk, viewWriter),
                ViewKind.LfFriendCls                                 => WriteType(chunk, viewWriter),
                ViewKind.LfFriendCls16t                              => WriteType(chunk, viewWriter),
                ViewKind.LfFriendFcn                                 => WriteType(chunk, viewWriter),
                ViewKind.LfFriendFcn16t                              => WriteType(chunk, viewWriter),
                ViewKind.LfFuncId                                    => WriteType(chunk, viewWriter),
                ViewKind.LfHLSL                                      => WriteType(chunk, viewWriter),
                ViewKind.LfIndex                                     => WriteType(chunk, viewWriter),
                ViewKind.LfIndex16t                                  => WriteType(chunk, viewWriter),
                ViewKind.LfLabel                                     => WriteType(chunk, viewWriter),
                ViewKind.LfList                                      => WriteType(chunk, viewWriter),
                ViewKind.LfLong                                      => WriteType(chunk, viewWriter),
                ViewKind.LfManaged                                   => WriteType(chunk, viewWriter),
                ViewKind.LfMatrix                                    => WriteType(chunk, viewWriter),
                ViewKind.LfMember                                    => WriteType(chunk, viewWriter),
                ViewKind.LfMember16t                                 => WriteType(chunk, viewWriter),
                ViewKind.LfMemberModify                              => WriteType(chunk, viewWriter),
                ViewKind.LfMethod                                    => WriteType(chunk, viewWriter),
                ViewKind.LfMethod16t                                 => WriteType(chunk, viewWriter),
                ViewKind.LfMethodList                                => WriteType(chunk, viewWriter),
                ViewKind.LfMethodList16t                             => WriteType(chunk, viewWriter),
                ViewKind.LfMFunc                                     => WriteType(chunk, viewWriter),
                ViewKind.LfMFunc16t                                  => WriteType(chunk, viewWriter),
                ViewKind.LfMFuncId                                   => WriteType(chunk, viewWriter),
                ViewKind.LfModifier                                  => WriteType(chunk, viewWriter),
                ViewKind.LfModifier16t                               => WriteType(chunk, viewWriter),
                ViewKind.LfModifierEx                                => WriteType(chunk, viewWriter),
                ViewKind.LfNestType                                  => WriteType(chunk, viewWriter),
                ViewKind.LfNestType16t                               => WriteType(chunk, viewWriter),
                ViewKind.LfNestTypeEx                                => WriteType(chunk, viewWriter),
                ViewKind.LfOct                                       => WriteType(chunk, viewWriter),
                ViewKind.LfOEM                                       => WriteType(chunk, viewWriter),
                ViewKind.LfOEM16t                                    => WriteType(chunk, viewWriter),
                ViewKind.LfOEM2                                      => WriteType(chunk, viewWriter),
                ViewKind.LfOneMethod                                 => WriteType(chunk, viewWriter),
                ViewKind.LfOneMethod16t                              => WriteType(chunk, viewWriter),
                ViewKind.LfPad                                       => WriteType(chunk, viewWriter),
                ViewKind.LfPointer                                   => WriteType(chunk, viewWriter),
                ViewKind.LfPointer16t                                => WriteType(chunk, viewWriter),
                ViewKind.LfPreComp                                   => WriteType(chunk, viewWriter),
                ViewKind.LfPreComp16t                                => WriteType(chunk, viewWriter),
                ViewKind.LfProc                                      => WriteType(chunk, viewWriter),
                ViewKind.LfProc16t                                   => WriteType(chunk, viewWriter),
                ViewKind.LfQuad                                      => WriteType(chunk, viewWriter),
                ViewKind.LfReal128                                   => WriteType(chunk, viewWriter),
                ViewKind.LfReal16                                    => WriteType(chunk, viewWriter),
                ViewKind.LfReal32                                    => WriteType(chunk, viewWriter),
                ViewKind.LfReal48                                    => WriteType(chunk, viewWriter),
                ViewKind.LfReal64                                    => WriteType(chunk, viewWriter),
                ViewKind.LfReal80                                    => WriteType(chunk, viewWriter),
                ViewKind.LfRefSym                                    => WriteType(chunk, viewWriter),
                ViewKind.LfShort                                     => WriteType(chunk, viewWriter),
                ViewKind.LfSkip                                      => WriteType(chunk, viewWriter),
                ViewKind.LfSkip16t                                   => WriteType(chunk, viewWriter),
                ViewKind.LfSTMember                                  => WriteType(chunk, viewWriter),
                ViewKind.LfSTMember16t                               => WriteType(chunk, viewWriter),
                ViewKind.LfStridedArray                              => WriteType(chunk, viewWriter),
                ViewKind.LfStringId                                  => WriteType(chunk, viewWriter),
                ViewKind.LfTypeServer                                => WriteType(chunk, viewWriter),
                ViewKind.LfTypeServer2                               => WriteType(chunk, viewWriter),
                ViewKind.LfUdtModSrcLine                             => WriteType(chunk, viewWriter),
                ViewKind.LfUdtSrcLine                                => WriteType(chunk, viewWriter),
                ViewKind.LfULong                                     => WriteType(chunk, viewWriter),
                ViewKind.LfUnion                                     => WriteType(chunk, viewWriter),
                ViewKind.LfUnion16t                                  => WriteType(chunk, viewWriter),
                ViewKind.LfUOct                                      => WriteType(chunk, viewWriter),
                ViewKind.LfUQuad                                     => WriteType(chunk, viewWriter),
                ViewKind.LfUShort                                    => WriteType(chunk, viewWriter),
                ViewKind.LfVarString                                 => WriteType(chunk, viewWriter),
                ViewKind.LfVBClass                                   => WriteType(chunk, viewWriter),
                ViewKind.LfVBClass16t                                => WriteType(chunk, viewWriter),
                ViewKind.LfVector                                    => WriteType(chunk, viewWriter),
                ViewKind.LfVftable                                   => WriteType(chunk, viewWriter),
                ViewKind.LfVFTPath                                   => WriteType(chunk, viewWriter),
                ViewKind.LfVFTPath16t                                => WriteType(chunk, viewWriter),
                ViewKind.LfVFuncOff                                  => WriteType(chunk, viewWriter),
                ViewKind.LfVFuncOff16t                               => WriteType(chunk, viewWriter),
                ViewKind.LfVFuncTab                                  => WriteType(chunk, viewWriter),
                ViewKind.LfVFuncTab16t                               => WriteType(chunk, viewWriter),
                ViewKind.LfVTShape                                   => WriteType(chunk, viewWriter),
                ViewKind.MlMethod                                    => WriteType(chunk, viewWriter),
                ViewKind.MlMethod16t                                 => WriteType(chunk, viewWriter),

                #endregion

                ViewKind.GSIHashHdr                                  => Write(new PDB.GSIHashHdr(chunk),                              viewWriter),
                ViewKind.PSGSIHDR                                    => Write(new PDB.PSGSIHDR(chunk),                                viewWriter),
                ViewKind.AddressMap                                  => WriteGlobalField(chunk, viewWriter, length, kind, chunk.PeekNativeSpan<int>(0, length / 4), Strings.AddressMap),
                ViewKind.ThunkMap                                    => WriteGlobalField(chunk, viewWriter, length, kind, chunk.PeekNativeSpan<int>(0, length / 4), Strings.ThunkMap),
                ViewKind.SectionMap                                  => WriteGlobalField(chunk, viewWriter, length, kind, chunk.PeekNativeSpan<SO>(0, length / 8), Strings.SectionMap),
                ViewKind.ImageSeparateDebugHeader                    => Write(new ImageSeparateDebugHeader(chunk),                    viewWriter),

                #region Sections

                ViewKind.drectve                                     => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.text                                        => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.text_mn                                     => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.data                                        => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.idata                                       => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.edata                                       => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.rdata                                       => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.bss                                         => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.rsrc                                        => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.sxdata                                      => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.chks64                                      => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.UnknownSection                              => GetBytes(chunk, viewWriter, length, kind),

                #endregion

                ViewKind.LIBFile_Signature                           => WriteFixedAnsiString(chunk, viewWriter, length, kind),
                ViewKind.ImageArchiveMemberHeader                    => Write(new ImageArchiveMemberHeader(chunk),                    viewWriter),
                ViewKind.FirstLinkerMember                           => Write(new LIB.FirstLinkerMember(chunk),                       viewWriter),
                ViewKind.SecondLinkerMember                          => Write(new LIB.SecondLinkerMember(chunk),                      viewWriter),
                ViewKind.LongNamesMember                             => Write(new LIB.LongNamesMember(chunk),                         viewWriter),
                ViewKind.ShortImportLibrary_DllName                  => WriteAnsiNullTerminated(chunk, viewWriter, kind),
                ViewKind.ShortImportLibrary_ImportName               => WriteAnsiNullTerminated(chunk, viewWriter, kind),
                ViewKind.ImportObjectHeader                          => Write(new ImportObjectHeader(chunk),                          viewWriter),
                ViewKind.OMFDirHeader                                => Write(new OMFDirHeader(chunk),                                viewWriter),
                ViewKind.OMFDirEntry                                 => GetOMFDirEntry(chunk, viewWriter),
                ViewKind.CodeViewSig                                 => viewWriter.NewValue(chunk.AbsoluteOffset, (int) chunk.PeekInt32(0), sizeof(int), kind),
                ViewKind.OMFGlobalTypes                              => GetOMFGlobalTypes(chunk, viewWriter),
                ViewKind.OMFModule                                   => Write(new OMFModule(chunk),                                   viewWriter),
                ViewKind.OMFSegDesc                                  => Write(new OMFSegDesc(chunk),                                  viewWriter),
                ViewKind.OMFSymHash                                  => Write(new OMFSymHash(chunk),                                  viewWriter),
                ViewKind.OMFSourceFile                               => GetOMFSourceFile(chunk, viewWriter),
                ViewKind.OMFSourceLine                               => Write(new OMFSourceLine(chunk),                               viewWriter),
                ViewKind.OMFSourceModule                             => Write(new OMFSourceModule(chunk),                             viewWriter),
                ViewKind.OMFTypeFlags                                => WriteUnmanaged<OMFTypeFlags>(chunk, viewWriter, kind),
                ViewKind.SymHash32                                   => Write((IViewable) OMFDirEntry.SymHash32(chunk, 2), viewWriter),
                ViewKind.SymHash32Long                               => Write((IViewable) OMFDirEntry.SymHash32Long(chunk, 10), viewWriter),
                ViewKind.AddrHash32v4                                => Write((IViewable) OMFDirEntry.AddrHash32(chunk, 4), viewWriter),
                ViewKind.AddrHash32v5                                => Write((IViewable) OMFDirEntry.AddrHash32(chunk, 5), viewWriter),
                ViewKind.AddrHash32v8                                => Write((IViewable) OMFDirEntry.AddrHash32(chunk, 8), viewWriter),
                ViewKind.AddrHash32v12                               => Write((IViewable) OMFDirEntry.AddrHash32(chunk, 12), viewWriter),
                ViewKind.UnknownSymHash                              => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.UnknownAddrHash                             => GetBytes(chunk, viewWriter, length, kind),
                ViewKind.LibraryName                                 => WriteSymString(chunk, viewWriter, length, kind),
                ViewKind.SegmentName                                 => WriteAnsiNullTerminated(chunk, viewWriter, kind),
                ViewKind.LfoDir                                      => viewWriter.NewValue(chunk.AbsoluteOffset, (int) chunk.PeekInt32(0), sizeof(int), kind),
                ViewKind.LfoBase                                     => viewWriter.NewValue(chunk.AbsoluteOffset, (int) chunk.PeekInt32(0), sizeof(int), kind),
                ViewKind.cDir                                        => viewWriter.NewValue(chunk.AbsoluteOffset, (int) chunk.PeekInt32(0), sizeof(int), kind),

                _ => throw new InvalidOperationException($"Don't know how to handle kind '{kind}'")
            };
        }

        private static IView WriteRow(in MemoryChunk chunk, ViewWriter viewWriter, ViewKind kind)
        {
            var ecmaMetadata = GetEcmaMetadata(chunk);

            var heap = ecmaMetadata.CompressedModelHeap;

            return kind switch
            {
                ViewKind.Metadata_String                             => Write(ecmaMetadata.StringHeap.GetString((int) (chunk.AbsoluteOffset - ecmaMetadata.StringHeap.Offset)), viewWriter, kind),
                ViewKind.Metadata_UserString                         => Write(ecmaMetadata.UserStringHeap.GetString((int) (chunk.AbsoluteOffset - ecmaMetadata.UserStringHeap.Offset)), viewWriter),
                ViewKind.Metadata_Blob                               => Write(ecmaMetadata.BlobHeap.GetBlob((int) (chunk.AbsoluteOffset - ecmaMetadata.BlobHeap.Offset)), viewWriter),
                ViewKind.Metadata_Guid                               => Write(ecmaMetadata.GuidHeap.GetGuid((int) (chunk.AbsoluteOffset - ecmaMetadata.GuidHeap.Offset)), viewWriter, kind),
                ViewKind.Metadata_ModuleRow                          => Write(heap.ModuleTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_TypeRefRow                         => Write(heap.TypeRefTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_TypeDefRow                         => Write(heap.TypeDefTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_FieldPtrRow                        => Write(heap.FieldPtrTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_FieldRow                           => Write(heap.FieldTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_MethodPtrRow                       => Write(heap.MethodPtrTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_MethodDefRow                       => Write(heap.MethodDefTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_ParamPtrRow                        => Write(heap.ParamPtrTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_ParamRow                           => Write(heap.ParamTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_InterfaceImplRow                   => Write(heap.InterfaceImplTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_MemberRefRow                       => Write(heap.MemberRefTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_ConstantRow                        => Write(heap.ConstantTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_CustomAttributeRow                 => Write(heap.CustomAttributeTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_FieldMarshalRow                    => Write(heap.FieldMarshalTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_DeclSecurityRow                    => Write(heap.DeclSecurityTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_ClassLayoutRow                     => Write(heap.ClassLayoutTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_FieldLayoutRow                     => Write(heap.FieldLayoutTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_StandAloneSigRow                   => Write(heap.StandAloneSigTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_EventMapRow                        => Write(heap.EventMapTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_EventPtrRow                        => Write(heap.EventPtrTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_EventRow                           => Write(heap.EventTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_PropertyMapRow                     => Write(heap.PropertyMapTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_PropertyPtrRow                     => Write(heap.PropertyPtrTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_PropertyRow                        => Write(heap.PropertyTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_MethodSemanticsRow                 => Write(heap.MethodSemanticsTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_MethodImplRow                      => Write(heap.MethodImplTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_ModuleRefRow                       => Write(heap.ModuleRefTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_TypeSpecRow                        => Write(heap.TypeSpecTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_ImplMapRow                         => Write(heap.ImplMapTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_FieldRvaRow                        => Write(heap.FieldRvaTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_EncLogRow                          => Write(heap.EncLogTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_EncMapRow                          => Write(heap.EncMapTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_AssemblyRow                        => Write(heap.AssemblyTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_AssemblyProcessorRow               => Write(heap.AssemblyProcessorTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_AssemblyOSRow                      => Write(heap.AssemblyOSTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_AssemblyRefRow                     => Write(heap.AssemblyRefTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_AssemblyRefProcessorRow            => Write(heap.AssemblyRefProcessorTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_AssemblyRefOSRow                   => Write(heap.AssemblyRefOSTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_FileRow                            => Write(heap.FileTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_ExportedTypeRow                    => Write(heap.ExportedTypeTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_ManifestResourceRow                => Write(heap.ManifestResourceTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_NestedClassRow                     => Write(heap.NestedClassTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_GenericParamRow                    => Write(heap.GenericParamTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_MethodSpecRow                      => Write(heap.MethodSpecTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_GenericParamConstraintRow          => Write(heap.GenericParamConstraintTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.PortablePdb_DocumentRow                     => Write(heap.DocumentTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.PortablePdb_MethodDebugInformationRow       => Write(heap.MethodDebugInformationTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.PortablePdb_LocalScopeRow                   => Write(heap.LocalScopeTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.PortablePdb_LocalVariableRow                => Write(heap.LocalVariableTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.PortablePdb_LocalConstantRow                => Write(heap.LocalConstantTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.PortablePdb_ImportScopeRow                  => Write(heap.ImportScopeTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.PortablePdb_StateMachineMethodRow           => Write(heap.StateMachineMethodTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
                ViewKind.PortablePdb_CustomDebugInformationRow       => Write(heap.CustomDebugInformationTable.FromOffset((int) chunk.AbsoluteOffset), viewWriter),
            };
        }

        private static IView WriteAnsiNullTerminated(in MemoryChunk chunk, ViewWriter viewWriter, ViewKind kind)
        {
            var str = chunk.PeekAnsiNullTerminatedString(0);
            return viewWriter.NewValue(chunk.AbsoluteOffset, str, str.Length + 1, kind);
        }

        private static IView WriteFixedAnsiString(in MemoryChunk chunk, ViewWriter viewWriter, int length, ViewKind kind)
        {
            var str = chunk.PeekAnsiFixedLength(0, length);
            return viewWriter.NewValue(chunk.AbsoluteOffset, str, str.Length, kind);
        }

        private static IView WriteFixedUtf8String(in MemoryChunk chunk, ViewWriter viewWriter, int length, ViewKind kind)
        {
            var str = chunk.PeekUtf8FixedLength(0, length);
            return viewWriter.NewValue(chunk.AbsoluteOffset, str, str.Length, kind);
        }

        private static IView WriteSymString(in MemoryChunk chunk, ViewWriter viewWriter, int length, ViewKind kind)
        {
            bool isLengthPrefixed;

            switch (kind)
            {
                case ViewKind.LibraryName: //LibraryName is from NB02/NB05 era data; always length prefixed
                    isLengthPrefixed = true;
                    break;

                default:
                    throw new NotImplementedException();
            }

            var str = chunk.PeekSymString(0, isLengthPrefixed);
            return viewWriter.NewValue(chunk.AbsoluteOffset, str, str.Length + 1, kind);
        }

        private static IView WriteGlobalField<T>(in MemoryChunk chunk, ViewWriter viewWriter, int length, ViewKind kind, T value, FixedUtf8String name)
        {
            return new FieldView<T>(chunk.AbsoluteOffset, name.ToString(), value, length, default, viewWriter._fileAccessor, kind);
        }

        private static unsafe IView WriteSymbol(in MemoryChunk chunk, ViewWriter viewWriter)
        {
            var oldOffset = viewWriter.UnmanagedOffset;
            viewWriter.UnmanagedOffset = chunk.AbsoluteOffset;

            try
            {
                return viewWriter.SymTypeDispatcher.Dispatch(new SymType((SYMTYPE*) chunk.Pointer));
            }
            finally
            {
                viewWriter.UnmanagedOffset = oldOffset;
            }
        }

        private static unsafe IView WriteType(in MemoryChunk chunk, ViewWriter viewWriter)
        {
            var oldOffset = viewWriter.UnmanagedOffset;
            viewWriter.UnmanagedOffset = chunk.AbsoluteOffset;

            try
            {
                //I think we should only be writing top level types, which means we're guaranteed to be a TypType
                return viewWriter.TypTypeDispatcher.Dispatch(new TypType((TYPTYPE*) chunk.Pointer));
            }
            finally
            {
                viewWriter.UnmanagedOffset = oldOffset;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IStructView Write<T>(in T value, ViewWriter viewWriter) where T : IViewable =>
            (IStructView) value.WriteStruct(viewWriter)!;

        private static IView Write(RawValue<Utf8String> value, ViewWriter viewWriter, ViewKind viewKind) =>
            viewWriter.NewValue(value.Offset, value.Value, value.Value.Length + 1, viewKind);

        private static IView Write(RawValue<Guid> value, ViewWriter viewWriter, ViewKind viewKind) =>
            viewWriter.NewValue(value.Offset, value.Value, 16, viewKind);

        private static unsafe IView WriteUnmanaged<T>(in MemoryChunk chunk, ViewWriter viewWriter, ViewKind viewKind) where T : unmanaged =>
            viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekUnmanaged<T>(0), sizeof(T), viewKind);

        private static ByteBlobView GetBytes(in MemoryChunk chunk, ViewWriter viewWriter, int length, ViewKind kind)
        {
            return new ByteBlobView(chunk.AbsoluteOffset, chunk.PeekNativeSpan<byte>(0, length), kind, viewWriter._fileAccessor);
        }

        private static IStructView GetBundleFileEntry(in MemoryChunk chunk, ViewWriter viewWriter)
        {
            var peFile = chunk.PEFile();

            var files = peFile.AppHostSignature.BundleHeaderOffset.Value.Files;

            foreach (var file in files)
            {
                if (file.Offset == chunk.AbsoluteOffset)
                    return Write(file, viewWriter);
            }

            throw new InvalidOperationException($"Failed to find the file entry associated with offset '0x{chunk.AbsoluteOffset:X}'");
        }

        private static IStructView GetBundleFileEntryFixed(in MemoryChunk chunk, ViewWriter viewWriter)
        {
            var peFile = chunk.PEFile();

            var files = peFile.AppHostSignature.BundleHeaderOffset.Value.Files;

            foreach (var file in files)
            {
                var header = file.Header;

                if (header.Offset == chunk.AbsoluteOffset)
                    return Write(file, viewWriter);
            }

            throw new InvalidOperationException($"Failed to find the file entry associated with offset '0x{chunk.AbsoluteOffset:X}'");
        }

        private static IStructView GetCoffSymbolTable(in MemoryChunk chunk, ViewWriter viewWriter)
        {
            if (chunk.block is GlobalSubMemoryBlock s)
            {
                var l = (LongImportLibraryMember) s.Owner;

                return Write(l.FileHeader.PointerToSymbolTable.Value, viewWriter);
            }

            var file = chunk.File();

            ImageDebugDirectory[] debugTable;
            int fileOffset;
            bool isLoaded;

            switch (file.Kind)
            {
                case FileKind.PE:
                    //Ostensibly the PointerToSymbolTable should point to the same address as IMAGE_DEBUG_TYPE_COFF.
                    //IMAGE_DEBUG_TYPE_COFF may not exist, but PoitnerToSymbolTable definitely should

                    var peFile = (PEFile) file;

                    var pointerToSymbolTable = peFile.FileHeader.PointerToSymbolTable;

                    if (pointerToSymbolTable.IsValid && pointerToSymbolTable.ActualOffset == chunk.AbsoluteOffset)
                        return Write(pointerToSymbolTable.Value, viewWriter);

                    //Try IMAGE_DEBUG_TYPE_COFF instead

                    debugTable = peFile.DebugTable;
                    fileOffset = peFile.Offset;
                    isLoaded = peFile.IsLoadedImage;
                    break;

                case FileKind.DBG:
                    var dbgFile = (DBGFile) file;

                    debugTable = dbgFile.DebugTable;
                    fileOffset = 0;
                    isLoaded = false;
                    break;

                case FileKind.OBJ:
                    return Write(((OBJFile) file).FileHeader.PointerToSymbolTable.Value, viewWriter);

                default:
                    throw new NotImplementedException();
            }

            var fileRelativeChunkOffset = chunk.AbsoluteOffset - fileOffset;

            for (var i = 0; i < debugTable.Length; i++)
            {
                ref var entry = ref debugTable[i];

                var target = isLoaded ? entry.AddressOfRawData : entry.PointerToRawData;

                if (entry.Type == IMAGE_DEBUG_TYPE.IMAGE_DEBUG_TYPE_COFF)
                {
                    var imageCoffSymbolsHeader = (ImageCoffSymbolsHeader) entry.Data;

                    if (imageCoffSymbolsHeader.LvaToFirstSymbol.IsValid)
                    {
                        var coffSymbolTable = imageCoffSymbolsHeader.LvaToFirstSymbol.Value;

                        if (coffSymbolTable.Offset == chunk.AbsoluteOffset)
                            return Write(coffSymbolTable, viewWriter);
                    }
                }
            }

            throw new NotImplementedException();
        }

        private static IStructView GetDotNetRuntimeDebugHeader(in MemoryChunk chunk, ViewWriter viewWriter)
        {
            var peFile = chunk.PEFile();

            return Write(peFile.DotNetRuntimeDebugHeader, viewWriter);
        }

        private static IStructView GetImageSymbol(in MemoryChunk chunk, ViewWriter viewWriter)
        {
            throw new NotImplementedException();
        }

        private static IStructView GetImageResourceDirectory(in MemoryChunk chunk, ViewWriter viewWriter)
        {
            var peFile = chunk.PEFile();

            var root = peFile.ResourceDirectory;

            if (root.Offset == chunk.AbsoluteOffset)
                return Write(root, viewWriter);

            var queue = new Queue<ImageResourceDirectory>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var directory = queue.Dequeue();

                foreach (var entry in directory.Entries)
                {
                    if (entry.DataIsDirectory && entry.OffsetToDirectory.IsValid)
                    {
                        directory = entry.OffsetToDirectory.Value;

                        if (directory.Offset == chunk.AbsoluteOffset)
                            return Write(directory, viewWriter);

                        queue.Enqueue(directory);
                    }
                }
            }

            throw new InvalidOperationException($"Failed to find the {nameof(ImageResourceDirectory)} associated with address '0x{chunk.AbsoluteOffset:X}'");
        }

        private static IStructView GetImageResourceDirectoryEntry(in MemoryChunk chunk, ViewWriter viewWriter)
        {
            var peFile = chunk.PEFile();

            var root = peFile.ResourceDirectory;

            var queue = new Queue<ImageResourceDirectory>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var directory = queue.Dequeue();

                foreach (var entry in directory.Entries)
                {
                    if (entry.Offset == chunk.AbsoluteOffset)
                        return Write(entry, viewWriter);

                    if (entry.DataIsDirectory && entry.OffsetToDirectory.IsValid)
                    {
                        queue.Enqueue(entry.OffsetToDirectory.Value);
                    }
                }
            }

            throw new NotImplementedException();
        }

        private static IStructView GetImageResourceDataEntry(in MemoryChunk chunk, ViewWriter viewWriter)
        {
            var peFile = chunk.PEFile();

            var root = peFile.ResourceDirectory;

            var queue = new Queue<ImageResourceDirectory>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var directory = queue.Dequeue();

                foreach (var entry in directory.Entries)
                {
                    if (entry.DataIsDirectory)
                    {
                        if (entry.OffsetToDirectory.IsValid)
                            queue.Enqueue(entry.OffsetToDirectory.Value);
                    }
                    else if (entry.OffsetToData.IsValid)
                    {
                        var data = entry.OffsetToData.Value;

                        if (data.Offset == chunk.AbsoluteOffset)
                            return Write(data, viewWriter);
                    }
                }
            }

            throw new NotImplementedException();
        }

        private static IStructView GetImageThunkData(in MemoryChunk chunk, ViewWriter viewWriter)
        {
            var peFile = chunk.PEFile();

            var iat = peFile.OptionalHeader.ImportAddressTableDirectory;

            var isIAT = false;

            if (iat.HasData && peFile.TryGetOffset(iat.VirtualAddress, out var offset))
            {
                if (chunk.AbsoluteOffset >= offset && chunk.AbsoluteOffset <= offset + iat.Size)
                    isIAT = true;
            }

            return Write(new ImageThunkData(chunk, isIAT), viewWriter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IStructView GetModi60Persist(in MemoryChunk chunk, ViewWriter viewWriter)
        {
            //Modi will try and register its symbols if it hasn't already; as such we need to get the existing modi
            var modules = chunk.PDBFile().DBI.Modules;

            foreach (var module in modules)
            {
                if (module.Offset == chunk.AbsoluteOffset)
                    return Write(module, viewWriter);
            }

            throw new NotImplementedException();
        }

        private static IStructView GetOMFDirEntry(in MemoryChunk chunk, ViewWriter viewWriter)
        {
            var nb05Data = GetNB05Data(chunk);

            var entries = nb05Data.DirEntries;

            for (var i = 0; i < entries.Length; i++)
            {
                ref var entry = ref entries[i];

                if (entry.Offset == chunk.AbsoluteOffset)
                    return Write(entry, viewWriter);
            }

            throw new NotImplementedException();
        }

        private static IStructView GetOMFFileIndex(in MemoryChunk chunk, ViewWriter viewWriter)
        {
            var file = chunk.File();

            switch (file.Kind)
            {
                case FileKind.PE:
                    return WriteNB05Data(SST.sstFileIndex, chunk, viewWriter);

                case FileKind.PDB:
                    return Write(((PDBFile) file).DBI.FileInfo, viewWriter);
            }

            throw new NotImplementedException();
        }

        private static IStructView GetOMFGlobalTypes(in MemoryChunk chunk, ViewWriter viewWriter) =>
            WriteNB05Data(SST.sstGlobalTypes, chunk, viewWriter);

        private static IStructView GetOMFSourceFile(in MemoryChunk chunk, ViewWriter viewWriter)
        {
            var nb05Data = GetNB05Data(chunk);

            var entries = nb05Data.DirEntries;

            for (var i = 0; i < entries.Length; i++)
            {
                ref var entry = ref entries[i];

                if (entry.SubSection == SST.sstSrcModule)
                {
                    var data = (OMFSourceModule) entry.Data;

                    var sourceFiles = data.baseSrcFile;

                    for (var j = 0; j < sourceFiles.Length; j++)
                    {
                        ref var sourceFile = ref sourceFiles[j];

                        if (sourceFile.Offset == chunk.AbsoluteOffset)
                            return Write(sourceFile, viewWriter);
                    }
                }
            }

            throw new NotImplementedException();
        }

        private static IStructView WriteNB05Data(SST sst, in MemoryChunk chunk, ViewWriter viewWriter)
        {
            var nb05Data = GetNB05Data(chunk);

            var entries = nb05Data.DirEntries;

            for (var i = 0; i < entries.Length; i++)
            {
                ref var entry = ref entries[i];

                if (entry.SubSection == sst)
                {
                    var data = entry.Data;

                    if (data.Offset == chunk.AbsoluteOffset)
                        return Write((IViewable) data, viewWriter);
                }
            }

            throw new NotImplementedException();
        }

        private static NB05Data GetNB05Data(in MemoryChunk chunk)
        {
            var file = chunk.File();

            switch (file.Kind)
            {
                case FileKind.PE:
                    var debugTable = ((PEFile) file).DebugTable;

                    for (var i = 0; i < debugTable.Length; i++)
                    {
                        ref var entry = ref debugTable[i];

                        if (entry.Type == IMAGE_DEBUG_TYPE.IMAGE_DEBUG_TYPE_CODEVIEW)
                            return (NB05Data) entry.Data;
                    }

                    break;
            }

            throw new NotImplementedException();
        }

        private static IStructView GetEmbeddedPortablePdb(in MemoryChunk chunk, ViewWriter viewWriter)
        {
            var entry = GetDebugDirectory(chunk, viewWriter, IMAGE_DEBUG_TYPE.IMAGE_DEBUG_TYPE_EMBEDDED_PORTABLE_PDB);
            return Write(new EmbeddedPortablePdb(chunk, entry.SizeOfData), viewWriter);
        }

        private static IStructView GetPdbChecksum(in MemoryChunk chunk, ViewWriter viewWriter)
        {
            var entry = GetDebugDirectory(chunk, viewWriter, IMAGE_DEBUG_TYPE.IMAGE_DEBUG_TYPE_PDB_CHECKSUM);
            return Write(new PdbChecksum(chunk, entry.SizeOfData), viewWriter);
        }

        private static IStructView GetPogoData(in MemoryChunk chunk, ViewWriter viewWriter)
        {
            var entry = GetDebugDirectory(chunk, viewWriter, IMAGE_DEBUG_TYPE.IMAGE_DEBUG_TYPE_POGO);
            return Write(new PogoData(chunk, entry.SizeOfData), viewWriter);
        }

        private static ImageDebugDirectory GetDebugDirectory(in MemoryChunk chunk, ViewWriter viewWriter, IMAGE_DEBUG_TYPE type)
        {
            var file = chunk.File();

            ImageDebugDirectory[] debugTable;
            int fileOffset;
            bool isLoaded;

            switch (file.Kind)
            {
                case FileKind.PE:
                    var peFile = (PEFile) file;
                    debugTable = peFile.DebugTable;
                    fileOffset = peFile.Offset;
                    isLoaded = peFile.IsLoadedImage;
                    break;

                case FileKind.DBG:
                    var dbgFile = (DBGFile) file;
                    debugTable = dbgFile.DebugTable;
                    fileOffset = 0;
                    isLoaded = false;
                    break;

                default:
                    throw new NotImplementedException();
            }

            //Regardless of whether we're presenting as virtual or not, the MemoryChunk is going to be virtual based if we're loaded
            //and physical based if we're not

            var fileRelativeChunkOffset = chunk.AbsoluteOffset - fileOffset;

            for (var i = 0; i < debugTable.Length; i++)
            {
                ref var entry = ref debugTable[i];

                var target = isLoaded ? entry.AddressOfRawData : entry.PointerToRawData;

                if (entry.Type == type && target == fileRelativeChunkOffset)
                {
                    return entry;
                }
            }

            throw new NotImplementedException();
        }

        private static IStructView GetRichHeader(in MemoryChunk chunk, ViewWriter viewWriter)
        {
            //Rich Header is special in that you must decode it; you can't just construct it from a memory chunk

            var peFile = chunk.PEFile();
            var richHeader = peFile.RichHeader;
            Debug.Assert(richHeader.Offset == chunk.AbsoluteOffset);

            //RichHeader is a reference type so this is OK
            return (IStructView) ((IViewable) richHeader).WriteStruct(viewWriter);
        }

        private static IStructView GetStorageHeader(in MemoryChunk chunk, ViewWriter viewWriter)
        {
            var ecmaMetadata = GetEcmaMetadata(chunk);

            return Write(ecmaMetadata.Header, viewWriter);
        }

        private static EcmaMetadata GetEcmaMetadata(in MemoryChunk chunk)
        {
            var file = chunk.File();

            if (file.Kind == FileKind.PE)
            {
                //There are several locations in PE Files that contain EcmaMetadata, depending on whether it's a managed, NGEN or single file app

                var peFile = (PEFile) file;
                var fileRelativeChunkOffset = chunk.AbsoluteOffset - peFile.Offset;

                //todo: make virtual
                Debug.Assert(!peFile.IsLoadedImage);

                var cor20Header = peFile.Cor20Header;

                if (cor20Header != null && peFile.TryGetDirectoryOffset(cor20Header.Metadata, out var offset, false) && (fileRelativeChunkOffset >= offset && fileRelativeChunkOffset < offset + cor20Header.Metadata.Size))
                {
                    return peFile.EcmaMetadata;
                }

                var ngenHeader = peFile.NgenHeader;

                if (ngenHeader != null && peFile.TryGetDirectoryOffset(ngenHeader.ManifestMetaData, out offset, false) && (fileRelativeChunkOffset >= offset && fileRelativeChunkOffset < offset + ngenHeader.ManifestMetaData.Size))
                {
                    throw new NotImplementedException();
                }

                //If we're a nested file, we should not be given the top level PEFile; the caller should have given us the correct PEFile
                throw new InvalidOperationException($"Failed to find the {nameof(EcmaMetadata)} associated with offset '0x{chunk.AbsoluteOffset:X}'");
            }
            else if (file.Kind == FileKind.PortablePDB)
            {
                throw new NotImplementedException();
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private static IStructView GetStreamTable(in MemoryChunk chunk, ViewWriter viewWriter)
        {
            //Is it the main stream table or the previous stream table?

            var pdbFile = chunk.PDBFile();

            var streamTable = pdbFile.StreamTable;

            if (streamTable.Offset != chunk.AbsoluteOffset)
            {
                streamTable = pdbFile.PreviousStreamTable;

                if (streamTable == null || streamTable.Offset != chunk.AbsoluteOffset)
                    throw new InvalidOperationException($"Failed to find the {nameof(IStreamTable)} associated with offset '0x{chunk.AbsoluteOffset:X}'");
            }

            return Write(streamTable, viewWriter);
        }
    }
}
