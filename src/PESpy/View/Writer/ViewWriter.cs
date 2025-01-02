using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using PESpy.View.Builder;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.View
{
    public enum ViewTag
    {
        Import = 1,
        DelayImport
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
        private List<IView> scopedList;
        private Stack<List<IView>> listPool;
        private TryGetOffsetDelegate tryGetViewOffset;
        private Func<int, int> getRealOffset;

        internal delegate bool TryGetOffsetDelegate(int offset, out int viewOffset);
        
        internal ViewWriter(ref FileReader reader, ViewMode mode, TryGetOffsetDelegate tryGetViewOffset, Func<int, int> getRealOffset)
        internal ViewWriter(IFileReader reader, ViewMode mode, TryGetOffsetDelegate tryGetViewOffset, Func<int, int> getRealOffset)
        {
            this.mode = mode;
            this.tryGetViewOffset = tryGetViewOffset;
            this.getRealOffset = getRealOffset;
            extension = new Extension(reader);
            globalList = new List<IView>();
            listPool = new Stack<List<IView>>();
        }

        private void Push(List<IView> list) => viewStack.Push(list);

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
            if (viewable == null)
                return;

            Push(globalList);

            viewable.WriteView(this);

            Pop();
        }

        public void WriteGlobal<T>(T[]? viewable) where T : IViewable
        {
            if (viewable == null)
                return;

            Push(globalList);

            for (var i = 0; i < viewable.Length; i++)
                viewable[i].WriteView(this);

            Pop();
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

        public void WriteGlobal<T>(RawOffset offset, in T value, int size, ViewKind kind)
        {
            var shouldAdd = tryGetViewOffset(offset, out var viewOffset);

            if (shouldAdd)
            {
                Push(globalList);
            
                AddView(new ValueView<T>(viewOffset, value, size, kind));
            
                Pop();   
            }
        }

        public void WriteDosStub(in ByteBlob byteBlob)
        {
            var shouldAdd = tryGetViewOffset(byteBlob.Offset, out var viewOffset);

            if (shouldAdd)
            {
                if (extension.TryParseRawBytes(viewOffset, ViewKind.DosStub, byteBlob.Bytes, out var views))
                    AddViews(views);
                else
                    AddView(new ByteBlobView(viewOffset, byteBlob.Bytes, ViewKind.DosStub));
            }
        }

        public void WriteByteBlob(ByteBlob byteBlob)
        {
            var shouldAdd = tryGetViewOffset(byteBlob.Offset, out var viewOffset);

            if (shouldAdd)
            {
                AddView(new ByteBlobView(viewOffset, byteBlob.Bytes, default));                
            }
        }

        public void WriteTaggedGlobal<T>(in T value) where T : IViewable
        {
            if (currentTag == 0)
                throw new NotImplementedException();

            var view = WriteIntercepted(value);

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

        internal StructWriter CreateStruct<T>(string name, in T value, ViewKind kind) where T : IValue
        {
            var shouldAdd = tryGetViewOffset(value.Offset, out var viewOffset);
            
            return new StructWriter(name, viewOffset, kind, this, shouldAdd);
        }

        internal RegionWriter CreateRegion(RawOffset offset, string name, ViewKind kind, bool global = false)
        {
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
        internal RegionWriter CreateScopedRegion(RawOffset offset, string name, ViewKind kind, ViewKind scopeKind)
        {
            var shouldAdd = tryGetViewOffset(offset, out var viewOffset);
            
            return new RegionWriter(viewOffset, name, kind, this, false, scopeKind, shouldAdd);
        }

        internal MetadataRowWriter CreateMetadataRow<T>(string name, in T value, ViewKind kind) where T : IValue
        {
            var shouldAdd = tryGetViewOffset(value.Offset, out var viewOffset);
            
            return new MetadataRowWriter(name, viewOffset, kind, (PEViewWriter) this, shouldAdd);
        }

        internal IView[] CreateByteBlob(ref int currentOffset, int size)
        {
            var views = extension.ReadBytes(ref currentOffset, currentOffset + size, null, getRealOffset, false);
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
                scopedList.Add(view);
            else
            {
                //When StructWriter.Dispose runs, the stack might be empty

                if (viewStack.Count == 0)
                    globalList.Add(view);
                else
                    viewStack.Peek().Add(view);
            }
        }

        private void AddViews(IView[] views)
        {
            //When StructWriter.Dispose runs, the stack might be empty

            if (viewStack.Count == 0)
                globalList.AddRange(views);
            else
                viewStack.Peek().AddRange(views);
        }

        /// <summary>
        /// Creates an <see cref="IView"/> from a specified <paramref name="value"/> however intercepts the <see cref="IView"/>
        /// without adding it to a list and returns it to the caller.
        /// </summary>
        /// <typeparam name="T">The type of value to write.</typeparam>
        /// <param name="value">The value to write.</param>
        /// <returns>The <see cref="IView"/> that represents the <paramref name="value"/> value.</returns>
        private IView WriteIntercepted<T>(T value) where T : IViewable
        {
            if (interceptionList == null)
                interceptionList = new List<IView>();

            Debug.Assert(interceptionList.Count == 0);

            Push(interceptionList);

            value.WriteView(this);

            if (interceptionList.Count != 1)
                throw new InvalidOperationException("Expected interception list to contain only a single item after writing");

            Pop();

            var result = interceptionList[0];
            interceptionList.Clear();

            return result;
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
