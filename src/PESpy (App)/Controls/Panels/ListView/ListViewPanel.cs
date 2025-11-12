using System;
using System.ComponentModel;
#if WINFORMS
using System.Windows.Forms;
#endif
using PESpy.View;
#if !WINFORMS
using UserControl = PESpy.Controls.NativeWindow;
using ImageList = PESpy.NativeImageList;
#endif

namespace PESpy.Controls
{
    public partial class ListViewPanel : UserControl
    {
        [Browsable(true)]
        [Description("The ImageList for the ListView")]
        [Category("Behavior")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ImageList ImageList
        {
            get => treeListView.SmallImageList;
            set
            {
                treeListView.SmallImageList = value;
                btnImage.ImageList = value;
            }
        }

        public ListViewPanel()
        {
            InitializeComponent();
        }

#if !WINFORMS
        private TreeListView treeListView;

        private void InitializeComponent()
        {
        }
#endif

        internal unsafe void UpdateListView(TreeNodeEx node)
        {
            var fileAccessor = App.FileAccessor;

            switch (node.Kind)
            {
                case TreeNodeKind.Singleton:
                    ProcessSingleton((SingletonTreeNode) node, fileAccessor);
                    break;

                case TreeNodeKind.List:
                    ProcessList((ListTreeNode) node, fileAccessor);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private unsafe void ProcessSingleton(SingletonTreeNode node, FileAccessor fileAccessor)
        {
            var writer = new SingletonWriter(treeListView, fileAccessor);
            writer.Compute(node.Offset, node.ViewKind);

            Invalidate();
        }

        private void ProcessList(ListTreeNode node, FileAccessor fileAccessor)
        {
            //Lists come in two flavours: a straight array of elements, or an offset to the first
            //element with a count of the number of elements that follow

            ListWriter writer;

            if (node.Array != null)
                writer = new ArrayListWriter(node.Array, fileAccessor);
            else
                writer = new OffsetListWriter(node.Offset, node.Count, node.ViewKind, fileAccessor);

            writer.Configure(treeListView);
        }

        private void treeListView_BeforeExpand(object sender, TreeListViewCancelEventArgs e)
        {
            var nodes = e.Node.Nodes;

            if (nodes.Count != 0)
                return; //Have already evaluated our children

            var view = (IView) e.Node.Tag;

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

                                    default:
                                        throw new NotImplementedException();
                                }
                            }

                            //Doesn't seem like we can add multiple at once?
                            nodes.Add(listViewItem);
                        }
                    }
                    else
                        throw new NotImplementedException();

                    treeListView.AutoSizeColumns(nodes);
                }
                else if (view is IStructFieldView sv)
                {
                    var writer = new SingletonWriter(treeListView, App.FileAccessor);

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

                    throw new NotImplementedException();
                }
            }
            else
                throw new NotImplementedException();
        }
    }
}
