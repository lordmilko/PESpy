using System;
using System.Diagnostics;

namespace PESpy.View
{
    /// <summary>
    /// Describes a byte of data that has been found in a binary file.
    /// </summary>
    public struct ViewByte
    {
        /* We want to try and minimize memory usage as much as possible. As such,
         * we rely on reading bytes from the source (hopefully memory mapped) file, and
         * create a separate memory mapped region for storing all of our view bytes. a 1gb file
         * means 1gb of view bytes; for any information that is too complex to fit into a view byte,
         * we'll pack this data into a tertiary data structure that will be associated with the offset
         * of this byte */
        public const int Size = 1;

        //We have 8 bits to work with. Let's make them count!

        //Bottom 2 bits are reserved for storing the usage kind
        private const byte KindMask = 0b00000011; //0x3

        //Next two bits are reserved for storing various flags that are common to all usage kinds
        private const byte CommonMask = 0b00001100; //0xC

        //Code and data reserve 3 bytes each for storing additional information about themselves
        private const byte CodeMask = 0b01110000; //0x70
        private const byte DataKindMask = 0b01110000; //0x70

        //The top bit is used to store additional context, based on the data kind
        //Byte, Int16, Int32, Int64: 1 = Unsigned
        //String: 1 = Wide. Note that we don't need to track whether it's null terminated or not; we'll just keep reading as long as we have body bytes!
        private const byte DataMask = 0b10000000; //0x80

        private byte _value;

        public ViewByteKind Kind
        {
            get => (ViewByteKind) (_value & KindMask);
            set => _value = (byte) (((byte) (_value & ~KindMask)) | (byte) value);
        }

        #region Common Flags

        public ViewByteFlags Flags
        {
            get => (ViewByteFlags) (_value & CommonMask);
            set => _value = (byte) (((byte) (_value & ~CommonMask)) | (byte) value);
        }

        public bool HasName
        {
            get => (Flags & ViewByteFlags.HasName) != 0;
            set
            {
                if (value)
                    Flags |= ViewByteFlags.HasName;
                else
                    Flags &= ~ViewByteFlags.HasName;
            }
        }

        public bool HasXRefs
        {
            get => (Flags & ViewByteFlags.HasXRefs) != 0;
            set
            {
                if (value)
                    Flags |= ViewByteFlags.HasXRefs;
                else
                    Flags &= ~ViewByteFlags.HasXRefs;
            }
        }

        #endregion
        #region Code

        public ViewByteCodeFlags CodeFlags
        {
            get
            {
                var usage = Kind;

                if (usage == ViewByteKind.Data || usage == ViewByteKind.Body)
                    return 0;

                return (ViewByteCodeFlags) (_value & CodeMask);
            }
            set
            {
                var kind = Kind;

                //We allow setting a byte as a function before it's known to be code, so that our disassembler
                //does not think that the byte has already been disassembled and bail out

                Debug.Assert(kind != ViewByteKind.Body);

                if (kind == ViewByteKind.Data)
                {
                    //It's probably a public we previously failed to detect as being code, so we marked it as data.
                    //We now know that it's actually code. Clear out the usage so that we know to disassemble it
                    Kind = ViewByteKind.Unknown;
                }

                _value = (byte) (((byte) (_value & ~CodeMask)) | (byte) value);
            }
        }

        public bool IsFunction
        {
            get => (CodeFlags & ViewByteCodeFlags.Function) != 0;
            set
            {
                if (value)
                    CodeFlags |= ViewByteCodeFlags.Function;
                else
                    CodeFlags &= ~ViewByteCodeFlags.Function;
            }
        }

        public bool IsNoReturn
        {
            get => (CodeFlags & ViewByteCodeFlags.NoReturn) != 0;
            set
            {
                if (value)
                    CodeFlags |= ViewByteCodeFlags.NoReturn;
                else
                    CodeFlags |= ~ViewByteCodeFlags.NoReturn;
            }
        }

        public bool IsIL
        {
            get => (CodeFlags & ViewByteCodeFlags.IsIL) != 0;
            set
            {
                if (value)
                    CodeFlags |= ViewByteCodeFlags.IsIL;
                else
                    CodeFlags |= ~ViewByteCodeFlags.IsIL;
            }
        }

        #endregion
        #region Data

        public ViewByteDataKind DataKind
        {
            get
            {
                var kind = Kind;

                if (kind != ViewByteKind.Data)
                    return 0;

                return (ViewByteDataKind) (_value & DataKindMask);
            }
            set
            {
                var kind = Kind;

                if (kind != ViewByteKind.Data)
                    throw new NotImplementedException($"Don't know how to handle setting the data kind of a byte of type '{Kind}'"); //A public we thought was code but wasn't?

                _value = (byte) (((byte) (_value & ~DataKindMask)) | (byte) value);
            }
        }

        public bool IsUnsigned
        {
            get
            {
                switch (DataKind)
                {
                    case ViewByteDataKind.Byte:
                    case ViewByteDataKind.Int16:
                    case ViewByteDataKind.Int32:
                    case ViewByteDataKind.Int64:
                        return DataFlag;

                    default:
                        return false;
                }
            }
            set
            {
                switch (DataKind)
                {
                    case ViewByteDataKind.Byte:
                    case ViewByteDataKind.Int16:
                    case ViewByteDataKind.Int32:
                    case ViewByteDataKind.Int64:
                        DataFlag = value;
                        break;

                    default:
                        Debug.Assert(false);
                        break;
                }
            }
        }

        public bool IsWide
        {
            get
            {
                switch (DataKind)
                {
                    case ViewByteDataKind.String:
                        return DataFlag;

                    default:
                        return false;
                }
            }
            set
            {
                switch (DataKind)
                {
                    case ViewByteDataKind.String:
                        DataFlag = value;
                        break;

                    default:
                        Debug.Assert(false);
                        break;
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool DataFlag
        {
            get => (_value & DataMask) != 0;
            set => _value = (byte) (((byte) (_value & ~DataMask)) | (byte) (value ? 1 : 0));
        }

        #endregion

        public unsafe int GetLength(ViewByte* limit)
        {
            Debug.Assert(Kind != ViewByteKind.Body);

            fixed (ViewByte* me = &this)
            {
                var i = me + 1;

                while (i < limit && i->Kind == ViewByteKind.Body)
                    i++;

                return (int) (i - me);
            }
        }

        public unsafe int GetUnknownLength(ViewByte* limit)
        {
            Debug.Assert(limit->Kind == ViewByteKind.Unknown);

            fixed (ViewByte* me = &this)
            {
                var i = me + 1;

                while (i < limit && i->Kind == ViewByteKind.Unknown)
                    i++;

                return (int) (i - me);
            }
        }

        public override string ToString()
        {
            return Kind.ToString();
        }
    }
}
