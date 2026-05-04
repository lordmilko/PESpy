using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PESpy.Native;
using PESpy.View;


namespace PESpy
{
    internal class ScopeRecordDebugView
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ScopeTable.ScopeRecord[] Items => scopeTable.ToArray();

        private ScopeTable scopeTable;

        internal ScopeRecordDebugView(ScopeTable scopeTable)
        {
            this.scopeTable = scopeTable;
        }
    }

    /// <summary>
    /// Represents the <see cref="SCOPE_TABLE"/> structure.
    /// </summary>
    [Source(SourceKind.winnt_h)] //Also called SCOPE_TABLE_ARM64, SCOPE_TABLE_AMD64 and then typedef'd as SCOPE_TABLE. But the layout of both is the same
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(ScopeRecordDebugView))]
    public struct ScopeTable : IViewableValue, IEnumerable<ScopeTable.ScopeRecord>
    {
        private const int CountOffset = 0;
        private const int RecordsOffset = 4;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int Count => chunk.PeekInt32(CountOffset);

        public long Offset => chunk.AbsoluteOffset;

        internal int StructSize =>
            sizeof(int) + //Count
            (ScopeRecord.StructSize * Count);

        private readonly MemoryChunk chunk;

        internal ScopeTable(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        public ScopeRecord this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new IndexOutOfRangeException();

                return new ScopeRecord(chunk.Slice(sizeof(int) + (index * ScopeRecord.StructSize)));
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(chunk, Count);

        IEnumerator<ScopeRecord> IEnumerable<ScopeRecord>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            foreach (var record in this)
                writer.RelayGlobals(record);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.ScopeTable, StructSize);

        int IViewable.NumChildren() => 1 + Count;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Count), CountOffset, Count);
                    break;

                default:
                    structWriter.WriteInline(this[index - 1]);
                    break;
            }
        }

        public struct Enumerator : IEnumerator<ScopeRecord>
        {
            private readonly int _end;
            private readonly MemoryChunk chunk;
            private int _offset;

            internal Enumerator(in MemoryChunk chunk, int count)
            {
                this.chunk = chunk;
                _offset = sizeof(int);
                _end = sizeof(int) + (count * ScopeRecord.StructSize);
            }

            public ScopeRecord Current { get; private set; }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_offset < _end)
                {
                    Current = new ScopeRecord(chunk.Slice(_offset));
                    _offset += ScopeTable.ScopeRecord.StructSize;
                    return true;
                }

                return false;
            }

            public void Reset()
            {
            }

            public void Dispose()
            {
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

            public long Offset => chunk.AbsoluteOffset;

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
                var structOffset = Offset;

                writer.WriteUniqueRVAXRef(structOffset, BeginAddressOffset, BeginAddress);
                writer.WriteUniqueRVAXRef(structOffset, EndAddressOffset, EndAddress);

                var handlerAddress = HandlerAddress;

                if (handlerAddress > 1)
                    writer.WriteUniqueRVAXRef(structOffset, HandlerAddressOffset, handlerAddress);

                writer.WriteUniqueRVAXRef(structOffset, JumpTargetOffset, JumpTarget);
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct(this, ViewKind.ScopeRecord, StructSize);

            int IViewable.NumChildren() => 4;

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
