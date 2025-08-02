using System;
using System.Collections.Generic;
using System.Linq;
using ClrDebug;
using ClrDebug.DIA;
using PESpy.Ecma335;
using PESpy.View.Builder;
using static System.Collections.Specialized.BitVector32;

namespace PESpy.View
{
    interface IMachineWriter
    {
        IMAGE_FILE_MACHINE Machine { get; }
    }

    public class PEViewWriter : ViewWriter, IMachineWriter
    {
        private PEFile peFile;

        public bool Is32Bit => peFile.OptionalHeader.Magic == PEMagic.PE32;

        public IMAGE_FILE_MACHINE Machine => peFile.FileHeader.Machine;

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

        protected unsafe PEViewWriter(PEFile peFile) : this(peFile, (byte*) 1, 1, null, ViewMode.Default)
        {
        }

        internal unsafe PEViewWriter(
            PEFile peFile,
            byte* mmf,
            int length,
            IViewDisassembler? viewDisassembler,
            ViewMode mode) : base(mmf, length, viewDisassembler, mode, GetViewOffsetResolver(peFile, mode), GetRealOffsetResolver(peFile, mode))
        {
            this.peFile = peFile;
            viewDisassembler?.Initialize(peFile);
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

        public override IView Finalize()
        {
            if (viewStack.Count != 0)
                throw new InvalidOperationException("Expected viewStack to be empty");

            var structs = globalList;
            structs.Sort((a, b) => a.Offset.CompareTo(b.Offset));

            taggedViews.TryGetValue(ViewTag.DelayImport, out var delayNameViews);

            var dataDirectories = new List<DirectoryInfo>();

            void AddVirtualDirectory(ImageDataDirectory directory, string name)
            {
                if (directory.VirtualAddress != 0)
                {
                    bool isVirtualMode;

                    switch (mode)
                    {
                        case ViewMode.Default:
                            isVirtualMode = peFile.IsLoadedImage; //Whatever the PEFile says
                            break;

                        case ViewMode.Physical:
                            isVirtualMode = false;
                            break;

                        case ViewMode.Virtual:
                            isVirtualMode = true;
                            break;

                        default:
                            throw new NotImplementedException($"Don't know how to handle {nameof(ViewMode)} '{mode}'");
                    }

                    var sectionIndex = peFile.GetSectionContainingRVA(directory.VirtualAddress);

                    if (sectionIndex == -1)
                        return;

                    var section = peFile.SectionHeaders[sectionIndex];

                    int offset;

                    if (isVirtualMode)
                    {
                        offset = directory.VirtualAddress;
                    }
                    else
                    {
                        var relativeOffset = (int) (directory.VirtualAddress - section.VirtualAddress);

                        offset = section.PointerToRawData + relativeOffset;
                    }

                    dataDirectories.Add(new DirectoryInfo(name, offset, directory.Size));
                }
            }

            #region IMAGE_OPTIONAL_HEADER

            var o = peFile.OptionalHeader;

            AddVirtualDirectory(o.ExportTableDirectory, nameof(o.ExportTableDirectory));
            AddVirtualDirectory(o.ImportTableDirectory, nameof(o.ImportTableDirectory));
            AddVirtualDirectory(o.ResourceTableDirectory, nameof(o.ResourceTableDirectory));
            AddVirtualDirectory(o.ExceptionTableDirectory, nameof(o.ExceptionTableDirectory));

            if (o.SecurityTableDirectory.VirtualAddress != 0)
                dataDirectories.Add(new DirectoryInfo(nameof(o.SecurityTableDirectory), (Int32) o.SecurityTableDirectory.VirtualAddress, o.SecurityTableDirectory.Size));

            AddVirtualDirectory(o.BaseRelocationTableDirectory, nameof(o.BaseRelocationTableDirectory));
            AddVirtualDirectory(o.DebugTableDirectory, nameof(o.DebugTableDirectory));
            AddVirtualDirectory(o.CopyrightTableDirectory, nameof(o.CopyrightTableDirectory));
            AddVirtualDirectory(o.GlobalPointerTableDirectory, nameof(o.GlobalPointerTableDirectory));
            AddVirtualDirectory(o.ThreadLocalStorageTableDirectory, nameof(o.ThreadLocalStorageTableDirectory));
            AddVirtualDirectory(o.LoadConfigTableDirectory, nameof(o.LoadConfigTableDirectory));

            if (o.BoundImportTableDirectory.VirtualAddress != 0)
                dataDirectories.Add(new DirectoryInfo(nameof(o.BoundImportTableDirectory), (Int32) o.BoundImportTableDirectory.VirtualAddress, o.BoundImportTableDirectory.Size));

            AddVirtualDirectory(o.ImportAddressTableDirectory, nameof(o.ImportAddressTableDirectory));
            AddVirtualDirectory(o.DelayImportTableDirectory, nameof(o.DelayImportTableDirectory));
            AddVirtualDirectory(o.CorHeaderTableDirectory, nameof(o.CorHeaderTableDirectory));

            #endregion

            dataDirectories.Sort((a, b) => a.Start.CompareTo(b.Start));

            var merger = new PEMerger(peFile, structs, delayNameViews, dataDirectories, extension, mode);

            var results = merger.Merge();

            return new FileView(
                mode == ViewMode.Default
                    ? (peFile.IsLoadedImage ? ViewMode.Virtual : ViewMode.Physical)
                    : mode,
                results,
                ViewKind.PEFile
            );
        }
    }
}
