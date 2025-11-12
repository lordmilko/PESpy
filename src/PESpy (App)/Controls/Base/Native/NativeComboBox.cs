using PInvoke;

namespace PESpy.Controls
{
    internal class NativeComboBox : SystemWindow
    {
        protected override CreateParams CreateParams
        {
            get
            {
                var createParams = base.CreateParams;

                createParams.ClassName = "ComboBox";
                createParams.Style |= (int) (WINDOW_STYLE.WS_VSCROLL) | (int) (CBS.CBS_DROPDOWNLIST | CBS.CBS_AUTOHSCROLL);
                createParams.ExStyle = WINDOW_EX_STYLE.WS_EX_CLIENTEDGE;

                return createParams;
            }
        }

        protected override unsafe void WmCreate(ref Message m, CREATESTRUCTW* pCreateStruct)
        {
            base.WmCreate(ref m, pCreateStruct);

            //Remove the dotted line around the control when focused
            User32.SendMessageW(m.hWnd, (int) WM.WM_CHANGEUISTATE, Macros.MAKELONG((ushort) UIS.UIS_SET, (ushort) UISF.UISF_HIDEFOCUS), 0);
        }
    }
}
