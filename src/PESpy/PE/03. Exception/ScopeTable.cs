using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PESpy.Native;
using PESpy.View;


namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="SCOPE_TABLE"/> structure.
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    public struct ScopeTable : IValue, IViewable, IEnumerable<ScopeTable.ScopeRecord>
    {
        private const int CountOffset = 0;
        private const int RecordsOffset = 4;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int Count => chunk.PeekInt32(CountOffset);

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        private ScopeRecord[]? records;

        public ScopeRecord[] Records
        {
            get
            {
                if (records == null)
                {
                    var results = new ScopeRecord[Count];

                    for (var i = 0; i < Count; i++)
                        results[i] = new ScopeRecord(chunk.Slice(RecordsOffset + (i * ScopeRecord.StructSize)));

                    records = results;
                }

                return records;
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        internal int StructSize =>
            sizeof(int) + //Count
            (ScopeRecord.StructSize * Count);

        private readonly MemoryChunk chunk;

        internal ScopeTable(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            records = default;
        }

        public IEnumerator<ScopeRecord> GetEnumerator() => Records.Select(v => v).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.SCOPE_TABLE, this, ViewKind.ScopeTable, StructSize);

        int IViewable.NumChildren => 1 + Records.Length;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Count), CountOffset, Count);
                    break;

                default:
                    structWriter.WriteInline(Records[index - 1]);
                    break;
            }
        }

        #region Record

        //The native representation of is an anonymous struct in the ScopeRecord field of the SCOPE_TABLE
        [DebuggerDisplay("BeginAddress = {BeginAddress.ToString(\"X\"),nq}, EndAddress = {EndAddress.ToString(\"X\"),nq}, HandlerAddress = {HandlerAddress.ToString(\"X\"),nq}, JumpTarget = {JumpTarget.ToString(\"X\"),nq}")]
        public readonly struct ScopeRecord : IValue, IViewable
        {
            private const int BeginAddressOffset = 0;
            private const int EndAddressOffset = 4;
            private const int HandlerAddressOffset = 8;
            private const int JumpTargetOffset = 12;

            /// <summary>
            /// Gets the offset of the first instruction contained in the __try block.
            /// </summary>
            public int BeginAddress => chunk.PeekInt32(BeginAddressOffset);

            /// <summary>
            /// Gets the offset of the instruction after the last instruction contained in the __try block.<para/>
            /// This value can sometimes be after the last instruction of the __try block but before the next instruction
            /// that follows.<para/>
            /// This value is usually the same as <see cref="JumpTarget"/>, however sometimes there can also be random instructions
            /// in-between.
            /// </summary>
            public int EndAddress => chunk.PeekInt32(EndAddressOffset);

            /// <summary>
            /// Gets the offset of the exception filter specified to the __except statement.<para/>
            /// If no custom handler has been specified, this value is EXCEPTION_EXECUTE_HANDLER (1).
            /// Ostensibly, it should also be possible for this value to be EXCEPTION_CONTINUE_SEARCH (0)
            /// and EXCEPTION_CONTINUE_EXECUTION (-1)
            /// </summary>
            public int HandlerAddress => chunk.PeekInt32(HandlerAddressOffset);

            /// <summary>
            /// Gets the offset of the first instruction contained in the __except block associated with the __try block.<para/>
            /// This value is usually the same as <see cref="EndAddress"/>, however sometimes there can be random instructions
            /// in-between.
            /// </summary>
            public int JumpTarget => chunk.PeekInt32(JumpTargetOffset);

            public int Offset => chunk.AbsoluteOffset;

            internal const int StructSize =
                sizeof(int) + //BeginAddress
                sizeof(int) + //EndAddress
                sizeof(int) + //HandlerAddress
                sizeof(int);  //JumpTarget

            private readonly MemoryChunk chunk;

            internal ScopeRecord(in MemoryChunk chunk)
            {
                this.chunk = chunk;
            }

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct(Strings.ScopeRecord, this, ViewKind.ScopeRecord, StructSize);

            int IViewable.NumChildren => 4;

            void IViewable.WriteChild(int index, ref StructWriter structWriter)
            {
                switch (index)
                {
                    case 0:
                        structWriter.WriteField(nameof(BeginAddress), BeginAddressOffset, BeginAddress);
                        break;

                    case 1:
                        structWriter.WriteField(nameof(EndAddress), EndAddressOffset, EndAddress);
                        break;

                    case 2:
                        structWriter.WriteField(nameof(HandlerAddress), HandlerAddressOffset, HandlerAddress);
                        break;

                    case 3:
                        structWriter.WriteField(nameof(JumpTarget), JumpTargetOffset, JumpTarget);
                        break;

                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }

        #endregion
    }
}
