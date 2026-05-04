using System;
using System.Threading;

namespace PESpy.View
{
    internal class NEFileAnalyzer : FileAnalyzer
    {
        private readonly NEFile _neFile;

        internal NEFileAnalyzer(
            NEFileAccessor fileAccessor,
            IFileAnalyzerProgress? progress,
            bool trackXRefs,
            CancellationToken cancellationToken,
            IFileDisassembler? disassembler) : base(fileAccessor, progress, trackXRefs, cancellationToken, disassembler, LocatorHttpPolicy.None)
        {
            _neFile = fileAccessor.NEFile;
        }

        protected override ViewWriter CreateViewWriter()
        {
            //CreateViewWriter is called by the base ctor
            var neFile = (NEFile) _fileAccessor.File;

            return new ViewByteViewWriter(
                new SimpleViewWriterHelper(neFile),
                neFile.CreateByteViewProvider(_fileAccessor),
                ViewMode.Default,
                _fileAccessor,
                null,
                this,
                LocatorHttpPolicy.None,
                _progress
            );
        }

        public override void Execute() => ExecuteCode();
    }
}
