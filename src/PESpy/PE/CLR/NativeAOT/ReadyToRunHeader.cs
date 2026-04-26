using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.NativeAOT
{
    public struct ReadyToRunHeader : IViewableValue
    {
        internal const int R2RSignature = 0x00525452; //R2R
        private const int SignatureOffset = 0;
        private const int MajorVersionOffset = 4;
        private const int MinorVersionOffset = 6;
        private const int FlagsOffset = 8;
        public const short NumberOfSectionsOffset = 12;
        public const byte EntrySizeOffset = 14;
        internal const byte EntryTypeOffset = 15;

        public int Signature => chunk.PeekInt32(SignatureOffset);
        public short MajorVersion => chunk.PeekInt16(MajorVersionOffset);
        public short MinorVersion => chunk.PeekInt16(MinorVersionOffset);
        public int Flags => chunk.PeekInt32(FlagsOffset);

        public short NumberOfSections => chunk.PeekInt16(NumberOfSectionsOffset);

        //The size of a ModuleInfoRow. The shape of this type has changed over time. Prior to https://github.com/dotnet/runtime/pull/124202
        //the structure of ModuleRowInfo was SectionId, Flags, Start, End. Now it's SectionId, Length, Start
        public byte EntrySize => chunk.PeekByte(EntrySizeOffset);
        public byte EntryType => chunk.PeekByte(EntryTypeOffset); //ReadyToRunHeaderNode.GetData always emits "1"

        // Array of sections follows.

        private IModuleInfoRow[] _sections;

        public IModuleInfoRow[] Sections
        {
            get
            {
                if (_sections == null)
                {
                    //MajorVersion and MinorVersion come from ModuleHeaders.h. Generally speaking
                    //these values are incremented when changes are made to the NativeAOT metadata,
                    //but not always. When ModuleInfoRow was changed, the version was 18.1 and
                    //it doesn't seem like it was incremented, so for now we'll just check whether
                    //we're below 18.1
                    if (MajorVersion < 18 || MinorVersion == 0) //18.0 is OK
                    {
                        Debug.Assert(EntrySize == 24);

                        var result = new IModuleInfoRow[NumberOfSections];

                        var read = FixedStructSize;

                        var entrySize = EntrySize;

                        for (var i = 0; i < result.Length; i++)
                        {
                            result[i] = new ModuleInfoRowV1(chunk.Slice(read));
                            read += entrySize;
                        }

                        _sections = result;
                    }
                    else
                    {
                        //If we're after https://github.com/dotnet/runtime/pull/124202
                        //the size will only be 12 bytes, and the layout will be different
                        throw new NotImplementedException();
                    }
                }

                return _sections;
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            sizeof(int) + //Signature
            sizeof(short) + //MajorVersion
            sizeof(short) + //MinorVersion
            sizeof(int) + //Flags
            sizeof(short) + //NumberOfSections
            sizeof(byte) + //EntrySize
            sizeof(byte); //EntryType

        internal int StructSize => FixedStructSize + (NumberOfSections * EntrySize);

        private readonly MemoryChunk chunk;

        internal ReadyToRunHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            foreach (var section in Sections)
                writer.RelayGlobals(section);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.NativeAOTReadyToRunHeader, StructSize);

        int IViewable.NumChildren() => 7 + NumberOfSections;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Signature), SignatureOffset, Signature);
                    break;

                case 1:
                    structWriter.WriteField(nameof(MajorVersion), MajorVersionOffset, MajorVersion);
                    break;

                case 2:
                    structWriter.WriteField(nameof(MinorVersion), MinorVersionOffset, MinorVersion);
                    break;

                case 3:
                    structWriter.WriteField(nameof(Flags), FlagsOffset, Flags);
                    break;

                case 4:
                    structWriter.WriteField(nameof(NumberOfSections), NumberOfSectionsOffset, NumberOfSections);
                    break;

                case 5:
                    structWriter.WriteField(nameof(EntrySize), EntrySizeOffset, EntrySize);
                    break;

                case 6:
                    structWriter.WriteField(nameof(EntryType), EntryTypeOffset, EntryType);
                    break;

                default:
                    structWriter.WriteInline(Sections[index - 7]);
                    break;
            }
        }
    }
}
