using System;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_IMPORT_BY_NAME"/> structure.
    /// </summary>
    public readonly struct ImageImportByName : IValue, IViewable
    {
        private const int HintOffset = 0;
        private const int NameOffset = 2;

        //Used to index into the "export name pointer table". If that fails, a full search is done to resolve the import
        //at runtime
        public short Hint => chunk.PeekInt16(HintOffset);

        public AnsiString Name => chunk.PeekAnsiNullTerminatedString(NameOffset);

        public int Offset => chunk.AbsoluteOffset;

        internal int StructSize =>
            sizeof(short) +
            Name.Length + 1;

        private readonly MemoryChunk chunk;

        internal ImageImportByName(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_IMPORT_BY_NAME, this, ViewKind.ImageImportByName, StructSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Hint), HintOffset, Hint);
                    break;

                case 1:
                    structWriter.WriteAnsiNullTerminatedField(nameof(Name), NameOffset, Name);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
