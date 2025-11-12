using PInvoke;

namespace PESpy
{
    public struct Message
    {
        public HWND hWnd { get; }

        public WM Msg { get; }

        public WPARAM wParam { get; }

        public LPARAM lParam { get; }

        public LRESULT Result { get; set; }

        public Message(HWND hWnd, WM msg, WPARAM wParam, LPARAM lParam)
        {
            this.hWnd = hWnd;
            Msg = msg;
            this.wParam = wParam;
            this.lParam = lParam;
            Result = 0;
        }

        public override string ToString()
        {
            return Msg.ToString();
        }
    }
}
