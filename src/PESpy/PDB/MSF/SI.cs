using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Describes information about a stream.<para/>
    /// This structure only exists in memory, and is not persisted to disk.
    /// </summary>
    [Source(SourceKind.msf_cpp)]
    [DebuggerDisplay("ByteCount = {ByteCount}, PageList = [{string.Join(\",\", PageList),nq}]")]
    public struct SI : IValue, IViewable
    {
        /* The Stream Info type aggregates two pieces of information together:
         * the size of a stream, and the pages that it spans across. These two pieces
         * of information do not live next to each other however: they are stored in two separate arrays,
         * which are then zipped together to create the Stream Info type.
         *
         * SI records are stored in an array inside the StrmTbl type, the in-memory representation of the stream table */

        /// <summary>
        /// Gets the total number of bytes that exist in this stream. This value is provided from a location external to this structure.
        /// </summary>
        public int ByteCount { get; set; }

        /// <summary>
        /// Gets the list of pages contained in this stream.<para/>
        /// This value must be passed to <see cref="PagedMemoryBlock"/> as an array and so is eagerly read.<para/>
        /// Do not directly add/remove items from this list. Use the appropriate APIs on StreamTable instead
        /// </summary>
        public PN[] PageList { get; }

        public long Offset => chunk.AbsoluteOffset; //This is just the location of the page list, but this type doesn't actually fully exist on disk

        private readonly MemoryChunk chunk;

        //Create an SI from v7 data
        internal SI(in MemoryChunk chunk, int byteCount, int pageSize)
        {
            this.chunk = chunk;
            ByteCount = byteCount;
            var numPages = DivideUp(byteCount, pageSize);

            PageList = chunk.PeekNativeSpan<PN>(0, numPages).ToArray();
        }

        //Create an SI from v2 data
        internal SI(in MemoryChunk chunk, in SI_PERSIST siPersist, int pageSize)
        {
            this.chunk = chunk;
            ByteCount = siPersist.ByteCount;
            var numPages = DivideUp(siPersist.ByteCount, pageSize);

            var pageList = new PN[numPages];
            var pagesSpan = chunk.PeekNativeSpan<ushort>(0, numPages);

            for (var i = 0; i < numPages; i++)
                pageList[i] = pagesSpan[i];

            PageList = pageList;
        }

        //Create an SI from a known set of pages. Used when bootstrapping a new PDB
        internal SI(in MemoryChunk chunk, int byteCount, PN[] pageList)
        {
            this.chunk = chunk;
            ByteCount = byteCount;
            PageList = pageList;
        }

        internal static int DivideUp(int value, int divisor)
        {
            //Integer division always rounds down, even if the result is higher than .5
            //So suppose we have 5000 bytes. If each page is 4096 bytes, 5000/4096 = 1, which is wrong.
            //If we do (5000+4096-1)/4096 however, this gives us 2. The -1 is required because if your size is
            //exactly 4096 bytes, (4096+4096)/4096 = 2, when we wanted 1. (4096+4096-1)/4096 gives 1 as expected
            return (value + divisor - 1) / divisor;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //PageList could cross page boundaries
            writer.WritePagedGlobal(chunk.RelativeOffset, (PagedMemoryBlock) chunk.block, PageList, ViewKind.PN);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        int IViewable.NumChildren() => throw new NotSupportedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();
    }
}
