using System;
using System.Collections.Generic;
using System.Threading;

namespace PESpy.View
{
    internal class DOSFileAnalyzer : FileAnalyzer
    {
        private readonly DOSFile _dosFile;

        internal DOSFileAnalyzer(
            DOSFileAccessor fileAccessor,
            IFileAnalyzerProgress? progress,
            bool trackXRefs,
            CancellationToken cancellationToken,
            IFileDisassembler? disassembler) : base(fileAccessor, progress, trackXRefs, cancellationToken, disassembler, LocatorHttpPolicy.None)
        {
            _dosFile = fileAccessor.DOSFile;
        }

        protected override ViewWriter CreateViewWriter()
        {
            //CreateViewWriter is called by the base ctor
            var dosFile = (DOSFile) _fileAccessor.File;

            return new ViewByteViewWriter(
                new SimpleViewWriterHelper(dosFile),
                dosFile.CreateByteViewProvider(_fileAccessor),
                ViewMode.Default,
                _fileAccessor,
                null,
                this,
                LocatorHttpPolicy.None,
                _progress
            );
        }

        public override void Execute() => ExecuteCode();

        protected override void DiscoverCodeRoots()
        {
            Log(FileAnalyzerProgressPhase.DiscoverCodeRoots);

            _cancellationToken.ThrowIfCancellationRequested();

            //Not yet implemented
        }

        protected override void MarkRegions()
        {
            CreateOMFRegion(_dosFile.CodeViewData);
        }
    }
}
