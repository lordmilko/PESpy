using System;
using System.Diagnostics;
using System.Text;
using PESpy.View;

namespace PESpy
{
    //Typically OMF* structs are only used for older CodeView formats, however OMFSegMap is definitely what is used
    //by DBI. The data is deserialized from the PDB into bufSecMap which is regularly casted to OMFSegMap*
    [Source(SourceKind.cvexefmt_h)]
    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public readonly struct OMFSegMapDesc : IValue, IViewable
    {
        private string DebuggerDisplay()
        {
            var builder = new StringBuilder();

            if (TryGetSegName(iSegName, out var segName))
                builder.Append($"SegName = {segName}");
            else
                builder.Append($"iSegName = {iSegName}");

            builder.Append(", ");

            if (TryGetSegName(iClassName, out var className))
                builder.Append($"ClassName = {className}");
            else
                builder.Append($"iClassName = {iClassName}");

            return builder.ToString();
        }

        private const int flagsOffset = 0;
        private const int ovlOffset = 2;
        private const int groupOffset = 4;
        private const int frameOffset = 6;
        private const int iSegNameOffset = 8;
        private const int iClassNameOffset = 10;
        private const int offsetOffset = 12;
        private const int cbSegOffset = 16;

        /// <summary>
        /// descriptor flags bit field.
        /// </summary>
        public OMFSegMapFlags flags => chunk.PeekUInt16(flagsOffset);

        /// <summary>
        /// the logical overlay number
        /// </summary>
        public short ovl => chunk.PeekInt16(ovlOffset);

        /// <summary>
        /// group index into the descriptor array
        /// </summary>
        public short group => chunk.PeekInt16(groupOffset);

        /// <summary>
        /// logical segment index - interpreted via flags
        /// </summary>
        public short frame => chunk.PeekInt16(frameOffset);

        /// <summary>
        /// segment or group name - index into sstSegName
        /// </summary>
        public short iSegName => chunk.PeekInt16(iSegNameOffset);

        /// <summary>
        /// class name - index into sstSegName
        /// </summary>
        public short iClassName => chunk.PeekInt16(iClassNameOffset);

        /// <summary>
        /// byte offset of the logical within the physical segment
        /// </summary>
        public int offset => chunk.PeekInt16(offsetOffset);

        /// <summary>
        /// byte count of the logical segment or group
        /// </summary>
        public int cbSeg => chunk.PeekInt16(cbSegOffset);

        public AnsiString SegName
        {
            get
            {
                var index = iSegName;

                if (TryGetSegName(index, out var str))
                    return str;

                return default;
            }
        }

        public AnsiString ClassName
        {
            get
            {
                var index = iClassName;

                if (TryGetSegName(index, out var str))
                    return str;

                return default;
            }
        }

        private bool TryGetSegName(int offset, out AnsiString str)
        {
            if (offset == -1)
            {
                str = default;
                return false;
            }

            //PDBs have an OMFSegMap, but no sstSegName
            var file = chunk.File() as IFileWithCodeViewData;

            if (file == null)
            {
                str = default;
                return false;
            }

            var codeViewData = file.CodeViewData;
            Debug.Assert(codeViewData != null); //We're in an OMFSegMapDesc, so implicitly there should be data!

            //Not sure if DNRB or NB02 could have an OMFSegMap
            var nb05 = (NB05Data) codeViewData!;
            return nb05.TryGetSegName(offset, out str);
        }

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(short) + //flags
            sizeof(short) + //ovl
            sizeof(short) + //group
            sizeof(short) + //frame
            sizeof(short) + //iSegName
            sizeof(short) + //iClassName
            sizeof(int) + //offset
            sizeof(int);

        private readonly MemoryChunk chunk;

        internal OMFSegMapDesc(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.OMFSegMapDesc, this, ViewKind.OMFSegMapDesc, StructSize);

        int IViewable.NumChildren() => 17;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                #region BitField

                case 0:
                    structWriter.WriteBitField("fRead", flagsOffset, flags.fRead, sizeof(ushort), 1);
                    break;

                case 1:
                    structWriter.WriteBitField("fWrite", flagsOffset, flags.fWrite, sizeof(ushort), 1);
                    break;

                case 2:
                    structWriter.WriteBitField("fExecute", flagsOffset, flags.fExecute, sizeof(ushort), 1);
                    break;

                case 3:
                    structWriter.WriteBitField("f32Bit", flagsOffset, flags.f32Bit, sizeof(ushort), 1);
                    break;

                case 4:
                    structWriter.WriteBitField("res1", flagsOffset, flags.res1, sizeof(ushort), 4);
                    break;

                case 5:
                    structWriter.WriteBitField("fSel", flagsOffset, flags.fSel, sizeof(ushort), 1);
                    break;

                case 6:
                    structWriter.WriteBitField("fAbs", flagsOffset, flags.fAbs, sizeof(ushort), 1);
                    break;

                case 7:
                    structWriter.WriteBitField("res2", flagsOffset, flags.res2, sizeof(ushort), 2);
                    break;

                case 8:
                    structWriter.WriteBitField("fGroup", flagsOffset, flags.fRead, sizeof(ushort), 1);
                    break;

                case 9:
                    structWriter.WriteBitField("res3", flagsOffset, flags.res3, sizeof(ushort), 3);
                    break;

                #endregion

                case 10:
                    structWriter.WriteField(nameof(ovl), ovlOffset, ovl);
                    break;

                case 11:
                    structWriter.WriteField(nameof(group), groupOffset, group);
                    break;

                case 12:
                    structWriter.WriteField(nameof(frame), frameOffset, frame);
                    break;

                case 13:
                    structWriter.WriteField(nameof(iSegName), iSegNameOffset, iSegName);
                    break;

                case 14:
                    structWriter.WriteField(nameof(iClassName), iClassNameOffset, iClassName);
                    break;

                case 15:
                    structWriter.WriteField(nameof(offset), offsetOffset, offset);
                    break;

                case 16:
                    structWriter.WriteField(nameof(cbSeg), cbSegOffset, cbSeg);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
