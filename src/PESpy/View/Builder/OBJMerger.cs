using System.Collections.Generic;
using System.Linq;
using PESpy.OBJ;

namespace PESpy.View.Builder
{
    internal class OBJMerger : Merger
    {
        private OBJFile objFile;

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

            var sizeOfHeaders = ImageFileHeader.StructSize + objFile.SectionHeaders.Length * ImageSectionHeader.StructSize;

            var headerMetadata = new HeaderView(sizeOfHeaders, BuildSection(0, sizeOfHeaders, v => v, v => v));
            results.Add(headerMetadata);

            var lastSectionEnd = -1;

            foreach (var section in objFile.SectionHeaders)
            {
                var start = section.PointerToRawData;
                var size = section.SizeOfRawData;

                if (objFile.AnonObjectHeader != null)
                    start += objFile.FileHeader.Offset;

                //You can have data in between sections
                if (lastSectionEnd != -1 && start > lastSectionEnd)
                {
                    var interSectionLength = start - lastSectionEnd;
                    headerMetadata = new HeaderView(interSectionLength, BuildSection(lastSectionEnd, lastSectionEnd + interSectionLength, v => v, v => v), lastSectionEnd);
                    results.Add(headerMetadata);
                }

                var data = BuildSection(start, start + size, null, null);

                results.Add(new SectionView(start, section, data, size));

                lastSectionEnd = start + size;
            }

            var length = (int) extension.GetInputLength();

            if (length > lastSectionEnd)
            {
                var overlayData = BuildSection(lastSectionEnd, length, v => v, v => v, true);
                var size = overlayData.Sum(v => v.Size);
                results.Add(new OverlayView(lastSectionEnd, overlayData, size));
            }

            return results.ToArray();
        }
    }
}
