using System;
using PInvoke;

namespace PESpy.Controls
{
    public class TextView : NativeWindow
    {
        private IGraphics _graphics;
        private TextViewRenderer _renderer;

        private new RECT ClientRectangle
        {
            get
            {
                var baseRect = base.ClientRectangle;
                baseRect.top += _renderer._yTop;
                return baseRect;
            }
        }

        public TextView()
        {
            App.PositionChanged += (s, e) =>
            {
                if (s == _renderer)
                    return;

                _renderer.Goto(e, false);
            };

            App.AnalysisCompleted += (s, e) =>
            {
                //Ensure we've been created
                _ = hWnd;

                _renderer = new TextViewRenderer(_graphics, App.FileAccessor, Width, Height, Children[0].Height);
            };
        }

        protected override unsafe void WmCreate(ref Message m, CREATESTRUCTW* pCreateStruct)
        {
            _graphics = new Win32Graphics(m.hWnd);
            _graphics.Initialize();            
        }

        protected override void WmNcDestroy(ref Message m)
        {
            _graphics?.Dispose();
            _renderer?.Dispose();
        }

        protected unsafe override void WmPaint(HDC hdc)
        {
            if (_renderer == null)
                return;

            _renderer.Paint(hdc);
        }

        protected override void WmVScroll(ref Message m, SB sb)
        {
            switch (sb)
            {
                case SB.SB_THUMBPOSITION:
                    //Updating the scroll position during thumbtrack will mess it up and cause it to jump up and down,
                    //so we need to do this after the user releases the mouse button
                    _renderer.UpdateScrollBar();
                    break;

                case SB.SB_THUMBTRACK:
                    ScrollThumbPosition();
                    break;

                case SB.SB_LINEUP:
                    _renderer.ScrollLinesUp(1);
                    break;

                case SB.SB_LINEDOWN:
                    _renderer.ScrollLinesDown(1);
                    break;
        private unsafe void ScrollThumbPosition()
        {
            var pos = _graphics.GetVerticalScrollThumb();            

            App.RaisePositionChanged(this, pos);

            _renderer.Goto(pos, true);
        }

        protected override void WmMouseWheel(ref Message m, int delta)
        {
            if (delta < 0)
                _renderer.ScrollLinesDown(3);
            else
                _renderer.ScrollLinesUp(3);
        }
}
