using System;
using System.Collections.Generic;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public readonly struct ImageDynamicRelocation : IValue, IViewable
    {
        public ImageDynamicRelocationKind Symbol { get; }

        public int BaseRelocSize { get; }

        //IMAGE_DYNAMIC_RELOCATION says that this field is called BaseReloactions,
        //however when Symbol == 7 this is a ImageFunctionOverrideHeader
        public object Data { get; }

        public int Offset { get; }

        internal ImageDynamicRelocation(ref FileReader reader, PEFile peFile)
        {
            Offset = (int) reader.Position;

            Symbol = (ImageDynamicRelocationKind) (peFile.OptionalHeader.Magic == PEMagic.PE32 ? reader.ReadUInt32() : reader.ReadInt64());
            BaseRelocSize = reader.ReadInt32(); //This appears to be the size of everything that comes after this member (so doesn't include Symbol and BaseRelocSize)

            var end = (int) reader.Position + BaseRelocSize;

            //What comes next depends on Symbol, which specifies a version format

            switch (Symbol)
            {
                case ImageDynamicRelocationKind.GUARD_RF_PROLOGUE: //1
                case ImageDynamicRelocationKind.GUARD_RF_EPILOGUE: //2
                    Debug.Assert(false, $"Reading {Symbol} is not implemented");
                    Data = null;
                    break;

                case ImageDynamicRelocationKind.GUARD_IMPORT_CONTROL_TRANSFER: //3
                {
                    //Don't know how many entries each ImageBaseRelocation will have
                    var list = new List<ImageBaseRelocation<ImageImportControlTransferDynamicRelocation>>();

                    while (reader.Position < end)
                    {
                        var offset = (int) reader.Position;

                        var virtualAddress = reader.ReadInt32();
                        var sizeOfBlock = reader.ReadInt32();

                        var numEntries = (sizeOfBlock - 8) / 4;

                        var entries = new ImageImportControlTransferDynamicRelocation[numEntries];

                        for (var i = 0; i < numEntries; i++)
                            entries[i] = new ImageImportControlTransferDynamicRelocation(ref reader);

                        list.Add(new ImageBaseRelocation<ImageImportControlTransferDynamicRelocation>(offset, virtualAddress, sizeOfBlock, entries));

                        //Must be 32-bit aligned
                        var alignedPosition = (reader.Position + 3) & ~3;

                        Debug.Assert(alignedPosition == reader.Position); //todo: dont know 100% if we have to be aligned

                        while (reader.Position < alignedPosition)
                            reader.ReadByte();
                    }

                    Data = list.ToArray();
                    break;
                }

                case ImageDynamicRelocationKind.GUARD_INDIR_CONTROL_TRANSFER: //4
                {
                    //Don't know how many entries each ImageBaseRelocation will have
                    var list = new List<ImageBaseRelocation<ImageIndirControlTransferDynamicRelocation>>();

                    while (reader.Position < end)
                    {
                        var offset = (int) reader.Position;

                        var virtualAddress = reader.ReadInt32();
                        var sizeOfBlock = reader.ReadInt32();

                        var numEntries = (sizeOfBlock - 8) / 2;

                        var entries = new ImageIndirControlTransferDynamicRelocation[numEntries];

                        for (var i = 0; i < numEntries; i++)
                            entries[i] = new ImageIndirControlTransferDynamicRelocation(ref reader);

                        list.Add(new ImageBaseRelocation<ImageIndirControlTransferDynamicRelocation>(offset, virtualAddress, sizeOfBlock, entries));

                        //Must be 32-bit aligned
                        var alignedPosition = (reader.Position + 3) & ~3;

                        Debug.Assert(alignedPosition == reader.Position); //todo: dont know 100% if we have to be aligned

                        while (reader.Position < alignedPosition)
                            reader.ReadByte();
                    }

                    Data = list.ToArray();
                    break;
                }

                case ImageDynamicRelocationKind.GUARD_SWITCHTABLE_BRANCH: //5
                {
                    //Don't know how many entries each ImageBaseRelocation will have
                    var list = new List<ImageBaseRelocation<ImageSwitchTableBranchDynamicRelocation>>();

                    while (reader.Position < end)
                    {
                        var offset = (int) reader.Position;

                        var virtualAddress = reader.ReadInt32();
                        var sizeOfBlock = reader.ReadInt32();

                        var numEntries = (sizeOfBlock - 8) / 2;

                        var entries = new ImageSwitchTableBranchDynamicRelocation[numEntries];

                        for (var i = 0; i < numEntries; i++)
                            entries[i] = new ImageSwitchTableBranchDynamicRelocation(ref reader);

                        list.Add(new ImageBaseRelocation<ImageSwitchTableBranchDynamicRelocation>(offset, virtualAddress, sizeOfBlock, entries));

                        //Must be 32-bit aligned
                        var alignedPosition = (reader.Position + 3) & ~3;

                        Debug.Assert(alignedPosition == reader.Position); //todo: dont know 100% if we have to be aligned

                        while (reader.Position < alignedPosition)
                            reader.ReadByte();
                    }

                    Data = list.ToArray();
                    break;
                }

                case ImageDynamicRelocationKind.FUNCTION_OVERRIDE: //7
                    Data = new ImageFunctionOverrideHeader(ref reader, end);
                    break;

                default:
                {
                    //When parsing ntoskrnl you can get strange values starting with FFFF.
                    //This is apparently related to PTE randomization https://blog.csdn.net/zhuhuibeishadiao/article/details/110172123
                    //Not really sure what to do, but treating the entries as an array of regular old ImageBaseRelocation seems to work
                    var list = new List<ImageBaseRelocation>();

                    while (reader.Position < end)
                    {
                        list.Add(new ImageBaseRelocation(ref reader));

                        //Must be 32-bit aligned
                        var alignedPosition = (reader.Position + 3) & ~3;

                        Debug.Assert(alignedPosition == reader.Position); //todo: dont know 100% if we have to be aligned

                        while (reader.Position < alignedPosition)
                            reader.ReadByte();
                    }

                    Data = list.ToArray();
                    break;
                }
            }

            Debug.Assert(end == reader.Position);
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("IMAGE_DYNAMIC_RELOCATION", this, ViewKind.ImageDynamicRelocation);

            s.WriteField(nameof(Symbol), Symbol, ((PEViewWriter) writer).Is32Bit ? 4 : 8);
            s.WriteField(nameof(BaseRelocSize), BaseRelocSize);

            switch (Symbol)
            {
                case ImageDynamicRelocationKind.GUARD_RF_PROLOGUE: //1
                case ImageDynamicRelocationKind.GUARD_RF_EPILOGUE: //2
                    Debug.Assert(false, $"Writing {Symbol} is not implemented");
                    break;

                case ImageDynamicRelocationKind.GUARD_IMPORT_CONTROL_TRANSFER: //3
                    s.WriteInline((ImageBaseRelocation<ImageImportControlTransferDynamicRelocation>[]) Data);
                    break;

                case ImageDynamicRelocationKind.GUARD_INDIR_CONTROL_TRANSFER: //4
                    s.WriteInline((ImageBaseRelocation<ImageIndirControlTransferDynamicRelocation>[]) Data);
                    break;

                case ImageDynamicRelocationKind.GUARD_SWITCHTABLE_BRANCH: //5
                    s.WriteInline((ImageBaseRelocation<ImageSwitchTableBranchDynamicRelocation>[]) Data);
                    break;

                case ImageDynamicRelocationKind.FUNCTION_OVERRIDE: //7
                    s.WriteInline((ImageFunctionOverrideHeader) Data);
                    break;

                default: //ntoskrnl
                    s.WriteInline((ImageBaseRelocation[]) Data);
                    break;
            }
        }

        public override string ToString()
        {
            return Symbol.ToString();
        }
    }
}
