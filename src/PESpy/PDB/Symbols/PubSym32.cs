using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="PUBSYM32"/> structure.
    /// </summary>
    [DebuggerDisplay("{SymTypeProxy.DebuggerDisplay(this),nq}")] //For S_PUB32_ST / S_PUB32
    public readonly unsafe struct PubSym32 : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int pubsymflagsOffset = 4;
        private const int offOffset = 4;
        private const int segOffset = 8;
        private const int nameOffset = 10;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly PUBSYM32* value;

        /// <inheritdoc cref="PUBSYM32.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="PUBSYM32.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="PUBSYM32.pubsymflags"/>
        public CV_PUBSYMFLAGS pubsymflags => value->pubsymflags;

        /// <inheritdoc cref="PUBSYM32.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="PUBSYM32.seg"/>
        public ushort seg => value->seg;

        /// <inheritdoc cref="PUBSYM32.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        internal SymString GetName(ICodeViewAccessor? codeViewAccessor) => SymType.ReadString(value, value->name, codeViewAccessor);

        /* There is no way to get the "underlying" symbol of a PubSym32. The public symbol specifies a section
         * and offset, which can be used to calculate its RVA. It does _not_ behave similarly to a RefSym.
         * You cannot use the section or offset to lookup the "underlying" symbol from a module. You _can_
         * get the module that is associated with a given section and offset (based on the section contribs),
         * but that's as far as you can get */

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            4              + //pubsymflags
            sizeof(uint)   + //off
            sizeof(short);   //seg

        private int BytesUsed() => FixedStructSize + name.Length + 1;

        internal PubSym32(PUBSYM32* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.PUBSYM32, this, ViewKind.PubSym32, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(6, BytesUsed());

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
                    structWriter.WriteField(nameof(pubsymflags), pubsymflagsOffset, pubsymflags);
                    break;

                case 3:
                    structWriter.WriteField(nameof(off), offOffset, off);
                    break;

                case 4:
                    structWriter.WriteField(nameof(seg), segOffset, seg);
                    break;

                case 5:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, SymType.ReadString(value, value->name, structWriter.GetSymbolAccessor()));
                    break;

                case 6:
                    //Possible alignment
                    structWriter.AlignOrThrow(BytesUsed());
                    break;

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
