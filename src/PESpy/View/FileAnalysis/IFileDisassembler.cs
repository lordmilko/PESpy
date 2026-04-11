using System.Collections.Generic;
using System.Threading;

namespace PESpy.View
{
    public interface IFileDisassembler
    {
        void WriteDosStub(FileAccessor fileAccessor, FileAnalyzer fileAnalyzer, in ByteBlob byteBlob);

        void WorkThreadProc(
            FileAccessor fileAccessor,
            FileAnalyzer fileAnalyzer,
            Dictionary<long, int> importMap,
            Queue<WorkItem> globalWorkQueue,
            object globalWorkQueueLock,
            int numThreads,
            CancellationToken cancellationToken);
    }
}
