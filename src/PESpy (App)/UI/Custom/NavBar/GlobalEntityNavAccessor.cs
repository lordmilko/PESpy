using System;
using System.Diagnostics;
using System.Linq;
using PESpy.View;

namespace PESpy.UI
{
    //Represents a section accessor used to interact with the global items under a given
    //section index
    internal class GlobalEntityNavAccessor : INavAccessor
    {
        public NavComboBox Self { get; set; }

        public NavComboBox Next { get; set; }

        private ViewEntity[] _entities;

        private ValueStringBuilder.NonRef _stringBuilder;
        private int _lastIndex = -1;

        public GlobalEntityNavAccessor()
        {
            _stringBuilder = new ValueStringBuilder.NonRef(100);
        }

        public unsafe ReadOnlySpan<char> GetName(int index)
        {
            if (index == _lastIndex)
            {
                Debug.Assert(_stringBuilder.Length > 0);
                return _stringBuilder.AsSpan();
            }

            var entity = _entities[index];

            _stringBuilder.Clear();

            _stringBuilder.Append("0x");
            _stringBuilder.AppendHex((uint) entity.TargetAddress);
            _stringBuilder.Append(' ');
            entity.ToString(ref _stringBuilder);

            _lastIndex = index;

            return _stringBuilder.AsSpan();
        }

        public int GetCount()
        {
            if (App.FileAccessor == null)
                return 0;

            if (_entities == null)
                return 0;

            return _entities.Length;
        }

        public void SelectedIndexChanged(int selectedIndex)
        {
            if (_entities == null)
                return;

            Next?.Refresh(selectedIndex);

            //If we have a next, it's on them to set their position
            if (Next == null)
                App.RaisePositionChanged(this, _entities[selectedIndex].TargetAddress);
        }
        public void Goto(int targetAddress)
        {
            var index = Self.SelectedIndex;

            var currentEntity = _entities[index];

            if (!currentEntity.Contains(targetAddress))
            {
                if (targetAddress < currentEntity.TargetAddress)
                    //Binary search fallback
                    var lo = 0;
                    var hi = index - 1;

                    BinarySearch(targetAddress, lo, hi);
                }
                else
                {
                    //We want to fast path the user scrolling down without having to resort to a large binary search.
                    //Small items such as tiny padding can quickly zip by the screen; as such, we'll try and check whether
                    //the target address is within a "small" number of entries from the current position, and if so scope our
                    //search just to that. Otherwise, we'll be forced to binary search

                    //Try the next entity after us
                    var nextEntity = _entities[index + 1];

                    if (nextEntity.Contains(targetAddress))
                        Self.SelectedIndex = index + 1;
                    else
                    {
                        var toEnd = _entities.Length - (index + 1);

                        var distance = Math.Min(toEnd, 100);
                        var endDistance = distance + index + 2;

                        var limit = _entities[index + distance];

                        if (targetAddress < limit.TargetAddress)
                        {
                            for (var i = index + 2; i < endDistance; i++)
                            {
                                nextEntity = _entities[i];

                                if (nextEntity.Contains(targetAddress))
                                {
                                    Self.SelectedIndex = i;
                                    Next?.NavAccessor.Goto(targetAddress);
                                    return;
                                }
                            }
                        }

                        //Uh-oh, we're going to have to binary search
                        var lo = index + distance;
                        var hi = _entities.Length - 1;

                        BinarySearch(targetAddress, lo, hi);
                    }
                }
            }
            else
                Next?.NavAccessor.Goto(targetAddress);
        }

        private void BinarySearch(int targetAddress, int lo, int hi)
        {
            while (lo <= hi)
            {
                var mid = (lo + hi) / 2;

                var entry = _entities[mid];

                if (targetAddress < entry.TargetAddress)
                    hi = mid - 1;
                else if (targetAddress > (entry.TargetAddress + entry.Length))
                    lo = mid + 1;
                else
                {
                    Self.SelectedIndex = mid;
                    Next?.NavAccessor.Goto(targetAddress);
                    return;
                }
            }
}
