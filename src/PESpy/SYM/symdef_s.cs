using System;
using PESpy.View;

namespace PESpy.SYM
{
    //symdef_s
    //SYM is too obscure, so I think it's better to use the real name
    [Source(SourceKind.mapsym_h)]
    public readonly struct symdef_s : IViewableValue
    {
        private const int sd_lvalOffset = 0;
        private const int sd_cbnameOffset = 4;
        private const int sd_achnameOffset = 5;

        /// <summary>
        /// 32 bit symbol addr or const
        /// </summary>
        public int sd_lval => chunk.PeekInt32(sd_lvalOffset);

        /// <summary>
        /// 8 bit symbol name length
        /// </summary>
        public byte sd_cbname => chunk.PeekByte(sd_cbnameOffset);

        /// <summary>
        /// &lt;n&gt; symbol name
        /// </summary>
        public FixedAnsiString sd_achname => chunk.PeekAnsiFixedLength(sd_achnameOffset, sd_cbname);

        public long Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            sizeof(int) + //sd_lval
            sizeof(byte); //sd_cbname

        private readonly MemoryChunk chunk;

        internal symdef_s(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.symdef_s, FixedStructSize + sd_cbname);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(sd_lval), sd_lvalOffset, sd_lval);
                    break;

                case 1:
                    structWriter.WriteField(nameof(sd_cbname), sd_cbnameOffset, sd_cbname); ;
                    break;

                case 2:
                    structWriter.WriteAnsiFixedLengthField(nameof(sd_achname), sd_achnameOffset, sd_achname);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return sd_achname.ToString();
        }
    };
}
