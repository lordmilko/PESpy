using System;
using System.Threading;
using PESpy.LIB;
using PESpy.Native;

namespace PESpy.View
{
    internal class LIBFileAnalyzer : FileAnalyzer
    {
        private readonly LIBFile _libFile;

        internal LIBFileAnalyzer(
            LIBFileAccessor fileAccessor,
            IFileAnalyzerProgress? progress,
            bool trackXRefs,
            CancellationToken cancellationToken,
            IFileDisassembler? disassembler) : base(fileAccessor, progress, trackXRefs, cancellationToken, disassembler, LocatorHttpPolicy.None)
        {
            _libFile = fileAccessor.LIBFile;
        }

        protected override ViewWriter CreateViewWriter()
        {
            //CreateViewWriter is called by the base ctor
            var libFile = (LIBFile) _fileAccessor.File;

            return new ViewByteViewWriter(
                new LIBFileViewWriterHelper(libFile),
                libFile.CreateByteViewProvider(_fileAccessor),
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
            var length = _libFile.Length;

            //We also need to build regions around each section inside a long names member. This is a bit tricky, because we don't support building custom child regions
            //while we're in the process of building a parent region. So Plan B: we'll eagerly construct the section regions for each long names member, and then
            //add them to our list of sorted structs so that they naturally get included
            foreach (var item in _libFile.ImportLibrary)
            {
                if (item.IsLong)
                {
                    OBJFileAnalyzer.MarkOBJRegions(
                        item.Offset,
                        item.ArchiveHeader.Size,
                        (LongImportLibraryMember) item,
                        _fileAccessor,
                        _extraRegions
                    );
                }
            }
        }

        protected override unsafe void MarkPadding()
        {
            //Mark any unknown bytes as IMAGE_ARCHIVE_PAD. Ostensibly we should have
            //categorized all data within a section, even for sections we don't know how to interpret

            ref var sectionAccessor = ref _fileAccessor.SectionAccessors[0];

            var pBytes = _fileAccessor.GetRawSectionData(sectionAccessor);

            var pViewByte = sectionAccessor.pViewBytes;

            var limit = pViewByte + sectionAccessor.Length;

            while (pViewByte < limit)
            {
                if (pViewByte->Kind == ViewByteKind.Unknown)
                {
                    var relativeOffset = (int) (pViewByte - sectionAccessor.pViewBytes);
                    var pUnknownData = (byte*) (pBytes + relativeOffset);

                    if (*pUnknownData == IMAGE_ARCHIVE_MEMBER_HEADER.IMAGE_ARCHIVE_PAD)
                    {
                        //We've got padding; find out how much we have

                        var pStart = pViewByte;

                        pViewByte++;
                        pUnknownData++;

                        while (pViewByte < limit & pViewByte->Kind == ViewByteKind.Unknown && *pUnknownData == IMAGE_ARCHIVE_MEMBER_HEADER.IMAGE_ARCHIVE_PAD)
                        {
                            pViewByte++;
                            pUnknownData++;
                        }

                        _fileAccessor.AddStructKind(sectionAccessor.StartAddress + relativeOffset, ViewKind.ImageArchivePad);

                        pStart->Kind = ViewByteKind.Data;
                        pStart->DataKind = ViewByteDataKind.Padding;

                        for (var i = pStart + 1; i < pViewByte; i++)
                            i->Kind = ViewByteKind.Body;

                        //Don't increment the view byte again
                        continue;
                    }
                }

                pViewByte++;
            }
        }
    }
}
