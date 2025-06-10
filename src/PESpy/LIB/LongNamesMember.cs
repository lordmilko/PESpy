using System.Collections.Generic;
using PESpy.View;

namespace PESpy.LIB
{
    public class LongNamesMember : IValue, IViewable
    {
        private ImageArchiveMemberHeader archiveHeader;

        public ref readonly ImageArchiveMemberHeader ArchiveHeader => ref archiveHeader;

        public AnsiString[] Names { get; }

        public int Offset { get; }

        internal LongNamesMember(in MemoryChunk chunk)
        {
            Offset = chunk.AbsoluteOffset;
            archiveHeader = new ImageArchiveMemberHeader(chunk);

            var read = ImageArchiveMemberHeader.StructSize;

            var size = archiveHeader.Size + ImageArchiveMemberHeader.StructSize;

            var names = new List<AnsiString>();

            while (read < size)
            {
                var str = chunk.PeekAnsiNullTerminatedString(read);
                names.Add(str);
                read += str.Length + 1;
            }

            Names = names.ToArray();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            throw new System.NotImplementedException();
        }
    }
}
