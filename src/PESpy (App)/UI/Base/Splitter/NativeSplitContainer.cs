using PInvoke;

namespace PESpy.UI
{
    internal class NativeSplitContainer : NativeWindow
    {
        private const int SPLITTER_WIDTH = 5;

        public FixedPanel FixedPanel { get; set; }

        public NativePanel Panel1 { get; set; }

        public NativeSplitter Splitter { get; set; }

        public NativePanel Panel2 { get; set; }

        private int _splitterDistance;

        //The distance of the splitter from the size. The value of this
        //implicitly determines the size of the first panel
        public int SplitterDistance
        {
            get => _splitterDistance;
            set
            {
                if (_splitterDistance != value)
                {
                    _splitterDistance = value;

                    if (Orientation == Orientation.Vertical)
                        Panel1.Width = value;
                    else
                        Panel1.Height = value;

                    UpdateSplitter();
                }
            }
        }

        public Orientation Orientation
        {
            get => Splitter.Orientation;
            set => Splitter.Orientation = value;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var createParams = base.CreateParams;
                createParams.Style |= (int) (WINDOW_STYLE.WS_CLIPCHILDREN | WINDOW_STYLE.WS_CLIPSIBLINGS);
                return createParams;
            }
        }

        public NativeSplitContainer()
        {
            Panel1 = new NativePanel();
            Splitter = new NativeSplitter();
            Panel2 = new NativePanel();

            Children.Add(Panel1);
            Children.Add(Splitter);
            Children.Add(Panel2);
        }

        protected override void OnLayout()
        {
            base.OnLayout();

            UpdateSplitter();
        }

        private unsafe void UpdateSplitter()
        {
            if (Orientation == Orientation.Vertical)
            {
                Panel1.Height = Height;
                Panel1.Width = _splitterDistance;

                //Sets width and height
                Panel2.Size = new SIZE(Width - _splitterDistance - SPLITTER_WIDTH, Height);

                Panel1.Location = new POINT(0, 0);
                Panel2.Location = new POINT(_splitterDistance + SPLITTER_WIDTH, 0);

                Splitter.Bounds = new RECT
                {
                    X = Location.x + _splitterDistance,
                    Y = Location.y,
                    Width = SPLITTER_WIDTH,
                    Height = Height
                };
            }
            else
            {
                Panel1.Location = new POINT(0, 0);
                Panel1.Width = Width;

                Panel1.Height = _splitterDistance;

                var panel2Start = _splitterDistance + SPLITTER_WIDTH;
                Panel2.Size = new SIZE(Width, Height - panel2Start);
                Panel2.Location = new POINT(0, panel2Start);

                Splitter.Bounds = new RECT
                {
                    X = Location.x,
                    Y = Location.y + _splitterDistance,
                    Width = Width,
                    Height = SPLITTER_WIDTH
                };
            }

            if (!IsHandleCreated)
                return;

            User32.UpdateWindow(hWnd);
        }
    }
}
