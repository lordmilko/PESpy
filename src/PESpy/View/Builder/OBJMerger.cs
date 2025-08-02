using System.Collections.Generic;
using System.Linq;
using PESpy.OBJ;

namespace PESpy.View.Builder
{
    internal class OBJMerger : Merger
    {
        private readonly OBJFile objFile;

        public OBJMerger(
            OBJFile objFile,
            List<IView> sortedStructs,
            Extension extension) : base(sortedStructs, null, new List<DirectoryInfo>(), extension)
        {
            this.objFile = objFile;
        }

        internal override IView[] Merge()
        {
            var results = new List<IView>();

            //ImageFileHeader.Offset will either be 0 (indicating a classic OBJ file) or non-zero (indicating there's an Anon Header in front of it)
            var sizeOfHeaders = objFile.FileHeader.Offset + ImageFileHeader.StructSize + objFile.SectionHeaders.Length * ImageSectionHeader.StructSize;

            var headerMetadata = new HeaderView(sizeOfHeaders, BuildSection(0, sizeOfHeaders, v => v, v => v));
            results.Add(headerMetadata);

            var lastSectionEnd = -1;

            for (var i = 0; i < objFile.SectionHeaders.Length; i++)
            {
                ref var section = ref objFile.SectionHeaders[i];
                var start = section.PointerToRawData;
                var size = section.SizeOfRawData;

                if (objFile.AnonObjectHeader != null)
                    start += objFile.FileHeader.Offset;

                ProcessSectionHeader(section, start, size, lastSectionEnd, this, results);

                lastSectionEnd = start + size;
            }

            var length = (int) extension.GetInputLength();

            if (length > lastSectionEnd)
            {
                ProcessOverlay(lastSectionEnd, length, this, results);
            }

            return results.ToArray();
        }

        internal static void ProcessSectionHeader(
            in ImageSectionHeader section,
            int start,
            int size,
            int lastSectionEnd,
            Merger merger,
            List<IView> results)
        {
            //You can have data in between sections
            if (lastSectionEnd != -1 && start > lastSectionEnd)
            {
                var interSectionLength = start - lastSectionEnd;
                var children = merger.BuildSection(lastSectionEnd, lastSectionEnd + interSectionLength, v => v, v => v);

                var isRelocations = children.All(c => c is IStructView { Name: "IMAGE_RELOCATION" });

                var interRegion = new LogicalRegionView(lastSectionEnd, isRelocations ? "Relocations" : "Inter-Section Data", children, ViewKind.Value, interSectionLength);
                results.Add(interRegion);
            }

            var data = merger.BuildSection(start, start + size);

            results.Add(new SectionView(start, section.Name.ToString(), data, size));
        }

        internal static void ProcessOverlay(int lastSectionEnd, int length, Merger merger, List<IView> results)
        {
            var overlayData = merger.BuildSection(lastSectionEnd, length, v => v, v => v, true);

            //We will often just expect the COFF Symbol Table to be at the end. No point wrapping it up in an overlay
            if (overlayData.Length == 1)
                results.Add(overlayData[0]);
            else
            {
                var size = overlayData.Sum(v => v.Size);
                results.Add(new OverlayView(lastSectionEnd, overlayData, size));
            }
        }
    }
}
