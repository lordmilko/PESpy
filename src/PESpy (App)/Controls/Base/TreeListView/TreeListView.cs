using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
#if WINFORMS
using System.Windows.Forms;
#endif
using PInvoke;
using static PInvoke.Macros;
#if WINFORMS
using ListViewSubItem = System.Windows.Forms.ListViewItem.ListViewSubItem;
#else
using Font = PESpy.NativeFont;
using ListView = PESpy.Controls.NativeListView;
using ListViewItem = PESpy.Controls.NativeListViewItem;
using ListViewSubItem = PESpy.Controls.NativeListViewItem.NativeListViewSubItem;
#endif

namespace PESpy.Controls
{
    
}
