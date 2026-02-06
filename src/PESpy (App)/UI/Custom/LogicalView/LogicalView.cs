using System;
using System.Diagnostics;
using PESpy.View;
using PESpy.PDB;
using static PESpy.UI.ImageKind;
using static PESpy.IMAGE_DEBUG_TYPE;
using PInvoke;

namespace PESpy.UI
{
    internal class LogicalView : NativeTreeView
    {
        public LogicalView(out LogicalView field)
        {
            field = this;

            App.FileOpened += App_FileOpened;
        }

        internal void App_FileOpened(object? sender, FileOpenedEventArgs e)
        {
            if (e.EventKind == FileOpenedEventKind.OpenAccessor)
            {
                var fileAccessor = App.FileAccessor;

                if (fileAccessor == null)
                    return;

                NativeTreeNode root;

                switch (fileAccessor.File.Kind)
                {
                    case FileKind.PE:
                        root = BuildPEFileTree(fileAccessor);
                        break;

                    case FileKind.PDB:
                        root = BuildPDBFileTree((PDBFile) fileAccessor.File);
                        break;
        internal static unsafe NativeTreeNode BuildPEFileTree(PEFile peFile)
        {
            var peFile = (PEFile) fileAccessor.File;

            using var level1 = new PooledList<NativeTreeNode>();
            using var level2 = new PooledList<NativeTreeNode>();
            using var level3 = new PooledList<NativeTreeNode>();

            #region Overview / Headers

            //Add top-level nodes
            level1.Add(SpecialPaneNode("Overview", ImageEditAlignment, LogicalTreeNodeKind.Overview));
            level1.Add(SingletonNode("IMAGE_DOS_HEADER", ImageStruct, peFile.DosHeader.Offset, ViewKind.ImageDosHeader));
            level1.Add(CodeNode("DOS Stub", ImageDocumentBinary, peFile.DosStub.Offset));

            var richHeader = peFile.RichHeader;

            if (richHeader != null)
            {
                level1.Add(SingletonNode("Rich Header", ImageMoneyCoin, richHeader.Offset, ViewKind.RichHeader,
                    ListNode("PRODITEM", ImageStructStack, richHeader.Items, ViewKind.ProdItem)
                ));
            }

            var ntHeaders = peFile.NtHeaders;
            var optionalHeader = ntHeaders.OptionalHeader;

            level1.Add(
                SingletonNode("IMAGE_NT_HEADERS", ImageStruct, ntHeaders.Offset, ViewKind.ImageNtHeaders,
                    SingletonNode("IMAGE_FILE_HEADER", ImageStruct, ntHeaders.FileHeader.Offset, ViewKind.ImageFileHeader),
                    SingletonNode("IMAGE_OPTIONAL_HEADER", ImageStruct, optionalHeader.Offset, ViewKind.ImageOptionalHeader,
                        ListNode("IMAGE_DATA_DIRECTORY", ImageStructStack, optionalHeader.Offset + optionalHeader.ExportTableDirectoryOffset, optionalHeader.NumberOfRvaAndSizes, ViewKind.ImageDataDirectory)
                    )
                )
            );

            level1.Add(ListNode("IMAGE_SECTION_HEADER", ImageStructStack, peFile.SectionHeaders, ViewKind.ImageSectionHeader));

            #endregion
            #region Directories

            #region Export Table (0)

            var exports = peFile.ExportTable;

            if (exports != null)
            {
                level2.Add(
                    FolderNode("00: Export Table", ImageFolderStruct,
                        SingletonNode("IMAGE_EXPORT_DIRECTORY", ImageStruct, exports.Offset, ViewKind.ImageExportDirectory)
                    )
                );
            }

            #endregion
            #region Import Table (1)

            var imports = peFile.ImportTable;

            if (imports != null)
            {
                level2.Add(
                    FolderNode("01: Import Table", ImageFolderStruct,
                        ListNode("IMAGE_IMPORT_DESCRIPTOR", ImageStructStack, imports, ViewKind.ImageImportDescriptor)
                    )
                );
            }

            #endregion
            #region Resource Directory (2)

            var resources = peFile.ResourceDirectory;

            if (resources != null)
            {
                level2.Add(
                    FolderNode("02: Resource Directory", ImageFolderStruct)
                );
            }

            #endregion
            #region Exception Table (3)

            var exceptionTable = peFile.ExceptionTable;

            if (exceptionTable != null && exceptionTable.Count > 0)
            {
                level2.Add(FolderNode("03: Exception Table", ImageFolderStruct,
                    ListNode("RUNTIME_FUNCTION", ImageStructStack, exceptionTable[0].Offset, exceptionTable.Count, ViewKind.RuntimeFunction)
                ));
            }

            #endregion
            #region Security Table (4)

            var securityTable = peFile.SecurityTable;

            if (securityTable != null)
            {
                level2.Add(FolderNode("04: Security Table", ImageFolderStruct,
                    ListNode("WIN_CERTIFICATE", ImageStructStack, securityTable, ViewKind.WinCertificate)
                ));
            }

            #endregion
            #region Base Relocation Table (5)

            var baseRelocationTable = peFile.BaseRelocationTable;

            if (baseRelocationTable != null)
            {
                level2.Add(
                    FolderNode("05: Base Relocation Table", ImageFolderStruct,
                        ListNode("IMAGE_BASE_RELOCATION", ImageStructStack, baseRelocationTable, ViewKind.ImageBaseRelocation)
                    )
                );
            }

            #endregion
            #region Debug Table (6)

            var debugTable = peFile.DebugTable;

            if (debugTable != null)
            {
                //We want to add children for each struct found under the debug table

                for (var i = 0; i < debugTable.Length; i++)
                {
                    ref var debugDirectory = ref debugTable[i];

                    //We should have written a global that says what this type of data is

                    var entity = fileAccessor.GetEntity(peFile.IsLoadedImage ? debugDirectory.AddressOfRawData : debugDirectory.PointerToRawData);

                    FixedUtf8String name = default; //temp: need to lookup using viewkind
                    int targetAddress;
                    ViewKind viewKind;
                    ViewByteDataKind dataKind;

                    if (entity.ViewByte->Kind != ViewByteKind.Data)
                    {
                        //If we're in the middle of doing analysis, we may not have tagged the meaning of this data yet, in which case we need to manually inspect it

                        var data = debugDirectory.Data;

                        targetAddress = fileAccessor.IsLoaded ? debugDirectory.AddressOfRawData : debugDirectory.PointerToRawData;

                        switch (debugDirectory.Type)
                        {
                            case IMAGE_DEBUG_TYPE_COFF:
                                viewKind = ViewKind.ImageCoffSymbolsHeader;
                                dataKind = ViewByteDataKind.Struct;
                                break;

                            case IMAGE_DEBUG_TYPE_CODEVIEW:
                                dataKind = ViewByteDataKind.Struct;
                                switch (((ICodeViewData) data).Signature)
                                {
                                    case CodeViewSig.NB10:
                                        viewKind = ViewKind.NB10I;
                                        name = Strings.NB10I;
                                        break;
                            case IMAGE_DEBUG_TYPE_FPO:
                                name = Strings.FPO_DATA;
                                viewKind = ViewKind.FpoData;
                                dataKind = ViewByteDataKind.Struct;
                                break;

                            case IMAGE_DEBUG_TYPE_MISC:
                                name = Strings.IMAGE_DEBUG_MISC;
                                viewKind = ViewKind.ImageDebugMisc;
                                dataKind = ViewByteDataKind.Struct;
                                break;
                            case IMAGE_DEBUG_TYPE_VC_FEATURE:
                                name = Strings.VCFeature;
                                viewKind = ViewKind.VCFeature;
                                dataKind = ViewByteDataKind.Struct;
                                break;
                            case IMAGE_DEBUG_TYPE_PDB_CHECKSUM:
                                viewKind = ViewKind.PdbChecksum;
                                dataKind = ViewByteDataKind.Struct;
                                name = Strings.PdbChecksum;
                                break;

                            case IMAGE_DEBUG_TYPE_EX_DLLCHARACTERISTICS:
                                viewKind = ViewKind.ExDllCharacteristics;
                                dataKind = ViewByteDataKind.Enum;
                                break;
                    }
                    else
                    {
                        targetAddress = entity.TargetAddress;
                        viewKind = entity.Kind;
                        dataKind = ViewByteDataKind.Struct;
                        name = entity.Name;
                    }

                    if (dataKind == ViewByteDataKind.Struct)
                    {
                        switch (debugDirectory.Type)
                        {
                            //Single struct
                            case IMAGE_DEBUG_TYPE_COFF:
                            case IMAGE_DEBUG_TYPE_CODEVIEW:
                            case IMAGE_DEBUG_TYPE_MISC:
                            case IMAGE_DEBUG_TYPE_VC_FEATURE:
                            case IMAGE_DEBUG_TYPE_EMBEDDED_PORTABLE_PDB:
                            case IMAGE_DEBUG_TYPE_PDB_CHECKSUM:
                                level3.Add(SingletonNode(name.ToString(), ImageStruct, targetAddress, viewKind));
                                break;

                            //Synthetic
                            case IMAGE_DEBUG_TYPE_POGO:
                            case IMAGE_DEBUG_TYPE_REPRO:
                                level3.Add(SingletonNode(name.ToString(), ImageSynthetic, targetAddress, viewKind));
                                break;
            #region Global Pointer Table (8)

            //I've read some indications this may be specific to certain RISC architectures or something?

            //08: Global Pointer Table
            Debug.Assert(!optionalHeader.GlobalPointerTableDirectory.HasData);

            #endregion
            #region Thread Local Storage Table (9)

            var tlsDirectory = peFile.TlsDirectory;

            if (tlsDirectory != null)
            {
                level2.Add(
                    FolderNode("09: Thread Local Storage Table", ImageFolderStruct,
                        SingletonNode("IMAGE_TLS_DIRECTORY", ImageStruct, tlsDirectory.Offset, ViewKind.ImageTlsDirectory)
                    )
                );
            }

            #endregion
                if (loadConfigTable.SEHandlerTable.IsValid)
                    level3.Add(SingletonNode("___safe_se_handler_table", ImageStructStack, loadConfigTable.SEHandlerTable.ActualOffset, ViewKind.SEHandlerTable));

                if (loadConfigTable.GuardCFFunctionTable.IsValid)
                    level3.Add(SingletonNode("__guard_fids_table", ImageStructStack, loadConfigTable.GuardCFFunctionTable.ActualOffset, ViewKind.GuardCFFunctionTable));

                var codeIntegrity = loadConfigTable.CodeIntegrity;

                if (codeIntegrity.Offset != 0)
                    level3.Add(SingletonNode("IMAGE_LOAD_CONFIG_CODE_INTEGRITY", ImageStruct, codeIntegrity.Offset, ViewKind.ImageLoadConfigCodeIntegrity));

                if (loadConfigTable.GuardAddressTakenIatEntryTable.IsValid)
                    level3.Add(SingletonNode("__guard_iat_table", ImageStructStack, loadConfigTable.GuardAddressTakenIatEntryTable.ActualOffset, ViewKind.GuardAddressTakenIatEntryTable));

                if (loadConfigTable.GuardLongJumpTargetTable.IsValid)
                    level3.Add(SingletonNode("__guard_longjmp_table", ImageStructStack, loadConfigTable.GuardLongJumpTargetTable.ActualOffset, ViewKind.GuardLongJumpTargetTable));

                if (loadConfigTable.DynamicValueRelocTableOffset.IsValid)
                    level3.Add(SingletonNode("IMAGE_DYNAMIC_RELOCATION_TABLE", ImageStruct, loadConfigTable.DynamicValueRelocTableOffset.ActualOffset, ViewKind.ImageDynamicRelocationTable));

                if (loadConfigTable.EnclaveConfigurationPointer.IsValid)
                    throw temp(loadConfigTable.EnclaveConfigurationPointer.ActualOffset);

                if (loadConfigTable.GuardEHContinuationTable.IsValid)
                    level3.Add(SingletonNode("__guard_eh_cont_table", ImageStructStack, loadConfigTable.GuardEHContinuationTable.ActualOffset, ViewKind.GuardEHContinuationTable));

                level2.Add(
                    FolderNode("10: Load Config Table", ImageFolderStruct,
                        SingletonNode("IMAGE_LOAD_CONFIG_DIRECTORY", ImageStruct, loadConfigTable.Offset, ViewKind.ImageLoadConfigDirectory, level3.ToArrayAndClear())
                    )
                );
            }

            #endregion
            #region Bound Import Table (11)

            var boundImportTable = peFile.BoundImportTable;

            if (boundImportTable != null)
            {
                level2.Add(FolderNode("11: Bound Import Table", ImageFolderStruct,
                    ListNode("IMAGE_BOUND_IMPORT_DESCRIPTOR", ImageStructStack, boundImportTable, ViewKind.ImageBoundImportDescriptor)
                ));
            }

            #endregion
            #region Import Address Table (12)

            var importAddressTable = peFile.ImportAddressTable;

            if (importAddressTable != null)
            {
                level2.Add(
                    FolderNode("12: Import Address Table", ImageFolderStruct)
                );
            }

            #endregion
            #region Delay Import Table (13)

            var delayImportTable = peFile.DelayImportTable;

            if (delayImportTable != null)
            {
                level2.Add(
                    FolderNode("13: Delay Import Table", ImageFolderStruct,
                        ListNode("IMAGE_DELAYLOAD_DESCRIPTOR", ImageStructStack, delayImportTable, ViewKind.ImageDelayLoadDescriptor)
                    )
                );
            }

            #endregion
            #region Cor20Header (14)

            var cor20Header = peFile.Cor20Header;

            if (cor20Header != null)
            {
                level2.Add(
                    FolderNode("14: COM Descriptor Directory", ImageFolderStruct,
                        SingletonNode("IMAGE_COR20_HEADER", ImageStruct, cor20Header.Offset, ViewKind.ImageCor20Header)
                    )
                );
            }

            #endregion

            if (level2.Count > 0)
            {
                level1.Add(FolderNode("Directories", ImageFolders, level2.ToArrayAndClear()));
            }

            #endregion
            #region .NET

            //If we're not .NET, we won't end up adding anything

            #region Cor20Header

            if (cor20Header != null)
            {
                var cor20Resources = peFile.Cor20Resources;

                if (cor20Resources != null)
                {
                    level2.Add(FolderNode("Resources", ImageFolderStruct));
                }

                var cor20StrongNameSignature = peFile.Cor20StrongNameSignature;

                if (cor20StrongNameSignature != null)
                {
                    level2.Add(FolderNode("Strong Name Signature", ImageFolderStruct));
                }

                var cor20CodeManagerTable = peFile.Cor20CodeManagerTable;

                if (cor20CodeManagerTable != null)
                {
                    level2.Add(
                        FolderNode("Code Manager Table", ImageFolderStruct)
                    );
                }

                var cor20VTableFixups = peFile.Cor20VTableFixups;

                if (cor20VTableFixups != null)
                {
                    level2.Add(
                        FolderNode("VTable Fixups", ImageFolderStruct,
                            ListNode("IMAGE_COR_VTABLEFIXUP", ImageStructStack, cor20VTableFixups, ViewKind.ImageCorVTableFixup)
                        )
                    );
                }

                var cor20ExportAddressTableJumps = peFile.Cor20ExportAddressTableJumps;

                if (cor20ExportAddressTableJumps != null)
                {
                    level2.Add(
                        FolderNode("Export Address Table Jumps", ImageFolderStruct)
                    );
                }
            }

            #endregion

            var ilMethods = peFile.ILMethods;

            if (ilMethods != null)
            {
                level2.Add(
                    FolderNode("IMAGE_COR_ILMETHOD", ImageStructStack)
                );
            }

            #region NGEN

            var ngenHeader = peFile.NgenHeader;

            if (ngenHeader != null)
            {
                level3.Add(SingletonNode("CORCOMPILE_HEADER", ImageStruct, ngenHeader.Offset, ViewKind.CorCompileHeader));

                var ngenHelperTable = peFile.NgenHelperTable;

                if (ngenHelperTable != null)
                {
                    level3.Add(
                        FolderNode("Helper Table", ImageFolderStruct,
                            ListNode("Helper Table Entries", ImageStructStack, ngenHelperTable, ViewKind.NgenHelperEntry)
                        )
                    );
                }

                var ngenImportSections = peFile.NgenImportSections;

                if (ngenImportSections != null)
                {
                    level3.Add(
                        FolderNode("Import Sections", ImageFolderStruct,
                            ListNode("CORCOMPILE_IMPORT_SECTION", ImageStructStack, ngenImportSections, ViewKind.CorCompileHeader)
                        )
                    );
                }

                var ngenImportTable = peFile.NgenImportTable;

                if (ngenImportTable != null)
                {
                    level3.Add(
                        FolderNode("Import Table", ImageFolderStruct,
                            ListNode("CORCOMPILE_IMPORT_TABLE_ENTRY", ImageStructStack, ngenImportTable, ViewKind.CorCompileImportTableEntry)
                        )
                var ngenVersionInfo = peFile.NgenVersionInfo;

                if (ngenVersionInfo != null)
                {
                    level3.Add(
                        FolderNode("Version Info", ImageFolderStruct,
                            SingletonNode("CORCOMPILE_VERSION_INFO", ImageStruct, ngenVersionInfo.Offset, ViewKind.CorCompileVersionInfo)
                        )
                    );
                }

                var ngenDependencies = peFile.NgenDependencies;

                if (ngenDependencies != null)
                {
                    level3.Add(
                        FolderNode("Dependencies", ImageFolderStruct,
                            ListNode("CORCOMPILE_DEPENDENCY", ImageStructStack, ngenDependencies, ViewKind.CorCompileDepepdency)
                        )
                    );
                }

                var ngenDebugMap = peFile.NgenDebugMap;

                if (ngenDebugMap != null)
                {
                    level3.Add(
                        FolderNode("Debug Map", ImageFolderStruct)
                    );
                }

                var ngenModuleImage = peFile.NgenModuleImage;

                if (ngenModuleImage != null)
                {
                    level3.Add(
                        FolderNode("Module Image", ImageFolderStruct)
                    );
                }

                var ngenCodeManagerTable = peFile.NgenCodeManagerTable;

                if (ngenCodeManagerTable != null)
                {
                    level3.Add(
                        FolderNode("Code Manager Table", ImageFolderStruct,
                            SingletonNode("CORCOMPILE_CODE_MANAGER_ENTRY", ImageStruct, ngenCodeManagerTable.Offset, ViewKind.CorCompileCodeManagerEntry)
                        )
                    );
                }

                var ngenProfileDataList = peFile.NgenProfileDataList;

                if (ngenProfileDataList != null)
                {
                    level3.Add(
                        FolderNode("Profile Data List", ImageFolderStruct)
                    );
                }

                var ngenManifestMetadata = peFile.NgenManifestMetaData;

                if (ngenManifestMetadata != null)
                {
                    level3.Add(
                        FolderNode("Manifest Metadata", ImageFolderStruct)
                    );
                }

                var ngenVirtualSectionsTable = peFile.NgenVirtualSectionsTable;

                if (ngenVirtualSectionsTable != null)
                {
                    level3.Add(
                        FolderNode("Virtual Sections Table", ImageFolderStruct,
                            ListNode("CORCOMPILE_VIRTUAL_SECTION_INFO", ImageStructStack, ngenVirtualSectionsTable, ViewKind.CorCompileVirtualSectionInfo)
                        )
                    );
                }
            #region R2R

            var readyToRunHeader = peFile.ReadyToRunHeader;

            if (readyToRunHeader != null)
            {
                var coreHeader = readyToRunHeader.CoreHeader;

                level3.Add(SingletonNode("READYTORUN_HEADER", ImageStruct, readyToRunHeader.Offset, ViewKind.ReadyToRunHeader));
                level3.Add(SingletonNode("READYTORUN_CORE_HEADER", ImageStruct, coreHeader.Offset, ViewKind.ReadyToRunCoreHeader)); //In our object model it's inside the READYTORUN_HEADER, but it's also at the end so you can also say it's "after" it
                level3.Add(ListNode("READYTORUN_SECTION", ImageStructStack, coreHeader.Sections, ViewKind.ReadyToRunSection));

                level2.Add(FolderNode("R2R", ImageFolders, level3.ToArrayAndClear()));
            }

            #endregion
            #endregion

            var clrEngineMetrics = peFile.ClrEngineMetrics;

            if (clrEngineMetrics != null)
            {
                level2.Add(SingletonNode("CLR_ENGINE_METRICS", ImageStruct, clrEngineMetrics.Offset, ViewKind.ClrEngineMetrics));
            }

            var runtimeInfo = peFile.DotNetRuntimeInfo;

            if (runtimeInfo != null)
            {
                level2.Add(SingletonNode("RuntimeInfo", ImageStruct, runtimeInfo.Offset, ViewKind.RuntimeInfo));
            }

            var dotNetRuntimeDebugHeader = peFile.DotNetRuntimeDebugHeader;

            if (dotNetRuntimeDebugHeader != null)
            {
                level2.Add(
                    SingletonNode("DotNetRuntimeDebugHeader", ImageStruct, dotNetRuntimeDebugHeader.Offset, ViewKind.DotNetRuntimeDebugHeader)
                );
            }

            if (level2.Count > 0)
            {
                level1.Add(
                    FolderNode(".NET", ImageFolders, level2.ToArrayAndClear())
                );
            }

            #endregion

            //Must be added last
            level1.Add(SpecialPaneNode("Strings", ImageKind.ImageText, LogicalTreeNodeKind.Strings));
            level1.Add(SpecialPaneNode("Log", ImageKind.ImageLog, LogicalTreeNodeKind.Log));

            //We want to come up with a short summary of what this file is

            var overview = (PEFileOverview) fileAccessor.Overview;

            using var summaryKinds = new PooledList<string>();

            if (overview.IsNativeAOT)
                summaryKinds.Add("NativeAOT");
            else
            {
                if (overview.IsManaged)
                    summaryKinds.Add(".NET");

                if (overview.IsSingleFileApp)
                    summaryKinds.Add("SingleFileApp");
                else if (overview.IsAppHost)
                    summaryKinds.Add("AppHost");

                if (overview.IsNgen)
                    summaryKinds.Add("NGEN");
                else if (overview.IsR2R)
                    summaryKinds.Add("R2R");
            }

            if (summaryKinds.Count == 0)
                summaryKinds.Add("Native");

            using var str = new ValueStringBuilder();
            str.Append(peFile.Name);
            str.Append(" (");

            if (peFile.Is32Bit)
                str.Append("x86, ");
            else
                str.Append("x64, ");

            for (var i = 0; i < summaryKinds.Count; i++)
            {
                str.Append(summaryKinds[i]);

                str.Append(", ");
            }

            if ((peFile.FileHeader.Characteristics & IMAGE_FILE.IMAGE_FILE_DLL) != 0)
                str.Append("DLL");
            else
                str.Append("EXE");

            str.Append(')');

            var root = FolderNode(str.ToString(), ImageBox, level1.ToArray());

            return root;
        }

        internal static unsafe NativeTreeNode BuildPDBFileTree(FileAccessor fileAccessor)
        {
            var pdbFile = (PDBFile) fileAccessor.File;

            using var level1 = new PooledList<NativeTreeNode>();
            using var level2 = new PooledList<NativeTreeNode>();

            level1.Add(SpecialPaneNode("Overview", ImageEditAlignment, LogicalTreeNodeKind.Overview));

            level1.Add(SingletonNode("BIGMSF_HDR", ImageStruct, 0, ViewKind.BigMsfHdr));
            if (pdbFile.PDB != null)
            {
                var pdb = pdbFile.PDB;

                if (pdb.PDBHeader is PDBStream70)
                    level2.Add(SingletonNode("PDBStream70", ImageStruct, pdb.PDBHeader.Offset, ViewKind.PDBStream70));
                else
                    level2.Add(SingletonNode("PDBStream", ImageStruct, pdb.PDBHeader.Offset, ViewKind.PDBStream));

                if (pdb.StreamNameTable != null)
                {
                    level2.Add(SingletonNode("NMTNI", ImageStruct, pdb.StreamNameTable.Offset, ViewKind.StreamNameTable));

                    if (pdb.Features.Length > 0)
                        level2.Add(SingletonNode("Features", ImageField, pdb.FeaturesOffset, ViewKind.PdbFeature));
                }

                level1.Add(FolderNode("PDB", ImageFolders, level2.ToArrayAndClear()));
            }

            if (pdbFile.DBI != null)
            {
                var dbi = pdbFile.DBI;

                if (dbi.DbiHdr is NewDBIHdr)
                    level2.Add(SingletonNode("NewDBIHdr", ImageStruct, dbi.DbiHdr.Offset, ViewKind.NewDbiHdr));
                else
                    level2.Add(SingletonNode("DBIHdr", ImageStruct, dbi.DbiHdr.Offset, ViewKind.DbiHdr));

                if (dbi.Modules != null)
                {
                    var module = dbi.Modules[0];

                    string name;
                    ViewKind kind;

                    if (module is Modi60)
                    {
                        name = "MODI_60_Persist";
                        kind = ViewKind.Modi60Persist;
                    }
            if (pdbFile.TPI != null)
            {
                level1.Add(FolderNode("TPI", ImageFolders));
            }

            if (pdbFile.IPI != null)
            {
                level1.Add(FolderNode("IPI", ImageFolders));
            }

            if (pdbFile.NameMap != null)
                level1.Add(SingletonNode("/names", ImageStruct, pdbFile.NameMap.Offset, ViewKind.NameTable));

            if (pdbFile.SrcHeaders != null)
                level1.Add(FolderNode("/src/headerblock", ImageFolders));

            //Must be added last
            level1.Add(SpecialPaneNode("Strings", ImageKind.ImageText, LogicalTreeNodeKind.Strings));
            level1.Add(SpecialPaneNode("Log", ImageKind.ImageLog, LogicalTreeNodeKind.Log));

            var root = FolderNode(pdbFile.Name, ImageDatabase, level1.ToArray());

            return root;
        }

        private static NativeTreeNode SpecialPaneNode(string name, int iconIndex, LogicalTreeNodeKind kind) =>
            new SpecialPaneNode(name, iconIndex, kind);

        private static NativeTreeNode CodeNode(string name, int iconIndex, int offset) =>
            new CodeTreeNode(name, iconIndex, offset);

        private static NativeTreeNode SingletonNode(string name, int iconIndex, int offset, ViewKind viewKind, params NativeTreeNode[] children) =>
            new SingletonTreeNode(name, iconIndex, offset, viewKind, children);

        private static NativeTreeNode ListNode(string name, int iconIndex, Array array, ViewKind viewKind, params NativeTreeNode[] children) =>
            new ListTreeNode(name, iconIndex, array, viewKind, children);

        private static NativeTreeNode ListNode(string name, int iconIndex, int offset, int count, ViewKind viewKind, params NativeTreeNode[] children) =>
            new ListTreeNode(name, iconIndex, offset, count, viewKind, children);

        private static NativeTreeNode FolderNode(string name, int iconIndex, params NativeTreeNode[] children) =>
            new FolderTreeNode(name, iconIndex, children);
    }
}
