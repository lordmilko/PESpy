using System;
using System.Collections.Generic;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public struct ImageDynamicRelocation : IValue, IViewable
    {
        private const int SymbolOffset = 0;
        private int BaseRelocSizeOffset => chunk.PointerSize;

        public ImageDynamicRelocationKind Symbol => (ImageDynamicRelocationKind) chunk.PeekPointer(SymbolOffset);

        public SpecialAddressKind SpecialKind
        {
            get
            {
                //https://www.alex-ionescu.com/owning-the-image-object-file-format-the-compiler-toolchain-and-the-operating-system-solving-intractable-performance-problems-through-vertical-engineering/
                //https://github.com/MasonLeeBack/Longhorn_SDK_And_DDK_4074/blob/c07d26bb49ecfa056d00b1dffd8981f50e11c553/SDK/Include/ntddk.h#L3604

                //ntoskrnl.exe has some special values that we want to be able to represent

                var kind = (SpecialAddressKind) Symbol;

                switch (kind)
                {
                    case SpecialAddressKind.PXE_BASE:
                    case SpecialAddressKind.PXE_SELFMAP:
                    case SpecialAddressKind.PPE_BASE:
                    case SpecialAddressKind.PDE_BASE:
                    case SpecialAddressKind.PTE_BASE:
                    case SpecialAddressKind.PXE_TOP:
                    case SpecialAddressKind.PPE_TOP:
                    case SpecialAddressKind.PDE_TOP:
                    case SpecialAddressKind.PTE_TOP:
                        return kind;

                    default:
                        return SpecialAddressKind.None;
                }
            }
        }

        //This appears to be the size of everything that comes after this member (so doesn't include Symbol and BaseRelocSize)
        public int BaseRelocSize => chunk.PeekInt32(BaseRelocSizeOffset);

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

        int IViewable.NumChildren()
        {
            const int baseCount = 2;

            switch (Symbol)
            {
                case ImageDynamicRelocationKind.GUARD_RF_PROLOGUE: //1
                case ImageDynamicRelocationKind.GUARD_RF_EPILOGUE: //2
                    throw new NotImplementedException();

                case ImageDynamicRelocationKind.GUARD_IMPORT_CONTROL_TRANSFER: //3
                    return baseCount + ((ImageBaseRelocation<ImageImportControlTransferDynamicRelocation>[]) Data!).Length;

                case ImageDynamicRelocationKind.GUARD_INDIR_CONTROL_TRANSFER: //4
                    return baseCount + ((ImageBaseRelocation<ImageIndirControlTransferDynamicRelocation>[]) Data!).Length;

                case ImageDynamicRelocationKind.GUARD_SWITCHTABLE_BRANCH: //5
                    return baseCount + ((ImageBaseRelocation<ImageSwitchTableBranchDynamicRelocation>[]) Data!).Length;

                case ImageDynamicRelocationKind.FUNCTION_OVERRIDE: //7
                    return baseCount + 1;

                default: //ntoskrnl
                    return baseCount + ((ImageBaseRelocation[]) Data!).Length;
            }
        }

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Symbol), SymbolOffset, Symbol, chunk.PointerSize);
                    break;

                case 1:
                    structWriter.WriteField(nameof(BaseRelocSize), BaseRelocSizeOffset, BaseRelocSize);
                    break;

                default:
                    switch (Symbol)
                    {
                        case ImageDynamicRelocationKind.GUARD_RF_PROLOGUE: //1
                        case ImageDynamicRelocationKind.GUARD_RF_EPILOGUE: //2
                            Debug.Assert(false, $"Writing {Symbol} is not implemented");
                            break;

                        case ImageDynamicRelocationKind.GUARD_IMPORT_CONTROL_TRANSFER: //3
                            structWriter.WriteInline(((ImageBaseRelocation<ImageImportControlTransferDynamicRelocation>[]) Data!)[index - 2]);
                            break;

                        case ImageDynamicRelocationKind.GUARD_INDIR_CONTROL_TRANSFER: //4
                            structWriter.WriteInline(((ImageBaseRelocation<ImageIndirControlTransferDynamicRelocation>[]) Data!)[index - 2]);
                            break;

                        case ImageDynamicRelocationKind.GUARD_SWITCHTABLE_BRANCH: //5
                            structWriter.WriteInline(((ImageBaseRelocation<ImageSwitchTableBranchDynamicRelocation>[]) Data!)[index - 2]);
                            break;

                        case ImageDynamicRelocationKind.FUNCTION_OVERRIDE: //7
                            structWriter.WriteInline((ImageFunctionOverrideHeader) Data!);
                            break;

                        default: //ntoskrnl
                            structWriter.WriteInline(((ImageBaseRelocation[]) Data!)[index - 2]);
                            break;
                    }
                    break;
            }
        }

        public enum SpecialAddressKind : ulong
        {
            None,

            //Haven't been able to confirm if 0xFFFFDE0000000000 is indeed MM_PFN_DATABASE
            //https://twitter.com/sixtyvividtails/status/1928409811671978120/photo/1

            //AMD64
            PXE_BASE = 0xFFFFF6FB7DBED000,
            PXE_SELFMAP = 0xFFFFF6FB7DBEDF68,
            PPE_BASE = 0xFFFFF6FB7DA00000,
            PDE_BASE = 0xFFFFF6FB40000000,
            PTE_BASE = 0xFFFFF68000000000,

            PXE_TOP = 0xFFFFF6FB7DBEDFFF,
            PPE_TOP = 0xFFFFF6FB7DBFFFFF,
            PDE_TOP = 0xFFFFF6FB7FFFFFFF,
            PTE_TOP = 0xFFFFF6FFFFFFFFFF
        }

        public override string ToString()
        {
            if (Enum.IsDefined(typeof(ImageDynamicRelocationKind), Symbol))
                return Symbol.ToString();

            var specialKind = SpecialKind;

            if (specialKind != SpecialAddressKind.None)
                return specialKind.ToString();

            return ((ulong) Symbol).ToString("X");
        }
    }
}
