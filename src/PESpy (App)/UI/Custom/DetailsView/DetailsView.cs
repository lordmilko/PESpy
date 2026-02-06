using System;
using PESpy.View;
using PInvoke;

namespace PESpy.UI
{
    internal class DetailsView : NativeWindow
    {
        private TreeListView _treeListView;

        public DetailsView(out DetailsView field)
        {
            field = this;
        }

        internal unsafe void UpdateListView(LogicalTreeNode node, bool refresh)
        {
            var fileAccessor = App.FileAccessor;

            if (fileAccessor == null)
                return;
            User32.SendMessageW(_treeListView.hWnd, (int) WM.WM_SETREDRAW, 0, default);

            switch (node.Kind)
            {
                case LogicalTreeNodeKind.Singleton:
                    ProcessSingleton((SingletonTreeNode) node, fileAccessor, refresh);
                    break;

                case LogicalTreeNodeKind.List:
                    ProcessList((ListTreeNode) node, fileAccessor, refresh);
                    break;

                default:
                    throw new NotImplementedException();
            }

            User32.SendMessageW(_treeListView.hWnd, (int) WM.WM_SETREDRAW, 1, default);
        }
        private void ProcessList(ListTreeNode node, FileAccessor fileAccessor, bool refresh)
        {
            //Lists come in two flavours: a straight array of elements, or an offset to the first
            //element with a count of the number of elements that follow

            ListWriter writer;

            if (node.Array != null)
                writer = new ArrayListWriter(node.Array, fileAccessor, refresh);
            else
                writer = new OffsetListWriter(node.Offset, node.Count, node.ViewKind, fileAccessor, refresh);

            writer.Configure(_treeListView);
        }

        private void treeListView_BeforeExpand(object sender, TreeListViewCancelEventArgs e)
        {
            var nodes = e.Node.Nodes;

            if (nodes.Count != 0)
                return; //Have already evaluated our children

            var view = (IView) e.Node.Tag;

            var treeListView = _treeListView;

            if (view is IFieldView fv)
            {
                if (SingletonWriter.IsArray(fv))
                {
                    //Not ideal, but just check the various types
                    if (fv is FieldView<NativeSpan<short>> @as)
                    {
                        for (var i = 0; i < @as.Value.Length; i++)
                        {
                            var offset = (@as.Offset + (i * sizeof(short))).ToString();

                            var listViewItem = new TreeViewItem(offset);

                            var columns = treeListView.Columns;

                            using var valueBuilder = new ValueStringBuilder();

                            for (var j = 1; j < columns.Count; j++)
                            {
                                var header = columns[j];

                                switch (header.Text)
                                {
                                    case ColumnNames.Name:
                                    case ColumnNames.Description:
                                    case ColumnNames.Meaning:
                                        listViewItem.Add((string) null!);
                                        break;

                                    case ColumnNames.Value:
                                        valueBuilder.Append("0x");
                                        valueBuilder.AppendHex((ushort) @as.Value[i], 4);

                                        listViewItem.Add(valueBuilder.ToString());
                                        valueBuilder.Clear();
                                        break;
                                }
                            }

                            //Doesn't seem like we can add multiple at once?
                            nodes.Add(listViewItem);
                        }
                    }
                    else
                    treeListView.AutoSizeColumns(nodes);
                }
                else if (view is IStructFieldView sv)
                {
                    var writer = new SingletonWriter(treeListView, App.FileAccessor, false);

                    writer.AddChildren(sv.Value, c =>
                    {
                        foreach (var child in c)
                            nodes.Add(child);
                    });

                    treeListView.AutoSizeColumns(nodes);
                }
                else
                {
                    //Get the index of the Value column. If the subitem at that index has a Tag whose type is string[], it's an enum,
                    //so add rows that list all of the enum names and their associated underlying values

                    for (var i = 0; i < treeListView.Columns.Count; i++)
                    {
                        var column = treeListView.Columns[i];

                        if (column.Text == ColumnNames.Value)
                        {
                            var subItem = e.Node.SubItems[i];

                            if (subItem.Tag is EnumFlagInfo[] s)
                            {
                                using var builder = new ValueStringBuilder();

                                foreach (var flag in s)
                                {
                                    builder.Append("0x");
                                    builder.AppendHex(flag.Value, flag.NumChars);

                                    nodes.Add(new TreeViewItem(new[] { null, null, builder.ToString(), flag.Text }));

                                    builder.Clear();
                                }

                                treeListView.AutoSizeColumns(nodes);

                                return;
                            }

                            break;
                        }
                    }
}
