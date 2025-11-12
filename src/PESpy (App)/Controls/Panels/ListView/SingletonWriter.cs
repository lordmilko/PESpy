using System;
using PESpy.View;
using PESpy.Controls;
using static PESpy.Controls.ImageKind;

namespace PESpy
{
    enum MeaningValueKind
    {
        HexString = 1,
        StructXRef,
        ValueXRef,
        FunctionXRef,
        Enum,
        Size
    }

    internal struct SingletonWriter
    {
        private TreeListView _listView;
        private FileAccessor _fileAccessor;

        internal SingletonWriter(TreeListView listView, FileAccessor fileAccessor)
        {
            _listView = listView;
            _fileAccessor = fileAccessor;
        }

        public unsafe void Compute(int offset, ViewKind viewKind)
        {
            _listView.Clear();

            //Don't waste screen real estate showing an expander until we know one is needed
            _listView.CanShowExpander = false;

            _listView.Columns.Add(ColumnNames.Offset, 0);
            _listView.Columns.Add(ColumnNames.Name, 0);
            _listView.Columns.Add(ColumnNames.Value, 0);

            var structView = _fileAccessor.GetStructView(offset, viewKind);

            var listView = _listView;

            AddChildren(structView, c =>
            {
                listView.AutoSizeColumns(c);

                listView.Items.AddRange(c);
            });
        }

        public void AddChildren(IStructView structView, Action<TreeViewItem[]> addChildren)
        {
            var pendingListViewItems = new PooledList<TreeViewItem>();
            var meaningInfos = new PooledList<(int rowIndex, string value, MeaningValueKind kind)>();
            var eagerExpand = new PooledList<int>();
            var valueBuilder = new ValueStringBuilder.NonRef(100);

            try
            {
                foreach (var child in structView.Children)
                {
                    var relativeOffset = (child.Offset - structView.Offset).ToString();

                    var listViewItem = new TreeViewItem(relativeOffset)
                    {
                        UseItemStyleForSubItems = false,
                        Tag = child
                    };

                    if (child is IStructFieldView sv)
                        ProcessStructViewField(listViewItem, sv);
                    else if (child is IStructArrayFieldView av)
                        ProcessStructViewArrayField();
                    else if (child is IFieldView fv)
                        ProcessField(listViewItem, fv, ref meaningInfos, ref pendingListViewItems, ref eagerExpand, ref valueBuilder);
                    else if (child is IBitFieldView bv)
                        throw new NotImplementedException();
                    else
                        throw new NotImplementedException();

                    pendingListViewItems.Add(listViewItem);
                }

                ProcessDescription(structView, ref pendingListViewItems);
                ProcessMeaning(ref meaningInfos, eagerExpand.Count > 0, ref pendingListViewItems);

                //Add them all at once for improved perf
                addChildren(pendingListViewItems.ToArray());

                EagerExpand(ref eagerExpand, ref pendingListViewItems);
            }
            finally
            {
                pendingListViewItems.Dispose();
                meaningInfos.Dispose();
                eagerExpand.Dispose();
                valueBuilder.Dispose();
            }
        }

        private void ProcessField(
            TreeViewItem listViewItem,
            IFieldView fieldView,
            ref PooledList<(int rowIndex, string value, MeaningValueKind kind)> meaningInfos,
            ref PooledList<TreeViewItem> pendingListViewItems,
            ref PooledList<int> eagerExpand,
            ref ValueStringBuilder.NonRef valueBuilder)
        {
            var nameItem = listViewItem.Add(fieldView.Name);

            object? tag = null;

            if (IsArray(fieldView))
            {
                //Not ideal, but just check the various types
                if (fieldView is FieldView<NativeSpan<short>> @as)
                {
                    valueBuilder.Append("short[");
                    valueBuilder.Append(@as.Value.Length);
                    valueBuilder.Append(']');
                }
                else
                    throw new NotImplementedException();

                //When the ListViewItem is expanded, we'll figure out what type of children we have and show them
                listViewItem.HasVirtualChildren = true;
                _listView.CanShowExpander = true;
            }
            else
            {
                var type = ViewByteFormatter.FormatField(fieldView, _fileAccessor, ref valueBuilder);

                if (type.IsEnum)
                {
                    //If the value is not flags, or it's flags but has a single value, add a meaning with that value.
                    //Otherwise, this value requires expansion, so collect the children to show under it
                    if (ViewByteFormatter.TryGetMultiFlags(type, fieldView, out var singleValue, out var flags))
                    {
                        eagerExpand.Add(pendingListViewItems.Count);

                        tag = flags;

                        listViewItem.HasVirtualChildren = true;

                        _listView.CanShowExpander = true;
                    }
                    else
                    {
                        meaningInfos.Add((pendingListViewItems.Count, singleValue!, MeaningValueKind.Enum));
                    }
                }
            }

            var valueItem = listViewItem.Add(valueBuilder.ToString());
            valueItem.Tag = tag;

            valueBuilder.Clear();

            if (ViewByteFormatter.TryGetMeaning(_fileAccessor, fieldView, out var value, out var kind))
                meaningInfos.Add((pendingListViewItems.Count, value, kind));
        }

        private void ProcessStructViewField(
            TreeViewItem listViewItem,
            IStructFieldView structViewField)
        {
            listViewItem.Add(structViewField.Name);
            listViewItem.Add(structViewField.StructName.ToString());

            //Allow exploring this child inline
            listViewItem.HasVirtualChildren = true;
            _listView.CanShowExpander = true;
        }

        private void ProcessStructViewArrayField()
        {
            throw new NotImplementedException();
        }

        private void ProcessDescription(
            IStructView structView,
            ref PooledList<TreeViewItem> pendingListViewItems)
        {
            //Certain structs have incomprehensible names. For these, display a description column

            if (structView.Kind == ViewKind.ImageDosHeader)
            {
                if (_listView.Columns.IndexOfKey(ColumnNames.Description) == -1)
                {
                    //The field names are too obscure; add a description
                    _listView.Columns.Add(ColumnNames.Description, ColumnNames.Description);
                }

                var i = 0;

                foreach (IFieldView child in structView.Children)
                {
                    var listViewItem = pendingListViewItems[i];
                    var subItem = listViewItem.Add(ImageDosHeader.GetDescription(child.Name));
                    subItem.BackColor = listViewItem.BackColor;

                    i++;
                }
            }
        }

        private void ProcessMeaning(
            ref PooledList<(int rowIndex, string value, MeaningValueKind kind)> meaningInfos,
            bool hasEagerExpand,
            ref PooledList<TreeViewItem> pendingListViewItems)
        {
            //If we got any meaning columns, we need to add an empty meaning cell for every row
            //so that the alternating background color shows up

            if (meaningInfos.Count == 0 && !hasEagerExpand)
                return;

            if (_listView.Columns.IndexOfKey(ColumnNames.Meaning) == -1)
                _listView.Columns.Add(ColumnNames.Meaning, ColumnNames.Meaning);

            //First, add the empty cells. If we're just doing eager expands for enums, we need to do this to get the correct
            //background color

            for (var i = 0; i < pendingListViewItems.Count; i++)
            {
                var listViewItem = pendingListViewItems[i];
                listViewItem.Add((string) null!);
            }

            //Now, for each item that needs a special meaning, apply these meanings. If there are only eager expands,
            //there's nothing to do
            for (var i = 0; i < meaningInfos.Count; i++)
            {
                var meaningInfo = meaningInfos[i];

                var listViewItem = pendingListViewItems[meaningInfo.rowIndex];
                var subItems = listViewItem.SubItems;

                var meaningSubItem = (TreeViewItem.TreeViewSubItem) subItems[subItems.Count - 1];
                meaningSubItem.Text = meaningInfo.value;

                switch (meaningInfo.kind)
                {
                    //Make xrefs look like hyperlinks
                    case MeaningValueKind.StructXRef:
                        meaningSubItem.ImageIndex = ImageStruct;
                        meaningSubItem.IsLink = true;
                        break;

                    case MeaningValueKind.ValueXRef:
                        meaningSubItem.ImageIndex = ImageField;
                        meaningSubItem.IsLink = true;
                        break;

                    case MeaningValueKind.FunctionXRef:
                        meaningSubItem.ImageIndex = ImageFunction;
                        meaningSubItem.IsLink = true;
                        break;
                }
            }
        }

        private void EagerExpand(
            ref PooledList<int> eagerExpand,
            ref PooledList<TreeViewItem> pendingListViewItems)
        {
            if (eagerExpand.Count == 0)
                return;

            for (var i = 0; i < eagerExpand.Count; i++)
            {
                var listViewItem = pendingListViewItems[eagerExpand[i]];

                //We can't have a method "Expand" on the listviewitem directly, because when we've got
                //a situation like IMAGE_NT_HEADERS, it's got an inlined IMAGE_FILE_HEADER inside of it,
                //and when you expand the IMAGE_FILE_HEADER, the new virtual children underneath it
                //haven't been added to the listview yet, that happens after BeforeExpand completes.

                if (!listViewItem.IsExpanded)
                {
                    //Note that if we're already in the middle of an expand, we don't want to actually add ourselves to the tree;
                    //our caller will do that for us
                    _listView.Expand(listViewItem, listViewItem.Bounds);
                }
            }
        }

        internal static bool IsArray(IFieldView fieldView)
        {
            switch (fieldView.ValueType)
            {
                case "NativeSpan`1":
                    return true;

                default:
                    return false;
            }
        }
    }
}
