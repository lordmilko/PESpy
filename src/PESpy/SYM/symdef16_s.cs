using System;
using PESpy.View;

namespace PESpy.SYM
{
    //symdef16_s
    //SYM is too obscure, so I think it's better to use the real name
    [Source(SourceKind.mapsym_h)]
    public readonly struct symdef16_s : IViewableValue
    {
        private const int sd16_valOffset = 0;
        private const int sd16_cbnameOffset = 2;
        private const int sd16_achnameOffset = 3;

        /// <summary>
        /// 16 bit symbol addr or const
        /// </summary>
        public ushort sd16_val => chunk.PeekUInt16(sd16_valOffset);

        /// <summary>
        /// 8 bit symbol name length
        /// </summary>
        public byte sd16_cbname => chunk.PeekByte(sd16_cbnameOffset);

        /// <summary>
        /// &lt;n&gt; symbol name
        /// </summary>
        public FixedAnsiString sd16_achname => chunk.PeekAnsiFixedLength(sd16_achnameOffset, sd16_cbname);

        public long Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            sizeof(short) + //sd16_val
            sizeof(byte); //sd16_cbname

        private readonly MemoryChunk chunk;

        internal symdef16_s(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.symdef16_s, FixedStructSize + sd16_cbname);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(sd16_val), sd16_valOffset, sd16_val);
                    break;

                case 1:
                    structWriter.WriteField(nameof(sd16_cbname), sd16_cbnameOffset, sd16_cbname);
                    break;

                case 2:
                    structWriter.WriteAnsiFixedLengthField(nameof(sd16_achname), sd16_achnameOffset, sd16_achname);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return sd16_achname.ToString();
        }
    }
}
