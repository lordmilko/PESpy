using System;
using System.Collections.Generic;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
using RVA = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_THUNK_DATA32"/> / <see cref="IMAGE_THUNK_DATA64"/> structure.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public readonly struct ImageThunkData : IValue, IViewable
    {
        private const uint IMAGE_ORDINAL_FLAG32 = 0x80000000;
        private const ulong IMAGE_ORDINAL_FLAG64 = 0x8000000000000000;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay
        {
            get
            {
                if (Value == 0)
                    return "<Null>";

                return $"[{Kind}] {ToString()}";
            }
        }

        public DataKind Kind { get; }
        public ulong Value { get; }

        public ulong Function { get; } //The full address of a function contained in the assembly (use in delay loaded imports)
        public short Ordinal { get; }
        public RVA<ImageImportByName> Name { get; } //AddressOfData

        public RawOffset Offset { get; }

        internal ImageThunkData(IFileReader reader, PEFile peFile, bool is32Bit, bool isIAT)
        {
            Offset = (RawOffset) reader.Position;

            Kind = default;
            Function = default;
            Value = default;
            Ordinal = default;
            Name = default;

            /* The value stored in IMAGE_THUNK_DATA can have four possible meanings
             *
             *     ForwarderString: haven't figured that out yet
             *
             *     Function: the IMAGE_THUNK_DATA describes an entry in the IAT. The value is the address the PE thinks the real function definition will be found at.
             *               Sometimes this value is larger than ImageBase, but not always. So in the case of an IAT, we should just interpret the value as being a function address,
             *               whatever it is. In the case of delay loaded imports, if you subtract ImageBase from the function address, you may get a little stub used by
             *               the delay load imports process
             *
             *     Ordinal:  in an ILT entry, the high bit of the value is set, indicating its an import by ordinal
             *
             *     AddressOfData: in an ILT entry, the high bit of the value is not set, indicating its an IMAGE_IMPORT_BY_NAME */

            if (isIAT)
            {
                Kind = DataKind.Function;
                Value = is32Bit ? reader.ReadUInt32() : reader.ReadUInt64();
                Function = Value;

                return;
            }

            //ILT: Ordinal or IMAGE_IMPORT_BY_NAME

            bool isOrdinal = false;
            int ordinal = 0;

            if (is32Bit)
            {
                var value32 = reader.ReadUInt32();

                if (value32 == 0)
                    return;

                if ((value32 & IMAGE_ORDINAL_FLAG32) != 0)
                {
                    isOrdinal = true;
                    ordinal = IMAGE_ORDINAL32(value32);
                }

                Value = value32;
            }
            else
            {
                Value = reader.ReadUInt64();

                if (Value == 0)
                    return;

                if ((Value & IMAGE_ORDINAL_FLAG64) != 0)
                {
                    isOrdinal = true;
                    ordinal = IMAGE_ORDINAL64(Value);
                }
            }

            if (isOrdinal)
            {
                //Bits 0-15 are an ordinal
                Kind = DataKind.Ordinal;
                Ordinal = (short) ordinal;
            }
            else
            {
                RVA<ImageImportByName> name;

                if (!peFile.TryGetOffset((RVA)(int)Value, out var offset))
                    name = new RVA<ImageImportByName>((RVA)(int)Value);
                else
                {
                    reader.Seek(offset);

                    var importByName = new ImageImportByName(reader);

                    name = new RVA<ImageImportByName>((RVA) (int) Value, offset, importByName);
                }

                Kind = DataKind.Name;
                Name = name;
            }
        }

        private static int IMAGE_ORDINAL32(uint value) => (int)(value & 0xffff);

        private static int IMAGE_ORDINAL64(ulong value) => (int)(value & 0xffff);

        public enum DataKind
        {
            Forwarder = 1,
            Function,
            Ordinal,
            Name
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("IMAGE_THUNK_DATA", this, ViewKind.ImageThunkData);

            switch (Kind)
            {
                case DataKind.Name:
                    s.WritePointerField("AddressOfData", Value);

                    if (Name.IsValid)
                        writer.WriteTaggedGlobal(Name.Value);
                    break;

                case DataKind.Forwarder:
                    throw new NotImplementedException();

                case DataKind.Ordinal:
                    s.WritePointerField("Ordinal", Ordinal);
                    break;

                case DataKind.Function:
                    s.WritePointerField("Function", Value);
                    break;

                case 0: //It's the null record
                    s.WritePointerField("AddressOfData", Value);
                    break;

                default:
                    throw new NotImplementedException($"Don't know how to handle {nameof(DataKind)} '{Kind}'");
            }
        }

        public override string ToString()
        {
            if (Value == 0)
                return "<null>";

            return Kind switch
            {
                DataKind.Forwarder => throw new NotImplementedException($"Don't know how to handle {nameof(DataKind)} '{Kind}'"),
                DataKind.Function => "0x" + Function.ToString("X"),
                DataKind.Ordinal => Ordinal.ToString(),
                DataKind.Name => Name.ToString()
            };
        }
    }
}
