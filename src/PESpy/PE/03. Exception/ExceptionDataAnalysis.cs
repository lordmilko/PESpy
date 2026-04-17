namespace PESpy
{
    struct ExceptionDataAnalysis
    {
        /// <summary>
        /// The exception data begins with something that resembles <see cref="GsHandlerData"/>.
        /// </summary>
        public bool HasGSHandlerData;

        /// <summary>
        /// The exception data begins with something that resembles a <see cref="ScopeTable"/>.
        /// </summary>
        public bool HasScopeTable;

        /// <summary>
        /// The exception data has <see cref="GsHandlerData"/> after its <see cref="ScopeTable"/>.
        /// </summary>
        public bool HasScopeTableGSHandlerData;

        /// <summary>
        /// The exception data has an RVA pointing to a <see cref="FuncInfo"/> beginning with <see cref="EH_MAGIC_NUMBER"/>.
        /// </summary>
        public bool HasFuncInfo; //V1, V2, V3

        /// <summary>
        /// The exception data has an RVA pointing to a <see cref="FuncInfo4"/>.
        /// </summary>
        public bool HasFuncInfo4; //V4

        /// <summary>
        /// The exception data has <see cref="GsHandlerData"/> after the RVA to its <see cref="FuncInfo"/> or <see cref="FuncInfo4"/>
        /// </summary>
        public bool HasFuncInfoGSHandlerData;

        public EH_MAGIC_NUMBER FuncInfoMagicNumber;

        public WellKnownExceptionHandlerKind Kind
        {
            get
            {
                if (HasFuncInfo4)
                {
                    if (HasFuncInfoGSHandlerData)
                        return WellKnownExceptionHandlerKind.__GSHandlerCheck_EH4;

                    return WellKnownExceptionHandlerKind.__CxxFrameHandler4;
                }

                if (HasFuncInfo)
                {
                    if (HasFuncInfoGSHandlerData)
                    {
                        return FuncInfoMagicNumber switch
                        {
                            //EH_MAGIC_NUMBER.EH_MAGIC_NUMBER1 => WellKnownExceptionHandlerKind.__GSHandlerCheck_EH,
                            //EH_MAGIC_NUMBER.EH_MAGIC_NUMBER2 => WellKnownExceptionHandlerKind.__GSHandlerCheck_EH2,
                            EH_MAGIC_NUMBER.EH_MAGIC_NUMBER3 => WellKnownExceptionHandlerKind.__GSHandlerCheck_EH, //Magic 3 gets EH without 3 for some reason
                        };
                    }

                    return FuncInfoMagicNumber switch
                    {
                        EH_MAGIC_NUMBER.EH_MAGIC_NUMBER1 => WellKnownExceptionHandlerKind.__CxxFrameHandler,
                        EH_MAGIC_NUMBER.EH_MAGIC_NUMBER2 => WellKnownExceptionHandlerKind.__CxxFrameHandler2,
                        EH_MAGIC_NUMBER.EH_MAGIC_NUMBER3 => WellKnownExceptionHandlerKind.__CxxFrameHandler3,
                    };
                }

                if (HasScopeTableGSHandlerData)
                    return WellKnownExceptionHandlerKind.__GSHandlerCheck_SEH;

                //I tried checking various patterns like flags and unwind codes; you can have EHANDLER, UHANDLER or FHANDLER with C Specific Handler,
                //so we have no way of detecting C Specific Handler No Except.
                //Note that while you can technically have imports and exports on __C_specific_handler (kernel32
                //imports it from ntdll) I feel like this is so rare it's not worth checking
                if (HasScopeTable)
                    return WellKnownExceptionHandlerKind.__C_specific_handler;

                if (HasGSHandlerData)
                    return WellKnownExceptionHandlerKind.__GSHandlerCheck;

                return WellKnownExceptionHandlerKind.Unknown;
            }
        }

        public override string ToString()
        {
            return Kind.ToString();
        }
    }
}
