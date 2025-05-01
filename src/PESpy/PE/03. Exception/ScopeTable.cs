using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PESpy.Native;
using PESpy.View;

#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="SCOPE_TABLE"/> structure.
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    public struct ScopeTable : IValue, IViewable, IEnumerable<ScopeTable.ScopeRecord>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
#if PEFAST
        public int Count => chunk.PeekInt32(0);
#else
        public int Count { get; init; }
#endif

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
#if PEFAST
        private ScopeRecord[]? records;

        public ScopeRecord[] Records
        {
            get
            {
                if (records == null)
                {
                    var results = new ScopeRecord[Count];

                    for (var i = 0; i < Count; i++)
                        results[i] = new ScopeRecord(chunk.Slice(4 + (i * ScopeRecord.StructSize)));

                    records = results;
                }

                return records;
            }
        }
#else
        public ScopeRecord[] Records { get; init; }
#endif

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

#if PEFAST
        private readonly MemoryChunk chunk;

        internal ScopeTable(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            records = default;
        }
#else
        internal ScopeTable(IFileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            Count = reader.ReadInt32();

            var records = new ScopeRecord[Count];

            for (var i = 0; i < Count; i++)
                records[i] = new ScopeRecord(reader);

            Records = records;
        }
#endif

        public IEnumerator<ScopeRecord> GetEnumerator() => Records.Select(v => v).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #region Record

        //The native representation of is an anonymous struct in the ScopeRecord field of the SCOPE_TABLE
        [DebuggerDisplay("BeginAddress = {BeginAddress.ToString(\"X\"),nq}, EndAddress = {EndAddress.ToString(\"X\"),nq}, HandlerAddress = {HandlerAddress.ToString(\"X\"),nq}, JumpTarget = {JumpTarget.ToString(\"X\"),nq}")]
        public readonly struct ScopeRecord : IValue, IViewable
        {
            /// <summary>
            /// Gets the offset of the first instruction contained in the __try block.
            /// </summary>
#if PEFAST
            public int BeginAddress => chunk.PeekInt32(0);
#else
            public int BeginAddress { get; init; }
#endif

            /// <summary>
            /// Gets the offset of the instruction after the last instruction contained in the __try block.<para/>
            /// This value can sometimes be after the last instruction of the __try block but before the next instruction
            /// that follows.<para/>
            /// This value is usually the same as <see cref="JumpTarget"/>, however sometimes there can also be random instructions
            /// in-between.
            /// </summary>
#if PEFAST
            public int EndAddress => chunk.PeekInt32(4);
#else
            public int EndAddress { get; init; }
#endif

            /// <summary>
            /// Gets the offset of the exception filter specified to the __except statement.<para/>
            /// If no custom handler has been specified, this value is EXCEPTION_EXECUTE_HANDLER (1).
            /// Ostensibly, it should also be possible for this value to be EXCEPTION_CONTINUE_SEARCH (0)
            /// and EXCEPTION_CONTINUE_EXECUTION (-1)
            /// </summary>
#if PEFAST
            public int HandlerAddress => chunk.PeekInt32(8);
#else
            public int HandlerAddress { get; init; }
#endif

            /// <summary>
            /// Gets the offset of the first instruction contained in the __except block associated with the __try block.<para/>
            /// This value is usually the same as <see cref="EndAddress"/>, however sometimes there can be random instructions
            /// in-between.
            /// </summary>
#if PEFAST
            public int JumpTarget => chunk.PeekInt32(12);
#else
            public int JumpTarget { get; init; }
#endif

#if PEFAST
            public RawOffset Offset => chunk.AbsoluteOffset;
#else
            public RawOffset Offset { get; }
#endif

            internal const int StructSize =
                sizeof(int) + //BeginAddress
                sizeof(int) + //EndAddress
                sizeof(int) + //HandlerAddress
                sizeof(int);  //JumpTarget

#if PEFAST
            private readonly MemoryChunk chunk;

            internal ScopeRecord(in MemoryChunk chunk)
            {
                this.chunk = chunk;
            }
#else
            internal ScopeRecord(IFileReader reader)
            {
                Offset = (RawOffset) reader.Position;

                reader.FillBuffer(StructSize);

                BeginAddress = reader.ReadInt32();
                EndAddress = reader.ReadInt32();
                HandlerAddress = reader.ReadInt32();
                JumpTarget = reader.ReadInt32();
            }
#endif

            void IViewable.WriteView(ViewWriter writer)
            {
                using var s = writer.CreateStruct("ScopeRecord", this, ViewKind.ScopeRecord);

                s.WriteField(nameof(BeginAddress), BeginAddress);
                s.WriteField(nameof(EndAddress), EndAddress);
                s.WriteField(nameof(HandlerAddress), HandlerAddress);
                s.WriteField(nameof(JumpTarget), JumpTarget);
            }
        }

#endregion

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(SCOPE_TABLE), this, ViewKind.ScopeTable);

            s.WriteField(nameof(Count), Count);
            s.WriteInline(Records);
        }
    }
}
