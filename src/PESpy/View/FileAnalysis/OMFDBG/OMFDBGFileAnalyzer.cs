using System;
using System.Threading;

namespace PESpy.View
{
    internal class OMFDBGFileAnalyzer : FileAnalyzer
    {
        private readonly OMFDBGFile _omfDbgFile;

        internal OMFDBGFileAnalyzer(
            OMFDBGFileAccessor fileAccessor,
            IFileAnalyzerProgress? progress,
            bool trackXRefs,
            CancellationToken cancellationToken,
            IFileDisassembler? disassembler) : base(fileAccessor, progress, trackXRefs, cancellationToken, disassembler, LocatorHttpPolicy.None)
        {
            _omfDbgFile = fileAccessor.OMFDBGFile;
        }

        protected override ViewWriter CreateViewWriter()
        {
            //CreateViewWriter is called by the base ctor
            var omfDbgFile = (OMFDBGFile) _fileAccessor.File;

            return new ViewByteViewWriter(
                new SimpleViewWriterHelper(omfDbgFile),
                omfDbgFile.CreateByteViewProvider(_fileAccessor),
                ViewMode.Default,
                _fileAccessor,
                null,
                this,
                LocatorHttpPolicy.None,
                _progress
            );
        }

        public override void Execute() => ExecuteData();
    }
}
