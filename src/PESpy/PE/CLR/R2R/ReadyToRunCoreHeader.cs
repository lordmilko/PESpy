using System;
using ClrDebug;
using PESpy.View;

//This is in a namespace to distinguish this type from the NativeAOT types which share similar names
namespace PESpy.R2R
{
    //READYTORUN_CORE_HEADER
    public struct ReadyToRunCoreHeader : IViewableValue
    {
        private const int FlagsOffset = 0;
        private const int NumberOfSectionsOffset = 4;
        private const int SectionsOffset = 8;

        public ReadyToRunFlag Flags => (ReadyToRunFlag) chunk.PeekUInt32(FlagsOffset);

        public int NumberOfSections => chunk.PeekInt32(NumberOfSectionsOffset);

        private ReadyToRunSection[]? sections;

        public ReadyToRunSection[] Sections
        {
            get
            {
                if (sections == null)
                {
                    var results = new ReadyToRunSection[NumberOfSections];

                    for (var i = 0; i < results.Length; i++)
                        results[i] = new ReadyToRunSection(chunk.Slice(SectionsOffset + (i * ReadyToRunSection.StructSize)));

                    sections = results;
                }

                return sections;
            }
        }

        public long Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            sizeof(int) + //Flags
            sizeof(int);  //NumberOfSections

        public int StructSize => FixedStructSize + (NumberOfSections * ReadyToRunSection.StructSize);

        private readonly MemoryChunk chunk;

        internal ReadyToRunCoreHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            sections = default;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.ReadyToRunCoreHeader, StructSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Flags), FlagsOffset, Flags, sizeof(int));
                    break;

                case 1:
                    structWriter.WriteField(nameof(NumberOfSections), NumberOfSectionsOffset, NumberOfSections);
                    break;

                case 2:
                    structWriter.WriteStructField(nameof(Sections), Sections);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
