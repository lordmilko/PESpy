using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PESpy.Tests
{
    class TestMemoryReader : IMemoryReader
    {
        private int absoluteBlockStart;
        private int size;
        private IntPtr buffer;

        internal List<(long address, int size)> Requests = new List<(long address, int size)>();

        internal unsafe TestMemoryReader(int absoluteBlockStart, int size)
        {
            this.absoluteBlockStart = absoluteBlockStart;
            this.size = size;
            buffer = Marshal.AllocHGlobal(size);

            var numPages = size / RemoteMemoryBlock.PageSize;

            for (var i = 0; i < numPages; i++)
            {
                for (var j = 0; j < RemoteMemoryBlock.PageSize; j++)
                {
                    *(byte*) (buffer + (i * RemoteMemoryBlock.PageSize) + j) = (byte) (i + 1);
                }
            }
        }

        public unsafe void ReadVirtual(long address, IntPtr buffer, int size)
        {
            //address = baseAddress + RVA of block + offset into block.
            //Remove baseAddress + RVA, giving us just offset into block
            var offset = (int) (address - absoluteBlockStart);

            Requests.Add((offset, size));

            var source = new Span<byte>((byte*) this.buffer, this.size);
            var dest = new Span<byte>((byte*) buffer, size);

            source.Slice(offset, size).CopyTo(dest);
        }
    }

    [TestClass]
    public class RemoteMemoryBlockTests
    {
        [TestMethod]
        public void RemoteMemoryBlock_DemandMissingBlock()
        {
            Test(
                numPages: 2,

                //Demand an area from one page
                (start, block) => block.Demand(start, 10),
                requests =>
                {
                    //We should have demanded the entire page
                    Assert.AreEqual(1, requests.Count);
                    Assert.AreEqual(0, requests[0].address);
                    Assert.AreEqual(RemoteMemoryBlock.PageSize, requests[0].size);
                }
            );
        }

        [TestMethod]
        public void RemoteMemoryBlock_DemandExistingBlock()
        {
            //Demand 1 block and demand it again. We should already have the data
            Test(
                numPages: 2,

                (start, block) =>
                {
                    block.Demand(start, 10);

                    block.Demand(start, 20);
                },
                requests =>
                {
                    //We should have demanded the entire page on the first request,
                    //and done nothing on the second
                    Assert.AreEqual(1, requests.Count);
                    Assert.AreEqual(0, requests[0].address);
                    Assert.AreEqual(RemoteMemoryBlock.PageSize, requests[0].size);
                }
            );
        }

        [TestMethod]
        public void RemoteMemoryBlock_DemandTwoMissingBlocks()
        {
            //Demand 2 blocks, neither of which we have. We should load both blocks
            Test(
                numPages: 2,
                (start, block) => block.Demand(start, RemoteMemoryBlock.PageSize * 2),
                requests =>
                {
                    Assert.AreEqual(1, requests.Count);
                    Assert.AreEqual(0, requests[0].address);
                    Assert.AreEqual(RemoteMemoryBlock.PageSize * 2, requests[0].size);
                }
            );
        }

        [TestMethod]
        public void RemoteMemoryBlock_DemandTwoBlocks_HaveFirst()
        {
            Test(
                numPages: 2,
                (start, block) =>
                {
                    //Demand the first page
                    block.Demand(start, 10);

                    //Demand both pages. We should start from the second page
                    block.Demand(start, (int) (RemoteMemoryBlock.PageSize * 2));
                },
                requests =>
                {
                    Assert.AreEqual(2, requests.Count);

                    Assert.AreEqual(0, requests[0].address);
                    Assert.AreEqual(RemoteMemoryBlock.PageSize, requests[0].size);

                    //We start after the first page, and should have read half a page's worth
                    Assert.AreEqual(RemoteMemoryBlock.PageSize, requests[1].address);
                    Assert.AreEqual(RemoteMemoryBlock.PageSize, requests[1].size);
                }
            );
        }

        [TestMethod]
        public void RemoteMemoryBlock_DemandTwoBlocks_HaveSecond()
        {
            Test(
                numPages: 2,
                (start, block) =>
                {
                    //Demand the second page
                    block.Demand(start + RemoteMemoryBlock.PageSize, 10);

                    //Demand both pages. We should start from the second page
                    block.Demand(start, (int) (RemoteMemoryBlock.PageSize * 2));
                },
                requests =>
                {
                    Assert.AreEqual(2, requests.Count);

                    Assert.AreEqual(RemoteMemoryBlock.PageSize, requests[0].address);
                    Assert.AreEqual(RemoteMemoryBlock.PageSize, requests[0].size);

                    //We already have second, so we just need first, however we don't currently
                    //try and determine the maximum sequence of read pages we already have at the end,
                    //so we end up re-reading both pages
                    Assert.AreEqual(0, requests[1].address);
                    Assert.AreEqual(RemoteMemoryBlock.PageSize * 2, requests[1].size);
                }
            );
        }

        [TestMethod]
        public void RemoteMemoryBlock_DemandSingleBlock_HaveAllBlocks()
        {
            //Demand all blocks, and demand again. We should see that we already have all blocks
            throw new NotImplementedException();
        }

        [TestMethod]
        public void RemoteMemoryBlock_DemandMissingBlock_HasSinglePage_LessThanOnePageLarge()
        {
            //We should have demanded the maximum number of bytes (which is less than a single page)

            Test(
                numPages: 0.5,
                (start, block) => block.Demand(start, 10),
                requests =>
                {
                    Assert.AreEqual(1, requests.Count);
                    Assert.AreEqual(0, requests[0].address);
                    Assert.AreEqual(RemoteMemoryBlock.PageSize / 2, requests[0].size);
                }
            );
        }

        [TestMethod]
        public void RemoteMemoryBlock_DemandTwoMissingBlocks_LastPageLessThanOnePageLarge()
        {
            Test(
                numPages: 1.5,
                (start, block) => block.Demand(start, (int) (RemoteMemoryBlock.PageSize * 1.5)),
                requests =>
                {
                    Assert.AreEqual(1, requests.Count);
                    Assert.AreEqual(0, requests[0].address);
                    Assert.AreEqual(RemoteMemoryBlock.PageSize * 1.5, requests[0].size);
                }
            );
        }

        [TestMethod]
        public void RemoteMemoryBlock_DemandMissingBlock_LastPage_LastPageLessThanOnePageLarge()
        {
            //We already have the first page, so we should correctly see that we just need to get the last page,
            //and correctly calculate how many bytes it is

            Test(
                numPages: 1.5,
                (start, block) =>
                {
                    //Demand the first page
                    block.Demand(start, 10);

                    //Demand both pages. We should start from the second page
                    block.Demand(start, (int) (RemoteMemoryBlock.PageSize * 1.5));
                },
                requests =>
                {
                    Assert.AreEqual(2, requests.Count);

                    Assert.AreEqual(0, requests[0].address);
                    Assert.AreEqual(RemoteMemoryBlock.PageSize, requests[0].size);

                    //We start after the first page, and should have read half a page's worth
                    Assert.AreEqual(RemoteMemoryBlock.PageSize, requests[1].address);
                    Assert.AreEqual(RemoteMemoryBlock.PageSize / 2, requests[1].size);
                }
            );
        }

        private void Test(
            double numPages,
            Action<int, RemoteMemoryBlock> demand,
            Action<List<(long address, int size)>> verify)
        {
            var size = (int) (RemoteMemoryBlock.PageSize * numPages); //Pages are 0x1000 in size

            var baseAddress = 0x4000000;
            var start = 0x1000;

            var reader = new TestMemoryReader(baseAddress + start, size);

            var block = new RemoteMemoryBlock(
                baseAddress: 0x4000000,
                rva: start,
                size: size,
                reader: reader,
                provider: null,
                null,
                IntPtr.Size == 4
            );

            demand(start, block);

            verify(reader.Requests);
        }
    }
}
