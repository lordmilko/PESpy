using System.Collections.Generic;

namespace PESpy.View
{
    internal class OBJFileAnalyzer : FileAnalyzer
    {
        private readonly OBJFile _objFile;

        internal OBJFileAnalyzer(
            OBJFileAccessor fileAccessor,
            in FileAnalyzerOptions options) : base(fileAccessor, options)
        {
            _objFile = fileAccessor.OBJFile;
        }

        protected override ViewWriter CreateViewWriter()
        {
            //CreateViewWriter is called by the base ctor
            var objFile = (OBJFile) _fileAccessor.File;

            return new ViewByteViewWriter(
                new SimpleViewWriterHelper(objFile),
                objFile.CreateByteViewProvider(_fileAccessor),
                ViewMode.Default,
                _fileAccessor,
                null,
                this,
                LocatorHttpPolicy.None,
                _progress
            );
        }

        public override void Execute() => ExecuteData();

        protected override void MarkRegions()
        {
            //Thanks to the fact we add add FileHeader.Offset to the end anyway, this all automatically just works
            //for ANON_OBJECT_HEADER as well
            MarkOBJRegions(0, (int) _objFile.Length, _objFile, _fileAccessor, _extraRegions);
        }

        internal static void MarkOBJRegions(
            int startOffset,
            int length,
            IOBJFile objFile,
            FileAccessor fileAccessor,
            List<RegionBuilder> extraRegions)
        {
            //We want to show everything top-level except for the entities that reside inside sections; however, for the purpose
            //of analysis, we do need to create a section to encapsulate everything inside the header area

            var objEnd = objFile.FileHeader.Offset + length;

            var headerLength = objFile.SectionHeaders.Length == 0 ? length : (objFile.SectionHeaders[0].PointerToRawData);

            extraRegions.Add(new RegionBuilder
            {
                Name = "HEADER",
                Kind = ViewKind.Header,
                Start = startOffset,
                End = objFile.FileHeader.Offset + headerLength //This works for both normal and ANON_OBJECT_HEADER
            });

            var sectionHeaders = objFile.SectionHeaders;

            var lastSectionEnd = -1;

            for (var i = 0; i < sectionHeaders.Length; i++)
            {
                ref var section = ref sectionHeaders[i];

                var start = section.PointerToRawData;

                if (start == 0)
                    continue; //You can have a section like .bss which says it has a length of 8 but the PointerToRawData is0

                var size = section.SizeOfRawData;

                start += (int) objFile.FileHeader.Offset;

                //You can have data in between sections
                MarkInterSectionData(lastSectionEnd, start, canHaveRelocations: true, fileAccessor, extraRegions);

                extraRegions.Add(new RegionBuilder
                {
                    Name = section.Name.ToString(),
                    Kind = ViewKind.Section,
                    Start = start,
                    End = start + size
                });

                lastSectionEnd = start + size;
            }

            if (objEnd > lastSectionEnd && lastSectionEnd != -1)
            {
                //We have an overlay, however if there's only one entity contained inside of it,
                //we don't need a section

                //We only have one section
                var iterator = fileAccessor.EnumerateEntities(sectionAccessorIndex: 0);
                iterator.MoveTo(lastSectionEnd);

                var numEntities = 0;

                //Only need to count 2 entities; if we have more than 2, we need an overlay
                while (iterator.MoveNext() && iterator.Current.TargetAddress < objEnd && numEntities < 2)
                    numEntities++;

                if (numEntities > 1)
                {
                    var overlayLength = objEnd - lastSectionEnd;
                    extraRegions.Add(new RegionBuilder
                    {
                        Name = "OVERLAY",
                        Kind = ViewKind.Overlay,
                        Start = lastSectionEnd,
                        End = objEnd
                    });
                }
            }
        }
    }
}
