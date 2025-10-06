using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using PESpy.PDB;
using PESpy.View.Builder;

namespace PESpy.View
{
    public enum ViewTag
    {
        Import = 1,
        DelayImport,
        FatEH
    }

    public abstract partial  class ViewWriter
    {
        //When a struct wants to write another struct inside it, it will push its list of fields to the viewStack,
        //which will cause the inner struct to write itself to this list instead of the main global list
        protected Stack<List<IView>> viewStack = new Stack<List<IView>>();
        protected List<IView> globalList;
        private List<IView>? interceptionList;

        public IReadOnlyList<IView> Current => new ReadOnlyCollection<IView>(globalList);

        internal Extension extension;
        private HashSet<int> trackedAddresses = new HashSet<int>();
        protected Dictionary<ViewTag, HashSet<IView>> taggedViews = new Dictionary<ViewTag, HashSet<IView>>();
        protected ViewMode mode;
        private ViewTag currentTag;
        private ViewKind currentScope;
        private List<IView>? scopedList;
        private Stack<List<IView>> listPool;
        private TryGetOffsetDelegate tryGetViewOffset;
        private Func<int, int>? getRealOffset;
#if DEBUG
        private HashSet<long> globalFields = new HashSet<long>();
#endif

        internal int UnmanagedOffset;

        internal delegate bool TryGetOffsetDelegate(int offset, out int viewOffset);

        internal ViewTag CurrentTag => currentTag;

        internal unsafe ViewWriter(
            byte* mmf,
            int length,
            IViewDisassembler? viewDisassembler,
            ViewMode mode,
            TryGetOffsetDelegate tryGetViewOffset,
            Func<int, int>? getRealOffset)
        {
            this.mode = mode;
            this.tryGetViewOffset = tryGetViewOffset;
            this.getRealOffset = getRealOffset;
            extension = new Extension(mmf, length, viewDisassembler);
            globalList = new List<IView>();
            listPool = new Stack<List<IView>>();
        }

        private void Push(List<IView> list)
        {
            viewStack.Push(list);
        }

        private void Pop() => viewStack.Pop();

        private void PushScope(List<IView> list, ViewKind scope)
        {
            Debug.Assert(currentScope == 0);
            currentScope = scope;
            scopedList = list;
        }

        private void PopScope()
        {
            scopedList = null;
            currentScope = 0;
        }

        /// <summary>
        /// Writes the specified <see cref="IValue"/> to the global view list.
        /// </summary>
        /// <typeparam name="T">The type of value to write.</typeparam>
        /// <param name="viewable">The value to write.</param>
        public void WriteGlobal<T>(in T? viewable) where T : IViewable
        {
            //Note: in unoptimized code it may show that a boxing occurs here for value types. I have tried different variations of "is object", "is null",
            //"is not", etc. They all box. But in optimized code this check will be removed
            if (viewable == null)
                return;

            Push(globalList);

            //Write any nested globals first
            viewable.WriteGlobals(this);

            Pop();
        }

        public void WriteGlobal<T>(T[]? viewable) where T : IViewable
        {
            if (viewable == null)
                return;

            Push(globalList);

            for (var i = 0; i < viewable.Length; i++)
            {
                ref var item = ref viewable[i];

                //Write any nested globals first
                item.WriteGlobals(this);

                var result = item.WriteStruct(this);

                if (result != null)
                    globalList.Add(result);
            }
        }

        public void WriteGlobal(in RuntimeFunctionList list)
        {
            foreach (var item in list)
            {
                ((IViewable) item).WriteGlobals(this);

                var result = ((IViewable) item).WriteStruct(this);

                if (result != null)
                    globalList.Add(result);
            }
        }

        public void RelayGlobals<T>(T? viewable) where T : IViewable
        {
            viewable?.WriteGlobals(this);
        }

        public void RelayGlobals<T>(T[]? viewable) where T : IViewable
        {
            if (viewable == null)
                return;

            for (var i = 0; i < viewable.Length; i++)
                viewable[i].WriteGlobals(this);
        }

        public void WriteUniqueGlobal<T>(in T[]? value) where T : IValue, IViewable
        {
            if (value == null)
                return;

            for (var i = 0; i < value.Length; i++)
            {
                var item = value[i];

                var shouldAdd = tryGetViewOffset(item.Offset, out var viewOffset);

                if (shouldAdd && trackedAddresses.Add(viewOffset))
                    WriteGlobal(item);
            }
        }

        public void WriteUniqueGlobal<T>(in T value) where T : IValue, IViewable
        {
            var shouldAdd = tryGetViewOffset(value.Offset, out var viewOffset);

            if (shouldAdd && trackedAddresses.Add(viewOffset))
                WriteGlobal(value);
        }

        public void WriteGlobal<T>(int offset, in T value, int size, ViewKind kind)
        {
            var shouldAdd = tryGetViewOffset(offset, out var viewOffset);

            if (shouldAdd)
            {
                Debug.Assert(currentScope == 0);

                Push(globalList);

                var valueView = NewValue<T>(viewOffset, value, size, kind);

                if (valueView != null)
                    AddView(valueView);
            
                Pop();
            }
        }

        public unsafe void WriteGlobal(int offset, SymTypeList value)
        {
            foreach (var item in value)
            {
                var size = SymType.GetSymbolLength(item, value.symbolAccessor);
                WriteGlobal(offset, item, size, ViewKind.SymType);
                offset += size;
            }
        }

        public void WriteGlobalField<T>(int offset, string name, in T value, int size)
        {
            var shouldAdd = tryGetViewOffset(offset, out var viewOffset);

            if (shouldAdd)
            {
                if (shouldAdd)
                {
                    Push(globalList);

                    AddView(new FieldView<T>(offset, name, value, size));

                    Pop();
                }
            }
        }

        internal PageWriter CreatePagedWriter(int startRelativeOffset, PagedMemoryBlock block, bool global)
        {
            if (global)
                Push(globalList);

            var shouldAdd = tryGetViewOffset(block.RemoteStartOffset + startRelativeOffset, out var viewOffset);

            return new PageWriter(viewOffset - block.RemoteStartOffset, block, this, global, shouldAdd);
        }

        internal unsafe void WritePagedGlobal(int startRelativeOffset, PagedMemoryBlock block, SymTypeList value)
        {
            using var p = CreatePagedWriter(startRelativeOffset, block, global: true);

            foreach (var item in value)
                p.WriteValue(item, SymType.GetSymbolLength(item, value.symbolAccessor), ViewKind.SymType);
        }

        internal void WritePagedGlobal(int startRelativeOffset, PagedMemoryBlock block, TypTypeList value)
        {
            using var p = CreatePagedWriter(startRelativeOffset, block, global: true);

            foreach (var item in value)
                p.WriteValue(item, item.len + 2, ViewKind.TypType);
        }

        internal unsafe void WritePagedGlobal<T>(int startRelativeOffset, PagedMemoryBlock block, T[] value, ViewKind viewKind) where T : unmanaged
        {
            using var p = CreatePagedWriter(startRelativeOffset, block, global: true);

            foreach (var item in value)
                p.WriteValue(item, sizeof(T), viewKind);
        }

        internal unsafe void WritePagedGlobal<T>(int startRelativeOffset, PagedMemoryBlock block, NativeSpan<T> value, ViewKind viewKind) where T : unmanaged
        {
            using var p = CreatePagedWriter(startRelativeOffset, block, global: true);

            foreach (var item in value)
                p.WriteValue(item, sizeof(T), viewKind);
        }

        public void WriteGlobal(int offset, TypTypeList value)
        {
            var written = 0;

            Push(globalList);

            foreach (var item in value)
            {
                var totalLength = item.len + 2;
                var valueView = NewValue<TypType>(offset + written, item, totalLength, ViewKind.TypType);

                if (valueView != null)
                    AddView(valueView);

                written += totalLength;
            }

            Pop();
        }

        public void WriteDosStub(in ByteBlob byteBlob)
        {
            var shouldAdd = tryGetViewOffset(byteBlob.Offset, out var viewOffset);

            if (shouldAdd)
            {
                if (extension.TryParseRawBytes(
                    viewOffset,
                    ViewKind.DosStub,
                    byteBlob.Bytes,
                    null,
                    out var views))
                    AddViews(views!);
                else
                    AddView(new ByteBlobView(
                        viewOffset,
                        byteBlob.Bytes,
                        ViewKind.DosStub
                    ));
            }
        }

        public ByteBlobView? WriteByteBlob(ByteBlob byteBlob)
        {
            var shouldAdd = tryGetViewOffset(byteBlob.Offset, out var viewOffset);

            if (shouldAdd)
            {
                return new ByteBlobView(
                    viewOffset,
                    byteBlob.Bytes,
                    default
                );                
            }

            return null;
        }

        public void WriteTaggedGlobal<T>(in T value) where T : IViewable
        {
            if (currentTag == 0)
                throw new NotImplementedException();

            var view = value.WriteStruct(this);

            if (view != null)
            {
                if (!taggedViews.TryGetValue(currentTag, out var hashSet))
                {
                    hashSet = new HashSet<IView>();
                    hashSet.Add(view);
                    taggedViews[currentTag] = hashSet;
                }
                else
                    hashSet.Add(view);

                AddView(view);
            }
        }

        #region Field Globals
        #region Pointer

        public void WriteVAPointerField<T>(VA<T> value, int fieldOffset) where T : IViewable, IValue
        {
            if (value.IsValid)
            {
                WriteXRef(fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedAddress);
#endif

                WriteGlobal(value.Value);
            }
        }

        public void WriteUniqueVAPointerField<T>(VA<T> value, int fieldOffset) where T : IViewable, IValue
        {
            if (value.IsValid)
            {
                WriteXRef(fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedAddress);
#endif

                WriteUniqueGlobal(value.Value);
            }
        }

        public void WriteVAPointerField<T>(VA<T[]> value, int fieldOffset) where T : IViewable, IValue
        {
            if (value.IsValid)
            {
                WriteXRef(fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedAddress);
#endif

                WriteGlobal(value.Value);
            }
        }

        public void WriteSmallVAPointerField<T>(VA<T> value, int fieldOffset) where T : IViewable, IValue
        {
            if (value.IsValid)
            {
                WriteXRef(fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedAddress);
#endif

                WriteGlobal(value.Value);
            }
        }

        public void WriteSmallVAPointerField<T>(VA<T[]> value, int fieldOffset) where T : IViewable, IValue
        {
            if (value.IsValid)
            {
                WriteXRef(fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedAddress);
#endif

                WriteGlobal(value.Value);
            }
        }

        public void WriteVAPointerField(VA<long> value, ViewKind valueKind, int fieldOffset)
        {
            if (value.IsValid)
            {
                WriteXRef(fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedAddress);
#endif

                if (((PEViewWriter) this).Is32Bit)
                    WriteGlobal(value.ActualOffset, (int) value.Value, sizeof(int), valueKind);
                else
                    WriteGlobal(value.ActualOffset, value.Value, sizeof(long), valueKind);
            }
        }

        public void WriteVAPointerField(VA<ulong> value, ViewKind valueKind, int fieldOffset)
        {
            if (value.IsValid)
            {
                WriteXRef(fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedAddress);
#endif

                if (((PEViewWriter) this).Is32Bit)
                    WriteGlobal(value.ActualOffset, (int) value.Value, sizeof(int), valueKind);
                else
                    WriteGlobal(value.ActualOffset, value.Value, sizeof(long), valueKind);
            }
        }

        public void WriteVAPointerField(VA<ulong[]> value, ViewKind valueKind, int fieldOffset)
        {
            if (value.IsValid)
            {
                WriteXRef(fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedAddress);
#endif
                if (((PEViewWriter) this).Is32Bit)
                    WriteGlobal(value.ActualOffset, value.Value, value.Value.Length * sizeof(int), valueKind);
                else
                    WriteGlobal(value.ActualOffset, value.Value, value.Value.Length * sizeof(long), valueKind);
            }
        }

        public void WriteVAPointerField(VA<int[]> value, ViewKind valueKind, int fieldOffset)
        {
            if (value.IsValid)
            {
                WriteXRef(fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedAddress);
#endif

                WriteGlobal(value.ActualOffset, value.Value, value.Value.Length * sizeof(int), valueKind);
            }
        }

        #endregion
        #region RVA

        public void WriteRVAPointerField(RVA<long> value, int fieldOffset)
        {
            if (value.IsValid && value.ListedOffset != 0)
            {
                WriteXRef(fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedOffset);
#endif

                if (((PEViewWriter) this).Is32Bit)
                    WriteGlobal(value.ActualOffset, (int) value.Value, sizeof(int), default);
                else
                    WriteGlobal(value.ActualOffset, value.Value, sizeof(long), default);
            }
        }

        public void WriteRVAField(RVA<ulong[]> value, int fieldOffset)
        {
            if (value.IsValid && value.ListedOffset != 0)
            {
                WriteXRef(fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedOffset);
#endif

                WriteGlobal(value.ActualOffset, value.Value, value.Value.Length * sizeof(long), default);
            }
        }

        public void WriteRVAAnsiNullTerminatedField(RVA<string> value, ViewKind viewKind, int fieldOffset)
        {
            if (value.IsValid && value.ListedOffset != 0)
            {
                WriteXRef(fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedOffset);
#endif

                WriteGlobal(value.ActualOffset, value.Value, value.Value.Length + 1, viewKind);
            }
        }

        public void WriteVAAnsiNullTerminatedField(VA<string> value, ViewKind viewKind, int fieldOffset)
        {
            if (value.IsValid && value.ListedAddress != 0)
            {
                WriteXRef(fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedAddress);
#endif

                WriteGlobal(value.ActualOffset, value.Value, value.Value.Length + 1, viewKind);
            }
        }

        public void WriteVAAnsiNullTerminatedField(VA<AnsiString> value, ViewKind viewKind, int fieldOffset)
        {
            if (value.IsValid && value.ListedAddress != 0)
            {
                WriteXRef(fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedAddress);
#endif

                WriteGlobal(value.ActualOffset, value.Value, value.Value.Length + 1, viewKind);
            }
        }

        public void WriteRVAAnsiNullTerminatedField(RVA<AnsiString> value, ViewKind viewKind, int fieldOffset)
        {
            if (value.IsValid && value.ListedOffset != 0)
            {
                WriteXRef(fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedOffset);
#endif

                WriteGlobal(value.ActualOffset, value.Value, value.Value.Length + 1, viewKind);
            }
        }

        public void WriteRVAField<T>(RVA<T> value, int fieldOffset) where T : IViewable, IValue
        {
            if (value.IsValid)
            {
                WriteXRef(fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedOffset);
#endif

                WriteGlobal(value.Value);
            }
        }

        public void WriteRVAField<T>(RVA<T[]> value, int fieldOffset) where T : IViewable, IValue
        {
            if (value.IsValid && value.ListedOffset != 0)
            {
                WriteXRef(fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedOffset);
#endif

                WriteGlobal(value.Value);
            }
        }

        #endregion
        #endregion

        internal StructWriter CreateStruct<T>(string name, in T value, ViewKind kind) where T : IValue
        {
            var shouldAdd = tryGetViewOffset(value.Offset, out var viewOffset);
            
            return new StructWriter(name, viewOffset, kind, this, shouldAdd);
        }

        internal StructWriter CreateStruct(IView parent) => new StructWriter(parent.Offset, this);

        protected internal virtual IView? NewStruct<T>(FixedUtf8String name, in T value, ViewKind kind, int structSize) where T : IValue
        {
            var shouldAdd = tryGetViewOffset(value.Offset, out var viewOffset);

            if (!shouldAdd)
                return null;

            var view = new StructView<T>(viewOffset, name, value, null, structSize, kind, this);

            return view;
        }

        protected virtual void WriteXRef(int fieldOffset, int targetOffset)
        {
        }

        internal IView? NewUnmanagedStruct<T>(FixedUtf8String name, in T value, ViewKind kind, int structSize)
        {
            Debug.Assert(UnmanagedOffset != 0);
            var shouldAdd = tryGetViewOffset(UnmanagedOffset, out var viewOffset);

            if (!shouldAdd)
                return null;

            var view = new StructView<T>(viewOffset, name, value, null, structSize, kind, this);

            return view;
        }

        internal StructWriter CreateUnmanagedStruct(string name, ViewKind kind)
        {
            Debug.Assert(UnmanagedOffset != 0);
            var shouldAdd = tryGetViewOffset(UnmanagedOffset, out var viewOffset);

            return new StructWriter(name, viewOffset, kind, this, shouldAdd);
        }

        protected internal virtual IView? NewValue<T>(int offset, in T value, int size, ViewKind kind)
        {
            return new ValueView<T>(offset, value, size, kind);
        }

        internal RegionWriter CreateRegion(int offset, string name, ViewKind kind, bool global = false)
        {
            //todo: if its not global, are we writing the region under a structwriter? how does that make sense? will the offsets actually be inside the struct?
            //if not it doesnt make sense. if so, we wouldnt be correctly updating the offsets on the struct
            if (global)
                Push(globalList);

            var shouldAdd = tryGetViewOffset(offset, out var viewOffset);

            return new RegionWriter(viewOffset, name, kind, this, global, default, shouldAdd);
        }

        /// <summary>
        /// Creates a <see cref="RegionWriter"/> that captures any views written to global scope of a specific <see cref="ViewKind"/>.<para/>
        /// This allows creating a region that just captures <see cref="ImageThunkData"/> instances, without also capturing their inner
        /// <see cref="ImageImportByName"/> instances.
        /// </summary>
        /// <param name="offset">The offset at which the region begins.</param>
        /// <param name="name">The display name to use for the region.</param>
        /// <param name="kind">The kind of region that will be created.</param>
        /// <param name="scopeKind">The type of entity that this <see cref="RegionWriter"/> should be limited to capturing.</param>
        /// <returns></returns>
        internal RegionWriter CreateScopedRegion(int offset, string name, ViewKind kind, ViewKind scopeKind)
        {
            var shouldAdd = tryGetViewOffset(offset, out var viewOffset);
            
            return new RegionWriter(viewOffset, name, kind, this, false, scopeKind, shouldAdd);
        }

        internal MetadataRowWriter CreateMetadataRow<T>(FixedUtf8String name, in T value, ViewKind kind) where T : IValue
        {
            var shouldAdd = tryGetViewOffset(value.Offset, out var viewOffset);
            
            return new MetadataRowWriter(name, viewOffset, kind, (PEViewWriter) this, shouldAdd);
        }

        internal MetadataRowWriter CreateMetadataRow(IView parent) => new MetadataRowWriter(parent.Offset, (PEViewWriter) this);

        internal IView[]? CreateByteBlob(ref int currentOffset, int size)
        {
            var views = extension.ReadBytes(ref currentOffset, currentOffset + size, null, getRealOffset, null, false);
            currentOffset++; //ReadBytes subtracts 1 from the new offset
            return views;
        }

        internal TagScope EnterTag(ViewTag tag)
        {
            currentTag = tag;

            return new TagScope(this);
        }

        private void ExitTag()
        {
            currentTag = 0;
        }

        internal ref struct TagScope
        {
            private ViewWriter writer;

            public TagScope(ViewWriter writer)
            {
                this.writer = writer;
            }

            public void Dispose()
            {
                writer.ExitTag();
            }
        }

        private void AddView(IView view)
        {
            if (currentScope != 0 && view.Kind == currentScope)
                scopedList!.Add(view);
            else
            {
                //When StructWriter.Dispose runs, the stack might be empty

                if (viewStack.Count == 0)
                    globalList.Add(view);
                else
                    viewStack.Peek().Add(view);
            }
        }

        private void AddViews(IList<IView> views)
        {
            //When StructWriter.Dispose runs, the stack might be empty

            if (viewStack.Count == 0)
                globalList.AddRange(views);
            else
                viewStack.Peek().AddRange(views);
        }

        internal List<IView> RentList()
        {
            if (listPool.Count > 0)
                return listPool.Pop();

            return new List<IView>();
        }

        internal void ReturnList(List<IView> list)
        {
            list.Clear();
            listPool.Push(list);
        }

        public abstract IView Finalize();
    }
}
