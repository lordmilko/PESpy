using System;
using System.Diagnostics;

namespace PESpy.Controls
{
    //Represents an INavAccessor used to interact with SectionAccessor objects
    internal class SectionNavAccessor : INavAccessor
    {
        public NavComboBox Self { get; set; }

        public NavComboBox Next { get; set; }

        private ValueStringBuilder.NonRef _stringBuilder;
        private int _lastIndex = -1;

        public SectionNavAccessor()
        {
            _stringBuilder = new ValueStringBuilder.NonRef(100);
        }

        public ReadOnlySpan<char> GetName(int index)
        {
            if (App.FileAccessor == null)
                return default;

            if (index == _lastIndex)
            {
                Debug.Assert(_stringBuilder.Length > 0);
                return _stringBuilder.AsSpan();
            }

            _stringBuilder.Clear();

            ref var sectionAccessor = ref App.FileAccessor.SectionAccessors[index];

            _stringBuilder.Append("0x");
            _stringBuilder.AppendHex((uint) sectionAccessor.StartAddress);
            _stringBuilder.Append(' ');
            _stringBuilder.Append(sectionAccessor.Name);

            return _stringBuilder.ToString().AsSpan();
        }

        public int GetCount()
        {
            if (App.FileAccessor == null)
                return 0;

            return App.FileAccessor.SectionAccessors.Length;
        }

        public void SelectedIndexChanged(int selectedIndex)
        {
            if (selectedIndex == -1)
                return;

            Next?.Refresh(selectedIndex);

            ref var sectionAccessor = ref App.FileAccessor.SectionAccessors[selectedIndex];

            //If we have a next, it's on them to set their position
            if (Next == null)
                App.RaisePositionChanged(this, sectionAccessor.StartAddress);
        }

        public void NotifyParentChanged(int parentSelectedIndex)
        {
            //As this accessor is top level, this should never get called
            //(and if it did we would want to clear our _lastIndex)
            throw new NotSupportedException();
        }

        public void Goto(int targetAddress)
        {
            var index = Self.SelectedIndex;

            var sectionAccessors = App.FileAccessor.SectionAccessors;

            ref var sectionAccessor = ref sectionAccessors[index];

            if (!sectionAccessor.Contains(targetAddress))
            {
                //Find the accessor that contains the target index in the direction of it relative to us
                
                if (targetAddress < sectionAccessor.StartAddress)
                {
                    //Walk backwards trying to find it
                    throw new NotImplementedException();
                }
                else
                {
                    //Walk forwards trying to find it
                    
                    for (var i = index + 1; i < sectionAccessors.Length; i++)
                    {
                        sectionAccessor = ref sectionAccessors[i];

                        if (sectionAccessor.Contains(targetAddress))
                        {
                            Self.SelectedIndex = i;
                            Next?.NavAccessor.Goto(targetAddress);
                            return;
                        }
                    }
                }

                throw new NotImplementedException();
            }
            else
            {
                //This combobox is already at the entity that contains the target address; forward onto the next combobox
                Next?.NavAccessor.Goto(targetAddress);
            }
        }
    }
}
