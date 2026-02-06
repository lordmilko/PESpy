using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfEnumerate"/> structure.
    /// </summary>
    public readonly unsafe struct LfEnumerate : IViewable
    {
        private const int leafOffset = 0;
        private const int attrOffset = 2;
        private const int valueOffset = 4;
        private int nameOffset
        {
            get
            {
                TypType.ExtractNumericData(raw->value, out _, out var bytesRead);

                return valueOffset + bytesRead;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfEnumerate* raw;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => raw->leaf;

        public CV_fldattr_t attr => raw->attr;

        public ulong value
        {
            get
            {
                TypType.ExtractNumericData(raw->value, out var value, out _);

                return value;
            }
        }

        public SymString name => GetName(null);

        #region PESpy

        public SymString GetName(ICodeViewAccessor? codeViewAccessor)
        {
            TypType.ExtractNumericData(raw->value, out _, out var bytesRead);

            //I am assuming I need to use normal ST/UTF parsing logic
            return TypType.ReadString(raw->value + bytesRead, codeViewAccessor);
        }

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            2;               //attr

        internal int StructSize => GetStructSize(null);

        internal int GetStructSize(ICodeViewAccessor? codeViewAccessor)
        {
            TypType.ExtractNumericData(raw->value, out _, out var bytesRead);

            var str = TypType.ReadString(raw->value + bytesRead, codeViewAccessor);

            return FixedStructSize + bytesRead + str.Length + 1;
        }

        internal LfEnumerate(lfEnumerate* value)
        {
            this.raw = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfEnumerate, this, ViewKind.LfEnumerate, GetStructSize(writer.GetSymbolAccessor())); //Non-primary, should not have a TYPTYPE.len

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(leaf), leafOffset, leaf, sizeof(ushort));
                    break;

                case 1:
                    structWriter.WriteField(nameof(attr), attrOffset, attr);
                    break;

                case 2:
                    structWriter.WriteNumericData(nameof(value), valueOffset, raw->value);
                    break;

                case 3:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, GetName(structWriter.GetSymbolAccessor()));
                    break;

                //Do not align; the parent will apply padding

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
