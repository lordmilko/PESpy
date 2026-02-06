using PInvoke;

namespace PESpy.UI
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

                return createParams;
            }
        }

        public NativeComboBox()
        {
            Border = true;
        }
    }
}
