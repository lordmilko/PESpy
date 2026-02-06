using System;
using System.Runtime.CompilerServices;
using PESpy.Controls;
using PInvoke;

namespace PESpy
{
    internal static class Program
    {
        [STAThread]
        unsafe static void Main()
        {
            //Note that there seems to be an issue with SDK style projects wherein the cursor shows for several seconds when you attempt to debug them in Visual Studio.
            //The selected .NET version doesn't matter

            try
            {
                var form = new NativeForm();

                var hAccel = CreateAcceleratorTable();

                form.Visible = true;

                while (User32.GetMessageW(out var msg, default, default, default))
                {
                    if (User32.TranslateAcceleratorW(form.hWnd, hAccel, msg) == 0)
                    {
                        User32.TranslateMessage(msg);
                        User32.DispatchMessageW(msg);
                    }
                }

                User32.DestroyAcceleratorTable(hAccel);
            }
            catch (Exception ex)
            {
                App.RaiseError(ex.Message);
            }
        }

        private unsafe static HACCEL CreateAcceleratorTable()
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static ACCEL CreateAccelerator(VIRTUAL_KEY vk, WellKnownCommand cmd)
            {
                return new ACCEL
                {
                    fVirt = ACCEL_VIRT_FLAGS.FVIRTKEY | ACCEL_VIRT_FLAGS.FCONTROL,
                    key = (short) vk,
                    cmd = (short) cmd
                };
            }

            //Apparently you must always specify FVIRTKEY
            var accelerators = new ACCEL[]
            {
                CreateAccelerator(VIRTUAL_KEY.VK_C, WellKnownCommand.Copy) //Ctrl+C
            };

            fixed (ACCEL* p = accelerators)
            {
                var hAccel = User32.CreateAcceleratorTableW(p, accelerators.Length);

                return hAccel;
            }
        }
    }
}
