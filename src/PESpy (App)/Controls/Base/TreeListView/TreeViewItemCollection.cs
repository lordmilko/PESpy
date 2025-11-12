using System;
using System.Collections;
using System.Collections.Generic;

namespace PESpy.Controls
{
    class TreeViewItemCollection : IList<TreeViewItem>
    {
        private List<TreeViewItem> items = new List<TreeViewItem>();
        private TreeViewItem parent;

        public TreeViewItemCollection(TreeViewItem parent)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            this.parent = parent;
        }

        public IEnumerator<TreeViewItem> GetEnumerator() => items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(TreeViewItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            //The behavior of TreeNodeCollection seems to be to just steal the parent, without checking
            //if another parent has already been set
            item.Parent = parent;
            items.Add(item);
        }

        public void Clear() => items.Clear();

        public bool Contains(TreeViewItem item) => items.Contains(item);

        public void CopyTo(TreeViewItem[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);

        public bool Remove(TreeViewItem item) => items.Remove(item);

        public int Count => items.Count;

        public bool IsReadOnly => false;

        public int IndexOf(TreeViewItem item) => items.IndexOf(item);

        public void Insert(int index, TreeViewItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            items.Insert(index, item);
            item.Parent = parent;
        }

        public void RemoveAt(int index) => items.RemoveAt(index);

        public TreeViewItem this[int index]
        {
            get => items[index];
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                items[index] = value;
                value.Parent = parent;
            }
        }
    }
}
