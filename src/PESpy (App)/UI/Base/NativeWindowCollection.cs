using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PInvoke;

namespace PESpy.UI
{
    internal class NativeWindowCollectionDebugView
    {
        private NativeWindowCollection _collection;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public NativeWindow[] Items => _collection.ToArray();

        internal NativeWindowCollectionDebugView(NativeWindowCollection collection)
        {
            _collection = collection;
        }
    }

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(NativeWindowCollectionDebugView))]
    public class NativeWindowCollection : IEnumerable<NativeWindow>
    {
        //Gets the window that this child collection belongs to
        private readonly NativeWindow _parent;

        private List<NativeWindow> _items = new List<NativeWindow>();

        public int Count => _items.Count;

        internal NativeWindowCollection(NativeWindow parent)
        {
            _parent = parent;
        }

        public NativeWindow this[int index] => _items[index];

        public void Add(NativeWindow child)
        {
            //We don't support changing parents
            Debug.Assert(child.Parent == null);
            _items.Add(child);

            _parent.SuspendLayout();

            child.AssignParent(_parent);

            if (_parent.IsHandleCreated)
            {
                //WinForms calls SetParentHandle for the child here in case its already
                //been created; we do not support that. Child handles should not be created
                //until the child is attached to its parent
                Debug.Assert(!child.IsHandleCreated);

                if (child.Visible)
                {
                    //We need to create the child handle now
                    child.CreateControl();
                }
            }

            //WinForms calls InitLayout on the child here

            _parent.ResumeLayout(false);

            NativeWindow.DoLayout(_parent, child);
        }

        public void AddRange(params NativeWindow[] children)
        {
            foreach (var child in children)
                Add(child);
        }

        internal NativeWindow Find(HWND hWnd)
        {
            var items = _items;

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];

                if (item.hWnd == hWnd)
                    return item;
            }

            throw new InvalidOperationException("Failed to find child window");
        }

        public IEnumerator<NativeWindow> GetEnumerator() => _items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
