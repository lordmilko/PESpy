using System;
using System.Diagnostics;
using ClrDebug.DIA;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="CFLAGSYM"/> structure.
    /// </summary>
    public readonly unsafe struct CFlagSym : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int machineOffset = 4;
        private const int languageOffset = 5;
        private const int flags1Offset = 6;
        private const int flags2Offset = 7;
        private const int verOffset = 8;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly CFLAGSYM* value;

        /// <inheritdoc cref="CFLAGSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="CFLAGSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="CFLAGSYM.machine"/>
        public CV_CPU_TYPE_e machine => (CV_CPU_TYPE_e) value->machine;

        /// <inheritdoc cref="CFLAGSYM.language"/>
        public CV_CFL_LANG language => (CV_CFL_LANG) value->language;

        /// <inheritdoc cref="CFLAGSYM.pcode"/>
        public bool pcode => value->pcode;

        /// <inheritdoc cref="CFLAGSYM.floatprec"/>
        public byte floatprec => value->floatprec;

        /// <inheritdoc cref="CFLAGSYM.floatpkg"/>
        public CV_CFL_FPKG_e floatpkg => (CV_CFL_FPKG_e) value->floatpkg;

        /// <inheritdoc cref="CFLAGSYM.ambdata"/>
        public CV_CFL_DATA ambdata => (CV_CFL_DATA) value->ambdata;

        /// <inheritdoc cref="CFLAGSYM.ambcode"/>
        public CV_CFL_CODE_e ambcode => (CV_CFL_CODE_e) value->ambcode;

        /// <inheritdoc cref="CFLAGSYM.mode32"/>
        public bool mode32 => value->mode32;

        /// <inheritdoc cref="CFLAGSYM.pad"/>
        public byte pad => value->pad;

        /// <inheritdoc cref="CFLAGSYM.ver"/>
        public SymString ver => SymType.ReadString(value, value->ver);

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(byte)   + //machine
            sizeof(byte)   + //language
            sizeof(byte)   + //flags1
            sizeof(byte);    //flags2

        private int BytesUsed() => FixedStructSize + ver.Length + 1;

        internal CFlagSym(CFLAGSYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.CFLAGSYM, this, ViewKind.CFlagSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(12, BytesUsed());

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(reclen), reclenOffset, reclen);
                    break;

                case 1:
                    structWriter.WriteField(nameof(rectyp), rectypOffset, rectyp, sizeof(ushort));
                    break;

                case 2:
                    structWriter.WriteField(nameof(machine), machineOffset, machine, sizeof(byte));
                    break;

                case 3:
                    structWriter.WriteField(nameof(language), languageOffset, language, sizeof(byte)); //It's listed as a bitfield but it takes up all 8 bits!
                    break;

                #region BitField (x2)

                case 4:
                    structWriter.WriteBitField(nameof(pcode), flags1Offset, pcode, sizeof(byte), 1);
                    break;

                case 5:
                    structWriter.WriteBitField(nameof(floatprec), flags1Offset, floatprec, sizeof(byte), 2);
                    break;

                case 6:
                    structWriter.WriteBitField(nameof(floatpkg), flags1Offset, floatpkg, sizeof(byte), 2);
                    break;

                case 7:
                    structWriter.WriteBitField(nameof(ambdata), flags1Offset, ambdata, sizeof(byte), 3);
                    break;

                case 8:
                    structWriter.WriteBitField(nameof(ambcode), flags2Offset, ambcode, sizeof(byte), 3);
                    break;

                case 9:
                    structWriter.WriteBitField(nameof(mode32), flags2Offset, mode32, sizeof(byte), 1);
                    break;

                case 10:
                    structWriter.WriteBitField(nameof(pad), flags2Offset, pad, sizeof(byte), 4);
                    break;

                #endregion

                case 11:
                    structWriter.WriteSymStringField(nameof(ver), verOffset, SymType.ReadString(value, value->ver, structWriter.GetSymbolAccessor()));
                    break;

                case 12:
                    //Possible alignment
                    structWriter.AlignOrThrow(BytesUsed());
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return ver.ToString();
        }
    }
}
