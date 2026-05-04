using System;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    //Type is made up
    public readonly struct TypeMDTokenMap : IViewableValue
    {
        public int NumEntries { get; }

        public Entry[] Entries { get; }

        //Each entry contains a NativeSpan of the specific area of the type data
        //that pertains to it
        public NativeSpan<byte> TypeData { get; }

        public long Offset { get; }

        internal int StructSize => sizeof(int) + (Entries.Length * Entry.StructSize) + TypeData.Length;

        internal TypeMDTokenMap(long offset, int numEntries, Entry[] entries, NativeSpan<byte> typeData)
        {
            Offset = offset;
            NumEntries = numEntries;
            Entries = entries;
            TypeData = typeData;
        }

        public readonly struct Entry : IViewableValue
        {
            private const int TypeIndexOffset = 0;
            private const int OffsetOffset = 4;

            public TypOrEnumType TypeIndex { get; }

            public uint Offset { get; }

            //The top bit is cleared. Interpret this as an array of bytes to parse as an ECMA 335 type signature.
            //Not all 4 bytes may be used
            public int SmallTypeSig { get; }

            public NativeSpan<byte> LargeTypeSig { get; }

            public unsafe bool HasSmallTypeSig => (byte*) LargeTypeSig == default;

            private readonly long structOffset;
            long IValue.Offset => structOffset;

            internal const int StructSize =
                sizeof(int) +
                sizeof(int);

            internal Entry(long structOffset, TypOrEnumType typeIndex, int smallTypeSig)
            {
                this.structOffset = structOffset;
                TypeIndex = typeIndex;
                SmallTypeSig = smallTypeSig;
            }

            internal Entry(long structOffset, TypOrEnumType typeIndex, uint offset, NativeSpan<byte> largeTypeSig)
            {
                this.structOffset = structOffset;
                TypeIndex = typeIndex;
                Offset = offset;
                LargeTypeSig = largeTypeSig;
            }

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct(this, ViewKind.TypeMDTokenMap_Entry, StructSize);

            int IViewable.NumChildren() => HasSmallTypeSig ? 3 : 2;

            void IViewable.WriteChild(int index, ref StructWriter structWriter)
            {
                switch (index)
                {
                    case 0:
                        structWriter.WriteField(nameof(TypeIndex), TypeIndexOffset, (CV_typ_t) TypeIndex);
                        break;

                    case 1:
                        if (HasSmallTypeSig)
                        {
                            //Offset is a bitmask
                            structWriter.WriteBitField("SigInOffset", OffsetOffset, true, sizeof(int), 1);
                        }
                        else
                        {
                            //Offset is just an offset
                            structWriter.WriteField(nameof(Offset), OffsetOffset, Offset);
                        }
                        break;

                    case 2:
                        if (HasSmallTypeSig)
                            structWriter.WriteBitField("TypeSig", OffsetOffset, SmallTypeSig, sizeof(int), 31);
                        else
                            throw new IndexOutOfRangeException();

                        break;

                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override string ToString()
            {
                return TypeIndex.ToString();
            }
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.TypeMDTokenMap, StructSize);

        int IViewable.NumChildren() => 2 + Entries.Length;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            if (index == 0)
            {
                structWriter.WriteField(nameof(NumEntries), 0, NumEntries);
                return;
            }

            var i = index - 1;

            if (i < Entries.Length)
            {
                structWriter.WriteInline(Entries[i]);
            }
            else if (i == Entries.Length)
            {
                structWriter.WriteInline(4 + (Entries.Length * Entry.StructSize), TypeData, ViewKind.TypeMDTokenMap_TypeData);
            }
            else
                throw new IndexOutOfRangeException();
        }
    }
}
