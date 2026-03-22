using System.Collections.Generic;

namespace PESpy.View
{
    public interface IFileDisassembler
    {
        void WriteDosStub(FileAccessor fileAccessor, FileAnalyzer fileAnalyzer, in ByteBlob byteBlob);

        void WorkThreadProc(
            FileAccessor fileAccessor,
            Dictionary<long, int> importMap,
            Queue<WorkItem> globalWorkQueue,
            object globalWorkQueueLock,
            int numThreads);
    }
}
