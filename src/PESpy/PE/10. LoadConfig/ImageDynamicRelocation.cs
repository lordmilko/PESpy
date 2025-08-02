using System;
using System.Collections.Generic;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public struct ImageDynamicRelocation : IValue, IViewable
    {
        public ImageDynamicRelocationKind Symbol => (ImageDynamicRelocationKind) chunk.PeekPointer(0);

        //This appears to be the size of everything that comes after this member (so doesn't include Symbol and BaseRelocSize)
        public int BaseRelocSize => chunk.PeekInt32(chunk.PointerSize);

        //IMAGE_DYNAMIC_RELOCATION says that this field is called BaseReloactions,
        //however when Symbol == 7 this is a ImageFunctionOverrideHeader
        private object? data;

        public object? Data
        {
            get
            {
                if (data == null)
                {
                    //What comes next depends on Symbol, which specifies a version format

                    switch (Symbol)
                    {
                        case ImageDynamicRelocationKind.GUARD_RF_PROLOGUE: //1
                        case ImageDynamicRelocationKind.GUARD_RF_EPILOGUE: //2
                            Debug.Assert(false, $"Reading {Symbol} is not implemented");
                            data = null;
                            break;

                        case ImageDynamicRelocationKind.GUARD_IMPORT_CONTROL_TRANSFER: //3
                        {
                            //Don't know how many entries each ImageBaseRelocation will have
                            using var list = new PooledList<ImageBaseRelocation<ImageImportControlTransferDynamicRelocation>>();

                            var read = chunk.PointerSize + 4;
                            var end = BaseRelocSize + read;

                            while (read < end)
                            {
                                var offset = (int) chunk.AbsoluteOffset + read;

                                var virtualAddress = chunk.PeekInt32(read);
                                var sizeOfBlock = chunk.PeekInt32(read + 4);

                                var numEntries = (sizeOfBlock - 8) / 4;

                                var entries = new ImageImportControlTransferDynamicRelocation[numEntries];

                                for (var i = 0; i < numEntries; i++)
                                    entries[i] = new ImageImportControlTransferDynamicRelocation(chunk.Slice(read + 8 + (i * 4)));

                                list.Add(new ImageBaseRelocation<ImageImportControlTransferDynamicRelocation>(offset, virtualAddress, sizeOfBlock, entries));

                                //Don't know if we need to do 32-bit alignment
                                read += sizeOfBlock;
                            }

                            data = list.ToArray();
                            break;
                        }

                        case ImageDynamicRelocationKind.GUARD_INDIR_CONTROL_TRANSFER: //4
                        {
                            //Don't know how many entries each ImageBaseRelocation will have
                            using var list = new PooledList<ImageBaseRelocation<ImageIndirControlTransferDynamicRelocation>>();

                            var read = chunk.PointerSize + 4;
                            var end = BaseRelocSize + read;

                            while (read < end)
                            {
                                var offset = (int) chunk.AbsoluteOffset + read;

                                var virtualAddress = chunk.PeekInt32(read);
                                var sizeOfBlock = chunk.PeekInt32(read + 4);

                                var numEntries = (sizeOfBlock - 8) / 2;

                                var entries = new ImageIndirControlTransferDynamicRelocation[numEntries];

                                for (var i = 0; i < numEntries; i++)
                                    entries[i] = new ImageIndirControlTransferDynamicRelocation(chunk.Slice(read + 8 + (i * 2)));

                                list.Add(new ImageBaseRelocation<ImageIndirControlTransferDynamicRelocation>(offset, virtualAddress, sizeOfBlock, entries));

                                //Don't know if we need to do 32-bit alignment
                                read += sizeOfBlock;
                            }

                            data = list.ToArray();
                            break;
                        }

                        case ImageDynamicRelocationKind.GUARD_SWITCHTABLE_BRANCH: //5
                        {
                            //Don't know how many entries each ImageBaseRelocation will have
                            using var list = new PooledList<ImageBaseRelocation<ImageSwitchTableBranchDynamicRelocation>>();

                            var read = chunk.PointerSize + 4;
                            var end = BaseRelocSize + read;

                            while (read < end)
                            {
                                var offset = (int) chunk.AbsoluteOffset + read;

                                var virtualAddress = chunk.PeekInt32(read);
                                var sizeOfBlock = chunk.PeekInt32(read + 4);

                                var numEntries = (sizeOfBlock - 8) / 2;

                                var entries = new ImageSwitchTableBranchDynamicRelocation[numEntries];

                                for (var i = 0; i < numEntries; i++)
                                    entries[i] = new ImageSwitchTableBranchDynamicRelocation(chunk.Slice(read + 8 + (i * 2)));

                                list.Add(new ImageBaseRelocation<ImageSwitchTableBranchDynamicRelocation>(offset, virtualAddress, sizeOfBlock, entries));

                                //Don't know if we need to do 32-bit alignment
                                read += sizeOfBlock;
                            }

                            data = list.ToArray();
                            break;
                        }

                        case ImageDynamicRelocationKind.FUNCTION_OVERRIDE: //7
                        {
                            var read = chunk.PointerSize + 4;
                            data = new ImageFunctionOverrideHeader(chunk.Slice(read), BaseRelocSize);
                            break;
                        }

                        default:
                        {
                            //When parsing ntoskrnl you can get strange values starting with FFFF.
                            //This is apparently related to PTE randomization https://blog.csdn.net/zhuhuibeishadiao/article/details/110172123
                            //Not really sure what to do, but treating the entries as an array of regular old ImageBaseRelocation seems to work
                            using var list = new PooledList<ImageBaseRelocation>();

                                var read = chunk.PointerSize + 4;
                                var end = BaseRelocSize + read;

                            while (read < end)
                            {
                                var item = new ImageBaseRelocation(chunk.Slice(read));
                                list.Add(item);
                                read += item.SizeOfBlock;
                            }

                            data = list.ToArray();
                            break;
                        }
                    }
                }

                return data;
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        internal int StructSize(bool is32Bit) =>
            (is32Bit ? 4 : 8) + //Symbol
            sizeof(int) + //BaseRelocSize
            BaseRelocSize;

        private readonly MemoryChunk chunk;

        internal ImageDynamicRelocation(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            data = default;

#if STRESS_TEST
            _ = Data;
#endif
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_DYNAMIC_RELOCATION, this, ViewKind.ImageDynamicRelocation, StructSize(((PEViewWriter) writer).Is32Bit));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(Symbol), Symbol, chunk.PointerSize);
            s.WriteField(nameof(BaseRelocSize), BaseRelocSize);

            switch (Symbol)
            {
                case ImageDynamicRelocationKind.GUARD_RF_PROLOGUE: //1
                case ImageDynamicRelocationKind.GUARD_RF_EPILOGUE: //2
                    Debug.Assert(false, $"Writing {Symbol} is not implemented");
                    break;

                case ImageDynamicRelocationKind.GUARD_IMPORT_CONTROL_TRANSFER: //3
                    s.WriteInline((ImageBaseRelocation<ImageImportControlTransferDynamicRelocation>[]) Data!);
                    break;

                case ImageDynamicRelocationKind.GUARD_INDIR_CONTROL_TRANSFER: //4
                    s.WriteInline((ImageBaseRelocation<ImageIndirControlTransferDynamicRelocation>[]) Data!);
                    break;

                case ImageDynamicRelocationKind.GUARD_SWITCHTABLE_BRANCH: //5
                    s.WriteInline((ImageBaseRelocation<ImageSwitchTableBranchDynamicRelocation>[]) Data!);
                    break;

                case ImageDynamicRelocationKind.FUNCTION_OVERRIDE: //7
                    s.WriteInline((ImageFunctionOverrideHeader) Data!);
                    break;

                default: //ntoskrnl
                    s.WriteInline((ImageBaseRelocation[]) Data!);
                    break;
            }

            return s.ToArray();
        }

        public override string ToString()
        {
            return Symbol.ToString();
        }
    }
}
