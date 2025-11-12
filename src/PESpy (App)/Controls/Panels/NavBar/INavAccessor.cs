using System;

namespace PESpy.Controls
{
    internal interface INavAccessor
    {
        NavComboBox Self { get; set; }

        //Gets the next comobox after this one in the chain
        NavComboBox Next { get; set; }

        //Get the name of the child at the specified index under this accessor
        ReadOnlySpan<char> GetName(int index);

        //Get the number of items contained in this accessor
        int GetCount();

        //Notify this accessor that the selected index has changed. This accessor
        //should update its internal bookkeeping and replace the contents of the
        //next level combobox after it
        void SelectedIndexChanged(int selectedIndex);

        //Receive notification that the parent accessor of this accessor has changed.
        //Based on the type of parent this accessor expects to have, it should refresh
        //its internal list of children from the new parent
        void NotifyParentChanged(int parentSelectedIndex);

        void Goto(int targetAddress);
    }
}
