using System.Linq;

namespace PESpy.View.Builder
{
    internal ref partial struct Merger
    {
        internal IView[] MergeOBJ()
        {
            var objFile = (OBJFile) file;

            var results = new PooledList<IView>();

            try
            {
                //ImageFileHeader.Offset will either be 0 (indicating a classic OBJ file) or non-zero (indicating there's an Anon Header in front of it)
                var sizeOfHeaders = objFile.FileHeader.Offset + ImageFileHeader.StructSize + objFile.SectionHeaders.Length * ImageSectionHeader.StructSize;

                var headerMetadata = new HeaderView(0, sizeOfHeaders, BuildSection(0, sizeOfHeaders, v => v, v => v));
                results.Add(headerMetadata);

                var lastSectionEnd = -1;

                for (var i = 0; i < objFile.SectionHeaders.Length; i++)
                {
                    ref var section = ref objFile.SectionHeaders[i];
                    var start = section.PointerToRawData;
                    var size = section.SizeOfRawData;

                    if (objFile.AnonObjectHeader != null)
                        start += objFile.FileHeader.Offset;

                    ProcessSectionHeader(section, start, size, lastSectionEnd, ref this, ref results);

                    lastSectionEnd = start + size;
                }

                var length = byteViewProvider.FileOrSectionLength;

                if (length > lastSectionEnd)
                {
                    ProcessOverlay(lastSectionEnd, length, ref this, ref results);
                }

                return results.ToArray();
            }
            finally
            {
                results.Dispose();
            }
        }

        internal static void ProcessSectionHeader(
            in ImageSectionHeader section,
            int start,
            int size,
            int lastSectionEnd,
            ref Merger merger,
            ref PooledList<IView> results)
        {
            //You can have data in between sections
            if (lastSectionEnd != -1 && start > lastSectionEnd)
            {
                var interSectionLength = start - lastSectionEnd;
                var children = merger.BuildSection(lastSectionEnd, lastSectionEnd + interSectionLength, v => v, v => v);

                var isRelocations = children.All(c => c is IStructView s && s.Name == Strings.IMAGE_RELOCATION);

                string name;
                ViewKind kind;

                if (isRelocations)
                {
                    name = "Relocations";
                    kind = ViewKind.Relocations;
                }
                else
                {
                    name = "Inter-Section Data";
                    kind = ViewKind.InterSectionData;
                }

                var interRegion = new LogicalRegionView(lastSectionEnd, name, children, kind, interSectionLength);
                results.Add(interRegion);
            }

            var data = merger.BuildSection(start, start + size);

            results.Add(new SectionView(start, section.Name.ToString(), data, size));
        }

        internal static void ProcessOverlay(int lastSectionEnd, int end, ref Merger merger, ref PooledList<IView> results)
        {
            var overlayData = merger.BuildSection(lastSectionEnd, end, v => v, v => v, true);

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
