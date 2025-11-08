using System;
using System.Collections.Generic;
using System.Diagnostics;
using static PESpy.View.FileAccessor;

namespace PESpy.View
{
    /// <summary>
    /// Represents a lightweight descriptor over a <see cref="ViewByte"/> that also provides access to the information contained in its <see cref="ViewInfo"/>
    /// </summary>
    [DebuggerDisplay("[0x{TargetAddress.ToString(\"X\"),nq}-0x{(TargetAddress + Length).ToString(\"X\"),nq}] {ToString(),nq}")]
    public unsafe struct ViewEntity
    {
        public ViewByte* ViewByte;
        public int TargetAddress;
        public FixedUtf8String Name;
        public int Length;
        public ViewKind Kind;
        List<XRef> XRefs;
        public bool HasChildren;

        public NativeSpan<byte> Bytes => new NativeSpan<byte>(_pData, Length);

        private byte* _pData;

        internal ViewEntity(in SectionAccessor sectionAccessor, int sectionAccessorOffset, int sectionAccessorLength, IntPtr pBytes, Dictionary<int, ViewInfo> infoMap)
            : this(
                  targetAddress: sectionAccessor.StartAddress + sectionAccessorOffset,
                  pViewByte: sectionAccessor.pViewBytes + sectionAccessorOffset,
                  pStart: sectionAccessor.pViewBytes,
                  pEnd: sectionAccessor.pViewBytes + sectionAccessorLength,
                  pBytes,
                  infoMap)
        {
        }

        internal ViewEntity(
            int targetAddress,
            ViewByte* pViewByte,
            ViewByte* pStart,
            ViewByte* pEnd,
            IntPtr pBytes,
            Dictionary<int, ViewInfo> infoMap)
        {
            TargetAddress = targetAddress;
            ViewByte = pViewByte;

            var relativeOffset = (int) (pViewByte - pStart);
            var pData = pBytes + relativeOffset;

            var body = pViewByte + 1;

            if (ViewByte->IsFunction)
            {
                //This is the start of a contiguous code block; keep reading bytes until we hit something that is not code or body
                while (body < pEnd)
                {
                    if (body->Kind == ViewByteKind.Body || body->Kind == ViewByteKind.Code)
                        body++;
                    else
                        break;
                }

                HasChildren = true;
            }
            else if (ViewByte->Kind == ViewByteKind.Code)
            {
                //If this is the first instruction of a code chunk, roll all the code up into one chunk

                body = pViewByte - 1;

                var isFirstCode = true;

                while (body > pStart)
                {
                    switch (body->Kind)
                    {
                        case ViewByteKind.Body:
                            body--;
                            continue;

                        case ViewByteKind.Code:
                            isFirstCode = false;
                            break;

                    while (body < pEnd)
                    {
                        if (body->Kind == ViewByteKind.Body || body->Kind == ViewByteKind.Code)
                            body++;
                        else
                            break;
                    }
                }
            }
            else
            {
                while (body < pEnd)
                {
                    if (body->Kind == ViewByteKind.Body)
                        body++;
                    else
                        break;
                }

                HasChildren = false;
            }

            infoMap.TryGetValue(targetAddress, out var byteData);

            Name = byteData.Name;
            Kind = byteData.ViewKind;
            XRefs = byteData.XRefs;
            _pData = (byte*) pData;
        }

        public bool Contains(int targetAddress) => targetAddress >= TargetAddress && targetAddress < (TargetAddress + Length);

        internal void ToString(ref ValueStringBuilder.NonRef builder)
        {
            if (ViewByte == default)
                return;

            if (ViewByte->Kind == ViewByteKind.Data && ViewByte->DataKind == ViewByteDataKind.String)
            {
                if (ViewByte->IsWide)
                {
                    builder.Append("L\"");

                    var name = new FixedUtf16String((char*) (byte*) Bytes, Length / 2);
                    builder.AppendEscaped(name);
                }
                else
                {
                    builder.Append('\"');
                    builder.AppendEscaped(Name);
                }

                builder.Append('\"');
            }
            else if (ViewByte->Kind == ViewByteKind.Data && ViewByte->DataKind == ViewByteDataKind.Padding)
            {
                builder.Append("Padding (");

                var @byte = *(byte*) Bytes;

                if (@byte != 0)
                    builder.Append("0x");

                builder.AppendHex(@byte);
                builder.Append(')');
            }
            else if (ViewByte->Kind == ViewByteKind.Unknown)
            {
                builder.Append("Unknown (");
                builder.Append(Length);
                builder.Append(')');
            }
            else if (Name.Length > 0)
            {
                builder.Append(Name);
            }
            else if (Kind != 0)
                builder.Append(Kind.ToString());
            else if (ViewByte->Kind == ViewByteKind.Data)
                builder.Append(ViewByte->DataKind.ToString());
            else if (ViewByte->Kind == ViewByteKind.Code && ViewByte->IsFunction)
                builder.Append("Function");
            else
                builder.Append(ViewByte->Kind.ToString());
        }

        public override string ToString()
        {
            var builder = new ValueStringBuilder.NonRef(100);

            try
            {
                ToString(ref builder);

                return builder.ToString();
            }
            finally
            {
                builder.Dispose();
            }
        }
    }
}
