using System;

namespace PESpy.NE
{
    [Flags]
    public enum EntryFlags
    {
        //newexe.inc

        ENT_PUBLIC = 1,
        ENT_DATA = 2
    }

    //Name based on e32_bundle. Name and shape is made up
    public struct NEBundle
    {
        //newexe.inc
        //Special segment types
        public const int ENT_ABSSEG = 0xfe;
        public const int ENT_MOVEABLE = 0xff;

        public int Count { get; }

        public byte SegmentType { get; }

        public Entry[] Entries { get; }

        internal NEBundle(int count, byte segmentType, Entry[] entries)
        {
            Count = count;
            SegmentType = segmentType;
            Entries = entries;
        }

        public class Entry
        {
            public EntryFlags Flags { get; }

            public short OffsetWithinSegment { get; }

            internal Entry(EntryFlags flags, short offsetWithinSegment)
            {
                Flags = flags;
                OffsetWithinSegment = offsetWithinSegment;
            }
        }

        public class MoveableEntry : Entry
        {
            public byte SegmentNumber { get; }

            public NativeSpan<byte> Int3F { get; }

            internal MoveableEntry(EntryFlags flags, NativeSpan<byte> int3F, byte segmentNumber, short offsetWithinSegment) : base(flags, offsetWithinSegment)
            {
                SegmentNumber = segmentNumber;
                Int3F = int3F;
            }
        }
    }
}
