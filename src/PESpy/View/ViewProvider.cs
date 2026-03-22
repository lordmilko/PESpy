using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ClrDebug.PDB;
using PESpy.PDB;
using PESpy.View;

namespace PESpy
{
    
        public static unsafe IView CreateStructView(ViewKind kind, in MemoryChunk chunk, ViewWriter viewWriter, bool isSplit = false)
        {
            return kind switch
            {
                #region Headers

                ViewKind.ImageDosHeader                              => Write(new ImageDosHeader(chunk),                              viewWriter),
                ViewKind.RichHeader                                  => GetRichHeader(chunk, viewWriter),
                ViewKind.ImageNtHeaders                              => Write(new ImageNtHeaders(chunk),                              viewWriter),
                ViewKind.ImageFileHeader                             => Write(new ImageFileHeader(chunk),                             viewWriter),
                ViewKind.ImageOptionalHeader                         => Write(new ImageOptionalHeader(chunk),                         viewWriter),
                ViewKind.ImageDataDirectory                          => Write(new ImageDataDirectory(chunk),                          viewWriter),
                ViewKind.ImageSectionHeader                          => Write(new ImageSectionHeader(chunk),                          viewWriter),
                ViewKind.ImageRelocation                             => Write(new ImageRelocation(chunk),                             viewWriter),
                //ViewKind.CoffSymbolTable                             => Write(new CoffSymbolTable(chunk),                             viewWriter),
                //ViewKind.ImageSymbol                                 => Write(new ImageSymbol(chunk),                                 viewWriter),
                ViewKind.ImageAuxSymbol                              => Write(new ImageAuxSymbol(chunk),                              viewWriter),
                ViewKind.ImageLineNumber                             => Write(new ImageLineNumber(chunk),                             viewWriter),
                ViewKind.AnonObjectHeader                            => Write(new AnonObjectHeader(chunk),                            viewWriter),

                #endregion
                #region Exports Table (0)

                ViewKind.ImageExportDirectory                        => Write(new ImageExportDirectory(chunk),                        viewWriter),
                ViewKind.ImageExportDirectory_AddressOfNameOrdinals_Entry => viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekInt32(0), sizeof(int), kind),
                ViewKind.ImageExportDirectory_AddressOfNames_Entry   => viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekInt32(0), sizeof(int), kind),
                ViewKind.ImageExportDirectory_AddressOfFunctions_Entry => viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekInt32(0), sizeof(int), kind),

                #endregion
                #region Import Table (1)

                ViewKind.ImageImportDescriptor                       => Write(new ImageImportDescriptor(chunk),                       viewWriter),
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

                #endregion
                #region Exception Table (3)

                ViewKind.RuntimeFunction                             => Write(new RuntimeFunction(chunk),                             viewWriter),
                ViewKind.UnwindInfo                                  => Write(new UnwindInfo(chunk),                                  viewWriter),
                ViewKind.ScopeTable                                  => Write(new ScopeTable(chunk),                                  viewWriter),
                ViewKind.ScopeRecord                                 => Write(new ScopeTable.ScopeRecord(chunk),                      viewWriter),
                ViewKind.FuncInfo                                    => Write(new FuncInfo(chunk),                                    viewWriter),
                ViewKind.FuncInfoV1                                  => Write(new FuncInfoV1(chunk),                                  viewWriter),
                //ViewKind.FuncInfoHeader                              => Write(new FuncInfoHeader(chunk),                              viewWriter),
                ViewKind.HandlerType                                 => Write(new HandlerType(chunk),                                 viewWriter),
                ViewKind.IptoStateMapEntry                           => Write(new IptoStateMapEntry(chunk),                           viewWriter),
                ViewKind.TryBlockMapEntry                            => Write(new TryBlockMapEntry(chunk),                            viewWriter),
                ViewKind.TypeDescriptor                              => Write(new TypeDescriptor(chunk),                              viewWriter),
                ViewKind.UnwindMapEntry                              => Write(new UnwindMapEntry(chunk),                              viewWriter),

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
                ViewKind.VCFeature                                   => Write(new VCFeature(chunk),                                   viewWriter),
                ViewKind.PogoData                                    => GetPogoData(chunk, viewWriter),
                ViewKind.PogoItem                                    => Write(new PogoItem(chunk),                                    viewWriter),
                ViewKind.Reproducible                                => Write(new Reproducible(chunk),                                viewWriter),
                //ViewKind.EmbeddedPortablePdb                         => Write(new EmbeddedPortablePdb(chunk),                         viewWriter),
                ViewKind.PdbChecksum                                 => GetPdbChecksum(chunk, viewWriter),
                ViewKind.ExDllCharacteristics                        => viewWriter.NewValue(chunk.AbsoluteOffset, (IMAGE_DLLCHARACTERISTICS_EX) chunk.PeekUInt32(0), sizeof(int), kind),

                #endregion

                #region Load Config Table (10)

                ViewKind.ImageLoadConfigDirectory                    => Write(new ImageLoadConfigDirectory(chunk),                    viewWriter),
                ViewKind.ImageLoadConfigCodeIntegrity                => Write(new ImageLoadConfigCodeIntegrity(chunk),                viewWriter),
                ViewKind.ImageEnclaveConfig                          => Write(new ImageEnclaveConfig(chunk),                          viewWriter),
                ViewKind.ImageEnclaveImport                          => Write(new ImageEnclaveImport(chunk),                          viewWriter),
                ViewKind.GuardAddressTakenIatEntryTable              => Write(chunk.PEFile().LoadConfigTable.GuardAddressTakenIatEntryTable.Value, viewWriter),
                ViewKind.GuardCFFunctionTable                        => GetGuardCFFunctionTable(chunk, viewWriter),
                ViewKind.GuardEHContinuationTable                    => Write(chunk.PEFile().LoadConfigTable.GuardEHContinuationTable.Value, viewWriter),
                ViewKind.GuardLongJumpTargetTable                    => Write(chunk.PEFile().LoadConfigTable.GuardLongJumpTargetTable.Value, viewWriter),
                ViewKind.XFG                                         => viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekUInt64(0), sizeof(long), kind),
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
                ViewKind.ImageBoundForwarderRef                      => Write(new ImageBoundForwarderRef(chunk),                      viewWriter),

                #endregion
                #region Delay Import Table (13)

                ViewKind.ImageDelayLoadDescriptor                    => Write(new ImageDelayLoadDescriptor(chunk),                    viewWriter),

                #endregion
                #region CorHeader Directory (14)

                ViewKind.ImageCor20Header                            => Write(new ImageCor20Header(chunk),                            viewWriter),
                ViewKind.ImageCorILMethodTiny                        => Write(new ImageCorILMethod(chunk),                            viewWriter),
                ViewKind.ImageCorILMethodFat                         => Write(new ImageCorILMethod(chunk),                            viewWriter),
                //ViewKind.ImageCorILMethodSect                        => Write(new ImageCorILMethodSect(chunk),                        viewWriter),
                //ViewKind.ImageCorILMethodSectEHClause                => Write(new ImageCorILMethodSectEHClause(chunk),                viewWriter),
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

                ViewKind.ReadyToRunHeader                            => Write(new ReadyToRunHeader(chunk),                            viewWriter),
                ViewKind.ReadyToRunCoreHeader                        => Write(new ReadyToRunCoreHeader(chunk),                        viewWriter),
                ViewKind.ReadyToRunSection                           => Write(new ReadyToRunSection(chunk),                           viewWriter),
                ViewKind.ReadyToRunImportSection                     => Write(new ReadyToRunImportSection(chunk),                     viewWriter),

                #endregion

                //ViewKind.AppHostSignature                            => Write(new AppHostSignature(chunk),                            viewWriter),
                ViewKind.BundleManifest                              => Write(new Bundle.Manifest(chunk),                             viewWriter),
                ViewKind.BundleHeaderFixed                           => Write(new Bundle.HeaderFixed(chunk),                          viewWriter),
                ViewKind.BundleHeaderFixedV2                         => Write(new Bundle.HeaderFixedV2(chunk),                        viewWriter),
                //ViewKind.BundleFileEntry                             => Write(new Bundle.FileEntry(chunk),                            viewWriter),
                //ViewKind.BundleFileEntryFixed                        => Write(new Bundle.FileEntryFixed(chunk),                       viewWriter),
                ViewKind.BundleLocation                              => Write(new Bundle.Location(chunk),                             viewWriter),
                //ViewKind.BundleEncodedString                         => Write(new BundleEncodedString(chunk),                         viewWriter),
                ViewKind.DebugTypeEntry                              => Write(new DebugTypeEntry(chunk),                              viewWriter),
                ViewKind.GlobalValueEntry                            => Write(new GlobalValueEntry(chunk),                            viewWriter),

                #endregion
                #region RTTI

                //ViewKind.RTTIBaseClassArray                          => Write(new RTTIBaseClassArray(chunk),                          viewWriter),
                //ViewKind.RTTIClassHierarchyDescriptor                => Write(new RTTIClassHierarchyDescriptor(chunk),                viewWriter),
                //ViewKind.RTTICompleteObjectLocator                   => Write(new RTTICompleteObjectLocator(chunk),                   viewWriter),

                #endregion

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
                ViewKind.SectionContribsV40                          => Write((PDB.SectionContribsV40) chunk.PDBFile().DBI.SectionContribs, viewWriter),
                ViewKind.SectionContribsV60                          => Write((PDB.SectionContribsV60) chunk.PDBFile().DBI.SectionContribs, viewWriter),
                ViewKind.OMFSegMap                                   => Write(new OMFSegMap(chunk),                                   viewWriter),
                ViewKind.OMFFileIndex                                => GetOMFFileIndex(chunk, viewWriter),
                ViewKind.NameTable                                   => Write(new PDB.NMT(chunk),                                     viewWriter),
                ViewKind.DbgDataHdr                                  => Write(chunk.PDBFile().DBI.DbgHdr, viewWriter),
                ViewKind.CvSignature                                 => viewWriter.NewValue(chunk.AbsoluteOffset, (CV_SIGNATURE) chunk.PeekUInt32(0), sizeof(int), kind),

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

                ViewKind.PSGSIHDR                                    => Write(new PDB.PSGSIHDR(chunk),                                viewWriter),

                _ => throw new InvalidOperationException($"Don't know how to handle kind '{kind}'")
            };
        }

        private static IStructView WriteRow(in MemoryChunk chunk, ViewWriter viewWriter, ViewKind kind)
        {
            var peFile = chunk.PEFile();

            var heap = peFile.EcmaMetadata.CompressedModelHeap;

            return kind switch
            {
                ViewKind.Metadata_ModuleRow                          => Write(heap.ModuleTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_TypeRefRow                         => Write(heap.TypeRefTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_TypeDefRow                         => Write(heap.TypeDefTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_FieldPtrRow                        => Write(heap.FieldPtrTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_FieldRow                           => Write(heap.FieldTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_MethodPtrRow                       => Write(heap.MethodPtrTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_MethodDefRow                       => Write(heap.MethodDefTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_ParamPtrRow                        => Write(heap.ParamPtrTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_ParamRow                           => Write(heap.ParamTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_InterfaceImplRow                   => Write(heap.InterfaceImplTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_MemberRefRow                       => Write(heap.MemberRefTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_ConstantRow                        => Write(heap.ConstantTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_CustomAttributeRow                 => Write(heap.CustomAttributeTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_FieldMarshalRow                    => Write(heap.FieldMarshalTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_DeclSecurityRow                    => Write(heap.DeclSecurityTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_ClassLayoutRow                     => Write(heap.ClassLayoutTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_FieldLayoutRow                     => Write(heap.FieldLayoutTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_StandAloneSigRow                   => Write(heap.StandAloneSigTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_EventMapRow                        => Write(heap.EventMapTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_EventPtrRow                        => Write(heap.EventPtrTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_EventRow                           => Write(heap.EventTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_PropertyMapRow                     => Write(heap.PropertyMapTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_PropertyPtrRow                     => Write(heap.PropertyPtrTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_PropertyRow                        => Write(heap.PropertyTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_MethodSemanticsRow                 => Write(heap.MethodSemanticsTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_MethodImplRow                      => Write(heap.MethodImplTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_ModuleRefRow                       => Write(heap.ModuleRefTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_TypeSpecRow                        => Write(heap.TypeSpecTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_ImplMapRow                         => Write(heap.ImplMapTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_FieldRvaRow                        => Write(heap.FieldRvaTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_EncLogRow                          => Write(heap.EncLogTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_EncMapRow                          => Write(heap.EncMapTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_AssemblyRow                        => Write(heap.AssemblyTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_AssemblyProcessorRow               => Write(heap.AssemblyProcessorTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_AssemblyOSRow                      => Write(heap.AssemblyOSTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_AssemblyRefRow                     => Write(heap.AssemblyRefTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_AssemblyRefProcessorRow            => Write(heap.AssemblyRefProcessorTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_AssemblyRefOSRow                   => Write(heap.AssemblyRefOSTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_FileRow                            => Write(heap.FileTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_ExportedTypeRow                    => Write(heap.ExportedTypeTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_ManifestResourceRow                => Write(heap.ManifestResourceTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_NestedClassRow                     => Write(heap.NestedClassTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_GenericParamRow                    => Write(heap.GenericParamTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_MethodSpecRow                      => Write(heap.MethodSpecTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.Metadata_GenericParamConstraintRow          => Write(heap.GenericParamConstraintTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.PortablePdb_DocumentRow                     => Write(heap.DocumentTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.PortablePdb_MethodDebugInformationRow       => Write(heap.MethodDebugInformationTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.PortablePdb_LocalScopeRow                   => Write(heap.LocalScopeTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.PortablePdb_LocalVariableRow                => Write(heap.LocalVariableTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.PortablePdb_LocalConstantRow                => Write(heap.LocalConstantTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.PortablePdb_ImportScopeRow                  => Write(heap.ImportScopeTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.PortablePdb_StateMachineMethodRow           => Write(heap.StateMachineMethodTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
                ViewKind.PortablePdb_CustomDebugInformationRow       => Write(heap.CustomDebugInformationTable.FromOffset(chunk.AbsoluteOffset), viewWriter),
            };
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
}
