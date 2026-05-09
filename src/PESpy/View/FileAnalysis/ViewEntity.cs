using System;
using System.Collections.Generic;
using System.Diagnostics;
using static PESpy.View.FileAccessor;

namespace PESpy.View
{
    /// <summary>
    /// Represents a lightweight descriptor over a <see cref="ViewByte"/> that also provides access to the information contained in its <see cref="ViewInfo"/>
    /// </summary>
    [DebuggerDisplay("[0x{TargetAddress.ToString(\"X\"),nq}-0x{(TargetAddress + Length - 1).ToString(\"X\"),nq}] {ToString(),nq}")]
    public unsafe struct ViewEntity
    {
        public ViewByte* ViewByte;
        public int SectionAccessorIndex;
        public long TargetAddress;
        public FixedUtf8String Name
        {
            get
            {
                if (_name.Length == 0 && ViewByte->HasName)
                    _name = _fileAccessor.GetNameFromViewByte(TargetAddress, SectionAccessorIndex, ViewByte);

                return _name;
            }
        }

        private FixedUtf8String _name;

        public FixedUtf16String NameWide;
        public int Length;
        public ViewKind Kind;
        internal SpanAllocatorHandle XRefs;
        public bool HasChildren;
        public long Displacement;

        public bool IsSplit;

        public NativeSpan<byte> Bytes => new NativeSpan<byte>(_pData, Length);

        private readonly FileAccessor _fileAccessor;
        private byte* _pData;

        internal ViewEntity(
            FileAccessor fileAccessor,
            int sectionAccessorIndex,
            ISymbolAccessor symbolAccessor,
            in SectionAccessor sectionAccessor,
            int sectionAccessorOffset,
            int sectionAccessorLength,
            IntPtr pBytes,
            Dictionary<long, ViewInfo> infoMap,
            Dictionary<long, int> largeAddresses,
            bool measureOnly = false)
            : this(
                  fileAccessor,
                  sectionAccessorIndex,
                  symbolAccessor: symbolAccessor,
                  sectionAccessor: sectionAccessor,
                  targetAddress: sectionAccessor.StartAddress + sectionAccessorOffset,
                  pViewByte: sectionAccessor.pViewBytes + sectionAccessorOffset,
                  pStart: sectionAccessor.pViewBytes,
                  pEnd: sectionAccessor.pViewBytesEnd,
                  pBytes,
                  infoMap,
                  largeAddresses,
                  measureOnly)
        {
        }

        internal ViewEntity(
            FileAccessor fileAccessor,
            int sectionAccessorIndex,
            ISymbolAccessor symbolAccessor,
            in SectionAccessor sectionAccessor,
            long targetAddress,
            ViewByte* pViewByte,
            ViewByte* pStart,
            ViewByte* pEnd,
            IntPtr pBytes,
            Dictionary<long, ViewInfo> infoMap,
            Dictionary<long, int> largeAddresses,
            bool measureOnly = false)
        {
            _fileAccessor = fileAccessor;
            TargetAddress = targetAddress;
            ViewByte = pViewByte;
            SectionAccessorIndex = sectionAccessorIndex;

            var relativeOffset = (int) (pViewByte - pStart);
            var pData = pBytes + relativeOffset;

            var body = pViewByte + 1;

            if (ViewByte->IsFunction)
            {
                //This is the start of a contiguous code block; keep reading bytes until we hit something that is not code or body
                while (body < pEnd)
                {
                    if (body->Kind == ViewByteKind.Code)
                    {
                        if (body->IsFunction)
                            break;
                    }

                    if (body->Kind != ViewByteKind.Body)
                        break;

                    if (body->BodyKind == ViewByteBodyKind.SplitTail)
                        throw new NotImplementedException();

                    body++;
                }

                HasChildren = true;
            }
            else if (ViewByte->Kind == ViewByteKind.Code)
            {
                //If this is the first instruction of a code chunk, roll all the code up into one chunk

                if (!measureOnly &&
                    _fileAccessor.TryGetVirtualAddress(sectionAccessor, targetAddress, out var rva) &&
                    symbolAccessor.TryGetNameFromAddress(rva, out var symName, out var disp))
                {
                    _name = symName;
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
#if DEBUG
                //We shouldn't be interacting with entities until we've completed our analysis; however in debug,
                //we may want to inspect how our construction is going, so we need to be able to handle having
                //and not having unknown bodies set

                if (body < pEnd)
                {
                    var successorKind = body->Kind;
                    
                    if (successorKind == ViewByteKind.Unknown || successorKind == ViewByteKind.Body)
                    {
                        while (body < pEnd)
                        {
                            var kind = body->Kind;

                            if (kind == successorKind)
                                body++;
                            else
                                break;
                        }
                    }
                }
#else
                while (body < pEnd)
                {
                    var kind = body->Kind;

                    if (kind == ViewByteKind.Body) //In Release, we should only be accessing entities after we've set unknown bodies
                        body++;
                    else
                        break;
                }
#endif

                HasChildren = false;
            }
            else
            {
                if (largeAddresses != null && largeAddresses.TryGetValue(TargetAddress, out var length))
                {
                    //This double minus seems wrong but it's not
                    body += length - 1;

                    var last = body - 1;

                    if (last->BodyKind == ViewByteBodyKind.SplitTail)
                        IsSplit = true;
                }
                else
                {
                    while (body < pEnd)
                    {
                        if (body->Kind == ViewByteKind.Body)
                        {
                            if (body->BodyKind == ViewByteBodyKind.SplitTail)
                            {
                                //There's more data in the next page
                                body++; //We own this byte
                                IsSplit = true;
                                break;
                            }

                            body++;
                        }
                        else
                            break;
                    }
                }

                HasChildren = false;
            }

            infoMap.TryGetValue(targetAddress, out var viewInfo);

            Length = (int) (body - pViewByte);

            if (!measureOnly)
            {
                if (pViewByte->Kind == ViewByteKind.Data && pViewByte->DataKind == ViewByteDataKind.String && !pViewByte->HasName)
                {
                    if (pViewByte->IsWide)
                        NameWide = new FixedUtf16String((char*) pData, Length / 2);
                    else
                        _name = new FixedUtf8String((byte*) pData, Length);
                }
                else
                {
                    //Lazily request name when Name property is accessed
                }

                if (ViewByte->Kind == ViewByteKind.Body && ViewByte->BodyKind == ViewByteBodyKind.SplitHead)
                    IsSplit = true;
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

            if (ViewByte->Kind == ViewByteKind.Data && ViewByte->DataKind == ViewByteDataKind.String && !ViewByte->HasName)
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
            else if (ViewByte->Kind == ViewByteKind.Unknown || (ViewByte->Kind == ViewByteKind.Data && ViewByte->DataKind == ViewByteDataKind.Unknown))
            {
                if (Name.Length > 0)
                {
                    builder.Append(Name);
                    builder.Append(" -> ");
                }

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

        internal ViewEntity GetSplitHeadOrigin(FileAccessor fileAccessor, out int bytesRewound)
        {
            Debug.Assert(ViewByte->Kind == ViewByteKind.Body && ViewByte->BodyKind == ViewByteBodyKind.SplitHead);

            ViewByte* pViewByte = ViewByte;
            var offset = TargetAddress;

            ((PDBFileAccessor) fileAccessor).GetSplitHeadOrigin(ref pViewByte, ref offset, out var sectionIndex, out bytesRewound);

            return fileAccessor.GetEntity(offset, pViewByte, sectionIndex);
        }

        internal int GetFullLength(FileAccessor fileAccessor)
        {
            Debug.Assert(IsSplit);
            Debug.Assert(ViewByte->Kind != ViewByteKind.Body);

            return ((PDBFileAccessor) fileAccessor).GetFullLength(ViewByte, TargetAddress);
        }

        internal ViewEntity GetHead(FileAccessor fileAccessor, out int bytesRewound)
        {
            var pViewByte = ViewByte;

            while (pViewByte->Kind == ViewByteKind.Body && pViewByte->BodyKind != ViewByteBodyKind.SplitHead)
            {
                pViewByte--;
            }

            bytesRewound = (int) (ViewByte - pViewByte);

            if (pViewByte->Kind == ViewByteKind.Body)
            {
                Debug.Assert(pViewByte->BodyKind == ViewByteBodyKind.SplitHead);

                //Need to get the split head origin to continue
                var offset = TargetAddress - bytesRewound;

                var originalBytesRewound = bytesRewound;

                ((PDBFileAccessor) fileAccessor).GetSplitHeadOrigin(ref pViewByte, ref offset, out var sectionIndex, out bytesRewound);

                bytesRewound += originalBytesRewound;

                return fileAccessor.GetEntity(offset, pViewByte, sectionIndex);
            }

            return fileAccessor.GetEntity(TargetAddress - bytesRewound, pViewByte, SectionAccessorIndex);
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
