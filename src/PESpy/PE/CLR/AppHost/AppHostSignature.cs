using System;
using System.IO;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    //This type does not have a well-known native struct declaration

    /// <summary>
    /// Represents the .NET Core AppHost signature, which demarcates that this PE File was derived from apphost.exe
    /// </summary>
    public class AppHostSignature : IValue, IViewable
    {
        /* .NET Framework applications can be compiled to either an exe or a dll.
         * This works fine on Windows, because mscoree.dll is a system wide dll,
         * and the loader has special knowledge of how to handle .NET applications.
         * This approach does not work with .NET Core, nor does it work outside of Windows.
         *
         * As such, .NET Core applications must either be run via dotnet.exe, or via AppHost,
         * a standalone launcher that is capable of locating an installation of .NET Core on
         * your system, and then launching your actual .NET application
         *
         * An AppHost can be configured to run in one of two ways
         * 1. As a simple launcher for a DLL that sits in the same folder, next to the AppHost
         * 2. Single File Mode, wherein all dependencies of the application are "bundled" inside of the AppHost,
         *    and the AppHost must extract these dependencies when it attempts to run
         *
         * When an AppHost is compiled for launcher mode, the hash c3ab8ff13720e8ad9047dd39466b3c8974e592c2fa383d4a3960714caef0c4f2
         * is replaced with the name of the file that AppHost should attempt to load. MSBuild searches
         * for the location of these bytes inside the AppHost, and then replaces it with the name of the DLL
         * that should be loaded. corehost.cpp!is_exe_enabled_for_execution then reads this value out of the
         * static "embed" variable (defined inside the function), and looks for that file in the same directory
         * as the AppHost. Thus, it's entirely possible for an AppHost to have a different filename than the
         * embedded DLL that it looks for. Unfortunately however, we have no way of locating the "embed" variable,
         * so we must operate on the assumption that the DLL that the AppHost loads will be the same name as the AppHost
         * exe itself.
         *
         * When an AppHost is compiled in single file ("bundled") mode, MSBuild searches for an area of memory
         * within the executable that contains a SHA256 of the text ".net core bundle". In the preceding 8 bytes
         * prior to this, it will insert the location of the single file manifest that describes all of the
         * files that have been packaged into the AppHost bundle. Strictly speaking, once the single file manifest
         * address has been written, the SHA256 of ".net core bundle" is no longer required; unless someone attempts
         * to scrub this value out however, this inadvertently provides us with a mechanism for
         * a. determining whether a file is an AppHost or not, and
         * b. whether that AppHost is a single file bundle or not
         *
         * The solution everyone uses is to simply assume that the DLL will have the same filename as the AppHost.
         * This is also the approach that dotPeek uses. */

        //If the 8 bytes prior to the bundle header are 0, it's a non-bundle apphost.
        //If it's not 0, it's a single file apphost
        private static byte[] bundleHeaderPlaceholder =
        {
            // 8 bytes represent the bundle header-offset
            // Zero for non-bundle apphosts (default).
            //0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            // 32 bytes represent the bundle signature: SHA-256 for ".net core bundle"
            0x8b, 0x12, 0x02, 0xb9, 0x6a, 0x61, 0x20, 0x38,
            0x72, 0x7b, 0x93, 0x02, 0x14, 0xd7, 0xa0, 0x32,
            0x13, 0xf5, 0xb9, 0xe6, 0xef, 0xae, 0x33, 0x18,
            0xee, 0x3b, 0x2d, 0xce, 0x24, 0xb3, 0x6a, 0xae
        };

#if !PEFAST
        internal static AppHostSignature? New(IFileReader reader)
        {
            var stream = ((StreamFileReader) reader).GetStreamStartUnsafe();

            var index = KMPSearch(stream, bundleHeaderPlaceholder);

            if (index == -1)
                return null;

            reader.Seek(index - 8);

            return new AppHostSignature(reader);
        }
#endif

        public VA<Bundle.Manifest> BundleHeaderOffset { get; }

        public byte[] BundleSignature { get; }

        public RawOffset Offset { get; }

#if PEFAST
        internal AppHostSignature(in MemoryChunk chunk)
        {
            throw new NotImplementedException();
        }
#else
        internal AppHostSignature(IFileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            var bundleHeaderOffset = reader.ReadInt64();

            BundleSignature = reader.ReadBytes(24);

            if (bundleHeaderOffset != 0)
            {
                reader.Seek(bundleHeaderOffset);
                BundleHeaderOffset = new VA<Bundle.Manifest>(bundleHeaderOffset, (int) bundleHeaderOffset, new Bundle.Manifest(reader));
            }
            else
                BundleHeaderOffset = new VA<Bundle.Manifest>(bundleHeaderOffset);
        }
#endif

        void IViewable.WriteView(ViewWriter writer)
        {
            //throw new NotImplementedException();
        }

        // See: https://en.wikipedia.org/wiki/Knuth%E2%80%93Morris%E2%80%93Pratt_algorithm
        private static int KMPSearch(Stream stream, byte[] pattern)
        {
            var blockSize = 4096;

            int[] table = ComputeKMPFailureFunction(pattern);  // Failure function
            byte[] buffer = new byte[blockSize + pattern.Length];  // Buffer with space for overlap
            int bytesRead;
            int m = 0;  // The beginning of the current match in the buffer
            int i = 0;  // The current position in the pattern
            long totalBytesRead = 0;  // Track the total bytes read from the stream

            while ((bytesRead = stream.Read(buffer, pattern.Length, blockSize)) > 0)
            {
                int bufferLength = bytesRead + pattern.Length;

                // Search through the buffer
                for (int k = 0; m + i < bufferLength; k++)
                {
                    if (pattern[i] == buffer[m + i])
                    {
                        if (i == pattern.Length - 1)  // Found match
                        {
                            return (int)(totalBytesRead + m - pattern.Length);  // Return the starting index of the match in the stream
                        }
                        i++;
                    }
                    else
                    {
                        if (table[i] > -1)
                        {
                            m = m + i - table[i];  // Use the failure function to adjust the start of the match
                            i = table[i];
                        }
                        else
                        {
                            m++;  // No match, move to the next character in the buffer
                            i = 0;
                        }
                    }
                }

                // Update the total number of bytes read so far
                totalBytesRead += bytesRead;

                // Copy the last part (pattern.Length bytes) of the current block to the beginning of the buffer
                Array.Copy(buffer, blockSize, buffer, 0, pattern.Length);

                // Reset m to point to the start of the new data (right after the overlap)
                m = 0;
            }

            return -1;  // No match found
        }

        // See: https://en.wikipedia.org/wiki/Knuth%E2%80%93Morris%E2%80%93Pratt_algorithm
        private static int[] ComputeKMPFailureFunction(byte[] pattern)
        {
            int[] table = new int[pattern.Length];
            if (pattern.Length >= 1)
            {
                table[0] = -1;
            }
            if (pattern.Length >= 2)
            {
                table[1] = 0;
            }

            int pos = 2;
            int cnd = 0;
            while (pos < pattern.Length)
            {
                if (pattern[pos - 1] == pattern[cnd])
                {
                    table[pos] = cnd + 1;
                    cnd++;
                    pos++;
                }
                else if (cnd > 0)
                {
                    cnd = table[cnd];
                }
                else
                {
                    table[pos] = 0;
                    pos++;
                }
            }
            return table;
        }
    }
}
