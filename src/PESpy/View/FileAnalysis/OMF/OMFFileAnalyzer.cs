using System;
using System.Threading;

namespace PESpy.View
{
    internal class OMFFileAnalyzer : FileAnalyzer
    {
        private readonly OMFFile _omfFile;

        internal OMFFileAnalyzer(
            OMFFileAccessor fileAccessor,
            IFileAnalyzerProgress? progress,
            bool trackXRefs,
            CancellationToken cancellationToken) : base(fileAccessor, progress, trackXRefs, cancellationToken, null, LocatorHttpPolicy.None)
        {
            _omfFile = fileAccessor.OMFFile;
        }

        protected override ViewWriter CreateViewWriter()
        {
            //CreateViewWriter is called by the base ctor
            var omfFile = (OMFFile) _fileAccessor.File;

            return new ViewByteViewWriter(
                new SimpleViewWriterHelper(omfFile),
                omfFile.CreateByteViewProvider(_fileAccessor),
                ViewMode.Default,
                _fileAccessor,
                null,
                this,
                LocatorHttpPolicy.None,
                _progress
            );
        }

        //Maybe LEDATA actually has code? Haven't explored this yet; but ostensibly yes OMF files should actually contain disassembly
        public override void Execute() => ExecuteData();
    }
}
