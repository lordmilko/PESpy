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
    public readonly struct ScopeTable : IValue, IViewable, IEnumerable<ScopeTable.ScopeRecord>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int Count { get; init; }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ScopeRecord[] Records { get; init; }

        public RawOffset Offset { get; }

        internal ScopeTable(IFileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            Count = reader.ReadInt32();

            var records = new ScopeRecord[Count];

            for (var i = 0; i < Count; i++)
                records[i] = new ScopeRecord(reader);

            Records = records;
        }

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
            public int BeginAddress { get; init; }

            /// <summary>
            /// Gets the offset of the instruction after the last instruction contained in the __try block.<para/>
            /// This value can sometimes be after the last instruction of the __try block but before the next instruction
            /// that follows.<para/>
            /// This value is usually the same as <see cref="JumpTarget"/>, however sometimes there can also be random instructions
            /// in-between.
            /// </summary>
            public int EndAddress { get; init; }

            /// <summary>
            /// Gets the offset of the exception filter specified to the __except statement.<para/>
            /// If no custom handler has been specified, this value is EXCEPTION_EXECUTE_HANDLER (1).
            /// Ostensibly, it should also be possible for this value to be EXCEPTION_CONTINUE_SEARCH (0)
            /// and EXCEPTION_CONTINUE_EXECUTION (-1)
            /// </summary>
            public int HandlerAddress { get; init; }

            /// <summary>
            /// Gets the offset of the first instruction contained in the __except block associated with the __try block.<para/>
            /// This value is usually the same as <see cref="EndAddress"/>, however sometimes there can be random instructions
            /// in-between.
            /// </summary>
            public int JumpTarget { get; init; }

            public RawOffset Offset { get; }

            internal const int StructSize =
                sizeof(int) + //BeginAddress
                sizeof(int) + //EndAddress
                sizeof(int) + //HandlerAddress
                sizeof(int);  //JumpTarget

            internal ScopeRecord(IFileReader reader)
            {
                Offset = (RawOffset) reader.Position;

                reader.FillBuffer(StructSize);

                BeginAddress = reader.ReadInt32();
                EndAddress = reader.ReadInt32();
                HandlerAddress = reader.ReadInt32();
                JumpTarget = reader.ReadInt32();
            }

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
