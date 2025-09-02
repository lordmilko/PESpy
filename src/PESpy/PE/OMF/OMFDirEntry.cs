using System;
﻿using ClrDebug.OMF;
using ClrDebug.PDB;
using PESpy.PDB;
using PESpy.View;

namespace PESpy
{
    [DebuggerDisplay("[{iMod}] {SubSection}")]
    public readonly struct OMFDirEntry : IValue, IViewable
    {
        public SST SubSection => (SST) chunk.PeekUInt16(0);

        public ushort iMod => chunk.PeekUInt16(2);

        public int lfo => chunk.PeekInt32(4);

        public int cb => chunk.PeekInt32(8);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(ushort) + //SubSection
            sizeof(ushort) + //iMod
            sizeof(int) + //lfo
            sizeof(int); //cb

        private readonly MemoryChunk chunk;

        internal OMFDirEntry(
            in MemoryChunk chunk,
            in MemoryChunk outerChunk,
            ISymbolAccessor symbolAccessor,
            ref CV_SIGNATURE lastSignature)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.OMFDirEntry, this, ViewKind.OMFDirEntry, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(SubSection), SubSection, sizeof(ushort));
            s.WriteField(nameof(iMod), iMod);
            s.WriteField(nameof(lfo), lfo);
            s.WriteField(nameof(cb), cb);

            return s.ToArray();
        }

        public override string ToString()
        {
            return SubSection.ToString();
        }
    }
}
