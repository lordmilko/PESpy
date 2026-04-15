using System;
using ClrDebug;
using PESpy.Ecma335;
using PESpy.View.Builder;

namespace PESpy.View
{
    interface IMachineWriter
    {
        IMAGE_FILE_MACHINE GetMachine(in MemoryChunk chunk);
    }

    public class PEViewWriter : ViewWriter, IMachineWriter
    {
        private PEFile peFile;

        public bool Is32Bit => peFile.Is32Bit;

        IMAGE_FILE_MACHINE IMachineWriter.GetMachine(in MemoryChunk chunk) => peFile.FileHeader.Machine;

        private MetadataSizes metadataSizes;
        private bool hasMetadataSizes;

        internal ref readonly MetadataSizes MetadataReader
        {
            get
            {
                //This property is only accessed when we actually have metadata
                //(or when the debugger is inspecting the PEViewWriter)
                if (!hasMetadataSizes)
                {
                    var heap = peFile.EcmaMetadata?.CompressedModelHeap;

                    if (heap != null)
                        metadataSizes = heap.Sizes;

                    hasMetadataSizes = true;
                }

                return ref metadataSizes;
            }
        }

        internal PEViewWriter(PEFile peFile) : this(peFile, peFile.CreateByteViewProvider(null))
        {
        }

        internal unsafe PEViewWriter(PEFile peFile, ByteViewProvider byteViewProvider) : this(peFile, byteViewProvider, ViewMode.Default)
        {
        }

        internal PEViewWriter(PEViewWriter parentWriter, PEFile peFile, ByteViewProvider byteViewProvider) : base(parentWriter, byteViewProvider, GetViewOffsetResolver(peFile, parentWriter.mode), GetRealOffsetResolver(peFile, parentWriter.mode))
        {
            this.peFile = peFile;
        }

        internal unsafe PEViewWriter(
            PEFile peFile,
            ByteViewProvider byteViewProvider,
            ViewMode mode) : base(byteViewProvider, mode, GetViewOffsetResolver(peFile, mode), GetRealOffsetResolver(peFile, mode))
        {
            this.peFile = peFile;
        }

        private static TryGetOffsetDelegate GetViewOffsetResolver(PEFile peFile, ViewMode mode)
        {
            switch (mode)
            {
                case ViewMode.Default:
                    //Whatever the values are is what the values are
                    break;

                case ViewMode.Virtual:
                    if (!peFile.IsLoadedImage) //If we're already virtual, nothing to do
                    {
                        return (int offset, out int viewOffset) =>
                        {
                            //Physical and need to convert to virtual

                            if (!peFile.TryGetRVA(offset, out var rva))
                            {
                                //We're a physical file, trying to pretend that we're virtual. If an RVA can't be resolved to a particular section, this means that the RVA either exists in the file headers,
                                //or in the overlay. Overlay data is not loaded into virtual memory. As such, if we're overlay, we don't want to write the value
                                if (offset < peFile.OptionalHeader.SizeOfHeaders)
                                {
                                    viewOffset = offset;
                                    return true;
                                }

                                //Overlay, ignore
                                viewOffset = default;
                                return false;
                            }

                            viewOffset = rva;
                            return true;
                        };
                    }
                    break;

                case ViewMode.Physical:
                    if (peFile.IsLoadedImage) //If we're already physical, nothing to do
                    {
                        return (int rva, out int viewRVA) =>
                        {
                            //Virtual and need to convert to physical

                            if (!peFile.TryGetOffset(rva, out var offset))
                                viewRVA = rva; //Anything that exists virtually also exists physically

                            viewRVA = offset;
                            return true;
                        };
                    }
                    break;

                default:
                    throw new NotImplementedException($"Don't know how to handle {nameof(ViewMode)} '{mode}'");
            }

            return (int offset, out int viewOffset) =>
            {
                viewOffset = offset;
                return true;
            };
        }

        private static Func<int, int> GetRealOffsetResolver(PEFile peFile, ViewMode mode)
        {
            switch (mode)
            {
                case ViewMode.Default:
                    break;

                case ViewMode.Physical:
                    if (peFile.IsLoadedImage)
                    {
                        return offset =>
                        {
                            if (!peFile.TryGetRVA(offset, out var rva))
                                return offset;

                            return rva;
                        };
                    }
                    break;

                case ViewMode.Virtual:
                    if (!peFile.IsLoadedImage)
                    {
                        return rva =>
                        {
                            if (!peFile.TryGetOffset(rva, out var offset))
                                return rva;

                            return offset;
                        };
                    }
                    break;

                default:
                    throw new NotImplementedException($"Don't know how to handle {nameof(ViewMode)} '{mode}'");
            }

            return v => v;
        }

        internal override ViewWriter CreateNestedWriter(IFile file) => new NestedPEViewWriter(this, byteViewProvider, (PEFile) file);

        public override IView Finalize()
        {
            if (viewStack.Count != 0)
                throw new InvalidOperationException("Expected viewStack to be empty");

            var structs = globalList;
            structs.Sort((a, b) => a.Offset.CompareTo(b.Offset));

            var dataDirectories = new PooledList<DirectoryInfo>();

            try
            {
                CollectDataDirectories(ref dataDirectories);

                using var merger = new Merger(peFile, this, structs, dataDirectories.Span, byteViewProvider);

                var results = merger.MergePE(mode);

                return new FileView(
                    mode == ViewMode.Default
                        ? (peFile.IsLoadedImage ? ViewMode.Virtual : ViewMode.Physical)
                        : mode,
                    peFile.Name,
                    results,
                    this,
                    ViewKind.PEFile
                );
            }
            finally
            {
                dataDirectories.Dispose();
            }
        }

        internal override void CollectDataDirectories(ref PooledList<DirectoryInfo> dataDirectories)
        {
            #region IMAGE_OPTIONAL_HEADER

            var o = peFile.OptionalHeader;

            AddVirtualDirectory(ref dataDirectories, o.ExportTableDirectory, nameof(o.ExportTableDirectory));
            AddVirtualDirectory(ref dataDirectories, o.ImportTableDirectory, nameof(o.ImportTableDirectory));
            AddVirtualDirectory(ref dataDirectories, o.ResourceTableDirectory, nameof(o.ResourceTableDirectory));
            AddVirtualDirectory(ref dataDirectories, o.ExceptionTableDirectory, nameof(o.ExceptionTableDirectory));

            AddPhysicalDirectory(ref dataDirectories, o.SecurityTableDirectory, nameof(o.SecurityTableDirectory));

            AddVirtualDirectory(ref dataDirectories, o.BaseRelocationTableDirectory, nameof(o.BaseRelocationTableDirectory));
            AddVirtualDirectory(ref dataDirectories, o.DebugTableDirectory, nameof(o.DebugTableDirectory));
            AddVirtualDirectory(ref dataDirectories, o.CopyrightTableDirectory, nameof(o.CopyrightTableDirectory));
            AddVirtualDirectory(ref dataDirectories, o.GlobalPointerTableDirectory, nameof(o.GlobalPointerTableDirectory));
            AddVirtualDirectory(ref dataDirectories, o.ThreadLocalStorageTableDirectory, nameof(o.ThreadLocalStorageTableDirectory));
            AddVirtualDirectory(ref dataDirectories, o.LoadConfigTableDirectory, nameof(o.LoadConfigTableDirectory));

            AddPhysicalDirectory(ref dataDirectories, o.BoundImportTableDirectory, nameof(o.BoundImportTableDirectory));

            AddVirtualDirectory(ref dataDirectories, o.ImportAddressTableDirectory, nameof(o.ImportAddressTableDirectory));
            AddVirtualDirectory(ref dataDirectories, o.DelayImportTableDirectory, nameof(o.DelayImportTableDirectory));
            AddVirtualDirectory(ref dataDirectories, o.CorHeaderTableDirectory, nameof(o.CorHeaderTableDirectory));

            #endregion
            #region ImageCor20Header

            var cor20Header = peFile.Cor20Header;

            if (cor20Header != null)
            {
                AddVirtualDirectory(ref dataDirectories, cor20Header.Metadata, "Cor20 Metadata Directory");
                AddVirtualDirectory(ref dataDirectories, cor20Header.Resources, "Cor20 Resources Directory");
                AddVirtualDirectory(ref dataDirectories, cor20Header.StrongNameSignature, "Cor20 StrongNameSignature Directory");
                AddVirtualDirectory(ref dataDirectories, cor20Header.CodeManagerTable, "Cor20 CodeManagerTable Directory");
                AddVirtualDirectory(ref dataDirectories, cor20Header.VTableFixups, "Cor20 VTableFixups Directory");
                AddVirtualDirectory(ref dataDirectories, cor20Header.ExportAddressTableJumps, "Cor20 ExportAddressTableJumps Directory");
                AddVirtualDirectory(ref dataDirectories, cor20Header.ManagedNativeHeader, "Cor20 ManagedNativeHeader Directory");
            }

            #endregion
            #region NGEN

            var ngenHeader = peFile.NgenHeader;

            if (ngenHeader != null)
            {
                AddVirtualDirectory(ref dataDirectories, ngenHeader.HelperTable, "NGEN HelperTable Directory");
                AddVirtualDirectory(ref dataDirectories, ngenHeader.ImportSections, "NGEN ImportSections Directory");
                AddVirtualDirectory(ref dataDirectories, ngenHeader.ImportTable, "NGEN ImportTable Directory");
                AddVirtualDirectory(ref dataDirectories, ngenHeader.StubsData, "NGEN StubsData Directory");
                AddVirtualDirectory(ref dataDirectories, ngenHeader.VersionInfo, "NGEN VersionInfo Directory");
                AddVirtualDirectory(ref dataDirectories, ngenHeader.Dependencies, "NGEN Dependencies Directory");
                AddVirtualDirectory(ref dataDirectories, ngenHeader.DebugMap, "NGEN DebugMap Directory");
                AddVirtualDirectory(ref dataDirectories, ngenHeader.ModuleImage, "NGEN ModuleImage Directory");
                AddVirtualDirectory(ref dataDirectories, ngenHeader.CodeManagerTable, "NGEN CodeManagerTable Directory");
                AddVirtualDirectory(ref dataDirectories, ngenHeader.ProfileDataList, "NGEN ProfileDataList Directory");
                AddVirtualDirectory(ref dataDirectories, ngenHeader.ManifestMetaData, "NGEN ManifestMetaData Directory");
                AddVirtualDirectory(ref dataDirectories, ngenHeader.VirtualSectionsTable, "NGEN VirtualSectionsTable Directory");

                AddVirtualDirectory(ref dataDirectories, ngenHeader.EEInfoTable, "NGEN EEInfoTable Directory");
                AddVirtualDirectory(ref dataDirectories, ngenHeader.Dummy1, "NGEN Dummy1 Directory");
                AddVirtualDirectory(ref dataDirectories, ngenHeader.Dummy2, "NGEN Dummy2 Directory");
                AddVirtualDirectory(ref dataDirectories, ngenHeader.Dummy3, "NGEN Dummy3 Directory");
                AddVirtualDirectory(ref dataDirectories, ngenHeader.Dummy4, "NGEN Dummy4 Directory");

                var ngenCodeManagerTable = peFile.NgenCodeManagerTable;

                if (ngenCodeManagerTable != null)
                {
                    AddVirtualDirectory(ref dataDirectories, ngenCodeManagerTable.HotCode, "NGEN CodeManager HotCode Directory");
                    AddVirtualDirectory(ref dataDirectories, ngenCodeManagerTable.Code, "NGEN CodeManager Code Directory");
                    AddVirtualDirectory(ref dataDirectories, ngenCodeManagerTable.ColdCode, "NGEN CodeManager ColdCode Directory");
                    AddVirtualDirectory(ref dataDirectories, ngenCodeManagerTable.ROData, "NGEN CodeManager ROData Directory");
                }

                var ngenImportSections = peFile.NgenImportSections;

                if (ngenImportSections != null)
                {
                    foreach (var importSection in ngenImportSections)
                    {
                        AddVirtualDirectory(ref dataDirectories, importSection.Section, $"NGEN Import {importSection.Type} Directory");
                    }
                }
            }

            #endregion
            #region R2R

            var r2rHeader = peFile.ReadyToRunHeader;

            if (r2rHeader != null)
            {
                var sections = r2rHeader.CoreHeader.Sections;

                for (var i = 0; i < sections.Length; i++)
                {
                    ref var section = ref sections[i];

                    AddVirtualDirectory(ref dataDirectories, section.Section, $"R2R {section.Type} Directory");

                    if (section.Type == ReadyToRunSectionType.ImportSections)
                    {
                        var importSections = (R2R.ReadyToRunImportSection[]) section.Data!;

                        for (var j = 0; j < importSections.Length; j++)
                        {
                            ref var importSection = ref importSections[j];

                            AddVirtualDirectory(ref dataDirectories, importSection.Section, $"R2R Import {importSection.Type} Directory");
                        }
                    }
                }
            }

            #endregion

            dataDirectories.Sort((a, b) => a.Start.CompareTo(b.Start));
        }

        private void AddVirtualDirectory(ref PooledList<DirectoryInfo> dataDirectories, ImageDataDirectory directory, string name)
        {
            if (directory.HasData)
            {
                var wantVirtual = mode switch
                {
                    ViewMode.Default => peFile.IsLoadedImage,
                    ViewMode.Physical => false,
                    ViewMode.Virtual => true
                };

                var sectionIndex = peFile.GetSectionContainingRVA(directory.VirtualAddress);

                if (sectionIndex == -1)
                    return;

                var section = peFile.SectionHeaders[sectionIndex];

                int offset;

                if (wantVirtual)
                {
                    offset = directory.VirtualAddress;
                }
                else
                {
                    var relativeOffset = (int) (directory.VirtualAddress - section.VirtualAddress);

                    offset = section.PointerToRawData + relativeOffset;
                }

                var directoryInfo = new DirectoryInfo(name, offset + peFile.blockProvider.StartOffset, directory.Size);

                //If the directory is out of bounds, thats an issue, but it's not up to us to deal with that

                dataDirectories.Add(directoryInfo);
            }
        }

        private void AddPhysicalDirectory(ref PooledList<DirectoryInfo> dataDirectories, ImageDataDirectory directory, string name)
        {
            if (directory.VirtualAddress == 0)
                return;

            var wantVirtual = mode switch
            {
                ViewMode.Default => peFile.IsLoadedImage,
                ViewMode.Physical => false,
                ViewMode.Virtual => true
            };

            var sectionIndex = peFile.GetSectionContainingOffset(directory.VirtualAddress);

            if (sectionIndex == -1)
                return;

            var section = peFile.SectionHeaders[sectionIndex];

            int offset;

            if (wantVirtual)
            {
                var relativeOffset = (int) (directory.VirtualAddress - section.PointerToRawData);

                offset = section.VirtualAddress + relativeOffset;
            }
            else
                offset = directory.VirtualAddress;

            var directoryInfo = new DirectoryInfo(name, offset + peFile.blockProvider.StartOffset, directory.Size);

            //If the directory is out of bounds, thats an issue, but it's not up to us to deal with that

            dataDirectories.Add(directoryInfo);
        }
    }
}
