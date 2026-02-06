using System;
using System.Diagnostics;

namespace PESpy.UI
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
            var fileAccessor = App.FileAccessor;

            if (fileAccessor == null)
                return 0;

            return fileAccessor.SectionAccessors.Length;
        }

        public void SelectedIndexChanged(int selectedIndex)
        {
            if (selectedIndex == -1)
                return;

            var fileAccessor = App.FileAccessor;

            if (fileAccessor == null)
                return;

            Next?.Refresh(selectedIndex);

            ref var sectionAccessor = ref fileAccessor.SectionAccessors[selectedIndex];

            //If we have a next, it's on them to set their position
            if (Next == null)
                App.RaisePositionChanged(this, sectionAccessor.StartAddress);
        }
        public void Goto(int targetAddress)
        {
            var fileAccessor = App.FileAccessor;

            if (fileAccessor == null)
                return;

            var index = Self.SelectedIndex;

            var sectionAccessors = fileAccessor.SectionAccessors;

            ref var sectionAccessor = ref sectionAccessors[index];

            if (!sectionAccessor.Contains(targetAddress))
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
            else
            {
                //This combobox is already at the entity that contains the target address; forward onto the next combobox
                Next?.NavAccessor.Goto(targetAddress);
            }
        }
    }
}
