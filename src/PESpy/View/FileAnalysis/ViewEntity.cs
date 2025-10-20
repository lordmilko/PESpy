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

        internal ViewEntity(in SectionAccessor sectionAccessor, int sectionAccessorOffset, int sectionAccessorLength, Dictionary<int, ViewInfo> infoMap)
            : this(
                  targetAddress: sectionAccessor.StartAddress + sectionAccessorOffset,
                  pViewByte: sectionAccessor.pViewBytes + sectionAccessorOffset,
                  pStart: sectionAccessor.pViewBytes,
                  pEnd: sectionAccessor.pViewBytes + sectionAccessorLength,
                  infoMap)
        {
        }

        internal ViewEntity(int targetAddress, ViewByte* pViewByte, ViewByte* pStart, ViewByte* pEnd, Dictionary<int, ViewInfo> infoMap)
        {
            TargetAddress = targetAddress;
            ViewByte = pViewByte;

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
            else if (ViewByte->Kind == ViewByteKind.Code && false)
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

                        default:
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

            Length = (int) (body - pViewByte);
        }

        public override string ToString()
        {
            if (ViewByte == default)
                return base.ToString();

            if (Name.Length > 0)
                return Name.ToString();

            if (Kind != 0)
                return Kind.ToString();

            if (ViewByte->Kind == ViewByteKind.Data)
                return ViewByte->DataKind.ToString();

            if (ViewByte->Kind == ViewByteKind.Code && ViewByte->IsFunction)
                return "Function";

            return ViewByte->Kind.ToString();
        }
    }
}
