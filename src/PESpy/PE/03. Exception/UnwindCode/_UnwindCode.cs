using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    internal class UnwindCodeDebugView
    {
        private UnwindCode unwindCode;

        internal UnwindCodeDebugView(UnwindCode unwindCode)
        {
            this.unwindCode = unwindCode;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public object Value
        {
            get
            {
                if (unwindCode.IsNull)
                    return (UnwindCode.NullUnwindCode) unwindCode;

                return unwindCode.UnwindOp switch
                {
                    UWOP.UWOP_PUSH_NONVOL     => (UnwindCode.PushNonVolatile) unwindCode,
                    UWOP.UWOP_ALLOC_LARGE     => (UnwindCode.AllocLarge) unwindCode,
                    UWOP.UWOP_ALLOC_SMALL     => (UnwindCode.AllocSmall) unwindCode,
                    UWOP.UWOP_SET_FPREG       => (UnwindCode.SaveNonVolatile) unwindCode,
                    UWOP.UWOP_SAVE_NONVOL     => (UnwindCode.SaveNonVolatile) unwindCode,
                    UWOP.UWOP_SAVE_NONVOL_FAR => (UnwindCode.SaveNonVolatileFar) unwindCode,
                    UWOP.UWOP_EPILOG          => (UnwindCode.Epilog) unwindCode,
                    UWOP.UWOP_SAVE_XMM128     => (UnwindCode.SaveXmm128) unwindCode,
                    UWOP.UWOP_SAVE_XMM128_FAR => (UnwindCode.SaveXmm128Far) unwindCode,
                    UWOP.UWOP_PUSH_MACHFRAME  => (UnwindCode.PushMachFrame) unwindCode
                };
            }
        }
    }

    /// <summary>
    /// Represents the <see cref="UNWIND_CODE"/> structure.<para/>
    /// This type can be casted to a more specific unwind code type based on the type
    /// of value contained in <see cref="UnwindOp"/>. All derived unwind code types are
    /// nested inside this type.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    [DebuggerTypeProxy(typeof(UnwindCodeDebugView))]
    public readonly unsafe partial struct UnwindCode : IViewable
    {
        private string DebuggerDisplay()
        {
            if (IsNull)
                return "<null>";

            return UnwindOp switch
            {
                UWOP.UWOP_PUSH_NONVOL     => ((PushNonVolatile) this).DebuggerDisplay(),
                UWOP.UWOP_ALLOC_LARGE     => ((AllocLarge) this).DebuggerDisplay(),
                UWOP.UWOP_ALLOC_SMALL     => ((AllocSmall) this).DebuggerDisplay(),
                UWOP.UWOP_SET_FPREG       => ((SetFpReg) this).DebuggerDisplay(),
                UWOP.UWOP_SAVE_NONVOL     => ((SaveNonVolatile) this).DebuggerDisplay(),
                UWOP.UWOP_SAVE_NONVOL_FAR => ((SaveNonVolatileFar) this).DebuggerDisplay(),
                UWOP.UWOP_EPILOG          => ((Epilog) this).DebuggerDisplay(),
                UWOP.UWOP_SAVE_XMM128     => ((SaveXmm128) this).DebuggerDisplay(),
                UWOP.UWOP_SAVE_XMM128_FAR => ((SaveXmm128Far) this).DebuggerDisplay(),
                UWOP.UWOP_PUSH_MACHFRAME  => ((PushMachFrame) this).DebuggerDisplay()
            };
        }

        private const int CodeOffsetOffset = 0;
        private const int UnwindOpOffset = 1;

        private readonly byte* value;
        private readonly byte* pUnwindInfo; //Needed to get the offset and register in SetFpReg

        public byte CodeOffset => *value;

        public UWOP UnwindOp => (UWOP) (*(value + 1) & 0x0F);

        public byte OpInfo => (byte) ((*(value + 1) & 0xF0) >> 4);

        public bool IsNull => pUnwindInfo == null;

        internal int StructSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                switch (UnwindOp)
                {
                    case UWOP.UWOP_PUSH_NONVOL: //If pUnwindInfo is null, we're the null entry. UnwindOp will be 0 in any case
                    case UWOP.UWOP_ALLOC_SMALL:
                    case UWOP.UWOP_SET_FPREG:
                    case UWOP.UWOP_EPILOG:
                    case UWOP.UWOP_PUSH_MACHFRAME:
                        return sizeof(short);

                    case UWOP.UWOP_ALLOC_LARGE:
                        return OpInfo == 0 ? 4 : 6;

                    case UWOP.UWOP_SAVE_NONVOL:
                    case UWOP.UWOP_SAVE_XMM128:
                        return 4;

                    case UWOP.UWOP_SAVE_NONVOL_FAR:
                    case UWOP.UWOP_SAVE_XMM128_FAR:
                        return 6;

                    default:
                        throw new NotImplementedException($"Don't know how to handle {nameof(UWOP)} '{UnwindOp}'.");
                }
            }
        }

        public UnwindCode(byte* pUnwindCode, byte* pUnwindInfo)
        {
            this.value = pUnwindCode;
            this.pUnwindInfo = pUnwindInfo;
        }

        public static implicit operator AllocLarge(UnwindCode unwindCode) => new AllocLarge(unwindCode.value);
        public static implicit operator AllocSmall(UnwindCode unwindCode) => new AllocSmall(unwindCode.value);
        public static implicit operator Epilog(UnwindCode unwindCode) => new Epilog(unwindCode.value, unwindCode.pUnwindInfo);
        public static implicit operator NullUnwindCode(UnwindCode unwindCode) => new NullUnwindCode(unwindCode.value);
        public static implicit operator PushMachFrame(UnwindCode unwindCode) => new PushMachFrame(unwindCode.value);
        public static implicit operator PushNonVolatile(UnwindCode unwindCode) => new PushNonVolatile(unwindCode.value);
        public static implicit operator SaveNonVolatile(UnwindCode unwindCode) => new SaveNonVolatile(unwindCode.value);
        public static implicit operator SaveNonVolatileFar(UnwindCode unwindCode) => new SaveNonVolatileFar(unwindCode.value);
        public static implicit operator SaveXmm128(UnwindCode unwindCode) => new SaveXmm128(unwindCode.value);
        public static implicit operator SaveXmm128Far(UnwindCode unwindCode) => new SaveXmm128Far(unwindCode.value);
        public static implicit operator SetFpReg(UnwindCode unwindCode) => new SetFpReg(unwindCode.value, unwindCode.pUnwindInfo);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.UNWIND_CODE, this, ViewKind.UnwindCode, StructSize);

        int IViewable.NumChildren()
        {
            return UnwindOp switch
            {
                UWOP.UWOP_PUSH_NONVOL     => NumChildren<PushNonVolatile>(this),
                UWOP.UWOP_ALLOC_LARGE     => NumChildren<AllocLarge>(this),
                UWOP.UWOP_ALLOC_SMALL     => NumChildren<AllocSmall>(this),
                UWOP.UWOP_SET_FPREG       => NumChildren<SetFpReg>(this),
                UWOP.UWOP_SAVE_NONVOL     => NumChildren<SaveNonVolatile>(this),
                UWOP.UWOP_SAVE_NONVOL_FAR => NumChildren<SaveNonVolatileFar>(this),
                UWOP.UWOP_EPILOG          => NumChildren<Epilog>(this),
                UWOP.UWOP_SAVE_XMM128     => NumChildren<SaveXmm128>(this),
                UWOP.UWOP_SAVE_XMM128_FAR => NumChildren<SaveXmm128Far>(this),
                UWOP.UWOP_PUSH_MACHFRAME  => NumChildren<PushMachFrame>(this)
            };
        }

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            if (IsNull)
            {
                WriteChild<NullUnwindCode>(this, index, ref structWriter);
                return;
            }

            switch (UnwindOp)
            {
                case UWOP.UWOP_ALLOC_LARGE:
                    WriteChild<AllocLarge>(this, index, ref structWriter);
                    break;

                case UWOP.UWOP_ALLOC_SMALL:
                    WriteChild<AllocSmall>(this, index, ref structWriter);
                    break;

                case UWOP.UWOP_EPILOG:
                    WriteChild<Epilog>(this, index, ref structWriter);
                    break;

                case UWOP.UWOP_PUSH_MACHFRAME:
                    WriteChild<PushMachFrame>(this, index, ref structWriter);
                    break;

                case UWOP.UWOP_PUSH_NONVOL:
                    WriteChild<PushNonVolatile>(this, index, ref structWriter);
                    break;

                case UWOP.UWOP_SAVE_NONVOL:
                    WriteChild<SaveNonVolatile>(this, index, ref structWriter);
                    break;

                case UWOP.UWOP_SAVE_NONVOL_FAR:
                    WriteChild<SaveNonVolatileFar>(this, index, ref structWriter);
                    break;

                case UWOP.UWOP_SAVE_XMM128:
                    WriteChild<SaveXmm128>(this, index, ref structWriter);
                    break;

                case UWOP.UWOP_SAVE_XMM128_FAR:
                    WriteChild<SaveXmm128Far>(this, index, ref structWriter);
                    break;

                case UWOP.UWOP_SET_FPREG:
                    WriteChild<SetFpReg>(this, index, ref structWriter);
                    break;

                default:
                    throw new NotImplementedException($"Don't know how to handle {nameof(UWOP)} '{UnwindOp}'.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteChild<T>(T unwindCode, int index, ref StructWriter structWriter) where T : IViewable =>
            unwindCode.WriteChild(index, ref structWriter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int NumChildren<T>(T unwindCode) where T : IViewable =>
            unwindCode.NumChildren();
    }
}
