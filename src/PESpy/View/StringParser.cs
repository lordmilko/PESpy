using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace PESpy.View
{
    internal struct ExtractedString
    {
        public int Start;
        public int Length;
        public AnsiString Ansi;
        public FixedUtf16String Unicode;
        public bool IsUnicode;

        public override string ToString()
        {
            return IsUnicode ? Unicode.ToString() : Ansi.ToString();
        }
    }

    /// <summary>
    /// Implements a rudimentary string parser capable of extracting narrow and wide English language strings from a byte array.
    /// </summary>
    internal class StringParser
    {
        private static bool[] displayableAscii =
        {
            /*          0     1     2     3        4     5     6     7        8     9     A     B        C     D     E     F     */
	        /* 0x00 */  false,false,false,false,   false,false,false,false,   false,true ,true ,false,   false,true ,false,false, //0x9 (\t), 0xA (\n), 0xD (\r)
	        /* 0x10 */  false,false,false,false,   false,false,false,false,   false,false,false,false,   false,false,false,false,
	        /* 0x20 */  true ,true ,true ,true ,   true ,true ,true ,true ,   true ,true ,true ,true ,   true ,true ,true ,true , //0x20 (space)
	        /* 0x30 */  true ,true ,true ,true ,   true ,true ,true ,true ,   true ,true ,true ,true ,   true ,true ,true ,true ,
	        /* 0x40 */  true ,true ,true ,true ,   true ,true ,true ,true ,   true ,true ,true ,true ,   true ,true ,true ,true ,
	        /* 0x50 */  true ,true ,true ,true ,   true ,true ,true ,true ,   true ,true ,true ,true ,   true ,true ,true ,true ,
	        /* 0x60 */  true ,true ,true ,true ,   true ,true ,true ,true ,   true ,true ,true ,true ,   true ,true ,true ,true ,
	        /* 0x70 */  true ,true ,true ,true ,   true ,true ,true ,true ,   true ,true ,true ,true ,   true ,true ,true ,false, //0x7E (~)
	        /* 0x80 */  false,false,false,false,   false,false,false,false,   false,false,false,false,   false,false,false,false,
	        /* 0x90 */  false,false,false,false,   false,false,false,false,   false,false,false,false,   false,false,false,false,
	        /* 0xA0 */  false,false,false,false,   false,false,false,false,   false,false,false,false,   false,false,false,false,
	        /* 0xB0 */  false,false,false,false,   false,false,false,false,   false,false,false,false,   false,false,false,false,
	        /* 0xC0 */  false,false,false,false,   false,false,false,false,   false,false,false,false,   false,false,false,false,
	        /* 0xD0 */  false,false,false,false,   false,false,false,false,   false,false,false,false,   false,false,false,false,
	        /* 0xE0 */  false,false,false,false,   false,false,false,false,   false,false,false,false,   false,false,false,false,
	        /* 0xF0 */  false,false,false,false,   false,false,false,false,   false,false,false,false,   false,false,false,false
        };

        internal const int MinimumStringLength = 5; //4 + \0

        #region Standalone

        internal static unsafe ExtractedString[] GetAnsiNullTerminated(byte* bytes, int bytesLength)
        {
            var results = new PooledList<ExtractedString>();

            var arr = displayableAscii;

            try
            {
                for (var i = 0; i < bytesLength; i++)
                {
                    var b = bytes[i];

                    if (b < 0x7F && arr[b])
                    {
                        //We potentially found the start of an ASCII string. Continue reading characters as long as valid until we hit a \0

                        GetAnsiWorker(ref i, bytes, bytesLength, ref results);
                    }
                }

                return results.ToArray();
            }
            finally
            {
                results.Dispose();
            }
        }

        //Don't use Span/NativeSpan, it's too slow indexing into it
        public unsafe static ExtractedString[] GetStrings(byte* bytes, int bytesLength)
        {
            var results = new PooledList<ExtractedString>();

            var arr = displayableAscii;

            try
            {
                for (var i = 0; i < bytesLength; i++)
                {
                    var b = bytes[i];

                    if (b < 0x7F && arr[b])
                    {
                        //It's either an ASCII string or a unicode string

                        if (i < bytesLength - 1 && bytes[i + 1] == 0)
                        {
                            //It's either a random value followed by a 0, or a unicode string

                            GetUnicodeWorker(ref i, bytes, bytesLength, false, ref results);
                        }
                        else
                        {
                            //Try for a simple ASCII then
                            GetAnsiWorker(ref i, bytes, bytesLength, ref results);
                        }
                    }
                }

                return results.ToArray();
            }
            finally
            {
                results.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void GetAnsiWorker(ref int i, byte* bytes, int bytesLength, ref PooledList<ExtractedString> results)
        {
            var foundEnd = false;

            int j = i + 1;

            var arr = displayableAscii;

            for (; j < bytesLength; j++)
            {
                var b2 = bytes[j];

                if (b2 == 0)
                {
                    foundEnd = true;
                    break;
                }

                if (!arr[b2])
                {
                    //Not only was it not a valid string, but everything we read implicitly is also invalid
                    break;
                }
            }

            if (foundEnd)
            {
                //We think we've found an ANSI string. However, it's just as possible that in fact what we had was garbage, and the last character + null terminator we found was in fact
                //the first character of an _actual_ unicode string! To detect this, we'll try and read the next two characters. If they look like unicode characters, we're trampling over
                //another string; rewind and bail out

                if (j < bytesLength - 2)
                {
                    var u1 = bytes[j + 1];

                    //Don't consider a \0\0 here; not only might there just be padding after us, but also if it's \0\0, clearly we haven't run into another string!
                    if (arr[u1] && bytes[j + 2] == 0)
                    {
                        i = j - 2;
                        return;
                    }
                }

                var length = (j - i) + 1;

                if (length >= MinimumStringLength) //4 characters + \0
                {
                    var str = new AnsiString(bytes + i);

                    results.Add(new ExtractedString
                    {
                        Start = i,
                        Length = length,
                        Ansi = str
                    });
                }
            }

            i = j; //The caller will do i++ for us
        }

        //Don't pass NativeSpan around; it's too slow indexing into it
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void GetUnicodeWorker(ref int i, byte* bytes, int bytesLength, bool nullTerminated, ref PooledList<ExtractedString> results)
        {
            var foundEnd = false;

            int j = i + 2;

            for (; j < bytesLength - 1; j++)
            {
                var b2 = bytes[j];

                if (b2 == 0)
                {
                    if (bytes[j + 1] == 0)
                    {
                        //j += 2;
                        foundEnd = true;
                        break;
                    }
                    else
                    {
                        //Expected two 0 in a row. This is not a unicode string
                        break;
                    }
                }

                if (!displayableAscii[b2] || bytes[j + 1] != 0)
                {
                    if (!nullTerminated)
                    {
                        foundEnd = true;
                    }

                    //Either the current character is not ASCII, or it was not followed by a \0
                    break;
                }

                //Skip over the second byte
                j++;
            }

            if (foundEnd)
            {
                var length = (j - i) + 2;

                if (length >= MinimumStringLength * 2) //4 characters
                {
                    var str = new FixedUtf16String((char*) (byte*) (bytes + i), (length - 2) / 2);

                    results.Add(new ExtractedString
                    {
                        Start = i,
                        Length = length,
                        Unicode = str,
                        IsUnicode = true
                    });
                }

                i = j + 1; //This does seem to be right; don't need +2. The caller will do i++ for us
            }
        }

        #endregion
        #region ViewByte

        //Don't use Span/NativeSpan, it's too slow indexing into it
        public unsafe static void GetStrings(
            ViewByte* pViewByte,
            byte* bytes,
            int bytesLength,
            int sectionAddress,
            ViewByte* pSectionStart,
            List<int> stringAddresses)
        {
            var arr = displayableAscii;

            for (var i = 0; i < bytesLength; i++)
            {
                var b = bytes[i];

                if (b < 0x7F && arr[b])
                {
                    //It's either an ASCII string or a unicode string

                    if (i < bytesLength - 1 && bytes[i + 1] == 0)
                    {
                        //It's either a random value followed by a 0, or a unicode string

                        GetUnicodeWorker(ref i, bytes, bytesLength, false, pViewByte, sectionAddress, pSectionStart, stringAddresses);
                    }
                    else
                    {
                        //Try for a simple ASCII then
                        GetAnsiWorker(ref i, bytes, bytesLength, pViewByte, sectionAddress, pSectionStart, stringAddresses);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void GetAnsiWorker(
            ref int i,
            byte* bytes,
            int bytesLength,
            ViewByte* pViewByte,
            int sectionAddress,
            ViewByte* pSectionStart,
            List<int> stringAddresses)
        {
            var foundEnd = false;

            int j = i + 1;

            var arr = displayableAscii;

            for (; j < bytesLength; j++)
            {
                var b2 = bytes[j];

                if (b2 == 0)
                {
                    foundEnd = true;
                    break;
                }

                if (!arr[b2])
                {
                    //Not only was it not a valid string, but everything we read implicitly is also invalid
                    break;
                }
            }

            if (foundEnd)
            {
                //We think we've found an ANSI string. However, it's just as possible that in fact what we had was garbage, and the last character + null terminator we found was in fact
                //the first character of an _actual_ unicode string! To detect this, we'll try and read the next two characters. If they look like unicode characters, we're trampling over
                //another string; rewind and bail out

                if (j < bytesLength - 2)
                {
                    var u1 = bytes[j + 1];

                    //Don't consider a \0\0 here; not only might there just be padding after us, but also if it's \0\0, clearly we haven't run into another string!
                    if (arr[u1] && bytes[j + 2] == 0)
                    {
                        i = j - 2;
                        return;
                    }
                }

                var length = (j - i) + 1;

                if (length >= MinimumStringLength) //4 characters + \0
                {
                    var str = new AnsiString(bytes + i);

                    var pStart = pViewByte + i;

                    pStart->Kind = ViewByteKind.Data;
                    pStart->DataKind = ViewByteDataKind.String;

                    lock (stringAddresses)
                        stringAddresses.Add(sectionAddress + (int) (pStart - pSectionStart));

                    for (var k = pStart + 1; k < pStart + length; k++)
                        k->Kind = ViewByteKind.Body;
                }
            }

            i = j; //The caller will do i++ for us
        }

        //Don't pass NativeSpan around; it's too slow indexing into it
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void GetUnicodeWorker(
            ref int i,
            byte* bytes,
            int bytesLength,
            bool nullTerminated,
            ViewByte* pViewByte,
            int sectionAddress,
            ViewByte* pSectionStart,
            List<int> stringAddresses)
        {
            var foundEnd = false;

            int j = i + 2;

            for (; j < bytesLength - 1; j++)
            {
                var b2 = bytes[j];

                if (b2 == 0)
                {
                    if (bytes[j + 1] == 0)
                    {
                        //j += 2;
                        foundEnd = true;
                        break;
                    }
                    else
                    {
                        //Expected two 0 in a row. This is not a unicode string
                        break;
                    }
                }

                if (!displayableAscii[b2] || bytes[j + 1] != 0)
                {
                    if (!nullTerminated)
                    {
                        foundEnd = true;
                    }

                    //Either the current character is not ASCII, or it was not followed by a \0
                    break;
                }

                //Skip over the second byte
                j++;
            }

            if (foundEnd)
            {
                var length = (j - i);

                if (length >= MinimumStringLength * 2) //4 characters
                {
                    var pStart = pViewByte + i;

                    pStart->Kind = ViewByteKind.Data;
                    pStart->DataKind = ViewByteDataKind.String;
                    pStart->IsWide = true;

                    lock (stringAddresses)
                        stringAddresses.Add(sectionAddress + (int) (pStart - pSectionStart));

                    for (var k = pStart + 1; k < pStart + (length / 2); k++)
                        k->Kind = ViewByteKind.Body;
                }

                i = j + 1; //This does seem to be right; don't need +2. The caller will do i++ for us
            }
        }

        #endregion
    }
}
