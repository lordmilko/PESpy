using PESpy.View;

namespace PESpy
{
    //Writes a list of fixed sized structs where the list is identified by its starting offset
    class OffsetListWriter : ListWriter
    {
        private int _offset;
        private int _count;
        private ViewKind _viewKind;

        private IStructView? _first;

        internal OffsetListWriter(int offset, int count, ViewKind viewKind, FileAccessor fileAccessor) : base(fileAccessor)
        {
            _offset = offset;
            _count = count;
            _viewKind = viewKind;
        }

        protected override int GetCount() => _count;

        protected override IStructView GetFirst() =>
            _first ??= _fileAccessor.GetStructView(_offset, _viewKind);

        protected override IStructView GetItem(int index)
        {
            //Each node needs to be the same size in order for this to work
            return _fileAccessor.GetStructView(_offset + (index * _first!.Size), _viewKind);
        }
    }
}
