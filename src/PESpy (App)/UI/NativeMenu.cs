using System;
using System.Collections;
using PInvoke;

namespace PESpy.UI
{
    public struct NativeMenu : IDisposable, IEnumerable
    {
        public HMENU hMenu { get; private set; }

        public NativeMenu(HMENU hMenu)
        {
            this.hMenu = hMenu;
        }

        public NativeMenu()
        {
            hMenu = User32.CreatePopupMenu();
        }

        public void Add(string text, HMENU hSubMenu)
        {
            User32.AppendMenuW(hMenu, MENU_ITEM_FLAGS.MF_POPUP, (nuint) (nint) hSubMenu, text);
        }

        public void Add(string text, WellKnownCommand cmd)
        {
            User32.AppendMenuW(hMenu, MENU_ITEM_FLAGS.MF_STRING, (nuint) cmd, text);
        }

        public static implicit operator HMENU(NativeMenu menu) => menu.hMenu;

        public void Dispose()
        {
            if (hMenu != default)
            {
                //DestroyMenu destroys the menu and all submenus
                User32.DestroyMenu(hMenu);
                hMenu = default;
            }
        }
}
