using System;
using PInvoke;

namespace PESpy.Controls
{
    internal class NavBar : NativeWindow
    {
        private HFONT _hFont;

        internal NavBar()
        {
            App.AnalysisCompleted += App_AnalysisCompleted;
            App.PositionChanged += (s, e) =>
            {
                _ = hWnd;

                GetChild(0).NavAccessor.Goto(e);
            };
        }

        private void App_AnalysisCompleted(object? sender, EventArgs e)
        {
            //Load the NavBar up with our sections

            _ = hWnd; //Ensure we've been created

            var sectionAccessors = App.FileAccessor.SectionAccessors;

            var sectionComboBox = GetChild(0);
            sectionComboBox.ListBox.Count = sectionAccessors.Length;

            sectionComboBox.SelectedIndex = 0;
        }

        protected override unsafe void WmCreate(ref Message m, CREATESTRUCTW* pCreateStruct)
        {
            base.WmCreate(ref m, pCreateStruct);

            var lf = new LOGFONTW
            {
                lfHeight = -15,
            };

            "Segoe UI\0".AsSpan().CopyTo(new Span<char>(&lf.lfFaceName, 32));

            _hFont = Gdi32.CreateFontIndirectW(lf);

            AddChild("Section", new SectionNavAccessor());
            AddChild("GlobalEntity", new GlobalEntityNavAccessor());
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

        protected override void WmSize(ref Message m, int width, int height)
        {
            base.WmSize(ref m, width, height);

            //Ensure the window has been created
            _ = hWnd;

            Size = new SIZE(width, Height);

            //There's a 1 pixel white gap when the scrollbar is visible, so our
            //width will start after that
            var effectiveWidth = width - 1;

            var perBoxWidth = effectiveWidth / Children.Count;

            //If the width does not perfectly divide, assign the remainder to the last box
            var remaining = effectiveWidth - (perBoxWidth * Children.Count);

            for (var i = 0; i < Children.Count; i++)
            {
                var comboBox = Children[i];

                var myWidth = perBoxWidth;

                if (i == Children.Count - 1)
                    myWidth += remaining;

                var myX = (i * perBoxWidth) + 1;

                User32.SetWindowPos(
                    comboBox.hWnd,
                    default,
                    myX,
                    default,
                    myWidth,
                    Height,
                    SET_WINDOW_POS_FLAGS.SWP_NOZORDER
                );
            }
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
