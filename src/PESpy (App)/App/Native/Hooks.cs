#if NET
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PInvoke;

namespace PESpy
{
    internal static class Hooks
    {
        internal static unsafe void InstallCreateWindowExWHook()
        {
            var hModule = Kernel32.GetModuleHandleW("comctl32.dll");

            if (hModule == default)
            {
                //Throws on failure
                hModule = Kernel32.LoadLibraryW("comctl32.dll");
            }

            using var peFile = PEFile.FromProcess(Kernel32.GetCurrentProcess(), hModule);

            var imports = peFile.ImportTable;

            for (var i = 0; i < imports!.Length; i++)
            {
                ref var import = ref imports[i];

                if (import.Name.Value.EqualsIgnoreCase("USER32.dll"))
                {
                    for (var j = 0; j < import.OriginalFirstThunk.Value.Length; j++)
                    {
                        ref var thunk = ref import.OriginalFirstThunk.Value[j];

                        if (thunk.Name.IsValid && thunk.Name.Value.Name == "CreateWindowExW")
                        {
                            var iat = import.FirstThunk.Value[j];

                            var pFunction = (byte*) hModule + iat.Offset;
                            var value = *(IntPtr*) pFunction;

                            Kernel32.VirtualProtect((void*) pFunction, (nuint) IntPtr.Size, PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READWRITE, out var oldProtect);

                            *(IntPtr*) (pFunction) = (nint) (delegate* unmanaged[Stdcall]<
                                WINDOW_EX_STYLE,
                                PCWSTR,
                                PCWSTR,
                                WINDOW_STYLE,
                                int, int, int, int,
                                IntPtr,
                                IntPtr,
                                IntPtr,
                                void*,
                                IntPtr>) (&CreateWindowExWHook);

                            Kernel32.VirtualProtect((void*) pFunction, (nuint) IntPtr.Size, oldProtect, out _);

                            return;
                        }
                    }
                }
            }

            throw new NotImplementedException();
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        internal static unsafe IntPtr CreateWindowExWHook(
            WINDOW_EX_STYLE dwExStyle,
            PCWSTR lpClassName,
            PCWSTR lpWindowName,
            WINDOW_STYLE dwStyle,
            int X,
            int Y,
            int nWidth,
            int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            [Optional] void* lpParam)
        {
            if (new Utf16String(lpClassName.Value) == "ComboLBox")
            {
                //I checked the decompilation and it seems that ComboBox now forces LBS_HASSTRINGS even if you didn't ask for it

                var style = (int) dwStyle;

                style &= ~(int) LBS.LBS_HASSTRINGS;
                style |= (int) (LBS.LBS_OWNERDRAWFIXED | LBS.LBS_NODATA);

                dwStyle = (WINDOW_STYLE) style;
            }

            return User32.CreateWindowExW(dwExStyle, lpClassName, lpWindowName, dwStyle, X, Y, nWidth, nHeight, hWndParent, hMenu, hInstance, lpParam);
        }
    }
}
#endif
