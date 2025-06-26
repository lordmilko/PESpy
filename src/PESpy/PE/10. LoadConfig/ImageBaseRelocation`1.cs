using System;
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
            writer.NewStruct(nameof(IMAGE_BASE_RELOCATION), this, ViewKind.ImageBaseRelocation, SizeOfBlock);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(VirtualAddress), VirtualAddress);
            s.WriteField(nameof(SizeOfBlock), SizeOfBlock);

            s.WriteInline(Entries);

            return s.ToArray();
        }
    }
}
