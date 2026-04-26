using System;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents a type whose API contract says is an <see cref="IMAGE_BASE_RELOCATION"/> but has non-standard Type/Offset entries within it.
    /// </summary>
    /// <typeparam name="T">The type of entry contained in this object in lieu of the normal Type/Offset structure.</typeparam>
    public readonly struct ImageBaseRelocation<T> : IValue, IViewable where T : IValue, IViewable
    {
        private const int VirtualAddressOffset = 0;
        private const int SizeOfBlockOffset = 4;
        private const int EntriesOffset = 8;

        public int VirtualAddress { get; }

        public int SizeOfBlock { get; }

        public T[] Entries { get; }

        public int Offset { get; }

        public ImageBaseRelocation(int offset, int virtualAddress, int sizeOfBlock, T[] entries)
        {
            Offset = offset;

            VirtualAddress = virtualAddress;
            SizeOfBlock = sizeOfBlock;
            Entries = entries;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.ImageBaseRelocation, SizeOfBlock);

        int IViewable.NumChildren() => 2 + Entries.Length;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(VirtualAddress), VirtualAddressOffset, VirtualAddress);
                    break;

                case 1:
                    structWriter.WriteField(nameof(SizeOfBlock), SizeOfBlockOffset, SizeOfBlock);
                    break;

                default:
                    structWriter.WriteInline(Entries[index - 2]);
                    break;
            }
        }
    }
}
