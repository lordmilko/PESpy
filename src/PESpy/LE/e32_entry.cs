using System;
using System.Diagnostics;
using System.Text;

namespace PESpy.LE
{
    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public readonly struct e32_entry
    {
        private string DebuggerDisplay()
        {
            var builder = new StringBuilder();

            builder.Append($"e32_flags = {e32_flags.ToString().Replace(", ", " | ")}, ");

            switch (_bundleType)
            {
                case E32BundleType.ENTRY16:
                    builder.Append($"offset = 0x{e32_variant.offset.offset16:X}");
                    break;

                case E32BundleType.GATE16:
                    builder.Append($"offset = 0x{e32_variant.e32_callgate.offset:X}, callgate = {e32_variant.e32_callgate.callgate}");
                    break;

                case E32BundleType.ENTRY32:
                    builder.Append($"offset = 0x{e32_variant.offset.offset32:X}");
                    break;

                case E32BundleType.ENTRYFWD:
                    builder.Append($"modord = {e32_variant.e32_fwd.modord}, value = {e32_variant.e32_fwd.value}");
                    break;

                default:
                    throw new NotImplementedException();
            }

            return builder.ToString();
        }

        #region Static

        internal static e32_entry Entry16(in MemoryChunk chunk)
        {
            var flags = (E32BundleEntryFlags) chunk.PeekByte(0);

            var offset = chunk.PeekInt16(1);

            return new e32_entry(
                chunk.AbsoluteOffset,
                flags,
                new VariantUnion(
                    new offset(offset)
                ),
                E32BundleType.ENTRY16
            );
        }

        internal static e32_entry Gate16(in MemoryChunk chunk)
        {
            var flags = (E32BundleEntryFlags) chunk.PeekByte(0);

            var offset = chunk.PeekInt16(1);
            var callgate = chunk.PeekInt16(3);

            return new e32_entry(
                chunk.AbsoluteOffset,
                flags,
                new VariantUnion(
                    new CallGate(offset, callgate)
                ),
                E32BundleType.GATE16
            );
        }

        internal static e32_entry Entry32(in MemoryChunk chunk)
        {
            var flags = (E32BundleEntryFlags) chunk.PeekByte(0);

            var offset = chunk.PeekInt32(1);

            return new e32_entry(
                chunk.AbsoluteOffset,
                flags,
                new VariantUnion(
                    new offset(offset)
                ),
                E32BundleType.ENTRY32
            );
        }

        internal static e32_entry EntryFwd(in MemoryChunk chunk)
        {
            var flags = (E32BundleEntryFlags) chunk.PeekByte(0);

            var modord = chunk.PeekInt16(1);
            var value = chunk.PeekInt32(3);

            return new e32_entry(
                chunk.AbsoluteOffset,
                flags,
                new VariantUnion(
                    new Forwarder(modord, value)
                ),
                E32BundleType.ENTRYFWD
            );
        }

        #endregion
        #region Constant

        internal const int Entry16Size =
            sizeof(byte) + //e32_flags
            sizeof(short); //e32_offset (16-bit)

        internal const int Gate16Size =
            sizeof(byte) + //e32_flags
            sizeof(short) + //e32_callgate.offset
            sizeof(short); //e32_callgate.callgate

        internal const int Entry32Size =
            sizeof(byte) + //e32_flags
            sizeof(int); //e32_offset (32-bit)

        internal const int EntryFwdSize =
            sizeof(byte) + //e32_flags
            sizeof(short) + //e32_fwd.modord
            sizeof(int); //e32_fwd.value

        #endregion

        /// <summary>
        /// Entry point flags
        /// </summary>
        public E32BundleEntryFlags e32_flags { get; }

        public VariantUnion e32_variant { get; }

        public int Offset { get; }

        private readonly E32BundleType _bundleType;

        private e32_entry(
            int offset,
            E32BundleEntryFlags flags,
            VariantUnion variant,
            E32BundleType bundleType)
        {
            Offset = offset;
            e32_flags = flags;
            e32_variant = variant;
            _bundleType = bundleType;
        }

        public readonly struct VariantUnion
        {
            /// <summary>
            /// 16-bit/32-bit offset entry
            /// </summary>
            public offset offset { get; }

            /// <summary>
            /// 286 (16-bit) call gate
            /// </summary>
            public CallGate e32_callgate { get; }

            /// <summary>
            /// Forwarder
            /// </summary>
            public Forwarder e32_fwd { get; }

            internal VariantUnion(offset offset)
            {
                this.offset = offset;
            }

            internal VariantUnion(CallGate callGate)
            {
                this.e32_callgate = callGate;
            }

            internal VariantUnion(Forwarder forwarder)
            {
                this.e32_fwd = forwarder;
            }
        }

        public readonly struct CallGate
        {
            /// <summary>
            /// Offset in segment
            /// </summary>
            public short offset { get; }

            /// <summary>
            /// Callgate selector
            /// </summary>
            public short callgate { get; }

            internal CallGate(short offset, short callgate)
            {
                this.offset = offset;
                this.callgate = callgate;
            }
        }

        public readonly struct Forwarder
        {
            /// <summary>
            /// Module ordinal number
            /// </summary>
            public short modord { get; }

            /// <summary>
            /// Proc name offset or ordinal
            /// </summary>
            public int value { get; }

            internal Forwarder(short modord, int value)
            {
                this.modord = modord;
                this.value = value;
            }
        }
    }
}
