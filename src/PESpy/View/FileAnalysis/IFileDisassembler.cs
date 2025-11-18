using System.Collections.Generic;

namespace PESpy.View
{
    public interface IFileDisassembler
    {
        void WriteDosStub(FileAccessor fileAccessor, FileAnalyzer fileAnalyzer, in ByteBlob byteBlob);

        void WorkThreadProc(
            FileAccessor fileAccessor,
            Queue<WorkItem> globalWorkQueue,
            object globalWorkQueueLock,
            int numThreads);
    }
}
