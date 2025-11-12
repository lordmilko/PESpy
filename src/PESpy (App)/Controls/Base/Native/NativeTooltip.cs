using System;
using PInvoke;

namespace PESpy.Controls
{
    internal class NativeTooltip : NativeWindow
    {
        protected override CreateParams CreateParams
        {
            get
            {
                var createParams = base.CreateParams;
                createParams.ClassName = "tooltips_class32";

                if (ShowAlways)
                    createParams.Style |= (int) TTS.ALWAYSTIP;

                return createParams;
            }
        }

        private int[] _delayTimes = new int[4];
        private bool _showAlways;

        private const int DefaultDelay = 500;
        private const int ReshowRatio = 5;
        private const int AutoPopRatio = 10;

        public bool ShowAlways
        {
            get => _showAlways;
            set
            {
                if (_showAlways != value)
                {
                    _showAlways = value;

                    if (IsHandleCreated)
                        throw new InvalidOperationException("We don't support recreating our handle; all setup must be performed prior to creating the handle");
                }
            }
        }

        public int AutomaticDelay
        {
            get => _delayTimes[(int) TTDT.AUTOMATIC];
            set
            {
                _delayTimes[(int) TTDT.AUTOMATIC] = value;

                if (IsHandleCreated)
                    throw new InvalidOperationException("We don't support recreating our handle; all setup must be performed prior to creating the handle");

                AdjustBaseFromAuto();
            }
        }

        public void SetToolTip(NativeWindow window, string? caption)
        {
            throw new NotImplementedException();
        }

        public NativeTooltip()
        {
            _delayTimes[(int) TTDT.AUTOMATIC] = DefaultDelay;

            AdjustBaseFromAuto();
        }

        private void AdjustBaseFromAuto()
        {
            var delay = _delayTimes[(int) TTDT.AUTOMATIC];
            _delayTimes[(int) TTDT.RESHOW] = delay / ReshowRatio;
            _delayTimes[(int) TTDT.AUTOPOP] = delay * AutoPopRatio;
            _delayTimes[(int) TTDT.INITIAL] = delay;
        }
    }
}
