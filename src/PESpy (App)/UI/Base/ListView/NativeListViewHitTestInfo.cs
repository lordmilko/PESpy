namespace PESpy.UI
{
    internal struct NativeListViewHitTestInfo
    {
        public NativeListViewItem? Item { get; }

        public NativeListViewSubItem? SubItem { get; }

        public NativeListViewHitTestInfo(NativeListViewItem? item, NativeListViewSubItem? subItem)
        {
            Item = item;
            SubItem = subItem;
        }
    }
}
