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
    public readonly struct dnt : IValue, IViewable
    {
        public SST SubSection => (SST) chunk.PeekUInt16(0);

        public ushort iMod => chunk.PeekUInt16(2);

        public int lfo => chunk.PeekInt32(4);

        public ushort cb => chunk.PeekUInt16(8);

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
            Data = default;
            Data = GetData(SubSection, outerChunk.Slice(lfo), cb);
        }

        private static object GetData(SST subSection, in MemoryChunk valueChunk, int length)
        {
            switch (subSection)
            {
                case SST.SSTMODULE:
                {
                    var smd = new smd(valueChunk);
                    Debug.Assert(smd.StructSize == length);
                    return smd;
                }

                case SST.SSTPUBLIC:
                    return OMFReader.ReadNB02Publics(valueChunk, length);

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
                    return OMFReader.ReadNB02SourceLines(valueChunk, length, subSection == SST.SSTSRCLNSEG);

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
            writer.NewStruct(Strings.dnt, this, ViewKind.dnt, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(SubSection), SubSection, sizeof(ushort));
            s.WriteField(nameof(iMod), iMod);
            s.WriteField(nameof(lfo), lfo);
            s.WriteField(nameof(cb), cb);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        public override string ToString()
        {
            return SubSection.ToString();
        }
    }
}
