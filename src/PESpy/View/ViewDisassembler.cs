using System;
using System.Collections.Generic;

#nullable disable

namespace PESpy.View
{
    public abstract class ViewDisassembler<T> : IViewDisassembler
    {
        protected AsmRange<T>[] ranges;
        protected byte bitness;

        public abstract void Initialize(PEFile peFile);

        public abstract bool TryParseDosStub(ref int offset, ref NativeSpan<byte> bytes, List<IView> results);

        public abstract T[] Disassemble(in AsmRange<T> range, int instructionCount);

        protected abstract int RecomputeRangeLength(int startOffset, int startRVA, int endOffset);

        public bool TryParseBytes(ref int offset, int rva, ref NativeSpan<byte> bytes, List<IView> results)
        {
            var end = offset + bytes.Length;

            var index = BinarySearch(offset, end);

            if (index == -1)
                return false;

            while (true)
            {
                //To some extent, the range indicated by index overlaps with the bytes specified in bytes
                ref var range = ref ranges[index];

                //There's three ways we can overlap with the bytes we've been provided. The bytes could be left shifted, right shifted
                //or centered with respect to us
                if (offset == range.StartOffset)
                {
                    if (range.EndOffset == end)
                    {
                        // | Bytes |
                        // |  Asm  |

                        //Perfect! Our range literally matches the bytes completely
                        results.Add(new AsmView<T>(offset, bitness, range));
                        var length = range.Length;
                        offset += length;
                        rva += length;
                        bytes = default;
                        break;
                    }
                    else if (end < range.EndOffset)
                    {
                        // | Bytes |
                        // |  Asm     |

                        ReadAndTruncateRightRange(ref offset, ref rva, ref range, out bytes, results);
                        break;
                    }
                    else //end > range.EndOffset
                    {
                        // | Bytes    |
                        // |  Asm  |

                        //We've been given more bytes than this range encompasses. Since our ranges merge sequential assembly regions,
                        //this means we need to create a gap between the current range we're processing and the next one, which should
                        //be filled with a byte blob
                        if (!ReadRangeAndRightBytes(ref offset, ref rva, ref range, ref bytes, ref index, results))
                            break;
                    }
                }
                else if (offset > range.StartOffset)
                {
                    //The bytes we've been given are either right shifted or centered with respect to our assembly range

                    if (range.EndOffset == end)
                    {
                        //    | Bytes |
                        // |   Asm    |

                        //The bytes we've been given are only right shifted on the left side
                        throw new NotImplementedException();
                    }
                    else if (end < range.EndOffset)
                    {
                        //    | Bytes |
                        // |     Asm     |

                        //The bytes we've been given are centered within our range
                        throw new NotImplementedException();
                    }
                    else //end > range.EndOffset
                    {
                        //    | Bytes |
                        // |  Asm  |

                        //The bytes we've been given are completely right shifted with respect to our range. This also means
                        //that there are additional bytes we need to convert into byte blobs and potentially also additional ranges
                        //that may follow after the end of this range
                        throw new NotImplementedException();
                    }
                }
                else //offset < range.StartOffset
                {
                    //The bytes we've been given are left shifted with respect to our assembly range

                    if (range.EndOffset == end)
                    {
                        // |   Bytes  |
                        //    |  Asm  |

                        //The bytes we've been given are only left shifted on the left side
                        ReadLeftBytes(ref offset, ref rva, ref range, ref bytes, results);

                        results.Add(new AsmView<T>(offset, bitness, range));
                        var length = range.Length;
                        offset += length;
                        rva += length;
                        bytes = default;
                        break;
                    }
                    else if (end < range.EndOffset)
                    {
                        // | Bytes |
                        //    |  Asm  |

                        //The bytes we've been given are completely left shifted with respect to our range
                        ReadLeftBytes(ref offset, ref rva, ref range, ref bytes, results);

                        ReadAndTruncateRightRange(ref offset, ref rva, ref range, out bytes, results);
                        break;
                    }
                    else //end > range.EndOffset
                    {
                        // |    Bytes    |
                        //    |  Asm  |

                        //The bytes we've been given completely envelop our range. This also means
                        //that there are additional bytes we need to convert into byte blobs and potentially also additional ranges
                        //that may follow after the end of this range

                        ReadLeftBytes(ref offset, ref rva, ref range, ref bytes, results);

                        if (!ReadRangeAndRightBytes(ref offset, ref rva, ref range, ref bytes, ref index, results))
                            break;
                    }
                }
            }

            return results.Count > 0;
        }

        //Read bytes hanging off to the left
        private void ReadLeftBytes(ref int offset, ref int rva, ref AsmRange<T> range, ref NativeSpan<byte> bytes, List<IView> results)
        {
            //Read the bytes up to the start of our range. These are a byte blob
            var numBytes = range.StartOffset - offset;
            var startBytes = bytes.Slice(0, numBytes);
            results.Add(new ByteBlobView(offset, startBytes, null));
            offset += numBytes;
            rva += numBytes;
            bytes = bytes.Slice(numBytes);
        }

        private void ReadAndTruncateRightRange(ref int offset, ref int rva, ref AsmRange<T> range, out NativeSpan<byte> bytes, List<IView> results)
        {
            //Our range is bigger than the number of bytes we've been provided. This indicates we may have made a mistake
            //in our disassembly, treating something as disasm that isn't really

            //Re-disassemble the range until we hit the known end position

            var newRange = new AsmRange<T>(range.StartOffset, range.StartRVA, range.FunctionRVA, this, range.Name);

            //Note that we don't actually update the AsmRange in our ranges array. If another caller comes along and wants another chunk of it,
            //they can resize it themselves to suit their needs. Unrelated fun fact: if you do "range = newRange", the assignment travels through
            //ref and updates the value in the array!

            results.Add(new AsmView<T>(offset, bitness, newRange));
            var length = newRange.Length;
            offset += length;
            rva += length;
            bytes = default;
        }

        //Read the asm and then bytes hanging off to the right
        //Returns false if we should end, true if we should continue
        private bool ReadRangeAndRightBytes(ref int offset, ref int rva, ref AsmRange<T> range, ref NativeSpan<byte> bytes, ref int index, List<IView> results)
        {
            //Create a view around the entire assembly range
            results.Add(new AsmView<T>(offset, bitness, range));

            //Skip over both the bytes prior to this range and the bytes within the range itself
            var read = range.Length;
            offset += read;
            rva += read;
            bytes = bytes.Slice(read);

            if (index < ranges.Length - 1)
            {
                ref var nextRange = ref ranges[index + 1];

                //Read all bytes up to the next range. Because we split every time there's a new symbol name, we could potentially have a range adjacent
                //to the current one

                var numGapBytes = nextRange.StartOffset - offset;

                if (numGapBytes > bytes.Length)
                {
                    //The remaining bytes won't take us to the next range. Just read all remaining bytes
                    results.Add(new ByteBlobView(offset, bytes, default));

                    offset += bytes.Length;
                    bytes = default;
                    return false;
                }
                else if (numGapBytes == 0)
                {
                    //There's a range directly after us (which means there was a named symbol there)
                    index++;
                    return true;
                }
                else //numGapBytes < bytes.Length
                {
                    //There's a gap, and after it we have the beginning of the next range

                    var gapBytes = bytes.Slice(0, numGapBytes);
                    results.Add(new ByteBlobView(offset, gapBytes, null));

                    offset += numGapBytes;
                    rva += numGapBytes;

                    bytes = bytes.Slice(numGapBytes);

                    index++;
                    return true;
                }
            }
            else
            {
                //No more ranges. All remaining bytes are part of a byte blob
                results.Add(new ByteBlobView(offset, bytes, default));

                offset += bytes.Length;
                bytes = default;
                return false;
            }
        }

        private int BinarySearch(int offset, int end)
        {
            //Binary search asm ranges to find the first item that overlaps with the requested byte range

            var low = 0;
            var high = ranges.Length - 1;

            int bestMid = -1;

            while (low <= high)
            {
                var mid = low + (high - low) / 2;
                ref var item = ref ranges[mid];

                /* Suppose we have a range 1008-1020
                 *
                 * There's several ways we could overlap with it
                 *
                 * Left Shifted: If we've given offset 1004 which spans to 1015, this gives us
                 *
                 *     1004 < 1020 (true) && 1008 < 1015 (true)
                 *
                 * Right Shifted: If we're given offset 1010 which spans to 1025, this gives us
                 *
                 *     1010 < 1020 (true) && 1008 < 1025 (true)
                 *
                 * Centered: if we're given offset 1012 which spans to 1015, this gives us
                 *
                 *     1012 < 1020 (true) && 1008 < 1015 (true)
                 */

                if (item.StartOffset < offset)
                {
                    //We've gone too far to the left
                    low = mid + 1;
                }
                else if (end >= item.EndOffset)
                {
                    if (offset <= item.EndOffset && item.StartOffset <= end) //Overlaps
                    {
                        //This might give us a value that overlaps, but we specifically want the _minimum_ value that overlaps.
                        bestMid = mid;
                        high = mid - 1; //Move further left
                    }
                    else
                    {
                        //We've gone too far to the right
                        high = mid - 1;
                    }
                }
                else
                {
                    if (offset <= item.EndOffset && item.StartOffset <= end) //Overlaps
                    {
                        //This might give us a value that overlaps, but we specifically want the _minimum_ value that overlaps.
                        bestMid = mid;
                        high = mid - 1; //Move further left
                    }
                    else
                    {
                        high = mid - 1; //Move to the left
                    }
                }
            }

            return bestMid;
        }
    }
}
