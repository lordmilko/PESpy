using System.Collections.Generic;
using PESpy.View;

namespace PESpy.LIB
{
    public class LongNamesMember : IValue, IViewable
    {
        private readonly ImageArchiveMemberHeader archiveHeader;

        public ref readonly ImageArchiveMemberHeader ArchiveHeader => ref archiveHeader;

        public AnsiString[] Names { get; }

        public int Offset => chunk.AbsoluteOffset;

        public int StructSize => ImageArchiveMemberHeader.StructSize + ArchiveHeader.Size;

        private readonly MemoryChunk chunk;

        internal LongNamesMember(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            archiveHeader = new ImageArchiveMemberHeader(chunk);

            var read = ImageArchiveMemberHeader.StructSize;

            var size = archiveHeader.Size + ImageArchiveMemberHeader.StructSize;

            using var names = new PooledList<AnsiString>();

            while (read < size)
            {
                var str = chunk.PeekAnsiNullTerminatedString(read);
                names.Add(str);
                read += str.Length + 1;
            }

            Names = names.ToArray();
        }

        public AnsiString GetName(int offset)
        {
            if (offset < archiveHeader.Size)
                return chunk.PeekAnsiNullTerminatedString(ImageArchiveMemberHeader.StructSize + offset);

            return default;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.LongNamesMember, this, ViewKind.LongNamesMember, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteInline(ArchiveHeader);

            foreach (var value in Names)
                s.WriteInlineAnsiNullTerminated(value);

            return s.ToArray();
        }
    }
}
