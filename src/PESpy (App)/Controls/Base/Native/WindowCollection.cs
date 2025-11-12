using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using PInvoke;

namespace PESpy.Controls
{
    public class WindowCollection : IEnumerable<NativeWindow>
    {
        private NativeWindow parent;
        private List<NativeWindow> children = new List<NativeWindow>();

        public int Count => children.Count;

        internal WindowCollection(NativeWindow parent)
        {
            this.parent = parent;
        }

        public NativeWindow this[int index] => children[index];

        public void Add(NativeWindow child)
        {
            Debug.Assert(child.Parent == null);
            child.Parent = parent;
            children.Add(child);

            if (parent.IsHandleCreated)
            {
                //We need to create the child handle now
                child.CreateControl();
            }
        }

        public void AddRange(params NativeWindow[] children)
        {
            foreach (var child in children)
                Add(child);
        }

        internal NativeWindow Find(HWND hWnd)
        {
            for (var i = 0; i < children.Count; i++)
            {
                var item = children[i];

                if (item.hWnd == hWnd)
                    return item;
            }

            throw new System.NotImplementedException();
        }

        public IEnumerator<NativeWindow> GetEnumerator() => children.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
