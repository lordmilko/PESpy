using System;
using System.Threading;
using PESpy.LE;

namespace PESpy.View
{
    internal class LEFileAnalyzer : FileAnalyzer
    {
        private readonly LEFile _leFile;

        internal LEFileAnalyzer(
            LEFileAccessor fileAccessor,
            IFileAnalyzerProgress? progress,
            bool trackXRefs,
            CancellationToken cancellationToken,
            IFileDisassembler? disassembler) : base(fileAccessor, progress, trackXRefs, cancellationToken, disassembler, LocatorHttpPolicy.None)
        {
            _leFile = fileAccessor.LEFile;
        }

        protected override ViewWriter CreateViewWriter()
        {
            //CreateViewWriter is called by the base ctor
            var leFile = (LEFile) _fileAccessor.File;

            return new ViewByteViewWriter(
                new SimpleViewWriterHelper(leFile),
                leFile.CreateByteViewProvider(_fileAccessor),
                ViewMode.Default,
                _fileAccessor,
                null,
                this,
                LocatorHttpPolicy.None,
                _progress
            );
        }

        public override void Execute() => ExecuteCode();
        protected override void MarkRegions()
        {
            //The LE Header may be followed by several additional sections at locations relative to the start of the LE Header itself

            var sizeOfHeaders = _leFile.DosHeader.FileAddressOfNewExeHeader + ImageVXDHeader.StructSize;

            _extraRegions.Add(new RegionBuilder
            {
                Name = "HEADER",
                Kind = ViewKind.Header,
                Start = 0,
                End = sizeOfHeaders
            });

            var vxdHeader = _leFile.VXDHeader;

            var lastSectionEnd = sizeOfHeaders;

            foreach (var tableBound in _leFile.tableBounds)
            {
                if (!tableBound.IsPresent)
                    continue;

                if (tableBound.StartOffset != lastSectionEnd)
                {
                    //Read any data that may exist between the main headers and the table. This shouldn't be possible, but you never know!
                    MarkInterSectionData(lastSectionEnd, tableBound.StartOffset, canHaveRelocations: false);
                }

                SplitRegionBounds(tableBound.StartOffset, tableBound.EndOffset);

                _extraRegions.Add(new RegionBuilder
                {
                    Name = tableBound.Name,
                    Kind = tableBound.Kind,
                    Start = tableBound.StartOffset,
                    End = tableBound.EndOffset
                });

                lastSectionEnd = tableBound.EndOffset;
            }
        }
    }
}
