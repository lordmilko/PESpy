using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ClrDebug.PDB;
using PESpy.View;
using static ClrDebug.PDB.LEAF_ENUM_e;

namespace PESpy.PDB
{
    //Type is made up

    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public readonly struct NumericData : IViewable
    {
        private string DebuggerDisplay()
        {
            if (Kind == null)
                return _value.ToString();

            return $"[{Kind}] {ToString()}";
        }

        public readonly LEAF_ENUM_e? Kind { get; }

        public int Length { get; }

        public byte Byte => (byte) _value;
        public sbyte SByte => (sbyte) _value;

        public char Char => (char) _value;

        public short Int16 => unchecked((short) _value);
        public ushort UInt16 => (ushort) _value;

        public int Int32 => unchecked((int) _value);
        public uint UInt32 => (uint) _value;

        public long Int64 => unchecked((long) _value);
        public ulong UInt64 => _value;

        public unsafe float Float
        {
            get
            {
                var value = _value;
                return Unsafe.As<ulong, float>(ref value);
            }
        }

        public unsafe double Double
        {
            get
            {
                var value = _value;
                return Unsafe.As<ulong, double>(ref value);
            }
        }

        public unsafe FixedUtf8String String
        {
            get
            {
                if (Kind != LF_VARSTRING)
                    return default;

                return new FixedUtf8String((byte*) _value, Length - 4);
            }
        }

        private readonly ulong _value;

        public NumericData(ushort value)
        {
            Kind = null;
            _value = value;
            Length = sizeof(short);
        }

        public NumericData(LEAF_ENUM_e kind, ulong value, int length)
        {
            Kind = kind;
            Length = length;
            _value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.NumericData, Length);

        int IViewable.NumChildren()
        {
            if (Kind == null)
                return 1;

            if (Kind == LF_VARSTRING)
                return 3;

            return 2;
        }

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            if (Kind == null)
            {
                if (index != 0)
                    throw new IndexOutOfRangeException();

                //Write the singular value inline
                structWriter.WriteValue(0, UInt16, sizeof(short), ViewKind.NumericValue);
                return;
            }

            if (Kind == LF_VARSTRING)
            {
                //Write the leaf, the string length and the string
                switch (index)
                {
                    case 0:
                        structWriter.WriteValue(0, Kind.Value, sizeof(short));
                        break;

                    case 1:
                        structWriter.WriteValue(2, Length - 4, sizeof(short), ViewKind.NumericStringLength);
                        break;

                    case 2:
                        structWriter.WriteInlineFixedUtf8String(structWriter.ParentOffset + 4, String);
                        break;

                    default:
                        throw new IndexOutOfRangeException();
                }
                
                return;
            }

            //Write the leaf and the value

            switch (index)
            {
                case 0:
                    structWriter.WriteValue(0, Kind.Value, sizeof(short));
                    break;

                case 1:
                    switch (Kind) //LF_NUMERIC and LF_CHAR are both defined as 0x8000, but LF_NUMERIC is the semantic item that indicates "this is the beginning of the special kind range"
                    {
                        case LF_CHAR:
                            structWriter.WriteValue(2, Char, sizeof(byte), ViewKind.NumericValue);
                            break;

                        case LF_SHORT:
                            structWriter.WriteValue(2, Int16, sizeof(short), ViewKind.NumericValue);
                            break;

                        case LF_USHORT:
                            structWriter.WriteValue(2, UInt16, sizeof(ushort), ViewKind.NumericValue);
                            break;

                        case LF_LONG:
                            structWriter.WriteValue(2, Int32, sizeof(int), ViewKind.NumericValue);
                            break;

                        case LF_ULONG:
                            structWriter.WriteValue(2, UInt32, sizeof(int), ViewKind.NumericValue);
                            break;

                        case LF_REAL16:
                            throw new NotImplementedException();

                        case LF_REAL32:
                            structWriter.WriteValue(2, Float, sizeof(float), ViewKind.NumericValue);
                            break;

                        case LF_REAL48:
                            throw new NotImplementedException();

                        case LF_REAL64:
                            structWriter.WriteValue(2, Double, sizeof(double), ViewKind.NumericValue);
                            break;

                        case LF_REAL80:
                        case LF_REAL128:
                            throw new NotImplementedException();

                        case LF_QUADWORD:
                            structWriter.WriteValue(2, Int64, sizeof(long), ViewKind.NumericValue);
                            break;

                        case LF_UQUADWORD:
                            structWriter.WriteValue(2, UInt64, sizeof(long), ViewKind.NumericValue);
                            break;

                        case LF_COMPLEX32:
                        case LF_COMPLEX64:
                        case LF_COMPLEX80:
                        case LF_COMPLEX128:
                        case LF_OCTWORD:
                        case LF_UOCTWORD:
                        case LF_DECIMAL:
                        case LF_DATE:
                        case LF_UTF8STRING:
                            throw new NotImplementedException();

                        default:
                            throw new NotImplementedException();
                    }
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return Kind switch
            {
                LF_CHAR => Char.ToString(),
                LF_SHORT => Int16.ToString(),
                LF_USHORT => UInt16.ToString(),
                LF_LONG => Int32.ToString(),
                LF_ULONG => UInt32.ToString(),
                LF_REAL32 => Float.ToString(),
                LF_REAL64 => Double.ToString(),
                LF_QUADWORD => Int64.ToString(),
                LF_UQUADWORD => UInt64.ToString(),
                _ => _value.ToString()
            };
        }
    }
}
