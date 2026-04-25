using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    [Source(SourceKind.ehdata4_export_h)]
    public unsafe readonly struct HandlerType4 : IViewableValue
    {
        private const int MAX_CONT_ADDRESSES = 2;

        public HandlerTypeHeader header { get; }

        /// <summary>
        /// Handler Type adjectives (bitfield)
        /// </summary>
        public HT adjectives { get; }

        /// <summary>
        /// Image relative offset of the corresponding type descriptor
        /// </summary>
        public RVA<TypeDescriptor> dispType { get; }

        /// <summary>
        /// Displacement of catch object from base
        /// </summary>
        public int dispCatchObj { get; }

        /// <summary>
        /// Image relative offset of 'catch' code
        /// </summary>
        public int dispOfHandler { get; }

        /// <summary>
        /// Continuation address(es) of catch funclet
        /// </summary>
        public Span<int> continuationAddresses
        {
            get
            {
                fixed (HandlerType4* me = &this)
                    return new Span<int>(&me->_continuationAddress1, _numContinuationAddresses);
            }
        }

        public int Offset => header.Offset;

        internal int StructSize
        {
            get
            {
                var size = HandlerTypeHeader.StructSize;

                if (header.adjectives)
                    size += FuncInfo4.GetLength((uint) adjectives);

                if (header.dispType)
                    size += sizeof(int); //dispType

                if (header.dispCatchObj)
                    size += FuncInfo4.GetLength((uint) dispCatchObj);

                size += sizeof(int); //dispOfHandler

                if (header.contIsRVA)
                {
                    switch (header.contAddr)
                    {
                        case HandlerTypeHeader.contType.ONE:
                            size += sizeof(int);
                            break;

                        case HandlerTypeHeader.contType.TWO:
                            size += 2 * sizeof(int);
                            break;
                    }
                }
                else
                {
                    //We read unsigned here rather than int. You're supposed to add the RuntimeFunction.BeginAddress to this

                    switch (header.contAddr)
                    {
                        case HandlerTypeHeader.contType.ONE:
                            size += FuncInfo4.GetLength((uint) (_continuationAddress1 - functionAddress));
                            break;

                        case HandlerTypeHeader.contType.TWO:
                            size += FuncInfo4.GetLength((uint) (_continuationAddress1 - functionAddress));
                            size += FuncInfo4.GetLength((uint) (_continuationAddress2 - functionAddress));
                            break;
                    }
                }

                return size;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly int _continuationAddress1;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly int _continuationAddress2;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly int _numContinuationAddresses;

        //Need this for when we write
        private readonly int functionAddress;

        internal unsafe HandlerType4(int offset, PEFile peFile, ref byte* pData, int functionAddress, bool measureOnly)
        {
            header = new HandlerTypeHeader(offset, *pData);
            pData++;
            this.functionAddress = functionAddress;

            if (header.adjectives)
                adjectives = (HT) FuncInfo4.ReadUnsigned(ref pData);

            if (header.dispType)
            {
                var dispType = (int) FuncInfo4.ReadInt(ref pData);

                if (!measureOnly && peFile.TryGetValueChunkFromSection(dispType, out var valueChunk))
                {
                    var typeDescriptor = new TypeDescriptor(valueChunk);

                    this.dispType = new RVA<TypeDescriptor>(dispType, valueChunk.AbsoluteOffset, typeDescriptor);
                }
                else
                    this.dispType = new RVA<TypeDescriptor>(dispType);
            }

            if (header.dispCatchObj)
                dispCatchObj = (int) FuncInfo4.ReadUnsigned(ref pData);

            dispOfHandler = FuncInfo4.ReadInt(ref pData);

            if (header.contIsRVA)
            {
                switch (header.contAddr)
                {
                    case HandlerTypeHeader.contType.ONE:
                        _continuationAddress1 = FuncInfo4.ReadInt(ref pData);
                        _numContinuationAddresses = 1;
                        break;

                    case HandlerTypeHeader.contType.TWO:
                        _continuationAddress1 = FuncInfo4.ReadInt(ref pData);
                        _continuationAddress2 = FuncInfo4.ReadInt(ref pData);
                        _numContinuationAddresses = 2;
                        break;
                }
            }
            else
            {
                //We read unsigned here rather than int. You're supposed to add the RuntimeFunction.BeginAddress to this

                switch (header.contAddr)
                {
                    case HandlerTypeHeader.contType.ONE:
                        _continuationAddress1 = functionAddress + (int) FuncInfo4.ReadUnsigned(ref pData);
                        _numContinuationAddresses = 1;
                        break;

                    case HandlerTypeHeader.contType.TWO:
                        _continuationAddress1 = functionAddress + (int) FuncInfo4.ReadUnsigned(ref pData);
                        _continuationAddress2 = functionAddress + (int) FuncInfo4.ReadUnsigned(ref pData);
                        _numContinuationAddresses = 2;
                        break;
                }
            }
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            var read = sizeof(byte);

            if (header.adjectives)
                read += FuncInfo4.GetLength((uint) adjectives);

            if (header.dispType)
            {
                writer.WriteUniqueRVAField(dispType, Offset, read);
                read += sizeof(int);
            }

            if (header.dispCatchObj)
                read += FuncInfo4.GetLength((uint) dispCatchObj);

            writer.WriteUniqueRVAXRef(Offset, read, dispOfHandler);
            read += sizeof(int);

            if (header.contIsRVA)
            {
                switch (header.contAddr)
                {
                    case HandlerTypeHeader.contType.ONE:
                        writer.WriteUniqueRVAXRef(Offset, read, _continuationAddress1);
                        break;

                    case HandlerTypeHeader.contType.TWO:
                        writer.WriteUniqueRVAXRef(Offset, read, _continuationAddress1);
                        read += sizeof(int);
                        writer.WriteUniqueRVAXRef(Offset, read, _continuationAddress2);
                        break;
                }
            }
            else
            {
                //We read unsigned here rather than int. You're supposed to add the RuntimeFunction.BeginAddress to this

                switch (header.contAddr)
                {
                    case HandlerTypeHeader.contType.ONE:
                        writer.WriteUniqueRVAXRef(Offset, read, _continuationAddress1);
                        break;

                    case HandlerTypeHeader.contType.TWO:
                        writer.WriteUniqueRVAXRef(Offset, read, _continuationAddress1);
                        read += FuncInfo4.GetLength((uint) (_continuationAddress1 - functionAddress));
                        writer.WriteUniqueRVAXRef(Offset, read, _continuationAddress2);
                        break;
                }
            }
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.HandlerType4, this, ViewKind.HandlerType4, StructSize);

        int IViewable.NumChildren() =>
            throw StructWriter.GetEagerLoadOnlyException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            if (index != -1)
                throw StructWriter.GetEagerLoadOnlyException();

            using var s = structWriter.CreateEagerWriter();

            s.WriteStructField(nameof(header), header, sizeof(byte));

            if (header.adjectives)
                s.WriteField(nameof(adjectives), adjectives, FuncInfo4.GetLength((uint) adjectives));

            if (header.dispType)
                s.WriteRVAField(nameof(dispType), dispType);

            if (header.dispCatchObj)
                s.WriteField(nameof(dispCatchObj), dispCatchObj, FuncInfo4.GetLength((uint) dispCatchObj));

            s.WriteField(nameof(dispOfHandler), dispOfHandler);

            if (header.contIsRVA)
            {
                switch (header.contAddr)
                {
                    //This is a bit unfortunate, but I don't know how to handle this better
                    case HandlerTypeHeader.contType.ONE:
                        s.WriteField("continuationAddresses", new[] { _continuationAddress1 }, sizeof(int));
                        break;

                    case HandlerTypeHeader.contType.TWO:
                        s.WriteField("continuationAddresses", new[] { _continuationAddress1, _continuationAddress2 }, 2 * sizeof(int));
                        break;
                }
            }
            else
            {
                //We read unsigned here rather than int. You're supposed to add the RuntimeFunction.BeginAddress to this

                int continuationAddress1;

                switch (header.contAddr)
                {
                    case HandlerTypeHeader.contType.ONE:
                        continuationAddress1 = _continuationAddress1 - functionAddress;
                        s.WriteField("continuationAddresses", new[] { continuationAddress1 }, FuncInfo4.GetLength((uint) continuationAddress1));
                        break;

                    case HandlerTypeHeader.contType.TWO:
                        continuationAddress1 = _continuationAddress1 - functionAddress;
                        var continuationAddress2 = _continuationAddress2 - functionAddress;
                        s.WriteField(
                            "continuationAddresses",
                            new[] { continuationAddress1, continuationAddress2 },
                            FuncInfo4.GetLength((uint) continuationAddress1) + FuncInfo4.GetLength((uint) continuationAddress2)
                        );
                        break;
                }
            }

            structWriter.EagerFields = s.ToArray();
        }
    }
}
