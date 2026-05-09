using System.Diagnostics;

namespace PESpy
{
    //Type is made up. Based on MulNdrpInitializeContextFromProc
    [DebuggerDisplay("ProcNum = {ProcNum}")]
    public readonly struct RpcFormatStringItem
    {
        public FORMAT_CHARACTER HandleType { get; }

        public INTERPRETER_FLAGS InterpreterFlags { get; }

        public RPC_FLAGS RpcFlags { get; }

        public ushort ProcNum { get; }

        public ushort StackSize { get; }

        public FORMAT_CHARACTER ExplicitHandleType { get; }

        public NativeSpan<byte> ExplicitHandle { get; }

        public NdrProcDesc NdrProcDesc { get; }

        internal RpcFormatStringItem(
            FORMAT_CHARACTER handleType,
            INTERPRETER_FLAGS interpreterFlags,
            RPC_FLAGS rpcFlags,
            ushort procNum,
            ushort stackSize,
            FORMAT_CHARACTER explicitHandleType,
            NativeSpan<byte> explicitHandle,
            NdrProcDesc ndrProcDesc)
        {
            HandleType = handleType;
            InterpreterFlags = interpreterFlags;
            RpcFlags = rpcFlags;
            ProcNum = procNum;
            StackSize = stackSize;
            ExplicitHandleType = explicitHandleType;
            ExplicitHandle = explicitHandle;
            NdrProcDesc = ndrProcDesc;
        }
    }
}
