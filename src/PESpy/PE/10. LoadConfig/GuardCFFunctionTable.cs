using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public readonly struct GuardCFFunctionTable : IValue, IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public Entry[] Entries { get; }

        public int Offset { get; }

        private readonly int length;

#if PEFAST
        internal GuardCFFunctionTable(in MemoryChunk chunk, IMAGE_GUARD flags, long functionCount)
        {
            Offset = chunk.AbsoluteOffset;

            //Eagerly populate. If you're asking for the GuardCFFunctionTable, you want the entries

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

            var metadataSize = (int) (flags & IMAGE_GUARD.CF_FUNCTION_TABLE_SIZE_MASK) >> ImageLoadConfigDirectory.CF_FUNCTION_TABLE_SIZE_SHIFT;

            var entries = new Entry[functionCount];

            int read = 0;

            //On the first run, don't query XFG info, as we've already filled our reader's buffer with the data of all of the entries to be read
            for (var i = 0; i < functionCount; i++)
            {
                entries[i] = new Entry(chunk.Slice(read), metadataSize);
                read += 4 + metadataSize;
            }

            length = read;

            var peFile = chunk.PEFile();

            //Update each entry that should also have an XFG
            for (var i = 0; i < functionCount; i++)
            {
                ref var current = ref entries[i];

                if (current.Flags != null && (current.Flags.Value & IMAGE_GUARD_FLAG.FID_XFG) != 0)
                {
                    //Functions that are configured to use Extended Flow Guard (XFG) are preceded by an 8 byte signature, that is passed as an argument
                    //when attempting to perform an indirect jump to a function, which is then validated against the signature that is listed behind the
                    //start of the function
                    var xfgAddress = current.Function - 8;

                    RVA<ulong> xfg;

                    if (peFile.TryGetValueChunkFromSection(xfgAddress, out var xfgChunk))
                    {
                        var signature = xfgChunk.PeekUInt64(0);

                        xfg = new RVA<ulong>(xfgAddress, xfgChunk.AbsoluteOffset, signature);
                    }
                    else
                        xfg = new RVA<ulong>(xfgAddress);

                    entries[i] = new Entry
                    {
                        Offset = current.Offset,
                        Function = current.Function,
                        Flags = current.Flags,
                        XFG = xfg
                    };
                }
            }

            Entries = entries;
        }
#else
        internal GuardCFFunctionTable(IFileReader reader, PEFile peFile, IMAGE_GUARD flags, long functionCount)
        {
            Offset = (int) reader.Position;

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

            var metadataSize = (int) (flags & IMAGE_GUARD.CF_FUNCTION_TABLE_SIZE_MASK) >> ImageLoadConfigDirectory.CF_FUNCTION_TABLE_SIZE_SHIFT;

            reader.FillBuffer((int) ((4 + metadataSize) * functionCount));

            var entries = new Entry[functionCount];

            //On the first run, don't query XFG info, as we've already filled our reader's buffer with the data of all of the entries to be read
            for (var i = 0; i < functionCount; i++)
                entries[i] = new Entry(reader, metadataSize);

            //Update each entry that should also have an XFG
            for (var i = 0; i < functionCount; i++)
            {
                ref var current = ref entries[i];

                if (current.Flags != null && (current.Flags.Value & IMAGE_GUARD_FLAG.FID_XFG) != 0)
                {
                    //Functions that are configured to use Extended Flow Guard (XFG) are preceded by an 8 byte signature, that is passed as an argument
                    //when attempting to perform an indirect jump to a function, which is then validated against the signature that is listed behind the
                    //start of the function
                    var xfgAddress = current.Function - 8;

                    RVA<ulong> xfg;

                    if (peFile.TryGetOffset(xfgAddress, out var xfgOffset))
                    {
                        reader.Seek(xfgOffset);

                        var signature = reader.ReadUInt64();

                        xfg = new RVA<ulong>(xfgAddress, xfgOffset, signature);
                    }
                    else
                        xfg = new RVA<ulong>(xfgAddress);

                    entries[i] = new Entry
                    {
                        Offset = current.Offset,
                        Function = current.Function,
                        Flags = current.Flags,
                        XFG = xfg
                    };
                }
            }

            Entries = entries;
        }
#endif

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //We don't have any globals, but our children do
            writer.RelayGlobals(Entries);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(nameof(GuardCFFunctionTable), this, ViewKind.GuardCFFunctionTable, length);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteInline(Entries);

            return s.ToArray();
        }

        [DebuggerDisplay("{DebuggerDisplay,nq}")]
        public readonly struct Entry : IValue, IViewable
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private string DebuggerDisplay => $"Function = 0x{Function:X}, Flags = {(Flags.HasValue ? Flags.Value.ToString() : "null")}";

            public int Function { get; init; }

            public IMAGE_GUARD_FLAG? Flags { get; init; }

            public RVA<ulong>? XFG { get; init; } //This is set by GuardCFFunctionTableEntry after this type has been constructed

            public int Offset { get; init; }

#if PEFAST
            internal Entry(in MemoryChunk chunk, int metadataSize)
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
                        break;

                    default:
                        Debug.Assert(false, $"Don't know how to handle a GFIDS entry of size {metadataSize}");
                        Flags = null;
                        break;
                }

                XFG = null;
            }
#else
            internal Entry(IFileReader reader, int metadataSize)
            {
                Offset = (int) reader.Position;

                Function = reader.ReadInt32();

                switch (metadataSize)
                {
                    case 0:
                        Flags = null;
                        break;

                    case 1:
                        Flags = (IMAGE_GUARD_FLAG) reader.ReadByte();
                        break;

                    default:
                        Debug.Assert(false, $"Don't know how to handle a GFIDS entry of size {metadataSize}");
                        Flags = null;
                        break;
                }

                XFG = null;
            }
#endif

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //We do not need to write the listed address, because the address wasn't listed!
                //We calculated it based on the address stored in Function
                if (XFG != null && XFG.Value.IsValid)
                    writer.WriteGlobal(XFG.Value.ActualOffset, XFG.Value.Value, sizeof(long), ViewKind.XFG);
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct("GFIDS Entry", this, ViewKind.GuardCFFunctionTable_Entry, sizeof(int) + (Flags != null ? 1 : 0)); //The XFG RVA is not part of the structure

            IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
            {
                using var s = viewWriter.CreateStruct(parent);

                s.WriteField(nameof(Function), Function);

                if (Flags != null)
                    s.WriteField(nameof(Flags), Flags.Value, sizeof(byte));

                return s.ToArray();
            }
        }
    }
}
