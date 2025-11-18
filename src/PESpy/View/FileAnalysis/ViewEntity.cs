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
        public int SectionAccessorIndex;
        public int TargetAddress;
        public FixedUtf8String Name;
        public FixedUtf16String NameWide;
        public int Length;
        public ViewKind Kind;
        internal SpanAllocatorHandle XRefs;
        public bool HasChildren;
        public long Displacement;

        public NativeSpan<byte> Bytes => new NativeSpan<byte>(_pData, Length);

        private byte* _pData;

        internal ViewEntity(
            ISymbolAccessor symbolAccessor,
            int sectionAccessorIndex,
            in SectionAccessor sectionAccessor,
            int sectionAccessorOffset,
            int sectionAccessorLength,
            IntPtr pBytes,
            Dictionary<int, ViewInfo> infoMap,
            FixedUtf8String[] names)
            : this(
                  symbolAccessor: symbolAccessor,
                  sectionAccessorIndex: sectionAccessorIndex,
                  targetAddress: sectionAccessor.StartAddress + sectionAccessorOffset,
                  pViewByte: sectionAccessor.pViewBytes + sectionAccessorOffset,
                  pStart: sectionAccessor.pViewBytes,
                  pEnd: sectionAccessor.pViewBytes + sectionAccessorLength,
                  pBytes,
                  infoMap,
                  names)
        {
        }

        internal ViewEntity(
            ISymbolAccessor symbolAccessor,
            int sectionAccessorIndex,
            int targetAddress,
            ViewByte* pViewByte,
            ViewByte* pStart,
            ViewByte* pEnd,
            IntPtr pBytes,
            Dictionary<int, ViewInfo> infoMap,
            FixedUtf8String[] names)
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
                    {
                        if (body->BodyKind == ViewByteBodyKind.SplitTail)
                            throw new NotImplementedException();

                        body++;
                    }
                    else
                        break;
                }

                HasChildren = true;
            }
            else if (ViewByte->Kind == ViewByteKind.Code)
            {
                //If this is the first instruction of a code chunk, roll all the code up into one chunk

                if (symbolAccessor.TryGetNameFromAddress(targetAddress, out var symName, out var disp))
                {
                    Name = symName;
                    Displacement = disp;
                }

                body = pViewByte - 1;

                var isFirstCode = true;

                while (body > pStart)
                {
                    if (body->Kind == ViewByteKind.Body)
                    {
                        if (body->BodyKind == ViewByteBodyKind.SplitHead)
                            throw new NotImplementedException();

                        body--;
                        continue;
                    }
                    
                    if (body->Kind == ViewByteKind.Code)
                    {
                        //There was more code before us, so we're not the first code
                        isFirstCode = false;
                    }

                    break;
                }

                if (isFirstCode)
                {
                    //Roll up all code after us into us

                    body = pViewByte + 1;

                    while (body < pEnd)
                    {
                        if (body->Kind == ViewByteKind.Body || body->Kind == ViewByteKind.Code)
                        {
                            if (body->BodyKind == ViewByteBodyKind.SplitTail)
                                throw new NotImplementedException();

                            body++;
                        }
                        else
                            break;
                    }

                    HasChildren = true;
                }
                else
                {
                    //Calculate length normally; this will give us the length of a single instruction

                    body = pViewByte + 1;

                    while (body < pEnd)
                    {
                        if (body->Kind == ViewByteKind.Body)
                        {
                            if (body->BodyKind == ViewByteBodyKind.SplitTail)
                                throw new NotImplementedException();

                            body++;
                        }
                        else
                            break;
                    }

                    HasChildren = false;
                }
            }
            else if (ViewByte->Kind == ViewByteKind.Unknown)
            {
                while (body < pEnd)
                {
                    if (body->Kind == ViewByteKind.Unknown)
                        body++;
                    else
                        break;
                }

                HasChildren = false;
            }
            else
            {
                while (body < pEnd)
                {
                    if (body->Kind == ViewByteKind.Body)
                    {
                        if (body->BodyKind == ViewByteBodyKind.SplitTail)
                            throw new NotImplementedException();

                        body++;
                    }
                    else
                        break;
                }

                HasChildren = false;
            }

            infoMap.TryGetValue(targetAddress, out var viewInfo);

            Length = (int) (body - pViewByte);

            if (pViewByte->Kind == ViewByteKind.Data && pViewByte->DataKind == ViewByteDataKind.String)
            {
                if (pViewByte->IsWide)
                    NameWide = new FixedUtf16String((char*) pData, Length / 2);
                else
                    Name = new FixedUtf8String((byte*) pData, Length);
            }
            else
            {
                if (viewInfo.NameIndex != 0)
                    Name = names[viewInfo.NameIndex - 1];
            }

            Kind = viewInfo.ViewKind;
            XRefs = viewInfo.XRefs;
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

                //It's confusing in the navigation when we say Padding (0) when other times we say Unknown (3) and 3 is a count not a value
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

                if (Displacement != 0)
                {
                    builder.Append(Displacement < 0 ? "-0x" : "+0x");
                    builder.AppendHex((ulong) Math.Abs(Displacement));
                }
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
