using System;
using PESpy.View;

namespace PESpy
{
    //Provides facilities for constructing a ListView from a ListTreeNode
    //that is backed by an array
    class ArrayListWriter : ListWriter
    {
        private Array _array;

        public ArrayListWriter(Array array, FileAccessor fileAccessor) : base(fileAccessor)
        {
            _array = array;
        }

        protected override int GetCount() => _array.Length;

        protected override IStructView GetFirst() => GetItem(0);

        protected override IStructView GetItem(int index) =>
            _fileAccessor.GetStructView((IViewable) _array.GetValue(index)!);
    }
}
