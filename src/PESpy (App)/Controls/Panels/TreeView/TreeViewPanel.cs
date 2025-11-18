using System;
using System.Diagnostics;
#if WINFORMS
using System.Windows.Forms;
using PESpy.PDB;
#endif
using PESpy.View;
using PInvoke;
using static PESpy.Controls.ImageKind;
using static PESpy.IMAGE_DEBUG_TYPE;
#if !WINFORMS
using TreeView = PESpy.Controls.NativeTreeView;
using TreeNode = PESpy.Controls.NativeTreeNode;
#endif

namespace PESpy.Controls
{
    public class TreeViewPanel : TreeView
    {
#if WINFORMS
        public HWND hWnd => Handle;
#endif

        #region Double Buffering

        protected override void OnHandleCreated(EventArgs e)
        {
            //Fix flickering when expanding and collapsing items.
            //This fixes the flicker of the node itself, but doesn't fix it taking
            //two frames to collapse a top level node with several levels of expanded nodes under it

            //https://stackoverflow.com/questions/10362988/treeview-flickering
            User32.SendMessageW(hWnd, (int) TVM.TVM_SETEXTENDEDSTYLE, (int) TVS_EX.TVS_EX_DOUBLEBUFFER, (int) TVS_EX.TVS_EX_DOUBLEBUFFER);
            base.OnHandleCreated(e);
        }

        protected override void OnBeforeCollapse(TreeViewCancelEventArgs e)
        {
            base.OnBeforeCollapse(e);

            //Double buffering the TreeView fixes the items themselves flickering when you expand/collapse
            //the view, however when a bunch of stuff is already expanded, it might disappear over multiple frames,
            //which itself a different kind of flicker
            BeginUpdate();
        }

        protected override void OnAfterCollapse(TreeViewEventArgs e)
        {
            base.OnAfterCollapse(e);

            EndUpdate();
        }

        protected override void OnBeforeExpand(TreeViewCancelEventArgs e)
        {
            base.OnBeforeExpand(e);

            BeginUpdate();
        }

        protected override void OnAfterExpand(TreeViewEventArgs e)
        {
            base.OnAfterExpand(e);

            EndUpdate();
        }

        #endregion

        internal unsafe TreeNode BuildPEFileTree(PEFile peFile)
        {
            using var level1 = new PooledList<TreeNode>();
            using var level2 = new PooledList<TreeNode>();
            using var level3 = new PooledList<TreeNode>();

            #region Overview / Headers

            //Add top-level nodes
            level1.Add(SpecialPaneNode("Overview", ImageEditAlignment, TreeNodeKind.Overview));
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

                    var entity = App.FileAccessor.GetEntity(peFile.IsLoadedImage ? debugDirectory.AddressOfRawData : debugDirectory.PointerToRawData);

                    Debug.Assert(entity.ViewByte->Kind == ViewByteKind.Data);

                    if (entity.ViewByte->DataKind == ViewByteDataKind.Struct)
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
                                level3.Add(SingletonNode(entity.Name.ToString(), ImageStruct, entity.TargetAddress, entity.Kind));
                                break;

                            //Synthetic
                            case IMAGE_DEBUG_TYPE_POGO:
                            case IMAGE_DEBUG_TYPE_REPRO:
                                level3.Add(SingletonNode(entity.Name.ToString(), ImageSynthetic, entity.TargetAddress, entity.Kind));
                                break;
                            //Unknown

                            case IMAGE_DEBUG_TYPE_UNKNOWN:
                            case IMAGE_DEBUG_TYPE_EXCEPTION:
                            case IMAGE_DEBUG_TYPE_OMAP_TO_SRC:
                            case IMAGE_DEBUG_TYPE_OMAP_FROM_SRC:
                            case IMAGE_DEBUG_TYPE_BORLAND:
                            case IMAGE_DEBUG_TYPE_RESERVED10:
                            case IMAGE_DEBUG_TYPE_CLSID:
                            case IMAGE_DEBUG_TYPE_ILTCG:
                            case IMAGE_DEBUG_TYPE_MPX:
                            case IMAGE_DEBUG_TYPE_SPGO:
                            case IMAGE_DEBUG_TYPE_R2R_PERFMAP:
                                throw new NotImplementedException();
                        }
                    }
                    else if (entity.ViewByte->DataKind == ViewByteDataKind.Enum)
                    {
                        switch (debugDirectory.Type)
                        {
                            case IMAGE_DEBUG_TYPE_EX_DLLCHARACTERISTICS:
                                level3.Add(SingletonNode(nameof(IMAGE_DLLCHARACTERISTICS_EX), ImageField, entity.TargetAddress, entity.Kind));
                level2.Add(FolderNode("06: Debug Table", ImageFolderStruct,
                    ListNode("IMAGE_DEBUG_DIRECTORY", ImageStructStack, debugTable, ViewKind.ImageDebugDirectory, level3.ToArrayAndClear())
                ));
            }

            #endregion
            #region Copyright Table (7)

            //07: Copyright Table
            Debug.Assert(!optionalHeader.CopyrightTableDirectory.HasData);

            #endregion
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
            #region Load Config Table (10)

            var loadConfigTable = peFile.LoadConfigTable;

            if (loadConfigTable != null)
                if (codeIntegrity.Offset != 0)
                    level3.Add(SingletonNode("IMAGE_LOAD_CONFIG_CODE_INTEGRITY", ImageStruct, codeIntegrity.Offset, ViewKind.ImageLoadConfigCodeIntegrity));

                if (loadConfigTable.GuardAddressTakenIatEntryTable.IsValid)
                    level3.Add(SingletonNode("__guard_iat_table", ImageStructStack, loadConfigTable.GuardAddressTakenIatEntryTable.ActualOffset, ViewKind.GuardAddressTakenIatEntryTable));

                if (loadConfigTable.GuardLongJumpTargetTable.IsValid)
                    level3.Add(SingletonNode("__guard_longjmp_table", ImageStructStack, loadConfigTable.GuardLongJumpTargetTable.ActualOffset, ViewKind.GuardLongJumpTargetTable));

                if (loadConfigTable.DynamicValueRelocTableOffset.IsValid)
                    level3.Add(SingletonNode("IMAGE_DYNAMIC_RELOCATION_TABLE", ImageStruct, loadConfigTable.DynamicValueRelocTableOffset.ActualOffset, ViewKind.ImageDynamicRelocationTable));
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
                var metadata = peFile.EcmaMetadata;
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
                    );
                }
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

            //We want to come up with a short summary of what this file is

            var overview = (PEFileOverview) App.FileAccessor.Overview;

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

        private TreeNode OverviewNode(string name, int iconIndex) =>
            new OverviewTreeNode(name, iconIndex);

        private TreeNode CodeNode(string name, int iconIndex, int offset) =>
            new CodeTreeNode(name, iconIndex, offset);

        private TreeNode SingletonNode(string name, int iconIndex, int offset, ViewKind viewKind, params TreeNode[] children) =>
            new SingletonTreeNode(name, iconIndex, offset, viewKind, children);

        private TreeNode ListNode(string name, int iconIndex, Array array, ViewKind viewKind, params TreeNode[] children) =>
            new ListTreeNode(name, iconIndex, array, viewKind, children);

        private TreeNode ListNode(string name, int iconIndex, int offset, int count, ViewKind viewKind, params TreeNode[] children) =>
            new ListTreeNode(name, iconIndex, offset, count, viewKind, children);

        private TreeNodeEx FolderNode(string name, int iconIndex, params TreeNode[] children) =>
            new FolderTreeNode(name, iconIndex, children);
    }
}
