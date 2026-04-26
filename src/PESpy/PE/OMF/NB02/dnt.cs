using System;
using System.Diagnostics;
using ClrDebug.OMF;
using PESpy.View;

namespace PESpy
{
    //"DirEntry" in cvexefmt.h, "DNT" (Directory eNTry?) in the Microsoft C 6.0 Developer's Toolkit Reference

    /// <summary>
    /// Represents an NB02 Directory Entry.<para/>
    /// Described as "DirEntry" in cvexefmt.h, however is listed as "dnt" (Directory eNTry?) in section 3.7 of the Microsoft C 6.0 Developer's Toolkit Reference (https://www.pcjs.org/documents/books/mspl13/c/ctoolkit/)
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public readonly struct dnt : IValue, IViewable
    {
        private string DebuggerDisplay()
        {
            switch (SubSection)
            {
                case SST.SSTMODULE:
                case SST.sstModule:
                    return $"{SubSection} {Data}";

                default:
                    return SubSection.ToString();
            }
        }

        private const int SubSectionOffset = 0;
        private const int iModOffset = 2;
        private const int lfoOffset = 4;
        private const int cbOffset = 8;

        public SST SubSection => (SST) chunk.PeekUInt16(SubSectionOffset);

        public ushort iMod => chunk.PeekUInt16(iModOffset);

        public int lfo => chunk.PeekInt32(lfoOffset);

        public ushort cb => chunk.PeekUInt16(cbOffset);

        public object Data { get; }

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(ushort) + //SubSection
            sizeof(ushort) + //iMod
            sizeof(int) + //lfo
            sizeof(ushort); //cb

        private readonly MemoryChunk chunk;

        internal dnt(in MemoryChunk chunk, in MemoryChunk outerChunk)
        {
            this.chunk = chunk;
            Data = GetData(SubSection, outerChunk.Slice(lfo), cb);
        }

        private static object GetData(SST subSection, in MemoryChunk valueChunk, int length)
        {
            var isLEFile = valueChunk.File().Kind == FileKind.LE;

            switch (subSection)
            {
                case SST.SSTMODULE:
                    if (isLEFile)
                    {
                        var smd32 = new smd32(valueChunk);
                        Debug.Assert(smd32.StructSize == length);
                        return smd32;
                    }
                    else
                    {
                        var smd = new smd(valueChunk);
                        Debug.Assert(smd.StructSize == length);
                        return smd;
                    }

                case SST.SSTPUBLIC:
                    if (isLEFile)
                        return OMFReader.ReadNB02Publics32(valueChunk, length);
                    else
                        return OMFReader.ReadNB02Publics16(valueChunk, length);

                case SST.SSTTYPES:
                case SST.SSTCOMPACTED: //Has the same format as SSTTYPES
                    return OMFReader.ReadNB02Types(valueChunk, length);

                case SST.SSTSYMBOLS:
                    return OMFReader.ReadNB02Symbols(valueChunk, length);

                case SST.SSTLIBRARIES:
                {
                    //See the comments in OMFDirEntry regarding the typedef for sstLibraries

                    //The first entry is an empty string, because library indices are 1-based
                    var read = 0;

                    using var libraries = new PooledList<FixedAnsiString>();

                    while (read < length)
                    {
                        var strLen = valueChunk.PeekByte(read);
                        read++;

                        var str = valueChunk.PeekAnsiFixedLength(read, strLen);
                        read += strLen;
                        libraries.Add(str);
                    }

                    return new RawValue<FixedAnsiString[]>(valueChunk.AbsoluteOffset, libraries.ToArray());
                }

                case SST.SSTIMPORTS: //Not listed in the spec, not present in my samples and I can't find any examples of its use
                    Debug.Assert(false);
                    return null;

                case SST.SSTSRCLINES: //Only emitted in NB00. NB01 and NB02 use SSTSRCLNSEG
                case SST.SSTSRCLNSEG: //Same format as SSTSRCLINES except there's also a segment index
                    if (isLEFile)
                        return OMFReader.ReadNB02SourceLines32(valueChunk, length, subSection == SST.SSTSRCLNSEG);
                    else
                        return OMFReader.ReadNB02SourceLines16(valueChunk, length, subSection == SST.SSTSRCLNSEG);

                default:
                    Debug.Assert(false, $"Don't know how to handle {subSection}");
                    return null;
            }
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.dnt, StructSize);

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(SubSection), SubSectionOffset, SubSection, sizeof(ushort));
                    break;

                case 1:
                    structWriter.WriteField(nameof(iMod), iModOffset, iMod);
                    break;

                case 2:
                    structWriter.WriteField(nameof(lfo), lfoOffset, lfo);
                    break;

                case 3:
                    structWriter.WriteField(nameof(cb), cbOffset, cb);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return SubSection.ToString();
        }
    }
}
