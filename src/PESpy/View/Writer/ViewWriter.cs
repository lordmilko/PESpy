using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using PESpy.PDB;
using PESpy.View.Builder;

namespace PESpy.View
{
    //We want to be able to tell IMAGE_IMPORT_BY_NAME entities that belong to imports vs delay imports. For regular imports, we create
    //a ViewKind ImportStrings. Otherwise its DelayImportStrings

    public enum ViewTag
    {
        Import = 1,
        DelayImport
    }

    public abstract partial class ViewWriter
    {
        //When a struct wants to write another struct inside it, it will push its list of fields to the viewStack,
        //which will cause the inner struct to write itself to this list instead of the main global list
        protected Stack<List<IView>> viewStack;
        protected List<IView> globalList;

        public IReadOnlyList<IView> Current => new ReadOnlyCollection<IView>(globalList);

        internal virtual ICodeViewAccessor GetSymbolAccessor()
        {
            //We've got a bit of an issue with OBJ files; we can set the accessor when we construct the symbol,
            //but we then clear it afterwards, so symbols are going to be forced to lookup their appropriate symbol accessor manually
            return ManualSymbolAccessor;
        }

        internal ICodeViewAccessor ManualSymbolAccessor { get; set; }

        internal void Clear() => globalList.Clear();

        internal ByteViewProvider byteViewProvider;
        private HashSet<int> trackedAddresses;
        protected ViewMode mode;
        private ViewTag currentTag;
        private ViewKind currentScope;
        private List<IView>? scopedList;
        private Stack<List<IView>> listPool;
        internal TryGetOffsetDelegate tryGetViewOffset;
        internal Func<int, int>? getRealOffset;
#if DEBUG
        private HashSet<long> globalFields;
        internal bool ShouldVerifyXRefs;
#endif

        internal ViewWriter? NestedViewWriter;

        internal int UnmanagedOffset;

        internal bool FromRegion;

        internal delegate bool TryGetOffsetDelegate(int offset, out int viewOffset);

        internal ViewTag CurrentTag => currentTag;

        private ViewSymTypeDispatcher? _symTypeDispatcher;

        internal ViewSymTypeDispatcher SymTypeDispatcher => _symTypeDispatcher ??= new ViewSymTypeDispatcher(this);

        private ViewTypTypeDispatcher? _typTypeDispatcher;

        internal ViewTypTypeDispatcher TypTypeDispatcher => _typTypeDispatcher ??= new ViewTypTypeDispatcher(this);

        internal unsafe ViewWriter(
            ByteViewProvider byteViewProvider,
            ViewMode mode,
            TryGetOffsetDelegate tryGetViewOffset,
            Func<int, int>? getRealOffset)
        {
            this.mode = mode;
            this.tryGetViewOffset = tryGetViewOffset;
            this.getRealOffset = getRealOffset;
            this.byteViewProvider = byteViewProvider;
            globalList = new List<IView>();
            listPool = new Stack<List<IView>>();
            viewStack = new Stack<List<IView>>();
            trackedAddresses = new HashSet<int>();

#if DEBUG
            globalFields = new HashSet<long>();
            ShouldVerifyXRefs = true;
#endif
        }

        internal ViewWriter(
            ViewWriter parentWriter,
            ByteViewProvider byteViewProvider,
            TryGetOffsetDelegate tryGetViewOffset,
            Func<int, int>? getRealOffset)
        {
            this.mode = parentWriter.mode;
            this.tryGetViewOffset = tryGetViewOffset;
            this.getRealOffset = getRealOffset;
            this.byteViewProvider = byteViewProvider;
            globalList = new List<IView>(); ;
            listPool = parentWriter.listPool;
            viewStack = new Stack<List<IView>>();
            trackedAddresses = parentWriter.trackedAddresses;

#if DEBUG
            globalFields = parentWriter.globalFields;
#endif
        }

        internal bool TryGetViewOffset(int rva, out int offset) =>
            tryGetViewOffset(rva, out offset);

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

            //Write any nested globals first
            viewable.WriteGlobals(this);

            var result = viewable.WriteStruct(this);

            if (result != null)
            {
                if (currentScope != 0 && result.Kind == currentScope)
                    scopedList!.Add(result);
                else
                    globalList.Add(result);
            }
        }

        public void WriteRegionGlobal<T>(in T? viewable) where T : IViewable
        {
            //Note: in unoptimized code it may show that a boxing occurs here for value types. I have tried different variations of "is object", "is null",
            //"is not", etc. They all box. But in optimized code this check will be removed
            if (viewable == null)
                return;

            var oldFromRegion = FromRegion;

            try
            {
                FromRegion = false;

                //Write any nested globals first
                viewable.WriteGlobals(this);

                FromRegion = true;

                var result = viewable.WriteStruct(this);

                if (result != null)
                {
                    if (currentScope != 0 && result.Kind == currentScope)
                        scopedList!.Add(result);
                    else
                        globalList.Add(result);
                }
            }
            finally
            {
                FromRegion = oldFromRegion;
            }
        }

        public void WriteGlobal<T>(T[]? viewable) where T : IViewable
        {
            if (viewable == null)
                return;

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

        public void WriteGlobal(RuntimeFunctionList? list)
        {
            if (list == null)
                return;

            //Prevent boxing the RuntimeFunction
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            IView Write<T>(T value) where T : IViewable
            {
                value.WriteGlobals(this);

                return value.WriteStruct(this);
            }

            foreach (var item in list)
            {
                var result = Write(item);

                if (result != null)
                    globalList.Add(result);
            }
        }

        public unsafe void WriteNestedFile(PEFile peFile, int length)
        {
            /* The nested writer provides the mechanism for the bitness of the PEFile to be exposed
             * to its descendants. Furthermore, we need to create a brand new ByteViewProvider so we can
             * scope it to just the range of memory that this PEFile resides in. Otherwise, the nested file
             * might start trying to read the overlay of the outer file */

            var provider = (LocalMemoryBlockProvider) peFile.blockProvider;

            /* People are going to give us absolute offsets into the file, so we need to be able to operate against the global file pointer.
             * However, we don't want to be reading past the end of this nested file. So we'll say that the length of the byte view is from
             * the beginning of the outer file, right up until the end of this nested file
             *
             * Furthermore, since the pointer inside the nested PEFile is relative to its start, we need to rewind back to the very start of the file, so that we can offset
             * against it using global addresses
             */

            var byteViewProvider = new LocalByteViewProvider(provider.Pointer - provider.StartOffset, provider.StartOffset + length, this.byteViewProvider.ViewDisassembler);

            EnterNestedFile(provider.StartOffset, length, peFile);

            var nestedWriter = new NestedPEViewWriter((PEViewWriter) this, byteViewProvider, peFile);

            nestedWriter.WriteGlobal(peFile);

            var view = nestedWriter.Finalize();

            ExitNestedFile();

            globalList.Add(view);
        }

        public void RelayGlobals<T>(T? viewable) where T : IViewable
        {
            viewable?.WriteGlobals(this);
        }

        public void RelayGlobals(ImageDataDirectory imageDataDirectory)
        {
            if (imageDataDirectory.VirtualAddress == 0)
                return;

            RelayGlobals<ImageDataDirectory>(imageDataDirectory);
        }

        //The security and bound import tables use physical addresses. Handle them manually
        public void RelayPhysicalGlobals(ImageDataDirectory imageDataDirectory)
        {
            if (imageDataDirectory.VirtualAddress == 0)
                return;

            WriteOffsetXRef(imageDataDirectory.Offset, fieldOffset: 0, imageDataDirectory.VirtualAddress);
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

        public void WriteRegionUniqueGlobal<T>(in T[]? value) where T : IValue, IViewable
        {
            if (value == null)
                return;

            for (var i = 0; i < value.Length; i++)
            {
                var item = value[i];

                var shouldAdd = tryGetViewOffset(item.Offset, out var viewOffset);

                if (shouldAdd && trackedAddresses.Add(viewOffset))
                    WriteRegionGlobal(item);
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

        public void WriteUniqueGlobal<T>(int offset, in T value, int size, ViewKind kind)
        {
            var shouldAdd = tryGetViewOffset(offset, out var viewOffset);

            if (shouldAdd && trackedAddresses.Add(viewOffset))
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
            var dispatcher = SymTypeDispatcher;

            var oldOffset = UnmanagedOffset;
            UnmanagedOffset = offset;

            foreach (var item in value)
            {
                var view = dispatcher.Dispatch(item);

                if (view != null)
                {
                    AddView(view);
                    UnmanagedOffset += view.Size;
                }
                else
                    UnmanagedOffset += SymType.GetSymbolLength(item, value.codeViewAccessor);
            }

            UnmanagedOffset = oldOffset;
        }

        public virtual void WriteGlobalField<T>(int offset, FixedUtf8String name, in T value, int size, ViewKind kind)
        {
            var shouldAdd = tryGetViewOffset(offset, out var viewOffset);

            if (shouldAdd)
            {
                Push(globalList);

                AddView(new FieldView<T>(offset, name.ToString(), value, size, default));

                Pop();
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

            var oldOffset = UnmanagedOffset;

            var dispatcher = SymTypeDispatcher;

            foreach (var item in value)
                p.WriteStruct(item, dispatcher, value.codeViewAccessor);

            UnmanagedOffset = oldOffset;
        }

        internal void WritePagedGlobal(int startRelativeOffset, PagedMemoryBlock block, TypTypeList value)
        {
            using var p = CreatePagedWriter(startRelativeOffset, block, global: true);

            var oldOffset = UnmanagedOffset;

            var dispatcher = TypTypeDispatcher;

            foreach (var item in value)
                p.WriteStruct(item, dispatcher);

            UnmanagedOffset = oldOffset;
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
            var dispatcher = TypTypeDispatcher;

            var oldOffset = UnmanagedOffset;
            UnmanagedOffset = offset;

            foreach (var item in value)
            {
                var view = dispatcher.Dispatch(item);

                if (view != null)
                {
                    AddView(view);
                    UnmanagedOffset += view.Size;
                }
                else
                    UnmanagedOffset += item.len + 2;
            }

            UnmanagedOffset = oldOffset;
        }

        public virtual void WriteDosStub(in ByteBlob byteBlob)
        {
            var shouldAdd = tryGetViewOffset(byteBlob.Offset, out var viewOffset);

            if (shouldAdd)
            {
                if (byteViewProvider.TryParseRawBytes(
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

        public virtual ByteBlobView? WriteByteBlob(ByteBlob byteBlob)
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

        #region Field Globals
        #region Pointer

        public void WriteVAPointerField<T>(VA<T> value, int structOffset, int fieldOffset) where T : IViewable, IValue
        {
            if (value.IsValid && value.ListedAddress != 0)
            {
                WriteOffsetXRef(structOffset, fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedAddress);
#endif

                WriteGlobal(value.Value);
            }
        }

        public void WriteUniqueVAPointerField<T>(VA<T> value, int structOffset, int fieldOffset) where T : IViewable, IValue
        {
            if (value.IsValid && value.ListedAddress != 0)
            {
                WriteOffsetXRef(structOffset, fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedAddress);
#endif

                WriteUniqueGlobal(value.Value);
            }
        }

        public void WriteVAPointerField<T>(VA<T[]> value, int structOffset, int fieldOffset) where T : IViewable, IValue
        {
            if (value.IsValid && value.ListedAddress != 0)
            {
                WriteOffsetXRef(structOffset, fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedAddress);
#endif

                WriteGlobal(value.Value);
            }
        }

        public void WriteSmallVAPointerField<T>(VA<T> value, int structOffset, int fieldOffset) where T : IViewable, IValue
        {
            if (value.IsValid && value.ListedAddress != 0)
            {
                WriteOffsetXRef(structOffset, fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedAddress);
#endif

                WriteGlobal(value.Value);
            }
        }

        public void WriteSmallVAPointerField<T>(VA<T[]> value, int structOffset, int fieldOffset) where T : IViewable, IValue
        {
            if (value.IsValid && value.ListedAddress != 0)
            {
                WriteOffsetXRef(structOffset, fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedAddress);
#endif

                WriteGlobal(value.Value);
            }
        }

        public void WriteVAPointerField(VA<long> value, ViewKind valueKind, int structOffset, int fieldOffset)
        {
            if (value.IsValid && value.ListedAddress != 0)
            {
                WriteOffsetXRef(structOffset, fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedAddress);
#endif

                if (((PEViewWriter) this).Is32Bit)
                    WriteGlobal(value.ActualOffset, (int) value.Value, sizeof(int), valueKind);
                else
                    WriteGlobal(value.ActualOffset, value.Value, sizeof(long), valueKind);
            }
        }

        public void WriteVAPointerField(VA<ulong> value, ViewKind valueKind, int structOffset, int fieldOffset)
        {
            if (value.IsValid && value.ListedAddress != 0)
            {
                WriteOffsetXRef(structOffset, fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedAddress);
#endif

                if (((PEViewWriter) this).Is32Bit)
                    WriteGlobal(value.ActualOffset, (int) value.Value, sizeof(int), valueKind);
                else
                    WriteGlobal(value.ActualOffset, value.Value, sizeof(long), valueKind);
            }
        }

        public void WriteVAPointerField(VA<ulong[]> value, ViewKind valueKind, int structOffset, int fieldOffset)
        {
            if (value.IsValid && value.ListedAddress != 0)
            {
                WriteOffsetXRef(structOffset, fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedAddress);
#endif
                if (((PEViewWriter) this).Is32Bit)
                    WriteGlobal(value.ActualOffset, value.Value, value.Value.Length * sizeof(int), valueKind);
                else
                    WriteGlobal(value.ActualOffset, value.Value, value.Value.Length * sizeof(long), valueKind);
            }
        }

        public void WriteVAPointerField(VA<NativeSpan<int>> value, ViewKind valueKind, int structOffset, int fieldOffset)
        {
            if (value.IsValid && value.ListedAddress != 0)
            {
                WriteOffsetXRef(structOffset, fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedAddress);
#endif

                WriteGlobal(value.ActualOffset, value.Value, value.Value.Length * sizeof(int), valueKind);
            }
        }

        #endregion
        #region RVA

        public void WriteRVAPointerField(RVA<long> value, int structOffset, int fieldOffset, ViewKind kind)
        {
            if (value.IsValid && value.ListedOffset != 0)
            {
                WriteOffsetXRef(structOffset, fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedOffset);
#endif

                if (((PEViewWriter) this).Is32Bit)
                    WriteGlobal(value.ActualOffset, (int) value.Value, sizeof(int), kind);
                else
                    WriteGlobal(value.ActualOffset, value.Value, sizeof(long), kind);
            }
        }

        public void WriteRVAField(RVA<ulong[]> value, int structOffset, int fieldOffset)
        {
            if (value.IsValid && value.ListedOffset != 0)
            {
                WriteOffsetXRef(structOffset, fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedOffset);
#endif

                WriteGlobal(value.ActualOffset, value.Value, value.Value.Length * sizeof(long), default);
            }
        }

        public void WriteRVAAnsiNullTerminatedField(RVA<string> value, ViewKind viewKind, int structOffset, int fieldOffset)
        {
            if (value.IsValid && value.ListedOffset != 0)
            {
                WriteOffsetXRef(structOffset, fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedOffset);
#endif

                WriteGlobal(value.ActualOffset, value.Value, value.Value.Length + 1, viewKind);
            }
        }

        public void WriteVAAnsiNullTerminatedField(VA<string> value, ViewKind viewKind, int structOffset, int fieldOffset)
        {
            if (value.IsValid && value.ListedAddress != 0)
            {
                WriteOffsetXRef(structOffset, fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedAddress);
#endif

                WriteGlobal(value.ActualOffset, value.Value, value.Value.Length + 1, viewKind);
            }
        }

        public void WriteVAAnsiNullTerminatedField(VA<AnsiString> value, ViewKind viewKind, int structOffset, int fieldOffset)
        {
            if (value.IsValid && value.ListedAddress != 0)
            {
                WriteOffsetXRef(structOffset, fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedAddress);
#endif

                WriteGlobal(value.ActualOffset, value.Value, value.Value.Length + 1, viewKind);
            }
        }

        public void WriteRVAAnsiNullTerminatedField(RVA<AnsiString> value, ViewKind viewKind, int structOffset, int fieldOffset)
        {
            if (value.IsValid && value.ListedOffset != 0)
            {
                WriteOffsetXRef(structOffset, fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedOffset);
#endif

                WriteGlobal(value.ActualOffset, value.Value, value.Value.Length + 1, viewKind);
            }
        }

        public void WriteUniqueRVAAnsiNullTerminatedField(RVA<AnsiString> value, ViewKind viewKind, int structOffset, int fieldOffset)
        {
            if (value.IsValid && value.ListedOffset != 0)
            {
                WriteOffsetXRef(structOffset, fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedOffset);
#endif

                WriteUniqueGlobal(value.ActualOffset, value.Value, value.Value.Length + 1, viewKind);
            }
        }

        public void WriteRVAField<T>(RVA<T> value, int structOffset, int fieldOffset) where T : IViewable, IValue
        {
            if (value.IsValid && value.ListedOffset != 0)
            {
                WriteOffsetXRef(structOffset, fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedOffset);
#endif

                WriteGlobal(value.Value);
            }
        }

        public void WriteUniqueRVAField<T>(RVA<T> value, int structOffset, int fieldOffset) where T : IViewable, IValue
        {
            if (value.IsValid && value.ListedOffset != 0)
            {
                WriteOffsetXRef(structOffset, fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedOffset);
#endif

                WriteUniqueGlobal(value.Value);
            }
        }

        public void WriteRVAField<T>(RVA<T[]> value, int structOffset, int fieldOffset) where T : IViewable, IValue
        {
            if (value.IsValid && value.ListedOffset != 0)
            {
                WriteOffsetXRef(structOffset, fieldOffset, value.ActualOffset);

#if DEBUG
                globalFields.Add(value.ListedOffset);
#endif

                WriteGlobal(value.Value);
            }
        }

        #endregion
        #endregion

        protected internal virtual IView? NewStruct<T>(FixedUtf8String name, in T value, ViewKind kind, int structSize)
            where T : IValue, IViewable
        {
            var shouldAdd = tryGetViewOffset(value.Offset, out var viewOffset);

            if (!shouldAdd)
                return null;

            var view = new StructView(viewOffset, name, value, structSize, kind, NestedViewWriter ?? this);

            return view;
        }

        //NOTE: anyone that calls this method must provide the _target_ offset, after having resolved an RVA to its physical location
        public virtual void WriteOffsetXRef(int structOffset, int fieldOffset, int targetOffset)
        {
        }

        public virtual void WriteRVAXRef(int structOffset, int fieldOffset, int targetRVA)
        {
        }

        public void WriteRVAXRef(int structOffset, int fieldOffset, VA<NativeSpan<int>> value)
        {
            if (value.IsValid)
            {
                for (var i = 0; i < value.Value.Length; i++)
                    WriteRVAXRef(structOffset, fieldOffset + (i * sizeof(int)), value.Value[i]);
            }
        }

        public virtual void WriteVAXRef(int structOffset, int fieldOffset, int targetVA)
        {
        }

        public void WriteVAXRef(int structOffset, int fieldOffset, VA<NativeSpan<int>> value)
        {
            if (value.IsValid)
            {
                for (var i = 0; i < value.Value.Length; i++)
                    WriteVAXRef(structOffset, fieldOffset + (i * sizeof(int)), value.Value[i]);
            }
        }

        protected internal virtual IView? NewUnmanagedStruct<T>(FixedUtf8String name, in T value, ViewKind kind, int structSize) where T : IViewable
        {
            Debug.Assert(UnmanagedOffset != 0);
            var shouldAdd = tryGetViewOffset(UnmanagedOffset, out var viewOffset);

            if (!shouldAdd)
                return null;

            var view = new StructView(viewOffset, name, value, structSize, kind, this);

            return view;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal IView? NewSplittableValue<T>(int offset, in T value, int size, ViewKind kind, bool isSplit) => NewValue<T>(offset, value, size, (ViewKind) ((ushort) kind | (isSplit ? 0x8000 : 0)));

        protected internal virtual IView? NewValue<T>(int offset, in T value, int size, ViewKind kind, bool fromRegion = false)
        {
            return new ValueView<T>(offset, value, size, kind);
        }

        internal virtual RegionWriter CreateRegion(int offset, string name, ViewKind kind, bool global = false, ViewWriter nestedViewWriter = null)
        {
            return new ValueView<T>(offset, value, size, kind);
            var writer = nestedViewWriter ?? this;

            if (global)
                writer.Push(globalList);

            var shouldAdd = tryGetViewOffset(offset, out var viewOffset);

            return new RegionWriter(viewOffset, name, kind, writer, global, default, shouldAdd);
        }

        internal virtual RegionWriter CreateRegion(int offset, int structOffset, int fieldOffset, string name, ViewKind kind, bool global
#if DEBUG
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
            , long listedAddress
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
#endif
            , ViewWriter nestedViewWriter = null
            )
        {
            WriteOffsetXRef(structOffset, fieldOffset, offset);

#if DEBUG
            globalFields.Add(listedAddress);
#endif

            var writer = nestedViewWriter ?? this;

            //todo: if its not global, are we writing the region under a structwriter? how does that make sense? will the offsets actually be inside the struct?
            //if not it doesnt make sense. if so, we wouldnt be correctly updating the offsets on the struct
            if (global)
                writer.Push(globalList);

            var shouldAdd = tryGetViewOffset(offset, out var viewOffset);

            return new RegionWriter(viewOffset, name, kind, writer, global, default, shouldAdd);
        }

        /// <summary>
        /// Creates a <see cref="RegionWriter"/> that captures any views written to global scope of a specific <see cref="ViewKind"/>.<para/>
        /// This allows creating a region that just captures <see cref="ImageThunkData"/> instances, without also capturing their inner
        /// <see cref="ImageImportByName"/> instances.
        /// </summary>
        /// <param name="offset">The offset at which the region begins.</param>
        /// <param name="structOffset">The offset of the struct that contains this region.</param>
        /// <param name="fieldOffset">The offset of the field containing the XRef to this region.</param>
        /// <param name="name">The display name to use for the region.</param>
        /// <param name="kind">The kind of region that will be created.</param>
        /// <param name="scopeKind">The type of entity that this <see cref="RegionWriter"/> should be limited to capturing.</param>
        /// <param name="nestedViewWriter">The nested <see cref="ViewWriter"/> that the region should write to.</param>
        /// <returns></returns>
        internal virtual RegionWriter CreateScopedRegion(int offset, int structOffset, int fieldOffset, string name, ViewKind kind, ViewKind scopeKind
#if DEBUG
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
            , int listedOffset
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
#endif
            , ViewWriter nestedViewWriter = null
            )
        {
            //Offset should be RVA<T>.ActualOffset. It is the caller's responsibility to check RVA<T>.IsValid and RVA<T>.ListedOffset != 0

            WriteOffsetXRef(structOffset, fieldOffset, offset);

#if DEBUG
            globalFields.Add(listedOffset);
#endif

            var shouldAdd = tryGetViewOffset(offset, out var viewOffset);

            return new RegionWriter(viewOffset, name, kind, nestedViewWriter ?? this, false, scopeKind, shouldAdd);
        }

        internal virtual void ExitRegion()
        {
        }

        internal virtual void EnterNestedFile(int startOffset, int length, IFile file)
        {
        }

        internal virtual void ExitNestedFile()
        {
        }

        internal IView[]? CreateByteBlob(ref int currentOffset, int size)
        {
            var views = byteViewProvider.ReadBytes(ref currentOffset, currentOffset + size, null, getRealOffset, null, false);
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

        public IView GetChild<TParent>(int parentOffset, TParent parent, int index) where TParent : IViewable
        {
            var structWriter = new StructWriter(this, parentOffset);

            parent.WriteChild(index, ref structWriter);

            //If you're asking for a child, you should be using a ViewWriter that supports creating child entities
            Debug.Assert(structWriter.Field != null);

            return structWriter.Field!;
        }

        public IView[] GetChildren<TParent>(int parentOffset, TParent parent) where TParent : IViewable
        {
            var structWriter = new StructWriter(this, parentOffset);

            //For entities that only support eager loading, -1 is the magic index
            parent.WriteChild(-1, ref structWriter);

            Debug.Assert(structWriter.EagerFields != null);

            return structWriter.EagerFields;
        }

        [Conditional("DEBUG")]
        internal void VerifyXRef<T>(VA<T> value)
        {
#if DEBUG
            //Assert that the global has already been written
            if (value.IsValid)
            {
                Debug.Assert(!ShouldVerifyXRefs || globalFields.Contains(value.ListedAddress));
            }
#endif
        }

        [Conditional("DEBUG")]
        internal void VerifyXRef<T>(RVA<T> value)
        {
#if DEBUG
            if (value.IsValid && value.ListedOffset != 0)
            {
                Debug.Assert(!ShouldVerifyXRefs || globalFields.Contains(value.ListedOffset));
            }
#endif
        }

        private void AddViews(IList<IView> views)
        {
            //When StructWriter.Dispose runs, the stack might be empty

            if (viewStack.Count == 0)
                globalList.AddRange(views);
            else
                viewStack.Peek().AddRange(views);
        }

        internal void WriteField<T>(
            string name,
            int parentOffset,
            int fieldOffset,
            T value,
            int size,
            FieldViewFlags flags,
            ref StructWriter structWriter)
        {
            structWriter.Field = new FieldView<T>(parentOffset + fieldOffset, name, value, size, flags);
        }

        internal void WriteBitField<T>(
            string name,
            int parentOffset,
            int fieldOffset,
            T value,
            int size,
            int bits,
            ref StructWriter structWriter)
        {
            structWriter.Field = new BitFieldView<T>(parentOffset + fieldOffset, name, value, bits, size);
        }

        internal void WriteByteBlob(
            int parentOffset,
            int fieldOffset,
            int size,
            ref StructWriter structWriter)
        {
            var offset = parentOffset + fieldOffset;

            //The offset passed in has already been translated by the parent, so we don't need to call tryGetViewOffset

            structWriter.Field = byteViewProvider.ReadBlob(offset, getRealOffset, size);
        }

        internal void WriteValue<T>(
            int parentOffset,
            int fieldOffset,
            T value,
            int size,
            ViewKind kind,
            ref StructWriter structWriter)
        {
            structWriter.Field = new ValueView<T>(parentOffset + fieldOffset, value, size, kind);
        }

        internal void WriteStructField<T>(
            string fieldName,
            int parentOffset,
            int relativeOffset,
            T value,
            ref StructWriter structWriter) where T : IViewable
        {
            var oldOffset = UnmanagedOffset;
            UnmanagedOffset = parentOffset + relativeOffset;

            WriteStructField<T>(fieldName, value, ref structWriter);

            UnmanagedOffset = oldOffset;
        }

        internal void WriteStructField<T>(
            string fieldName,
            int parentOffset,
            int relativeOffset,
            T[] value,
            ref StructWriter structWriter) where T : unmanaged, IViewable
        {
            var oldOffset = UnmanagedOffset;
            UnmanagedOffset = parentOffset + relativeOffset;

            WriteStructField<T>(fieldName, value, ref structWriter);

            UnmanagedOffset = oldOffset;
        }

        internal void WriteStructField<T>(
            string fieldName,
            T value,
            ref StructWriter structWriter) where T : IViewable
        {
            //I don't think we need to write globals, those should have already been written

            var view = (StructView) value.WriteStruct(this);

            structWriter.Field = new StructFieldView(
                view,
                fieldName
            );
        }

        internal void WriteStructField<T>(
            string fieldName,
            T[] value,
            ref StructWriter structWriter) where T : IViewable
        {
            var results = new StructView[value.Length];

            var old = UnmanagedOffset;

            for (var i = 0; i < results.Length; i++)
            {
                //I don't think we need to write globals, those should have already been written
                var item = (StructView) value[i].WriteStruct(this);

                UnmanagedOffset += item.Size;

                results[i] = item;
            }

            UnmanagedOffset = old;

            structWriter.Field = new StructArrayFieldView(
                results,
                fieldName
            );
        }

        internal virtual void CollectDataDirectories(ref PooledList<DirectoryInfo> dataDirectories)
        {
        }

        internal virtual ViewWriter CreateNestedWriter(IFile file) => throw new NotSupportedException();

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
