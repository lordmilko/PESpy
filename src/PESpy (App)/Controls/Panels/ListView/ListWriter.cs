using PESpy.Controls;
using PESpy.View;
using static PESpy.Controls.ImageKind;

namespace PESpy
{
    public abstract class ListWriter
    {
        protected FileAccessor _fileAccessor;

        protected ListWriter(FileAccessor fileAccessor)
        {
            _fileAccessor = fileAccessor;
        }

        protected abstract int GetCount();

        protected abstract IStructView GetFirst();

        protected abstract IStructView GetItem(int index);

        internal void Configure(TreeListView listView)
        {
            listView.Clear();

            listView.CanShowExpander = false;

            var first = GetFirst();

            var values = new PooledList<(string value, int imageIndex, bool isLink)>();
            var valueBuilder = new ValueStringBuilder.NonRef(100);

            var numHeader = listView.Columns.Add(ColumnNames.Hash, 0);

            try
            {
                values.Add(("0", -1, false)); //#

                foreach (var child in first.Children)
                {
                    if (child is IFieldView f)
                    {
                        listView.Columns.Add(f.Name);

                        ProcessField(f, ref valueBuilder, ref values);
                    }
                    else if (child is IBitFieldView b)
                    {
                        values.Add((values.Count.ToString(), -1, false)); //Count

                        listView.Columns.Add(b.Name);
                        values.Add((b.Value.ToString()!, -1, false));
                    }
                }

                var toAdd = new PooledList<TreeViewItem>();

                toAdd.Add(CreateTreeViewItem(ref values));

                var count = GetCount();

                for (var i = 1; i < count; i++)
                {
                    values.Clear();

                    values.Add((toAdd.Count.ToString(), -1, false)); //#

                    //When the list is not backed by an array, each item needs to be the same size in order for this to work
                    var item = GetItem(i);

                    foreach (var child in item.Children)
                    {
                        if (child is IFieldView f)
                            ProcessField(f, ref valueBuilder, ref values);
                        else if (child is IBitFieldView b)
                        {
                            values.Add((b.Value.ToString()!, -1, false));
                        }
                    }

                    toAdd.Add(CreateTreeViewItem(ref values));
                }

                var array = toAdd.ToArray();

                listView.AutoSizeColumns(array);

                //Adding lots of items to the ListView is slow, so we need to do it in bulk
                listView.Items.AddRange(array);
            }
            finally
            {
                valueBuilder.Dispose();
                values.Dispose();
            }
        }

        private void ProcessField(
            IFieldView fieldView,
            ref ValueStringBuilder.NonRef valueBuilder,
            ref PooledList<(string value, int imageIndex, bool isLink)> values)
        {
            //In list mode, there isn't enough room to display a separate meaning for each value, so we need to compress things:
            //if a given value has a special meaning, we'll display things in the format <specialMeaning> (0xRawValue)

            var imageIndex = -1;
            var isLink = false;

            var type = ViewByteFormatter.FormatField(fieldView, _fileAccessor, ref valueBuilder, listFormat: true); //No room for padding numbers

            string str;

            if (ViewByteFormatter.TryGetMeaningIncludeEnum(_fileAccessor, fieldView, type, out var specialMeaning, out var specialMeaningKind))
            {
                valueBuilder.Append(" (");
                valueBuilder.Append(specialMeaning);
                valueBuilder.Append(')');

                switch (specialMeaningKind)
                {
                    case MeaningValueKind.StructXRef:
                        isLink = true;
                        imageIndex = ImageStruct;
                        break;

                    case MeaningValueKind.ValueXRef:
                        isLink = true;
                        imageIndex = ImageField;
                        break;

                    case MeaningValueKind.FunctionXRef:
                        isLink = true;
                        imageIndex = ImageFunction;
                        break;
                }
            }

            values.Add((valueBuilder.ToString(), imageIndex, isLink));
            valueBuilder.Clear();
        }

        private TreeViewItem CreateTreeViewItem(ref PooledList<(string value, int imageIndex, bool isLink)> values)
        {
            var subItemNames = new string[values.Count];

            for (var i = 0; i < subItemNames.Length; i++)
                subItemNames[i] = values[i].value;

            var lvi = new TreeViewItem(subItemNames);

            var subItems = lvi.SubItems;

            for (var i = 0; i < values.Count; i++)
            {
                var item = values[i];

                if (item.imageIndex != -1)
                    ((TreeViewItem.TreeViewSubItem) subItems[i]).ImageIndex = item.imageIndex;

                if (item.isLink)
                    ((TreeViewItem.TreeViewSubItem) subItems[i]).IsLink = true;
            }

            return lvi;
        }
    }
}
