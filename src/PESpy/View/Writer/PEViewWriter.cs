using System;
using System.Collections.Generic;
using PESpy.View.Builder;

namespace PESpy.View
{
    public class PEViewWriter : ViewWriter
    {
        private PEFile peFile;

        public bool Is32Bit => peFile.OptionalHeader.Magic == PEMagic.PE32;

        public PEViewWriter(PEFile peFile, ref FileReader reader, ViewMode mode) : base(ref reader, mode)
        {
            this.peFile = peFile;
        }
            var structs = globalList;
            structs.Sort((a, b) => a.Offset.CompareTo(b.Offset));

            taggedViews.TryGetValue(ViewTag.DelayImport, out var delayNameViews);

            var dataDirectories = new List<DirectoryInfo>();

            void AddVirtualDirectory(ImageDataDirectory directory, string name)
            {
                if (directory.VirtualAddress != 0)
                {
                    if (peFile.TryGetDirectoryOffset(directory, out var offset, true))
                        dataDirectories.Add(new DirectoryInfo(name, offset, directory.Size));
                }
            }
            dataDirectories.Sort((a, b) => a.Start.CompareTo(b.Start));

            var merger = new PEMerger(peFile, structs, delayNameViews, dataDirectories, extension);

            var results = merger.Merge();

            return new PEFileView(
                results,
                mode == ViewMode.Default
                    ? (peFile.IsLoadedImage ? ViewMode.Virtual : ViewMode.Physical)
                    : mode
            );
        }
    }
}
