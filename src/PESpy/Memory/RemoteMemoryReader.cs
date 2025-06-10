using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PESpy
{
    public unsafe class RemoteMemoryReader : IMemoryReader
    {
        public const int PAGE_SIZE = 0x1000;

        private IntPtr hProcess;

        public RemoteMemoryReader(IntPtr hProcess)
        {
            this.hProcess = hProcess;
        }

        public void ReadVirtual(long address, IntPtr buffer, int size)
        {
            var totalRead = 0;

            while (size > 0)
            {
                //This bit of magic ensures we're not reading more than 1 page worth of data. I don't understand how this works
                //however Microsoft use it all the time so you know it's right
                var readSize = PAGE_SIZE - (int) (address & (PAGE_SIZE - 1));
                readSize = Math.Min(size, readSize);

                var result = ReadProcessMemory(
                    hProcess,
                    (IntPtr) (void*) address,
                    buffer,
                    readSize,
                    out var bytesRead
                );

                if (!result)
                {
                    Debug.Assert(false, "Failed to read memory");
                    break;
                }

                totalRead += bytesRead;
                address += bytesRead;
                buffer = new IntPtr(buffer.ToInt64() + bytesRead);
                size -= bytesRead;
            }
        }

        private const uint ERROR_NOACCESS = 0x800703E6;

        public static bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            IntPtr lpBuffer,
            int dwSize,
            out int lpNumberOfBytesRead)
        {

            var result = ReadProcessMemory(hProcess, lpBaseAddress, lpBuffer, new IntPtr(dwSize), out var read);

            if (!result)
            {
                var hr = (uint) Marshal.GetHRForLastWin32Error();

                //If you attempt to read invalid memory, NtReadVirtualMemory can bail out immediately and return STATUS_ACCESS_VIOLATION
                //without even setting the size. This results in the number of bytes read being garbage memory, which can cause an overflow
                //exception when we try and cast it to an integer. STATUS_ACCESS_VIOLATION gets mapped to ERROR_NOACCESS by RtlNtStatusToDosError
                //using some crazy mapping tables I don't understand
                if (hr == ERROR_NOACCESS)
                {
                    lpNumberOfBytesRead = 0;
                    return false;
                }
            }

            lpNumberOfBytesRead = (int) read;

            return result;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(
            [In] IntPtr hProcess,
            [In] IntPtr lpBaseAddress,
            [Out] IntPtr lpBuffer,
            [In] IntPtr dwSize,
            [Out] out IntPtr lpNumberOfBytesRead);
    }
}
