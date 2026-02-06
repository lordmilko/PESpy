using System;
using PESpy.View;
using PInvoke;

namespace PESpy.UI
{
    internal class NavBar : NativeWindow
    {
        private HFONT _hFont;

        internal NavBar()
        {
            SetState(WindowState.Visible, true);

            App.FileOpened += App_FileOpened;
            App.FileClosed += App_FileClosed;
        protected override void CreateHandle()
        {
            base.CreateHandle();

            Size = new SIZE(Parent.Width, GetChild(0).Height + 2); //e.g. 28+2 = 30

            onCreateHandle?.Invoke();
        }
        protected override void WmCommand(ref Message m, int notificationCode, int controlId, HWND control)
        {
            base.WmCommand(ref m, notificationCode, controlId, control);

            var code = (CBN) notificationCode;

            if (code == CBN.CBN_SELCHANGE)
            {
                var owner = (NavComboBox) Children.Find(control);
                owner.RaiseSelectedIndexChanged();
            }
        }

        protected override void WmPaint(HDC hdc)
        {
            base.WmPaint(hdc);

            //This is needed because when we're horizontally resizing, the part of the navbar that lines up with the
            //vertical scrollbar won't repaint itself. Even though visually all of the combobox items are above the
            //white background of the TextView, you'll still end up with little grey scrollbar pieces appearing in
            //the TextView that we need to paint over
            User32.FillRect(hdc, ClientRectangle, DefaultBackgroundBrush);
        }
        private void AddChild(string name, INavAccessor navAccessor)
        {
            var comboBox = new NavComboBox(name, navAccessor, _hFont);

            if (Children.Count > 0)
                ((NavComboBox) Children[Children.Count - 1]).NavAccessor.Next = comboBox;

            Children.Add(comboBox);
        }

        private NavComboBox GetChild(int index) => (NavComboBox) Children[index];
    }
}
