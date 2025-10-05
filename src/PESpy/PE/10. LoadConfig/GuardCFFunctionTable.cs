using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PESpy.View;

namespace PESpy
{
    internal class GuardCFFunctionTableDebugView
    {
        private GuardCFFunctionTable table;

        public GuardCFFunctionTableDebugView(GuardCFFunctionTable table)
        {
            this.table = table;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public GuardCFFunctionTable.Entry[] Items => table.ToArray();
    }

    //This can allocate a lot of memory given the number of functions that might be present. As such, don't store any entries,
    //and instead lazily retrieve them
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(GuardCFFunctionTableDebugView))]
    public readonly struct GuardCFFunctionTable : IValue, IViewable, IEnumerable<GuardCFFunctionTable.Entry>
    {
        public int Count { get; }

        public int Offset => chunk.AbsoluteOffset;

        internal int StructSize => Count * (sizeof(int) + metadataSize);

        private readonly MemoryChunk chunk;
        private readonly byte metadataSize;

        internal GuardCFFunctionTable(in MemoryChunk chunk, IMAGE_GUARD flags, long functionCount)
        {
            this.chunk = chunk;

            Count = (int) functionCount;

            /*  https://learn.microsoft.com/en-us/windows/win32/secbp/pe-metadata
             *
             * Control Flow Guard defines a Guard Function IDs table ("GFIDS" table for short) that lists all of the addresses in the module
             * that are valid targets of indirect calls via function pointers (and that have opted into GFIDS protection).
             *
             * Per the referenced article:
             *
             *     The GFIDS table is an array of 4 + n bytes, where n is given by ((GuardFlags & IMAGE_GUARD_CF_FUNCTION_TABLE_SIZE_MASK) >> IMAGE_GUARD_CF_FUNCTION_TABLE_SIZE_SHIFT)
             *
             *     “GuardFlags” is the GuardFlags field of the load configuration directory. This allows for extra metadata to be attached to CFG call targets in the
             *     future. The only currently defined metadata is an optional 1-byte extra flags field (“GFIDS flags”) that is attached to each GFIDS entry if any call
             *     targets have metadata
             */

            metadataSize = (byte) ((int) (flags & IMAGE_GUARD.CF_FUNCTION_TABLE_SIZE_MASK) >> ImageLoadConfigDirectory.CF_FUNCTION_TABLE_SIZE_SHIFT);
        }

        public Entry this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new IndexOutOfRangeException();

                var peFile = chunk.PEFile();
                var entry = new Entry(chunk.Slice(index * (sizeof(int) + metadataSize)), metadataSize, peFile);

                return entry;
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(Count, metadataSize, chunk);

        IEnumerator<Entry> IEnumerable<Entry>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //We don't have any globals, but our children do
            foreach (var entry in this)
                ((IViewable) entry).WriteGlobals(writer);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.GuardCFFunctionTable, this, ViewKind.GuardCFFunctionTable, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteInline<GuardCFFunctionTable, Entry>(this);

            return s.ToArray();
        }

        [DebuggerDisplay("{DebuggerDisplay,nq}")]
        public readonly struct Entry : IValue, IViewable
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private string DebuggerDisplay => $"Function = 0x{Function:X}, Flags = {(Flags.HasValue ? Flags.Value.ToString() : "null")}";

            public int Function { get; init; }

            public IMAGE_GUARD_FLAG? Flags { get; init; }

            public RVA<ulong>? XFG { get; init; }

            public int Offset { get; init; }

            internal Entry(in MemoryChunk chunk, int metadataSize, PEFile peFile)
            {
                Offset = (int) chunk.AbsoluteOffset;

                Function = chunk.PeekInt32(0);

                switch (metadataSize)
                {
                    case 0:
                        Flags = null;
                        break;

                    case 1:
                        Flags = (IMAGE_GUARD_FLAG) chunk.PeekByte(4);

                        if ((Flags.Value & IMAGE_GUARD_FLAG.FID_XFG) != 0)
                        {
                            //Functions that are configured to use Extended Flow Guard (XFG) are preceded by an 8 byte signature, that is passed as an argument
                            //when attempting to perform an indirect jump to a function, which is then validated against the signature that is listed behind the
                            //start of the function
                            var xfgAddress = Function - 8;

                            if (peFile.TryGetValueChunkFromSection(xfgAddress, out var xfgChunk))
                            {
                                var signature = xfgChunk.PeekUInt64(0);

                                XFG = new RVA<ulong>(xfgAddress, xfgChunk.AbsoluteOffset, signature);
                            }
                            else
                                XFG = new RVA<ulong>(xfgAddress);
                        }

                        break;

                    default:
                        Debug.Assert(false, $"Don't know how to handle a GFIDS entry of size {metadataSize}");
                        Flags = null;
                        break;
                }
            }

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //We do not need to write the listed address, because the address wasn't listed!
                //We calculated it based on the address stored in Function
                if (XFG != null && XFG.Value.IsValid)
                    writer.WriteGlobal(XFG.Value.ActualOffset, XFG.Value.Value, sizeof(long), ViewKind.XFG);
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct(Strings.GFIDSEntry, this, ViewKind.GuardCFFunctionTable_Entry, sizeof(int) + (Flags != null ? 1 : 0)); //The XFG RVA is not part of the structure

            IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
            {
                using var s = viewWriter.CreateStruct(parent);

                s.WriteField(nameof(Function), Function);

                if (Flags != null)
                    s.WriteField(nameof(Flags), Flags.Value, sizeof(byte));

                return s.ToArray();
            }
        }

        public struct Enumerator : IEnumerator<Entry>
        {
            private readonly MemoryChunk chunk;
            private int index;
            private readonly int count;
            private readonly int metadataSize;
            private readonly PEFile peFile;

            internal Enumerator(int count, int metadataSize, in MemoryChunk chunk)
            {
                this.chunk = chunk;
                this.count = count;
                this.metadataSize = metadataSize;
                peFile = chunk.PEFile();
                index = default;
            }

            public Entry Current { get; private set; }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (index < count)
                {
                    Current = new Entry(chunk.Slice(index * (sizeof(int) + metadataSize)), metadataSize, peFile);
                    index++;
                    return true;
                }

                Current = default;
                return false;
            }

            public void Reset()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
