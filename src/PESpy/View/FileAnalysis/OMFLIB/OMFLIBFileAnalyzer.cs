using System;
using System.Threading;

namespace PESpy.View
{
    internal class OMFLIBFileAnalyzer : FileAnalyzer
    {
        private readonly OMFLIBFile _omfLibFile;

        internal OMFLIBFileAnalyzer(
            OMFLIBFileAccessor fileAccessor,
            IFileAnalyzerProgress? progress,
            bool trackXRefs,
            CancellationToken cancellationToken) : base(fileAccessor, progress, trackXRefs, cancellationToken, null, LocatorHttpPolicy.None)
        {
            _omfLibFile = fileAccessor.OMFLIBFile;
        }

        protected override ViewWriter CreateViewWriter()
        {
            //CreateViewWriter is called by the base ctor
            var omfLibFile = (OMFLIBFile) _fileAccessor.File;

            return new ViewByteViewWriter(
                new SimpleViewWriterHelper(omfLibFile),
                omfLibFile.CreateByteViewProvider(_fileAccessor),
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
