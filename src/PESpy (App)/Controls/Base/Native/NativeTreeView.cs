using System;
using PInvoke;

namespace PESpy.Controls
{
    /// <summary>
    /// Represents a lightweight wrapper around the native SysTreeView32 TreeView control.
    /// </summary>
    public class NativeTreeView : NativeWindow
    {
        protected override CreateParams CreateParams
        {
            get
            {
                var createParams = base.CreateParams;
                createParams.ClassName = "SysTreeView32";

                createParams.Style |= (int) (TVS.TVS_HASLINES | TVS.TVS_HASBUTTONS | TVS.TVS_LINESATROOT);
                createParams.ExStyle |= (WINDOW_EX_STYLE) TVS_EX.TVS_EX_DOUBLEBUFFER;

                return createParams;
            }
        }

#if !WINFORMS
        protected virtual void OnBeforeCollapse(TreeViewCancelEventArgs e) => throw new System.NotImplementedException();

        protected virtual void OnAfterCollapse(TreeViewEventArgs e) => throw new System.NotImplementedException();

        protected virtual void OnBeforeExpand(TreeViewCancelEventArgs e) => throw new NotImplementedException();

        protected virtual void OnAfterExpand(TreeViewEventArgs e) => throw new NotImplementedException();
#endif
    }
}
