namespace PESpy
{
    //http://www.hexblog.com/wp-content/uploads/2012/06/Recon-2012-Skochinsky-Compiler-Internals.pdf
    //Once you understand the general pattern, it's relatively easy to extrapolate and understand the data
    //that will be present in GSHandlerCheck items

    public enum WellKnownExceptionHandlerKind
    {
        //We set Unknown to 1 so we can check if we've got it yet; if we're 0 we don't know
        Unknown = 1,
        None, //When ExceptionHandler is 0

        #region GSHandlerCheck

        /// <summary>
        /// Data is a <see cref="GsHandlerData"/>.
        /// </summary>
        __GSHandlerCheck,

        /// <summary>
        /// Data is a <see cref="ScopeTable"/> followed by <see cref="GsHandlerData"/>.<para/>
        /// <see cref="UnwindInfo.ExceptionData"/> wraps these as a <see cref="ScopeTableAndGsHandlerData"/>.
        /// </summary>
        __GSHandlerCheck_SEH,

        /// <summary>
        /// Data is an <see cref="RVA{T}"/> to a <see cref="FuncInfo"/> whose magic is <see cref="EH_MAGIC_NUMBER.EH_MAGIC_NUMBER3"/> (not 1),
        /// followed by <see cref="GsHandlerData"/> after the RVA.<para/>
        /// <see cref="UnwindInfo.ExceptionData"/> wraps these as a <see cref="FuncInfoAndGsHandlerData"/>
        /// </summary>
        __GSHandlerCheck_EH,

        /// <summary>
        /// Data is an <see cref="RVA{T}"/> to a <see cref="FuncInfo4"/> whose magic is <see cref="EH_MAGIC_NUMBER.EH_MAGIC_NUMBER3"/>,
        /// followed by <see cref="GsHandlerData"/> after the RVA.<para/>
        /// <see cref="UnwindInfo.ExceptionData"/> wraps these as a <see cref="FuncInfo4AndGsHandlerData"/>
        /// </summary>
        __GSHandlerCheck_EH4,

        #endregion
        #region C Specific Handler

        /// <summary>
        /// Data is a <see cref="ScopeTable"/>.
        /// </summary>
        __C_specific_handler,

        /// <summary>
        /// Data is a <see cref="ScopeTable"/>. If symbols are not available, this function may be mislabelled as <see cref="__C_specific_handler"/>.
        /// </summary>
        __C_specific_handler_noexcept,

        #endregion
        #region CxxFrameHandler

        /// <summary>
        /// Data is an <see cref="RVA{T}"/> to a <see cref="FuncInfo"/> whose magic is <see cref="EH_MAGIC_NUMBER.EH_MAGIC_NUMBER"/>.
        /// </summary>
        __CxxFrameHandler,

        /// <summary>
        /// Data is an <see cref="RVA{T}"/> to a <see cref="FuncInfo"/> whose magic is <see cref="EH_MAGIC_NUMBER.EH_MAGIC_NUMBER2"/>.
        /// </summary>
        __CxxFrameHandler2,

        /// <summary>
        /// Data is an <see cref="RVA{T}"/> to a <see cref="FuncInfo"/> whose magic is <see cref="EH_MAGIC_NUMBER.EH_MAGIC_NUMBER3"/>.
        /// </summary>
        __CxxFrameHandler3,

        /// <summary>
        /// Data is an <see cref="RVA{T}"/> to a <see cref="FuncInfo4"/>.
        /// </summary>
        __CxxFrameHandler4,

        #endregion

        LastSupportedHandler,

        LdrpICallHandler, //ntdll
        KiUserApcHandler, //ntdll
        KiUserCallbackDispatcherHandler, //ntdll
        RtlpExceptionHandler, //ntdll
        RtlpUnwindHandler, //ntdll
        RtlpEnclaveCallDispatchFilter, //ntdll
        CrashForExceptionInNonABICompliantCodeRange, //msedge, but PEAnatomist can detect it so it might be more standard than we realize
    }
}
