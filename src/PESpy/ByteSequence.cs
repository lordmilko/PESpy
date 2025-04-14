using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PESpy
{
    //Based on DittedBitSequence.java, Pattern.java from Ghidra, licensed under the Apache License.
    //See ThirdPartyNotices.txt for full license notice.

    internal class ByteSequence
    {
        /// <summary>
        /// Gets the bytes that comprise this byte sequence.
        /// </summary>
        public byte[] Bytes { get; }

        /// <summary>
        /// Gets the masks that describe which bits in each byte in <see cref="Bytes"/> should be used
        /// in comparisons. e.g. if we want to match against the value 0x1. there will be a mask 0xf0 so
        /// that only the high byte is considered.
        /// </summary>
        public byte[] Masks { get; }

        /// <summary>
        /// Gets the index at which the "good" bytes of the pattern begin. Bytes prior to this index will be treated
        /// as junk bytes for the purposes of identifying the starting position of the bytes we want to consume
        /// from the resulting <see cref="ByteMatch"/>.
        /// </summary>
        public int Mark { get; }

        public int Length => Bytes.Length;

        private object[] items;
        private bool flirt;

        public ByteSequence(params byte[] bytes)
        {
            if (bytes.Length == 0)
                throw new ArgumentException("At least 1 byte must be specified", nameof(bytes));

            //Every bit in each byte is valid
            Bytes = bytes;

            Masks = new byte[bytes.Length];

            for (var i = 0; i < bytes.Length; i++)
                Masks[i] = 0xff; //Set a bitmask of all 1's for the 8 bits in this byte
        }

        public ByteSequence(params object[] items)
            : this(string.Join(" ", items))
        {
            this.items = items;
        }

        public ByteSequence(byte[] bytes, byte[] masks, int mark)
        {
            Bytes = bytes;
            Masks = masks;
            Mark = mark;
        }

        internal string debugStr; //temp

        public ByteSequence(string value, bool flirt = false)
        {
            value = Regex.Replace(value, "0x +", "0x");

            debugStr = value;
            this.flirt = flirt;

            var items = value.Split(new[]{' '}, StringSplitOptions.RemoveEmptyEntries);

            if (items.Length == 0)
                throw new ArgumentException("At least 1 byte pattern must be specified");

            var bytes = new List<byte>();
            var masks = new List<byte>();

            foreach (var item in items)
            {
                if (item.StartsWith("0x") || flirt)
                    ParseHexString(item, bytes, masks);
                else if (item == "*")
                    Mark = bytes.Count;
                else
                    ParseBinaryString(item, bytes, masks);
            }

            Bytes = bytes.ToArray();
            Masks = masks.ToArray();
        }

        private void ParseHexString(string item, List<byte> bytes, List<byte> masks)
        {
            //As a shorthand, hex numbers are represented as one big number. e.g. 0xff10
            //means we want bytes 0xff and 0x10

            if (item.Length % 2 != 0)
                throw new ArgumentException($"Cannot parse hex string '{item}': string length must have an even number of characters.");

            if (!flirt)
                item = item.Substring(2);

            for (var j = 0; j < item.Length; j += 2)
            {
                var s = new string(new[] { item[j], item[j + 1] });

                byte mask = 0xff;

                byte num = 0;

                if (s.Contains("."))
                {
                    if (s == "..")
                        mask = 0;
                    else
                    {
                        if (s[0] == '.')
                        {
                            //0x.1
                            //The lo byte is specified
                            num = byte.Parse(s[1].ToString(), NumberStyles.HexNumber);
                            mask = 0x0f;
                        }
                        else
                        {
                            //0x1.
                            //The hi byte is specified
                            num = byte.Parse(s[0].ToString(), NumberStyles.HexNumber);
                            num <<= 4;
                            mask = 0xf0;
                        }
                    }
                }
                else
                {
                    num = byte.Parse(s, NumberStyles.HexNumber);
                }

                bytes.Add(num);
                masks.Add(mask);
            }
        }

        private void ParseBinaryString(string item, List<byte> bytes, List<byte> masks)
        {
            //Should be a bit sequence
            if (item.Length % 8 != 0)
                throw new ArgumentException($"Cannot parse binary string '{item}': string length must be a multiple of 8. Actual length: {item.Length}.");

            for (var i = 0; i < item.Length;)
            {
                byte number = 0;
                byte mask = 0;

                for (var j = 0; j < 8; j++, i++)
                {
                    switch (item[i])
                    {
                        case '1':
                            number <<= 1; //Left shift it to make room for this bit
                            number |= 1; //Set a bit to one
                            mask <<= 1; //Left shift the mask to make room for this bit
                            mask |= 1; //Signal that this bit should be considered in the mask
                            break;

                        case '0':
                            //Bit is already 0 in number
                            number <<= 1; //Left shift it to make room for the next bit
                            mask <<= 1; //Left shift the mask to make room for this bit
                            mask |= 1; //Signal that this bit should be considered in the mask
                            break;

                        case '.':
                            //Nothing to set in the number or the mask
                            number <<= 1;
                            mask <<= 1;
                            break;

                        default:
                            throw new InvalidOperationException("Encountered invalid character '{}' in binary string '{}'. Binary string must only consist of '1', '0', and '.'");
                    }
                }

                bytes.Add(number);
                masks.Add(mask);
            }
        }

        /// <summary>
        /// Gets whether this <see cref="ByteSequence"/> matches against a specified
        /// <paramref name="value"/> at a specified index after applying that indexes
        /// relevant bitmask.
        /// </summary>
        /// <param name="index">The index of the byte to compare against.</param>
        /// <param name="value">The value to be compared against (after applying the relevant mask for the specified <paramref name="index"/>).</param>
        /// <returns>True if there is a match, otherwise false.</returns>
        public bool HasByte(int index, int value)
        {
            if (index >= Bytes.Length)
                return false;

            var theirValue = value & Masks[index];

            //There's no need to mask Bytes[index]. If we were told to add a byte 0x1. then we will have a byte 0x10
            //and a mask 0xf0. The mask is already applied

            var ourValue = Bytes[index];

            return theirValue == ourValue;
        }

        public bool IsMatch(byte[] bytes)
        {
            if (bytes.Length != Bytes.Length)
                return false;

            for (var i = 0; i < Bytes.Length; i++)
            {
                var mask = Masks[i];
                var ourByte = Bytes[i] & mask;
                var theirByte = bytes[i] & mask;

                if (ourByte != theirByte)
                    return false;
            }

            return true;
        }

        public override string ToString()
        {
            var components = new List<Tuple<string, bool>>();

            for (var i = 0; i < Masks.Length; i++)
            {
                bool shouldTreatDotDotAsHex()
                {
                    if (flirt)
                        return true;

                    if (i < Masks.Length - 1)
                    {
                        var m = Masks[i + 1];

                        return m is not 0xf0 or 0x0f or 0xff;
                    }

                    //Should we be returning true here?
                    //https://github.com/lordmilko/ChaosDbg/issues/18

                    throw new NotImplementedException($"Don't know whether to treat dot dot as hex when index is {i}/{Masks.Length}");
                }

                var mask = Masks[i];
                var val = Bytes[i];

                if (mask == 0xff)
                    components.Add(Tuple.Create(val.ToString("x2"), true));
                else if (mask == 0xf0)
                    components.Add(Tuple.Create(val.ToString("x").Substring(0, 1) + ".", true));
                else if (mask == 0x0f)
                    components.Add(Tuple.Create("." + val.ToString("x"), true));
                else if (mask == 0 && shouldTreatDotDotAsHex())
                    components.Add(Tuple.Create("..", true));
                else
                {
                    //Binary

                    var binaryMask = 1;
                    var binaryStr = new List<string>();

                    for (var j = 0; j < 8; j++)
                    {
                        var bitMask = mask & binaryMask;

                        if (bitMask == 0)
                            binaryStr.Add(".");
                        else
                        {
                            var bit = (val & bitMask) >> j;

                            binaryStr.Add(bit.ToString());
                        }

                        binaryMask <<= 1;
                    }

                    binaryStr.Reverse();

                    components.Add(Tuple.Create(string.Join(string.Empty, binaryStr), false));
                }
            }

            var builder = new StringBuilder();

            if (items != null)
            {
                builder.Append("[");

                for (var i = 0; i < items.Length; i++)
                {
                    var item = items[i];

                    if (item is string s)
                        builder.Append(s);
                    else
                        throw new NotImplementedException($"Don't know how to process a value of type '{item.GetType().Name}'");

                    if (i < items.Length - 1)
                    {
                        if (items[i].ToString() == "*" || items[i + 1].ToString() == "*")
                            builder.Append(" ");
                        else
                            builder.Append(" / ");
                    }
                }

                builder.Append("] ");

                builder.Clear();
            }

            for (var i = 0; i < components.Count; i++)
            {
                if (components[i].Item2) //IsHex
                {
                    builder.Append("0x");

                    var numAppended = 0;

                    for (var j = i; j < components.Count && components[j].Item2; j++)
                    {
                        //Maybe the next item is a binary string
                        if (numAppended > 0 && components[j].Item1.Contains("."))
                            break;

                        builder.Append(components[j].Item1);
                        numAppended++;

                        if (components[j].Item1.Contains(".") || j + 1 == Mark)
                            break;
                    }

                    if (numAppended > 1)
                        i += numAppended - 1;
                }
                else
                {
                    var numAppended = 0;

                    for (var j = i; j < components.Count && !components[j].Item2; j++)
                    {
                        builder.Append(components[j].Item1);
                        numAppended++;
                    }

                    if (numAppended > 1)
                        i += numAppended - 1;
                }

                if (i < components.Count - 1)
                    builder.Append(" ");

                if (Mark >= 0 && Mark == i + 1)
                    builder.Append("* ");
            }

            return builder.ToString();
        }
    }
}
