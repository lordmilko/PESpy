using System;
using System.Buffers;
using System.Diagnostics;
using System.Text;
using ClrDebug.DIA;

#nullable disable

namespace PESpy
{
    //Based on undname-rs and llvm (see ThirdPartyNotices.txt)

    /* C++ STL templates look like shit. When you declare a variable of type std::string, the PDB says that the variable's type is in
     * fact std::basic_string<char,std::char_traits<char>,std::allocator<char> >. Separately to this, there is an S_UDT for std::string
     * that maps to this generic value. However, S_UDT records are not always guaranteed to be generated. If you do std::vector<int>, you'll
     * find that there is no S_UDT record, and the std::vector<int> is displayed as std::vector<int,std::allocator<int>> in the debugger.
     * Furthermore, if you step into a method like std::string::size(), you'll be surprised to find that the method displays normally in your
     * call stack! How is this possible!
     *
     * It does not appear that the natvis file plays any part in customizing how the variable is displayed. Rather, CppDebug.dll seems to have
     * a list of hard coded heuristics that it uses for controlling the visualization of types. All of the action happens in CppEE::CTypeFormatter.
     * It's all just a bunch of string manipulation! "std::string" seems to just come from CppEE::CTypeFormatter::ReverselyMapTypeAlias,which just
     * does a bunch of find and replace for the various standard string types that are known to Visual Studio
     *
     * There are two categories of types whose format might need fixing. Lines that start with "using " whose type does not start with an underscore,
     * and classes whose line above says "CLASS TEMPLATE". From my initial review of all items that match these criteria, I don't feel like there's actually
     * that many interesting types that are worth "tidying up". In any case, it _would_ be useful to have an extensible mechanism for tidying up
     * STL garbage
     */

    public static partial class Demangler
    {
        //MAX_SYM_NAME is 2000, however internally DbgHelp will allocate a buffer of 4096 bytes.
        //Ultimately though, there is no max symbol name. Symbol names can be as big as you like, especially when there's lots of generics
        public const int MaxSymbolName = 4096;

        #region IntrinsicFunctionKind

        //Not all ? identifiers are intrinsics *functions*. This table only maps
        //operator codes for the special functions, all others are handled elsewhere,
        //hence the None entries in the table.
        static IntrinsicFunctionKind[] Basic =
        {
            IntrinsicFunctionKind.None,             // ?0 # Foo::Foo()
            IntrinsicFunctionKind.None,             // ?1 # Foo::~Foo()
            IntrinsicFunctionKind.New,              // ?2 # operator new
            IntrinsicFunctionKind.Delete,           // ?3 # operator delete
            IntrinsicFunctionKind.Assign,           // ?4 # operator=
            IntrinsicFunctionKind.RightShift,       // ?5 # operator>>
            IntrinsicFunctionKind.LeftShift,        // ?6 # operator<<
            IntrinsicFunctionKind.LogicalNot,       // ?7 # operator!
            IntrinsicFunctionKind.Equals,           // ?8 # operator==
            IntrinsicFunctionKind.NotEquals,        // ?9 # operator!=
            IntrinsicFunctionKind.ArraySubscript,   // ?A # operator[]
            IntrinsicFunctionKind.None,             // ?B # Foo::operator <type>()
            IntrinsicFunctionKind.Pointer,          // ?C # operator->
            IntrinsicFunctionKind.Dereference,      // ?D # operator*
            IntrinsicFunctionKind.Increment,        // ?E # operator++
            IntrinsicFunctionKind.Decrement,        // ?F # operator--
            IntrinsicFunctionKind.Minus,            // ?G # operator-
            IntrinsicFunctionKind.Plus,             // ?H # operator+
            IntrinsicFunctionKind.BitwiseAnd,       // ?I # operator&
            IntrinsicFunctionKind.MemberPointer,    // ?J # operator->*
            IntrinsicFunctionKind.Divide,           // ?K # operator/
            IntrinsicFunctionKind.Modulus,          // ?L # operator%
            IntrinsicFunctionKind.LessThan,         // ?M operator<
            IntrinsicFunctionKind.LessThanEqual,    // ?N operator<=
            IntrinsicFunctionKind.GreaterThan,      // ?O operator>
            IntrinsicFunctionKind.GreaterThanEqual, // ?P operator>=
            IntrinsicFunctionKind.Comma,            // ?Q operator,
            IntrinsicFunctionKind.Parens,           // ?R operator()
            IntrinsicFunctionKind.BitwiseNot,       // ?S operator~
            IntrinsicFunctionKind.BitwiseXor,       // ?T operator^
            IntrinsicFunctionKind.BitwiseOr,        // ?U operator|
            IntrinsicFunctionKind.LogicalAnd,       // ?V operator&&
            IntrinsicFunctionKind.LogicalOr,        // ?W operator||
            IntrinsicFunctionKind.TimesEqual,       // ?X operator*=
            IntrinsicFunctionKind.PlusEqual,        // ?Y operator+=
            IntrinsicFunctionKind.MinusEqual,       // ?Z operator-=
        };

        static IntrinsicFunctionKind[] Under =
        {
            IntrinsicFunctionKind.DivEqual,           // ?_0 operator/=
            IntrinsicFunctionKind.ModEqual,           // ?_1 operator%=
            IntrinsicFunctionKind.RshEqual,           // ?_2 operator>>=
            IntrinsicFunctionKind.LshEqual,           // ?_3 operator<<=
            IntrinsicFunctionKind.BitwiseAndEqual,    // ?_4 operator&=
            IntrinsicFunctionKind.BitwiseOrEqual,     // ?_5 operator|=
            IntrinsicFunctionKind.BitwiseXorEqual,    // ?_6 operator^=
            IntrinsicFunctionKind.None,               // ?_7 # vftable
            IntrinsicFunctionKind.None,               // ?_8 # vbtable
            IntrinsicFunctionKind.None,               // ?_9 # vcall
            IntrinsicFunctionKind.None,               // ?_A # typeof
            IntrinsicFunctionKind.None,               // ?_B # local static guard
            IntrinsicFunctionKind.None,               // ?_C # string literal
            IntrinsicFunctionKind.VbaseDtor,          // ?_D # vbase destructor
            IntrinsicFunctionKind.VecDelDtor,         // ?_E # vector deleting destructor
            IntrinsicFunctionKind.DefaultCtorClosure, // ?_F # default constructor closure
            IntrinsicFunctionKind.ScalarDelDtor,      // ?_G # scalar deleting destructor
            IntrinsicFunctionKind.VecCtorIter,        // ?_H # vector constructor iterator
            IntrinsicFunctionKind.VecDtorIter,        // ?_I # vector destructor iterator
            IntrinsicFunctionKind.VecVbaseCtorIter,   // ?_J # vector vbase constructor iterator
            IntrinsicFunctionKind.VdispMap,           // ?_K # virtual displacement map
            IntrinsicFunctionKind.EHVecCtorIter,      // ?_L # eh vector constructor iterator
            IntrinsicFunctionKind.EHVecDtorIter,      // ?_M # eh vector destructor iterator
            IntrinsicFunctionKind.EHVecVbaseCtorIter, // ?_N # eh vector vbase constructor iterator
            IntrinsicFunctionKind.CopyCtorClosure,    // ?_O # copy constructor closure
            IntrinsicFunctionKind.None,               // ?_P<name> # udt returning <name>
            IntrinsicFunctionKind.None,               // ?_Q # <unknown>
            IntrinsicFunctionKind.None,               // ?_R0 - ?_R4 # RTTI Codes
            IntrinsicFunctionKind.None,               // ?_S # local vftable
            IntrinsicFunctionKind.LocalVftableCtorClosure, // ?_T # local vftable constructor closure
            IntrinsicFunctionKind.ArrayNew,                // ?_U operator new[]
            IntrinsicFunctionKind.ArrayDelete,             // ?_V operator delete[]
            IntrinsicFunctionKind.None,                    // ?_W <unused>
            IntrinsicFunctionKind.None,                    // ?_X <unused>
            IntrinsicFunctionKind.None,                    // ?_Y <unused>
            IntrinsicFunctionKind.None,                    // ?_Z <unused>
        };

        static IntrinsicFunctionKind[] DoubleUnder =
        {
            IntrinsicFunctionKind.None,                       // ?__0 <unused>
            IntrinsicFunctionKind.None,                       // ?__1 <unused>
            IntrinsicFunctionKind.None,                       // ?__2 <unused>
            IntrinsicFunctionKind.None,                       // ?__3 <unused>
            IntrinsicFunctionKind.None,                       // ?__4 <unused>
            IntrinsicFunctionKind.None,                       // ?__5 <unused>
            IntrinsicFunctionKind.None,                       // ?__6 <unused>
            IntrinsicFunctionKind.None,                       // ?__7 <unused>
            IntrinsicFunctionKind.None,                       // ?__8 <unused>
            IntrinsicFunctionKind.None,                       // ?__9 <unused>
            IntrinsicFunctionKind.ManVectorCtorIter,          // ?__A managed vector ctor iterator
            IntrinsicFunctionKind.ManVectorDtorIter,          // ?__B managed vector dtor iterator
            IntrinsicFunctionKind.EHVectorCopyCtorIter,       // ?__C EH vector copy ctor iterator
            IntrinsicFunctionKind.EHVectorVbaseCopyCtorIter,  // ?__D EH vector vbase copy ctor iter
            IntrinsicFunctionKind.None,                       // ?__E dynamic initializer for `T'
            IntrinsicFunctionKind.None,                       // ?__F dynamic atexit destructor for `T'
            IntrinsicFunctionKind.VectorCopyCtorIter,         // ?__G vector copy constructor iter
            IntrinsicFunctionKind.VectorVbaseCopyCtorIter,    // ?__H vector vbase copy ctor iter
            IntrinsicFunctionKind.ManVectorVbaseCopyCtorIter, // ?__I managed vector vbase copy ctor
                                            // iter
            IntrinsicFunctionKind.None,                       // ?__J local static thread guard
            IntrinsicFunctionKind.None,                       // ?__K operator ""_name
            IntrinsicFunctionKind.CoAwait,                    // ?__L operator co_await
            IntrinsicFunctionKind.Spaceship,                  // ?__M operator<=>
            IntrinsicFunctionKind.None,                       // ?__N <unused>
            IntrinsicFunctionKind.None,                       // ?__O <unused>
            IntrinsicFunctionKind.None,                       // ?__P <unused>
            IntrinsicFunctionKind.None,                       // ?__Q <unused>
            IntrinsicFunctionKind.None,                       // ?__R <unused>
            IntrinsicFunctionKind.None,                       // ?__S <unused>
            IntrinsicFunctionKind.None,                       // ?__T <unused>
            IntrinsicFunctionKind.None,                       // ?__U <unused>
            IntrinsicFunctionKind.None,                       // ?__V <unused>
            IntrinsicFunctionKind.None,                       // ?__W <unused>
            IntrinsicFunctionKind.None,                       // ?__X <unused>
            IntrinsicFunctionKind.None,                       // ?__Y <unused>
            IntrinsicFunctionKind.None,                       // ?__Z <unused>
        };

        #endregion

        /// <summary>
        /// Demangles the specified symbol name, returning a <see cref="DemangleTree"/> on success and throwing an exception
        /// on failure.<para/>
        /// Note: the singleton arena used by the demangler is transferred to the <see cref="DemangleTree"/>. The <see cref="DemangleTree"/>
        /// should be disposed when it is no longer needed. Any subsequent parse attempts that are made while the arena is held by the <see cref="DemangleTree"/>
        /// will result in allocations being made.
        /// </summary>
        /// <param name="str">The symbol name that should be demangled.</param>
        /// <returns>The <see cref="DemangleTree"/> that contains the result of the demangling.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static DemangleTree Parse(string str)
        {
            var textWindow = new TextWindow(str);

            try
            {
                if (TryParseInternal(ref textWindow, out var symbol))
                {
                    var symbolTree = new DemangleTree(symbol, textWindow.ExtractStrings(), textWindow.ExtractArena());

                    return symbolTree;
                }

                throw new InvalidOperationException($"Failed to demangle string '{str}'");
            }
            finally
            {
                textWindow.Dispose();
            }
        }

        /// <summary>
        /// Tries to demangle the specified symbol name, returning a <see cref="DemangleTree"/> on success.<para/>
        /// Note: the singleton arena used by the demangler is transferred to the <see cref="DemangleTree"/>. The <see cref="DemangleTree"/>
        /// should be disposed when it is no longer needed. Any subsequent parse attempts that are made while the arena is held by the <see cref="DemangleTree"/>
        /// will result in allocations being made.
        /// </summary>
        /// <param name="str">The symbol name that should be demangled.</param>
        /// <param name="symbolTree">The <see cref="DemangleTree"/> that contains the result of the demangling.</param>
        /// <returns>Whether the specified string could be successfully parsed.</returns>
        public static unsafe bool TryParse(FixedUtf8String str, out DemangleTree symbolTree)
        {
            var textWindow = new TextWindow(str.Value, str.Length);

            try
            {
                if (TryParseInternal(ref textWindow, out var symbol))
                {
                    symbolTree = new DemangleTree(symbol, textWindow.ExtractStrings(), textWindow.ExtractArena());
                    return true;
                }

                symbolTree = default;
                return false;
            }
            finally
            {
                textWindow.Dispose();
            }
        }

        public static unsafe void ParseString(FixedUtf8String str, ref Utf8StringBuilder builder, UNDNAME flags)
        {
            var textWindow = new TextWindow(str.Value, str.Length);

            try
            {
                if (TryParseInternal(ref textWindow, out var symbol))
                {
                    symbol.Output(ref builder, flags);
                }
                else
                {
                    builder.Append(str);
                }
            }
            finally
            {
                textWindow.Dispose();
            }
        }

        public static int ParseString(FixedUtf8String str, Span<byte> outputSpan, UNDNAME flags)
        {
            var builder = new Utf8StringBuilder(outputSpan);

            try
            {
                ParseString(str, ref builder, flags);

                //If we wrote beyond the end of the span, we rented a buffer to write the rest. But that
                //data will be truncated
                return Math.Min(builder.Length, outputSpan.Length);
            }
            finally
            {
                builder.Dispose();
            }
        }

        public static unsafe string ParseString(string str, UNDNAME flags = UNDNAME.UNDNAME_COMPLETE)
        {
            //If it's not a mangled string, return the input string as is
            if (!str.StartsWith("?"))
                return str;

            var length = str.Length;
            var maxBytes = Encoding.UTF8.GetMaxByteCount(str.Length);
            var array = ArrayPool<byte>.Shared.Rent(maxBytes);

            try
            {
                fixed (char* c = str)
                fixed (byte* p = array)
                {
                    var actual = Encoding.UTF8.GetBytes(c, str.Length, p, maxBytes);

                    var textWriter = new TextWindow(p, actual);

                    try
                    {
                        if (!TryParseInternal(ref textWriter, out var symbolNode))
                            return str;

                        return symbolNode.ToString(flags);
                    }
                    finally
                    {
                        textWriter.Dispose();
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(array);
            }
        }

        public static unsafe string ParseString(FixedUtf8String str, UNDNAME flags = UNDNAME.UNDNAME_COMPLETE)
        {
            var textWriter = new TextWindow(str.Value, str.Length);

            try
            {
                if (!TryParseInternal(ref textWriter, out var symbolNode))
                    return str.ToString();

                return symbolNode.ToString(flags);
            }
            finally
            {
                textWriter.Dispose();
            }
        }

        public static unsafe bool TryParseString(FixedUtf8String str, UNDNAME flags, out string result)
        {
            var textWriter = new TextWindow(str.Value, str.Length);

            try
            {
                if (!TryParseInternal(ref textWriter, out var symbolNode))
                {
                    result = default;
                    return false;
                }

                result = symbolNode.ToString(flags);
                return true;
            }
            finally
            {
                textWriter.Dispose();
            }
        }

        public static unsafe bool CrackVftable(
            string str,
            out string className,
            out string targetName)
        {
            className = default;
            targetName = default;

            if (!str.StartsWith("??_7"))
                return false;

            var length = str.Length;
            var maxBytes = Encoding.UTF8.GetMaxByteCount(str.Length);
            var array = ArrayPool<byte>.Shared.Rent(maxBytes);

            try
            {
                fixed (char* c = str)
                fixed (byte* p = array)
                {
                    var actual = Encoding.UTF8.GetBytes(c, str.Length, p, maxBytes);

                    var textWriter = new TextWindow(p, actual);

                    try
                    {
                        if (!TryParseInternal(ref textWriter, out var symbolNode))
                            return false;

                        var vftableSymbol = (SpecialTableSymbolNode) symbolNode;

                        //Name should be a qualified name whose last element is "vftable"
                        var classNameComponents = vftableSymbol.Name.Components;

                        var ptr = stackalloc char[MaxSymbolName];
                        var builder = new Utf8StringBuilder(new Span<byte>(ptr, MaxSymbolName));

                        try
                        {
                            classNameComponents.Output(ref builder, UNDNAME.UNDNAME_NAME_ONLY, "::", classNameComponents.Count - 1);

                            className = builder.ToString();
                        }
                        finally
                        {
                            builder.Dispose();
                        }

                        if (vftableSymbol.TargetName != null)
                            targetName = vftableSymbol.TargetName.ToString();

                        return true;
                    }
                    finally
                    {
                        textWriter.Dispose();
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(array);
            }
        }

        public static unsafe void ParseFunction(
            string str,
            Action<FunctionSymbolNode> callback)
        {
            if (str.StartsWith("??_R"))
                return; //Some type of RTTI descriptor

            var length = str.Length;
            var maxBytes = Encoding.UTF8.GetMaxByteCount(str.Length);
            var array = ArrayPool<byte>.Shared.Rent(maxBytes);

            try
            {
                fixed (char* c = str)
                fixed (byte* p = array)
                {
                    var actual = Encoding.UTF8.GetBytes(c, str.Length, p, maxBytes);

                    var textWriter = new TextWindow(p, actual);

                    try
                    {
                        if (!TryParseInternal(ref textWriter, out var symbolNode))
                            return;

                        if (symbolNode is FunctionSymbolNode f)
                        {
                            callback(f);
                        }
                    }
                    finally
                    {
                        textWriter.Dispose();
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(array);
            }
        }

        private static bool TryParseInternal(ref TextWindow textWindow, out SymbolNode symbolNode)
        {
            var c = textWindow.PeekChar();

            switch (c)
            {
                case '.':
                    textWindow.AdvanceChar();

                    if (TryParseTypeInfoName(ref textWindow, out var variableSymbol))
                    {
                        symbolNode = variableSymbol;
                        return true;
                    }

                    symbolNode = default;
                    return false;

                case '?':
                    if (textWindow.PeekChar(1) == '?' && textWindow.PeekChar(2) == '@')
                    {
                        //??@bae83f4efe5fbb0b1747f520095bd228@
                        //There's also apparently other variations that LLVM discusses

                        //Debug.Assert(false); //Not sure if MD5 symbols is a thing MSVC actually does
                        symbolNode = default;
                        return false;
                    }
                    else
                    {
                        //Normal mangled symbol
                        textWindow.AdvanceChar();

                        if (!TryParseSpecialIntrinsic(ref textWindow, out symbolNode))
                            return false;

                        if (symbolNode != null)
                            return true;

                        return TryParseDeclarator(ref textWindow, out symbolNode);
                    }

                default:
                    symbolNode = default;
                    return false;
            }
        }

        private static bool TryParseEncodedSymbol(
            ref TextWindow textWindow,
            QualifiedNameNode name,
            out SymbolNode symbolNode)
        {
            symbolNode = default;

            if (!textWindow.TryPeekChar(out var c))
                return false;

            switch (c)
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                    var storageClass = ParseVariableStorageClass(ref textWindow);
                    if (TryParseVariableEncoding(ref textWindow, storageClass, out var variableSymbol))
                    {
                        symbolNode = variableSymbol;
                        return true;
                    }

                    symbolNode = default;
                    return false;
            }

            if (!TryParseFunctionEncoding(ref textWindow, out var functionSymbol))
            {
                symbolNode = default;
                return false;
            }

            var identifier = name.UnqualifiedIdentifier;

            if (identifier.Kind == NodeKind.ConversionOperatorIdentifier)
            {
                var op = (ConversionOperatorIdentifierNode) identifier;

                if (functionSymbol != null)
                    op.TargetType = functionSymbol.Signature.ReturnType;
            }

            symbolNode = functionSymbol;
            return true;
        }

        private static bool TryParseDeclarator(ref TextWindow textWindow, out SymbolNode symbol)
        {
            if (!TryParseFullyQualifiedSymbolName(ref textWindow, out var qualifiedName))
            {
                symbol = default;
                return false;
            }

            if (!TryParseEncodedSymbol(ref textWindow, qualifiedName, out symbol))
                return false;

            symbol.Name = qualifiedName;

            var identifier = qualifiedName.UnqualifiedIdentifier;

            if (identifier.Kind == NodeKind.ConversionOperatorIdentifier)
            {
                var op = (ConversionOperatorIdentifierNode) identifier;

                if (op.TargetType == null)
                    return false;
            }

            return true;
        }

        private static bool TryParseTypeInfoName(ref TextWindow textWindow, out VariableSymbolNode variableSymbol)
        {
            variableSymbol = default;

            textWindow.AdvanceChar(); //Skip over the '.'

            if (!TryParseType(ref textWindow, QualifierMangleMode.Result, out var type))
                return false;

            if (!textWindow.IsEmpty) //The whole thing should have been parsed
                return false;

            variableSymbol = textWindow.AllocVariableSymbol(
                name: textWindow.AllocQualifiedName(
                    textWindow.AllocNodeArray(
                        textWindow.AllocNamedIdentifier(Strings.RTTITypeDescriptorName)
                    )
                ),
                type: type
            );

            return true;
        }

        private static bool TryParseVariableEncoding(
            ref TextWindow textWindow,
            StorageClass storageClass,
            out VariableSymbolNode variableSymbol)
        {
            variableSymbol = default;

            if (!TryParseType(ref textWindow, QualifierMangleMode.Drop, out var type))
                return false;

            variableSymbol = textWindow.AllocVariableSymbol(type, storageClass);

            switch (type.Kind)
            {
                case NodeKind.PointerType:
                    var pointerType = (PointerTypeNode) type;

                    //llvm-undname just merges the ext qualifiers onto the main set, but that's not
                    //how UnDecorateSymbolName works. It builds up a string as it goes, and you could have
                    //a type wchar_t const, wrapped in a pointer that is __ptr64 const. Extra flags
                    //need to be applied to the pointer type to say __ptr64 again, so we get wchar_t const * __ptr64 const __ptr64
                    pointerType.VariableEncodingQualifiers |= ParsePointerExtQualifiers(ref textWindow);

                    var affinity = PointerAffinity.Pointer;

                    if (!TryParseManagedQualifiers(ref textWindow, ref affinity))
                        return false;

                    if (!TryParseQualifiers(ref textWindow, out var extraChildQualifiers, out var isMember))
                        return false;

                    if (pointerType.ClassParent != null)
                    {
                        if (!TryParseFullyQualifiedTypeName(ref textWindow, out var backRefName))
                            return false;
                    }

                    pointerType.Pointee.Qualifiers |= extraChildQualifiers;
                    break;

                default:
                    if (!TryParseQualifiers(ref textWindow, out var qualifier, out _))
                        return false;

                    type.Qualifiers = qualifier;
                    break;
            }

            return true;
        }

        private static bool TryParseFunctionEncoding(ref TextWindow textWindow, out FunctionSymbolNode functionSymbol)
        {
            functionSymbol = default;

            var extraFlags = FunctionClass.None;

            if (textWindow.AdvanceIfMatches("$$J0"))
                extraFlags |= FunctionClass.ExternC;

            if (textWindow.IsEmpty)
                return false;

            if (!TryParseFunctionClass(ref textWindow, out var functionClass))
                return false;

            functionClass |= extraFlags;

            ThunkSignatureNode thunkSignature = null;

            if ((functionClass & FunctionClass.Adjustor) != 0)
            {
                thunkSignature = textWindow.AllocThunkSignature();

                if (!TryParseSigned(ref textWindow, out var staticOffset))
                    return false;

                thunkSignature.ThisAdjustor.StaticOffset = staticOffset;
            }
            else if ((functionClass & FunctionClass.VirtualThisAdjust) != 0)
            {
                thunkSignature = textWindow.AllocThunkSignature();

                if ((functionClass & FunctionClass.VirtualThisAdjustEx) != 0)
                {
                    if (!TryParseSigned(ref textWindow, out var vbPtrOffset))
                        return false;

                    if (!TryParseSigned(ref textWindow, out var vbOffsetOffset))
                        return false;

                    thunkSignature.ThisAdjustor.VBPtrOffset = vbPtrOffset;
                    thunkSignature.ThisAdjustor.VBOffsetOffset = vbOffsetOffset;
                }

                if (!TryParseSigned(ref textWindow, out var vtorDispOffset))
                    return false;

                if (!TryParseSigned(ref textWindow, out var staticOffset))
                    return false;

                thunkSignature.ThisAdjustor.VtordispOffset = vtorDispOffset;
                thunkSignature.ThisAdjustor.StaticOffset = staticOffset;
            }

            FunctionSignatureNode functionSignature;

            if ((functionClass & FunctionClass.NoParameterList) != 0)
                functionSignature = textWindow.AllocFunctionSignature();
            else
            {
                //If it's not global or static, then it's this
                var hasThisQuals = (functionClass & (FunctionClass.Global | FunctionClass.Static)) == 0;

                if (!TryParseFunctionType(ref textWindow, hasThisQuals, out functionSignature))
                    return false;
            }

            if (thunkSignature != null)
            {
                thunkSignature.Qualifiers = functionSignature.Qualifiers;
                thunkSignature.ExtQualifiers = functionSignature.ExtQualifiers;

                thunkSignature.CallingConvention = functionSignature.CallingConvention;
                thunkSignature.FunctionClass = functionSignature.FunctionClass;
                thunkSignature.RefQualifier = functionSignature.RefQualifier;
                thunkSignature.ReturnType = functionSignature.ReturnType;
                thunkSignature.IsVariadic = functionSignature.IsVariadic;
                thunkSignature.Parameters = functionSignature.Parameters;
                thunkSignature.IsNoExcept = functionSignature.IsNoExcept;

                functionSignature = thunkSignature;
            }

            functionSignature.FunctionClass = functionClass;

            var symbol = textWindow.AllocFunctionSymbol();
            symbol.Signature = functionSignature;

            functionSymbol = symbol;
            return true;
        }

        private static Qualifiers ParsePointerExtQualifiers(ref TextWindow textWindow)
        {
            Qualifiers qualifiers = default;

            if (textWindow.TryAdvance('E'))
                qualifiers |= Qualifiers.Pointer64;
            if (textWindow.TryAdvance('I'))
                qualifiers |= Qualifiers.Restrict;
            if (textWindow.TryAdvance('F'))
                qualifiers |= Qualifiers.Unaligned;

            return qualifiers;
        }

        private static bool TryParseManagedQualifiers(ref TextWindow textWindow, ref PointerAffinity affinity)
        {
            if (textWindow.TryAdvance('$'))
            {
                Debug.Assert(affinity == PointerAffinity.Pointer); //I'm assuming it'll always be P$

                var c = textWindow.PeekChar();

                switch (c)
                {
                    case 'A': //^
                        affinity = PointerAffinity.ManagedPointer;
                        textWindow.AdvanceChar();
                        break;

                    case 'B': //Pinned?
                        Debug.Assert(false); //Figure out what this looks like
                        return false;

                    default:
                        //Some type of array. What comes next is the rank. The rank is encoded in two characters

                        textWindow.AdvanceChar();

                        if (!textWindow.TryNextChar(out var c2))
                            return false;

                        var rank = ((c - '0') << 4) + (c2 - '0');

                        if (!textWindow.TryAdvance('$'))
                            return false; //Should be terminated by another $
                        break;
                }
            }

            return true;
        }

        private static bool TryParseType(ref TextWindow textWindow, QualifierMangleMode mangleMode, out TypeNode type)
        {
            Qualifiers qualifiers = default;
            type = default;
            bool isMember;

            switch (mangleMode)
            {
                case QualifierMangleMode.Mangle:
                    if (!TryParseQualifiers(ref textWindow, out qualifiers, out isMember))
                        return false;
                    break;
                case QualifierMangleMode.Result:
                    if (textWindow.TryAdvance('?'))
                    {
                        if (!TryParseQualifiers(ref textWindow, out qualifiers, out isMember))
                            return false;
                    }
                    break;
            }

            if (textWindow.IsEmpty)
                return false;

            switch (textWindow.PeekChar())
            {
                //TagType
                case 'T': //union
                case 'U': //struct
                case 'V': //class
                case 'W': //enum
                    if (!TryParseClassType(ref textWindow, out var tagType))
                        return false;

                    type = tagType;
                    break;

                //PointerType
                case 'A': //foo &
                case 'P': //foo *
                case 'Q': //foo *const
                case 'R': //foo *volatile
                case 'S': //foo *const volatile
                    //It's also a pointer type if we start with $$Q (which is in the default case)
                    if (!TryIsMemberPointer(textWindow, out var isMemberPointer))
                        return false;

                    if (isMemberPointer)
                    {
                        if (!TryParseMemberPointerType(ref textWindow, out var pointerType))
                            return false;

                        type = pointerType;
                    }
                    else
                    {
                        if (!TryParsePointerType(ref textWindow, out var pointerType))
                            return false;

                        type = pointerType;
                    }
                    break;

                //ArrayType
                case 'Y':
                    if (!TryParseArrayType(ref textWindow, out var arrayType))
                        return false;
                    type = arrayType;
                    break;

                //CustomType
                case '?':
                    if (!TryParseCustomType(ref textWindow, out var customType))
                        return false;
                    type = customType;
                    break;

                default:
                    if (textWindow.AdvanceIfMatches("$$A8@@"))
                    {
                        if (!TryParseFunctionType(ref textWindow, hasThisQuals: true, out var functionSignature))
                            return false;

                        type = functionSignature;
                    }
                    else if (textWindow.AdvanceIfMatches("$$A6"))
                    {
                        if (!TryParseFunctionType(ref textWindow, hasThisQuals: false, out var functionSignature))
                            return false;

                        type = functionSignature;
                    }
                    else if (textWindow.StartsWith("$$Q"))
                        goto case 'P'; //Pointer
                    else
                    {
                        if (!TryParsePrimitiveType(ref textWindow, out var primitiveType))
                            return false;

                        type = primitiveType;
                    }
                    break;
            }

            if (type == null)
                return true;

            type.Qualifiers |= qualifiers;
            return true;
        }

        private static bool TryIsMemberPointer(TextWindow textWindow, out bool isMemberPointer)
        {
            if (!textWindow.TryNextChar(out var c))
            {
                isMemberPointer = default;
                return false;
            }

            switch (c)
            {
                case '$':
                    //This is probably an rvalue reference (e.g. $$Q), and you cannot have an
                    //rvalue reference to a member.
                    isMemberPointer = false;
                    return true;

                case 'A':
                    //'A' indicates a reference, and you cannot have a reference to a member
                    //function or member.
                    isMemberPointer = false;
                    return true;

                case 'P':
                case 'Q':
                case 'R':
                case 'S':
                    //These 4 values indicate some kind of pointer, but we still don't know
                    //what.
                    break;

                default:
                    //The values that this method switches on should have been checked by the caller
                    Debug.Assert(false);
                    isMemberPointer = default;
                    return false;
            }

            //If it starts with a number, then 6 indicates a non-member function
            //pointer, and 8 indicates a member function pointer.
            if (textWindow.TryPeekChar(out c))
            {
                if (char.IsDigit(c))
                {
                    textWindow.AdvanceChar();

                    if (c != '6' && c != '8')
                    {
                        isMemberPointer = default;
                        return false;
                    }

                    isMemberPointer = c == '8';
                    return true;
                }
            }

            if (textWindow.TryAdvance('$'))
            {
                //Some type of managed value, e.g. ^
                switch (textWindow.PeekChar())
                {
                    case 'A': //^
                        textWindow.AdvanceChar();
                        break;

                    case 'B': //Pinned?
                        Debug.Assert(false); //Figure out what this looks like, and if there's any other characters we need to look at
                        isMemberPointer = default;
                        return false;

                    default:
                        //Some type of array. What comes next is the rank
                        isMemberPointer = false;
                        return true;
                }
            }

            textWindow.TryAdvance('E'); //64-bit
            textWindow.TryAdvance('I'); //restrict
            textWindow.TryAdvance('F'); //unaligned

            if (textWindow.IsEmpty)
            {
                isMemberPointer = default;
                return false;
            }

            c = textWindow.PeekChar();

            //The next value should be either ABCD (non-member) or QRST (member).
            switch (c)
            {
                case 'A':
                case 'B':
                case 'C':
                case 'D':
                    isMemberPointer = false;
                    return true;

                case 'Q':
                case 'R':
                case 'S':
                case 'T':
                    isMemberPointer = true;
                    return true;

                default:
                    isMemberPointer = default;
                    return false;
            }
        }

        private static bool TryParsePrimitiveType(ref TextWindow textWindow, out PrimitiveTypeNode primitiveType)
        {
            if (textWindow.AdvanceIfMatches("$$T"))
            {
                primitiveType = textWindow.AllocPrimitiveType(PrimitiveKind.Nullptr);
                return true;
            }

            if (!textWindow.TryNextChar(out var c))
            {
                primitiveType = default;
                return false;
            }

            PrimitiveKind kind;

            switch (c)
            {
                case 'X':
                    kind = PrimitiveKind.Void;
                    break;

                case 'D':
                    kind = PrimitiveKind.Char;
                    break;

                case 'C':
                    kind = PrimitiveKind.Schar;
                    break;

                case 'E':
                    kind = PrimitiveKind.Uchar;
                    break;

                case 'F':
                    kind = PrimitiveKind.Short;
                    break;

                case 'G':
                    kind = PrimitiveKind.Ushort;
                    break;

                case 'H':
                    kind = PrimitiveKind.Int;
                    break;

                case 'I':
                    kind = PrimitiveKind.Uint;
                    break;

                case 'J':
                    kind = PrimitiveKind.Long;
                    break;

                case 'K':
                    kind = PrimitiveKind.Ulong;
                    break;

                case 'M':
                    kind = PrimitiveKind.Float;
                    break;

                case 'N':
                    kind = PrimitiveKind.Double;
                    break;

                case 'O':
                    kind = PrimitiveKind.Ldouble;
                    break;

                case '_':
                    if (!textWindow.TryNextChar(out var c2))
                    {
                        primitiveType = default;
                        return false;
                    }

                    switch (c2)
                    {
                        case 'N':
                            kind = PrimitiveKind.Bool;
                            break;

                        case 'J':
                            kind = PrimitiveKind.Int64;
                            break;

                        case 'K':
                            kind = PrimitiveKind.Uint64;
                            break;

                        case 'W':
                            kind = PrimitiveKind.Wchar;
                            break;

                        case 'Q':
                            kind = PrimitiveKind.Char8;
                            break;

                        case 'S':
                            kind = PrimitiveKind.Char16;
                            break;

                        case 'U':
                            kind = PrimitiveKind.Char32;
                            break;

                        case 'P':
                            kind = PrimitiveKind.Auto;
                            break;

                        case 'T':
                            kind = PrimitiveKind.DecltypeAuto;
                            break;

                        default:
                            primitiveType = default;
                            return false;
                    }

                    break;

                default:
                    primitiveType = default;
                    return false;
            }

            primitiveType = textWindow.AllocPrimitiveType(kind);
            return true;
        }

        private static bool TryParseCustomType(ref TextWindow textWindow, out CustomTypeNode customType)
        {
            customType = default;

            if (!textWindow.TryAdvance('?'))
                return false;

            if (!TryParseUnqualifiedTypeName(ref textWindow, true, out var identifier))
                return false;

            if (!textWindow.TryAdvance('@'))
                return false;

            customType = textWindow.AllocCustomType(identifier);
            return true;
        }

        private static bool TryParseClassType(ref TextWindow textWindow, out TagTypeNode tagType)
        {
            tagType = default;

            if (!textWindow.TryNextChar(out var c))
                return false;

            TagKind kind;

            switch (c)
            {
                case 'T':
                    kind = TagKind.Union;
                    break;

                case 'U':
                    kind = TagKind.Struct;
                    break;

                case 'V':
                    kind = TagKind.Class;
                    break;

                case 'W':
                    if (!textWindow.TryAdvance('4'))
                        return false;

                    kind = TagKind.Enum;
                    break;

                default:
                    return false;
            }

            if (!TryParseFullyQualifiedTypeName(ref textWindow, out var qualifiedName))
                return false;

            tagType = textWindow.AllocTagType(kind, qualifiedName);
            return true;
        }

        private static bool TryParsePointerType(ref TextWindow textWindow, out PointerTypeNode pointerType)
        {
            pointerType = default;

            ParsePointerCVQualifiers(ref textWindow, out var qualifiers, out var affinity);

            if (textWindow.TryAdvance('6'))
            {
                if (!TryParseFunctionType(ref textWindow, hasThisQuals: false, out var functionType))
                    return false;

                pointerType = textWindow.AllocPointerType(qualifiers, affinity, functionType);
                return true;
            }

            if (!TryParseManagedQualifiers(ref textWindow, ref affinity))
                return false;

            //We store these qualifiers in the regular qualifiers; only when we're adding more qualifiers later on do we use ExtQualifiers
            var extQualifiers = ParsePointerExtQualifiers(ref textWindow);

            //Clang has a __ptrauth feature; MSVC does not, so we don't support that

            if (!TryParseType(ref textWindow, QualifierMangleMode.Mangle, out var type))
                return false;

            pointerType = textWindow.AllocPointerType(qualifiers | extQualifiers, affinity, type);
            return true;
        }

        private static bool TryParseMemberPointerType(ref TextWindow textWindow, out PointerTypeNode pointerType)
        {
            pointerType = default;

            ParsePointerCVQualifiers(ref textWindow, out var qualifiers, out var affinity);

            var extQualifiers = ParsePointerExtQualifiers(ref textWindow);

            QualifiedNameNode classParent;
            TypeNode pointee;

            if (textWindow.TryAdvance('8'))
            {
                if (!TryParseFullyQualifiedTypeName(ref textWindow, out classParent))
                    return false;

                if (!TryParseFunctionType(ref textWindow, hasThisQuals: true, out var rawPointee))
                    return false;

                pointee = rawPointee;
            }
            else
            {
                if (!TryParseQualifiers(ref textWindow, out var pointeeQualifiers, out var isMember))
                    return false;

                if (!TryParseFullyQualifiedTypeName(ref textWindow, out classParent))
                    return false;

                if (!TryParseType(ref textWindow, QualifierMangleMode.Drop, out pointee))
                    return false;

                if (pointee != null)
                    pointee.Qualifiers = pointeeQualifiers;
            }

            pointerType = textWindow.AllocPointerType(qualifiers | extQualifiers, affinity, pointee, classParent);
            return true;
        }

        private static bool TryParseFunctionType(ref TextWindow textWindow, bool hasThisQuals, out FunctionSignatureNode functionSignature)
        {
            Qualifiers qualifiers = default;
            Qualifiers extQualifiers = default;
            FunctionRefQualifier refQualifier = default;
            functionSignature = default;

            if (hasThisQuals)
            {
                var affinity = PointerAffinity.Pointer;

                if (!TryParseManagedQualifiers(ref textWindow, ref affinity))
                    return false;

                extQualifiers = ParsePointerExtQualifiers(ref textWindow);
                refQualifier = ParseFunctionRefQualifier(ref textWindow);

                if (!TryParseQualifiers(ref textWindow, out var extraQualifiers, out _))
                    return false;

                qualifiers |= extraQualifiers;
            }

            if (!TryParseCallingConvention(ref textWindow, out var callingConvention))
                return false;

            TypeNode returnType = null;

            //If the next character is @, it's a structor, which means it doesn't hjave a return type
            if (!textWindow.TryAdvance('@'))
            {
                if (!TryParseType(ref textWindow, QualifierMangleMode.Result, out returnType))
                    return false;
            }

            if (!TryParseFunctionParameterList(ref textWindow, out var isVariadic, out var parameterList))
                return false;

            if (!TryParseThrowSpecification(ref textWindow, out var isNoExcept))
                return false;

            functionSignature = textWindow.AllocFunctionSignature(
                qualifiers,
                extQualifiers,
                refQualifier,
                callingConvention,
                returnType,
                parameterList,
                isVariadic,
                isNoExcept
            );

            return true;
        }

        private static bool TryParseArrayType(ref TextWindow textWindow, out ArrayTypeNode arrayType)
        {
            arrayType = default;

            textWindow.AdvanceChar();

            if (!TryParseNumber(ref textWindow, out var isNegative, out var rank))
                return false;

            if (isNegative || rank == 0)
                return false;

            var dimensions = new PooledList<Node>((int) rank);

            try
            {
                for (ulong i = 0; i < rank; i++)
                {
                    if (!TryParseNumber(ref textWindow, out var isDimensionNegative, out var dimension))
                        return false;

                    if (isDimensionNegative)
                        return false;

                    var dimensionNode = textWindow.AllocIntegerLiteral(dimension, isNegative);
                    dimensions.Add(dimensionNode);
                }

                var nodeArray = textWindow.AllocNodeArray(ref dimensions);
                arrayType = textWindow.AllocArrayType(nodeArray);
            }
            finally
            {
                //Ownership of the rented array may have been transferred to the NodeArrayNode
                dimensions.Dispose();
            }

            if (textWindow.AdvanceIfMatches("$$C"))
            {
                if (!TryParseQualifiers(ref textWindow, out var qualifiers, out var isMember))
                    return false;

                if (isMember)
                    return false;

                arrayType.Qualifiers = qualifiers;
            }

            if (!TryParseType(ref textWindow, QualifierMangleMode.Drop, out var elementType))
                return false;

            arrayType.ElementType = elementType;

            return true;
        }

        private static bool TryParseFunctionParameterList(ref TextWindow textWindow, out bool isVariadic, out NodeArrayNode nodeArray)
        {
            nodeArray = default;
            isVariadic = default;

            if (textWindow.TryAdvance('X'))
            {
                //IsVariadic: false
                return true;
            }

            var nodes = new PooledList<Node>();

            try
            {
                while (true)
                {
                    var c = textWindow.PeekChar();

                    if (c == '@' || c == 'Z')
                        break;

                    if (char.IsDigit(c))
                    {
                        var n = c - '0';

                        if (n >= textWindow.BackRefFunctionParams.Count)
                            return false;

                        nodes.Add(textWindow.BackRefFunctionParams[n]);
                        textWindow.AdvanceChar();
                        continue;
                    }

                    var oldPosition = textWindow.Position;

                    if (!TryParseType(ref textWindow, QualifierMangleMode.Drop, out var type))
                        return false;

                    var charsConsumed = textWindow.Position - oldPosition;

                    //Single letter types aren't stored as backreferences beacuse memorizing them doesn't save anything
                    if (textWindow.BackRefFunctionParams.Count <= 9 && charsConsumed > 1)
                        textWindow.BackRefFunctionParams.Add(type);

                    nodes.Add(type);
                }

                nodeArray = textWindow.AllocNodeArray(ref nodes);

                //We either end in @ or Z. If we end in @Z, the Z after the @ is something else

                if (textWindow.TryAdvance('@'))
                {
                    isVariadic = false;
                    return true;
                }

                if (textWindow.TryAdvance('Z'))
                {
                    isVariadic = true;
                    return true;
                }

                return false;
            }
            finally
            {
                //If we successfully created a NodeArrayNode, ownership of the rented array was transferred from the PooledList
                //to the NodeArrayNode, and will be returned when the arena cleans up the node
                nodes.Dispose();
            }
        }

        private static bool TryParseTemplateParameterList(ref TextWindow textWindow, out NodeArrayNode nodeArray)
        {
            nodeArray = default;

            var nodes = new PooledList<Node>();

            try
            {
                TemplateParameterReferenceNode templateParameterReference = null;

                while (textWindow.PeekChar() != '@')
                {
                    if (textWindow.AdvanceIfMatches("$S") || textWindow.AdvanceIfMatches("$$V") || textWindow.AdvanceIfMatches("$$$V") || textWindow.AdvanceIfMatches("$$Z"))
                    {
                        //Parameter pack separator
                        continue;
                    }

                    var isAutoNTTP = false;

                    if (textWindow.AdvanceIfMatches("$M"))
                    {
                        //IsAutoNTTP
                        isAutoNTTP = true;

                        //Apparently this value doesn't get printed
                        if (!TryParseType(ref textWindow, QualifierMangleMode.Drop, out _))
                            return false;
                    }

                    if (textWindow.AdvanceIfMatches("$$Y"))
                    {
                        //Template alias

                        if (!TryParseFullyQualifiedTypeName(ref textWindow, out var item))
                            return false;

                        nodes.Add(item);
                    }
                    else if (textWindow.AdvanceIfMatches("$$B"))
                    {
                        //Array

                        if (!TryParseType(ref textWindow, QualifierMangleMode.Drop, out var item))
                            return false;

                        nodes.Add(item);
                    }
                    else if (textWindow.AdvanceIfMatches("$$C"))
                    {
                        //Type has qualifiers

                        if (!TryParseType(ref textWindow, QualifierMangleMode.Mangle, out var item))
                            return false;

                        nodes.Add(item);
                    }
                    else if (IsPointerToMember(ref textWindow, !isAutoNTTP))
                    {
                        templateParameterReference = textWindow.AllocTemplateParameterReference();
                        templateParameterReference.IsMemberPointer = true;

                        if (!isAutoNTTP)
                            textWindow.AdvanceChar(); //Remove leading '$'

                        var inheritanceSpecifier = textWindow.NextChar();

                        SymbolNode symbol = null;

                        if (textWindow.PeekChar() == '?')
                        {
                            if (!TryParseInternal(ref textWindow, out symbol))
                                return false;

                            if (symbol.Name == null)
                                return false;

                            MemorizeIdentifier(ref textWindow, symbol.Name.UnqualifiedIdentifier);
                        }

                        var thunkOffsets = ArrayPool<long>.Shared.Rent(3);
                        int numThunkOffsets = 0;

                        long offset;

                        try
                        {
                            //There may be at most 3 thunk offsets
                            switch (inheritanceSpecifier)
                            {
                                case 'J': //Unspecified inheritance
                                    if (!TryParseSigned(ref textWindow, out offset))
                                        return false;

                                    thunkOffsets[numThunkOffsets++] = offset;
                                    goto case 'I';

                                case 'I': //Virtual inheritance
                                    if (!TryParseSigned(ref textWindow, out offset))
                                        return false;

                                    thunkOffsets[numThunkOffsets++] = offset;
                                    goto case 'H';

                                case 'H':
                                    if (!TryParseSigned(ref textWindow, out offset))
                                        return false;

                                    thunkOffsets[numThunkOffsets++] = offset;
                                    goto case '1';

                                case '1':
                                    break;

                                default:
                                    //IsPointerToMember should have checked for all of the possible values
                                    Debug.Assert(false);
                                    return false;
                            }

                            if (numThunkOffsets == 0)
                                templateParameterReference.ThunkOffsets = Array.Empty<long>();
                            else
                            {
                                var arr = new long[numThunkOffsets];
                                Array.Copy(thunkOffsets, 0, arr, 0, numThunkOffsets);
                                templateParameterReference.ThunkOffsets = arr;
                            }
                        }
                        finally
                        {
                            ArrayPool<long>.Shared.Return(thunkOffsets);
                        }

                        templateParameterReference.Affinity = PointerAffinity.Pointer;
                        templateParameterReference.Symbol = symbol;

                        nodes.Add(templateParameterReference);
                    }
                    else if (textWindow.StartsWith("$E?"))
                    {
                        textWindow.AdvanceChar(2);

                        var templateParameterReferenceNode = textWindow.AllocTemplateParameterReference();

                        if (!TryParseInternal(ref textWindow, out var symbolNode))
                            return false;

                        templateParameterReference.Symbol = symbolNode;
                        templateParameterReference.Affinity = PointerAffinity.Reference;

                        nodes.Add(templateParameterReferenceNode);
                    }
                    else if (IsDataMemberPointer(ref textWindow, !isAutoNTTP))
                    {
                        var templateParameterReferenceNode = textWindow.AllocTemplateParameterReference();

                        if (!isAutoNTTP)
                            textWindow.AdvanceChar(); //Remove leading '$'

                        var inheritanceSpecifier = textWindow.NextChar();

                        var thunkOffsets = ArrayPool<long>.Shared.Rent(3);
                        int numThunkOffsets = 0;

                        long offset;

                        try
                        {
                            switch (inheritanceSpecifier)
                            {
                                case 'G': //Unspecified inheritance
                                    if (!TryParseSigned(ref textWindow, out offset))
                                        return false;

                                    thunkOffsets[numThunkOffsets++] = offset;
                                    goto case 'F';

                                case 'F': //Unspecified inheritance
                                    if (!TryParseSigned(ref textWindow, out offset))
                                        return false;

                                    thunkOffsets[numThunkOffsets++] = offset;

                                    if (!TryParseSigned(ref textWindow, out offset))
                                        return false;

                                    thunkOffsets[numThunkOffsets++] = offset;

                                    break;

                                default:
                                    //IsDataMemberPointer should have checked for all of the possible values
                                    Debug.Assert(false);
                                    return false;
                            }

                            if (numThunkOffsets == 0)
                                templateParameterReference.ThunkOffsets = Array.Empty<long>();
                            else
                            {
                                var arr = new long[numThunkOffsets];
                                Array.Copy(thunkOffsets, 0, arr, 0, numThunkOffsets);
                                templateParameterReference.ThunkOffsets = arr;
                            }
                        }
                        finally
                        {
                            ArrayPool<long>.Shared.Return(thunkOffsets);
                        }

                        templateParameterReferenceNode.IsMemberPointer = true;
                        nodes.Add(templateParameterReferenceNode);
                    }
                    else if ((!isAutoNTTP ? textWindow.AdvanceIfMatches("$0") : textWindow.AdvanceIfMatches("0")))
                    {
                        //Integral non-type template parameter

                        if (!TryParseNumber(ref textWindow, out var isNegative, out var number))
                            return false;

                        nodes.Add(textWindow.AllocIntegerLiteral(number, isNegative));
                    }
                    else
                    {
                        if (!TryParseType(ref textWindow, QualifierMangleMode.Drop, out var node))
                            return false;

                        nodes.Add(node);
                    }
                }

                textWindow.AdvanceChar(); //Advance over the @

                nodeArray = textWindow.AllocNodeArray(ref nodes);
            }
            finally
            {
                nodes.Dispose();
            }

            return true;
        }

        private static bool IsPointerToMember(ref TextWindow textWindow, bool hasDollar)
        {
            var i = 0;

            if (hasDollar)
            {
                if (textWindow.PeekChar() != '$')
                    return false;

                i++;
            }

            switch (textWindow.PeekChar(i))
            {
                case '1':
                case 'H':
                case 'I':
                case 'J':
                    return true;

                default:
                    return false;
            }
        }

        private static bool IsDataMemberPointer(ref TextWindow textWindow, bool hasDollar)
        {
            var i = 0;

            if (hasDollar)
            {
                if (textWindow.PeekChar() != '$')
                    return false;

                i++;
            }

            switch (textWindow.PeekChar(i))
            {
                case 'F':
                case 'G':
                    return true;

                default:
                    return false;
            }
        }

        internal static bool TryParseNumber(ref TextWindow textWindow, out bool isNegative, out ulong number)
        {
            isNegative = default;
            number = default;

            if (!textWindow.TryNextChar(out var c))
                return false;

            isNegative = false;

            if (c == '?')
            {
                isNegative = true;

                if (!textWindow.TryNextChar(out c))
                    return false;
            }

            if (char.IsDigit(c))
            {
                number = (ulong) (c - '0' + 1); //The number you see in the mangled name is 1 less than what it's actually meant to be
            }
            else
            {
                var at = textWindow.FindChar('@');

                if (at == -1)
                    return false;

                number = 0;

                var i = 0;

                do
                {
                    if ('A' <= c && c <= 'P')
                    {
                        number = (number << 4) + (ulong) (c - 'A');
                    }
                    else
                        return false;

                    c = textWindow.NextChar();
                    i++;
                } while (i <= at);
            }

            return true;
        }

        private static bool TryParseUnsigned(ref TextWindow textWindow, out ulong value)
        {
            if (!TryParseNumber(ref textWindow, out var isNegative, out value))
                return false;

            if (isNegative)
                return false;

            return true;
        }

        private static bool TryParseSigned(ref TextWindow textWindow, out long value)
        {
            value = default;

            if (!TryParseNumber(ref textWindow, out var isNegative, out var rawNumber))
                return false;

            if (rawNumber > ulong.MaxValue)
                return false;

            value = isNegative ? -((long) rawNumber) : (long) rawNumber;

            return true;
        }

        private static void MemorizeString(ref TextWindow textWindow, FixedUtf8String str)
        {
            if (textWindow.BackRefNames.Count >= 10)
                return;

            for (var i = 0; i < textWindow.BackRefNames.Count; i++)
            {
                if (textWindow.BackRefNames[i].key.AsSpan().SequenceEqual(str.AsSpan()))
                    return;
            }

            textWindow.BackRefNames.Add((str, textWindow.AllocNamedIdentifier(str)));
        }

        private static unsafe void MemorizeIdentifier(ref TextWindow textWindow, IdentifierNode identifier)
        {
            var ptr = stackalloc char[MaxSymbolName];
            var builder = new Utf8StringBuilder(new Span<byte>(ptr, MaxSymbolName));

            try
            {
                identifier.Output(ref builder, UNDNAME.UNDNAME_COMPLETE);

                var key = builder.ToPointer();
                textWindow.AddPointer(key);

                if (textWindow.BackRefNames.Count >= 10)
                    return;

                for (var i = 0; i < textWindow.BackRefNames.Count; i++)
                {
                    if (textWindow.BackRefNames[i].key.AsSpan().SequenceEqual(key.AsSpan()))
                        return;
                }

                textWindow.BackRefNames.Add((key, identifier));
            }
            finally
            {
                builder.Dispose();
            }
        }

        private static bool TryParseFullyQualifiedTypeName(ref TextWindow textWindow, out QualifiedNameNode qualifiedName)
        {
            //Parses a type name in the form of A@B@C@@ which represents C::B::A

            qualifiedName = default;

            if (!TryParseUnqualifiedTypeName(ref textWindow, true, out var identifier))
                return false;

            if (!TryParseNameScopeChain(ref textWindow, identifier, out qualifiedName))
                return false;

            return true;
        }

        private static bool TryParseFullyQualifiedSymbolName(ref TextWindow textWindow, out QualifiedNameNode qualifiedName)
        {
            qualifiedName = default;

            if (!TryParseUnqualifiedSymbolName(ref textWindow, NameBackRefBehavior.Simple, out var identifier))
                return false;

            if (!TryParseNameScopeChain(ref textWindow, identifier, out qualifiedName))
                return false;

            if (identifier.Kind == NodeKind.StructorIdentifier)
            {
                if (qualifiedName.Components.Count < 2)
                    return false;

                var structorIdentifier = (StructorIdentifierNode) identifier;
                var classNode = qualifiedName.Components[qualifiedName.Components.Count - 2];
                structorIdentifier.Class = (IdentifierNode) classNode;
            }

            return true;
        }

        private static bool TryParseUnqualifiedTypeName(ref TextWindow textWindow, bool memorize, out IdentifierNode identifier)
        {
            var c = textWindow.PeekChar();

            if (char.IsDigit(c))
            {
                return TryParseBackRefName(ref textWindow, out identifier);
            }
            else if (c == '?' && textWindow.PeekChar(1) == '$')
            {
                return TryParseTemplateInstantiationName(ref textWindow, NameBackRefBehavior.Template, out identifier);
            }
            else
            {
                return TryParseSimpleName(ref textWindow, memorize, out identifier);
            }
        }

        private static bool TryParseUnqualifiedSymbolName(ref TextWindow textWindow, NameBackRefBehavior backRefBehavior, out IdentifierNode identifier)
        {
            var c = textWindow.PeekChar();

            if (char.IsDigit(c))
            {
                return TryParseBackRefName(ref textWindow, out identifier);
            }
            else if (c == '?')
            {
                if (textWindow.PeekChar(1) == '$')
                    return TryParseTemplateInstantiationName(ref textWindow, backRefBehavior, out identifier);
                else
                    return TryParseFunctionIdentifierCode(ref textWindow, out identifier);
            }
            else
            {
                return TryParseSimpleName(ref textWindow, (backRefBehavior & NameBackRefBehavior.Simple) != 0, out identifier);
            }
        }

        private static bool TryParseNameScopeChain(
            ref TextWindow textWindow,
            IdentifierNode unqualifiedName,
            out QualifiedNameNode qualifiedName)
        {
            qualifiedName = default;

            var nodes = new PooledList<Node>();
            nodes.Add(unqualifiedName);

            try
            {
                while (true)
                {
                    if (textWindow.TryAdvance('@'))
                    {
                        nodes.Reverse();

                        var components = textWindow.AllocNodeArray(ref nodes);

                        qualifiedName = textWindow.AllocQualifiedName(components);
                        return true;
                    }
                    else if (textWindow.IsEmpty)
                        return false;
                    else
                    {
                        if (!TryParseNameScopePiece(ref textWindow, out var node))
                            return false;

                        nodes.Add(node);
                    }
                }
            }
            finally
            {
                //Ownership of the rented array may have been transferred to the NodeArrayNode
                nodes.Dispose();
            }
        }

        private static bool TryParseNameScopePiece(ref TextWindow textWindow, out IdentifierNode identifier)
        {
            var c = textWindow.PeekChar();

            if (char.IsDigit(c))
                return TryParseBackRefName(ref textWindow, out identifier);

            if (c == '?')
            {
                switch (textWindow.PeekChar(1))
                {
                    case '$':
                        return TryParseTemplateInstantiationName(ref textWindow, NameBackRefBehavior.Template, out identifier);

                    case 'A':
                        return TryParseAnonymousNamespaceName(ref textWindow, out identifier);
                }
            }

            if (StartsWithLocalScopePattern(textWindow, out var isWinRTBase)) //Not passed by ref
                return TryParseLocallyScopedNamePiece(ref textWindow, out identifier);
            else if (isWinRTBase)
                return TryParseWinRTBaseName(ref textWindow, out identifier);
            else
                return TryParseSimpleName(ref textWindow, true, out identifier);
        }

        private static bool TryParseBackRefName(ref TextWindow textWindow, out IdentifierNode identifier)
        {
            Debug.Assert(!textWindow.IsEmpty);

            var i = textWindow.NextChar() - '0';

            if (i >= textWindow.BackRefNames.Count)
            {
                identifier = default;
                return false;
            }

            identifier = textWindow.BackRefNames[i].node;

            return true;
        }

        private static bool TryParseTemplateInstantiationName(ref TextWindow textWindow, NameBackRefBehavior backRefBehavior, out IdentifierNode identifier)
        {
            Debug.Assert(textWindow.PeekChar() == '?' && textWindow.PeekChar(1) == '$');
            textWindow.AdvanceChar(2);

            var oldBackRefFunctionParams = textWindow.BackRefFunctionParams;
            var oldBackRefNames = textWindow.BackRefNames;

            textWindow.BackRefFunctionParams = default;
            textWindow.BackRefNames = default;

            if (!TryParseUnqualifiedSymbolName(ref textWindow, NameBackRefBehavior.Simple, out identifier))
                return false;

            if (!TryParseTemplateParameterList(ref textWindow, out var templateParameters))
                return false;

            identifier.TemplateParameters = templateParameters;

            textWindow.BackRefFunctionParams.Dispose();
            textWindow.BackRefNames.Dispose();

            textWindow.BackRefFunctionParams = oldBackRefFunctionParams;
            textWindow.BackRefNames = oldBackRefNames;

            if ((backRefBehavior & NameBackRefBehavior.Template) != 0)
            {
                switch (identifier.Kind)
                {
                    case NodeKind.ConversionOperatorIdentifier:
                    case NodeKind.StructorIdentifier:
                        return false;
                }

                MemorizeIdentifier(ref textWindow, identifier);
            }

            return true;
        }

        private static bool TryParseFunctionIdentifierCode(ref TextWindow textWindow, out IdentifierNode identifier)
        {
            textWindow.AdvanceChar(); //?

            if (textWindow.AdvanceIfMatches("__"))
                return TryParseFunctionIdentifierCode(ref textWindow, FunctionIdentifierCodeGroup.DoubleUnder, out identifier);

            if (textWindow.TryAdvance('_'))
                return TryParseFunctionIdentifierCode(ref textWindow, FunctionIdentifierCodeGroup.Under, out identifier);

            return TryParseFunctionIdentifierCode(ref textWindow, FunctionIdentifierCodeGroup.Basic, out identifier);
        }

        private static bool TryParseFunctionIdentifierCode(ref TextWindow textWindow, FunctionIdentifierCodeGroup group, out IdentifierNode identifier)
        {
            var c = textWindow.NextChar();

            switch (group)
            {
                case FunctionIdentifierCodeGroup.Basic:
                    switch (c)
                    {
                        case '0':
                        case '1':
                            identifier = ParseStructorIdentifier(ref textWindow, c == '1');
                            return true;

                        case 'B':
                            identifier = ParseConversionOperatorIdentifier(ref textWindow);
                            return true;

                        default:
                            return TryTranslateIntrinsicFunctionCode(ref textWindow, c, group, out identifier);
                    }

                case FunctionIdentifierCodeGroup.Under:
                    return TryTranslateIntrinsicFunctionCode(ref textWindow, c, group, out identifier);

                case FunctionIdentifierCodeGroup.DoubleUnder:
                    switch (c)
                    {
                        case 'K':
                            return TryParseLiteralOperatorIdentifier(ref textWindow, out identifier);

                        default:
                            return TryTranslateIntrinsicFunctionCode(ref textWindow, c, group, out identifier);
                    }

                default:
                    //All known enum values should be handled
                    Debug.Assert(false);
                    identifier = default;
                    return false;
            }
        }

        private static bool TryTranslateIntrinsicFunctionCode(
            ref TextWindow textWindow,
            char c,
            FunctionIdentifierCodeGroup group,
            out IdentifierNode intrinsicFunctionIdentifier)
        {
            if (!(c >= '0' && c <= '9') && !(c >= 'A' && c <= 'Z'))
            {
                intrinsicFunctionIdentifier = default;
                return false;
            }

            int index = (c >= '0' && c <= '9') ? (c - '0') : (c - 'A' + 10);

            switch (group)
            {
                case FunctionIdentifierCodeGroup.Basic:
                    intrinsicFunctionIdentifier = textWindow.AllocIntrinsicFunctionIdentifier(Basic[index]);
                    return true;

                case FunctionIdentifierCodeGroup.Under:
                    intrinsicFunctionIdentifier = textWindow.AllocIntrinsicFunctionIdentifier(Under[index]);
                    return true;

                case FunctionIdentifierCodeGroup.DoubleUnder:
                    intrinsicFunctionIdentifier = textWindow.AllocIntrinsicFunctionIdentifier(DoubleUnder[index]);
                    return true;

                default:
                    //All enum values sbould be handled
                    Debug.Assert(false);
                    intrinsicFunctionIdentifier = default;
                    return false;
            }
        }

        private static StructorIdentifierNode ParseStructorIdentifier(ref TextWindow textWindow, bool isDestructor)
        {
            return textWindow.AllocStructorIdentifier(isDestructor);
        }

        private static ConversionOperatorIdentifierNode ParseConversionOperatorIdentifier(ref TextWindow textWindow)
        {
            return textWindow.AllocConversionOperatorIdentifier();
        }

        private static bool TryParseLiteralOperatorIdentifier(ref TextWindow textWindow, out IdentifierNode literalOperatorIdentifier)
        {
            if (!TryParseSimpleString(ref textWindow, false, out var name))
            {
                literalOperatorIdentifier = default;
                return false;
            }

            literalOperatorIdentifier = textWindow.AllocLiteralOperatorIdentifier(name);
            return true;
        }

        private static bool TryParseSpecialIntrinsic(ref TextWindow textWindow, out SymbolNode symbolNode)
        {
            if (textWindow.PeekChar() == '?' && textWindow.PeekChar(1) == '_')
            {
                switch (textWindow.PeekChar(2))
                {
                    case '7': //?_7
                        //Vftable
                        textWindow.AdvanceChar(3);
                        return TryParseSpecialTableSymbolNode(ref textWindow, Strings.vftable, out symbolNode);

                    case '8': //?_8
                        //Vbtable
                        textWindow.AdvanceChar(3);
                        return TryParseSpecialTableSymbolNode(ref textWindow, Strings.vbtable, out symbolNode);

                    case '9': //?_9
                        //VcallThunk
                        textWindow.AdvanceChar(3);
                        return TryParseVCallThunkNode(ref textWindow, out symbolNode);

                    case 'A': //?_A
                        //Typeof
                        textWindow.AdvanceChar(3);
                        Debug.Assert(false);
                        symbolNode = default;
                        return false; //Not yet supported

                    case 'B': //?_B
                        //LocalStaticGuard
                        textWindow.AdvanceChar(3);
                        return TryParseLocalStaticGuard(ref textWindow, isThread: false, out symbolNode);

                    case 'C': //?_C
                        //StringLiteralSymbol
                        textWindow.AdvanceChar(3);
                        return TryParseStringLiteral(ref textWindow, out symbolNode);

                    case 'P': //?_P
                        //UdtReturning
                        textWindow.AdvanceChar(3);
                        Debug.Assert(false);
                        symbolNode = default;
                        return false; //Not yet supported

                    case 'R':
                        switch (textWindow.PeekChar(3))
                        {
                            case '0': //?_R0
                                //RttiTypeDescriptor
                                textWindow.AdvanceChar(4);

                                if (!TryParseType(ref textWindow, QualifierMangleMode.Result, out var type))
                                {
                                    symbolNode = default;
                                    return false;
                                }

                                if (!textWindow.AdvanceIfMatches("@8"))
                                {
                                    symbolNode = default;
                                    return false;
                                }

                                if (!textWindow.IsEmpty)
                                {
                                    symbolNode = default;
                                    return false;
                                }

                                symbolNode = textWindow.AllocVariableSymbol(
                                    name: textWindow.AllocQualifiedName(
                                        textWindow.AllocNodeArray(textWindow.AllocNamedIdentifier(Strings.RTTITypeDescriptor))
                                    ),
                                    type: type
                                );
                                return true;

                            case '1': //?_R1
                                //RttiBaseClassDescriptor
                                textWindow.AdvanceChar(4);
                                return TryParseRttiBaseClassDescriptor(ref textWindow, out symbolNode);

                            case '2': //?_R2
                                //RttiBaseClassArray
                                textWindow.AdvanceChar(4);
                                return TryParseUntypedVariable(ref textWindow, Strings.RTTIBaseClassArray, out symbolNode);

                            case '3': //?_R3
                                //RttiClassHierarchyDescriptor
                                textWindow.AdvanceChar(4);
                                return TryParseUntypedVariable(ref textWindow, Strings.RTTIClassHierarchyDescriptor, out symbolNode);

                            case '4': //?_R4
                                //RttiCompleteObjLocator
                                textWindow.AdvanceChar(4);
                                return TryParseSpecialTableSymbolNode(ref textWindow, Strings.RTTICompleteObjectLocator, out symbolNode);

                            default:
                                break;
                        }
                        break;

                    case 'S': //?_S
                        //LocalVftable
                        textWindow.AdvanceChar(3);
                        return TryParseSpecialTableSymbolNode(ref textWindow, Strings.localvftable, out symbolNode);

                    case '_':
                        switch (textWindow.PeekChar(3))
                        {
                            case 'E': //?__E
                                //DynamicInitializer
                                textWindow.AdvanceChar(4);
                                return TryParseInitFiniStub(ref textWindow, isDestructor: false, out symbolNode);

                            case 'F': //?__F
                                //DynamicAtexitDestructor
                                textWindow.AdvanceChar(4);
                                return TryParseInitFiniStub(ref textWindow, isDestructor: true, out symbolNode);

                            case 'J': //?__J
                                //LocalStaticThreadGuard
                                textWindow.AdvanceChar(4);
                                return TryParseLocalStaticGuard(ref textWindow, isThread: true, out symbolNode);
                        }

                        break;

                    default:
                        break;
                }
            }

            symbolNode = null;
            return true;
        }

        private static bool TryParseSpecialTableSymbolNode(
            ref TextWindow textWindow,
            FixedUtf8String name,
            out SymbolNode symbolNode)
        {
            symbolNode = default;

            var namedIdentifier = textWindow.AllocNamedIdentifier(name);

            if (!TryParseNameScopeChain(ref textWindow, namedIdentifier, out var qualifiedName))
                return false;

            var specialTableSymbol = textWindow.AllocSpecialTableSymbol(qualifiedName);

            if (!textWindow.TryNextChar(out var c))
                return false;

            if (c != '6' && c != '7')
                return false;

            if (!TryParseQualifiers(ref textWindow, out var qualifiers, out var isMember))
                return false;

            specialTableSymbol.Qualifiers = qualifiers;

            if (!textWindow.TryAdvance('@'))
            {
                if (!TryParseFullyQualifiedTypeName(ref textWindow, out var targetName))
                    return false;

                specialTableSymbol.TargetName = targetName;
            }

            symbolNode = specialTableSymbol;
            return true;
        }

        private static bool TryParseLocalStaticGuard(
            ref TextWindow textWindow,
            bool isThread,
            out SymbolNode symbolNode)
        {
            symbolNode = default;

            var identifier = textWindow.AllocLocalStaticGuardIdentifier(isThread);

            if (!TryParseNameScopeChain(ref textWindow, identifier, out var qualifiedName))
                return false;

            var variable = textWindow.AllocLocalStaticGuardVariable(qualifiedName);

            if (textWindow.AdvanceIfMatches("4IA"))
                variable.IsVisible = false;
            else if (textWindow.TryAdvance('5'))
                variable.IsVisible = true;
            else
                return false;

            if (!textWindow.IsEmpty)
            {
                if (!TryParseUnsigned(ref textWindow, out var scopeIndex))
                    return false;

                identifier.ScopeIndex = scopeIndex;
            }

            symbolNode = variable;
            return true;
        }

        private static bool TryParseUntypedVariable(
            ref TextWindow textWindow,
            FixedUtf8String name,
            out SymbolNode symbolNode)
        {
            symbolNode = default;

            var namedIdentifier = textWindow.AllocNamedIdentifier(name);

            if (!TryParseNameScopeChain(ref textWindow, namedIdentifier, out var qualifiedName))
                return false;

            var variableSymbol = textWindow.AllocVariableSymbol();
            variableSymbol.Name = qualifiedName;

            if (!textWindow.TryAdvance('8'))
                return false;

            symbolNode = variableSymbol;
            return true;
        }

        private static bool TryParseRttiBaseClassDescriptor(ref TextWindow textWindow, out SymbolNode symbolNode)
        {
            symbolNode = default;

            var rttiBaseClassDescriptor = textWindow.AllocRttiBaseClassDescriptor();

            if (!TryParseUnsigned(ref textWindow, out var nvOffset))
                return false;

            if (!TryParseSigned(ref textWindow, out var vbPtrOffset))
                return false;

            if (!TryParseUnsigned(ref textWindow, out var vbTableOffset))
                return false;

            if (!TryParseUnsigned(ref textWindow, out var flags))
                return false;

            rttiBaseClassDescriptor.NVOffset = nvOffset;
            rttiBaseClassDescriptor.VBPtrOffset = vbPtrOffset;
            rttiBaseClassDescriptor.VBTableOffset = vbTableOffset;
            rttiBaseClassDescriptor.Flags = flags;

            var variableSymbol = textWindow.AllocVariableSymbol();

            if (!TryParseNameScopeChain(ref textWindow, rttiBaseClassDescriptor, out var name))
                return false;

            variableSymbol.Name = name;

            if (!textWindow.TryAdvance('8'))
                return false;

            symbolNode = variableSymbol;
            return true;
        }

        private static bool TryParseInitFiniStub(ref TextWindow textWindow, bool isDestructor, out SymbolNode symbolNode)
        {
            FunctionSymbolNode functionSymbol = default;
            symbolNode = default;

            var dynamicStructorIdentifier = textWindow.AllocDynamicStructorIdentifier(isDestructor);

            var isKnownStaticDataMember = false;

            if (textWindow.TryAdvance('?'))
                isKnownStaticDataMember = true;

            if (!TryParseDeclarator(ref textWindow, out var symbol))
                return false;

            if (symbol.Kind == NodeKind.VariableSymbol)
            {
                dynamicStructorIdentifier.Variable = (VariableSymbolNode) symbol;

                if (!TryParseFunctionEncoding(ref textWindow, out functionSymbol))
                    return false;
            }

            if (functionSymbol != null)
            {
                functionSymbol.Name = textWindow.AllocQualifiedName(
                    textWindow.AllocNodeArray(
                        dynamicStructorIdentifier
                    )
                );
            }
            else
            {
                if (isKnownStaticDataMember)
                    return false;

                functionSymbol = (FunctionSymbolNode) symbol;
                dynamicStructorIdentifier.Name = symbol.Name;

                functionSymbol.Name = textWindow.AllocQualifiedName(
                    textWindow.AllocNodeArray(
                        dynamicStructorIdentifier
                    )
                );
            }

            symbolNode = functionSymbol;
            return true;
        }

        private static bool TryParseWinRTBaseName(ref TextWindow textWindow, out IdentifierNode identifier)
        {
            /* ?__abi_Release@?QObject@Platform@@_IAsyncActionToAsyncOperationConverter@details@Concurrency@@WM@$AAGKXZ
             * translates to [thunk]:public: virtual unsigned long __stdcall Concurrency::details::_IAsyncActionToAsyncOperationConverter::[Platform::Object]::__abi_Release`adjustor{12}' (void)
             * the [Platform::Object] part is the WinRT Base Name */

            identifier = default;

            textWindow.AdvanceChar();

            //Don't know what Q means
            if (!textWindow.TryAdvance('Q'))
                return false;

            //Eat tokens until we hit a @@

            var nodes = new PooledList<Node>();

            try
            {
                while (true)
                {
                    if (textWindow.TryAdvance('@'))
                    {
                        nodes.Reverse();

                        var components = textWindow.AllocNodeArray(ref nodes);

                        identifier = textWindow.AllocWinRTQualifiedBaseName(components);
                        return true;
                    }
                    else if (textWindow.IsEmpty)
                        return false;
                    else
                    {
                        if (!TryParseNameScopePiece(ref textWindow, out var node))
                        {
                            identifier = default;
                            return false;
                        }

                        nodes.Add(node);
                    }
                }
            }
            finally
            {
                //Ownership of the rented array may have been transferred to the NodeArrayNode
                nodes.Dispose();
            }
        }

        private static bool TryParseSimpleName(ref TextWindow textWindow, bool memorize, out IdentifierNode identifier)
        {
            if (!TryParseSimpleString(ref textWindow, memorize, out var name))
            {
                identifier = default;
                return false;
            }

            identifier = textWindow.AllocNamedIdentifier(name);
            return true;
        }

        private static bool TryParseAnonymousNamespaceName(ref TextWindow textWindow, out IdentifierNode identifier)
        {
            textWindow.AdvanceChar(2); //?A

            identifier = textWindow.AllocNamedIdentifier(Strings.anonymousnamespace);

            var at = textWindow.FindChar('@');

            if (at == -1)
                return false;

            var namespaceKey = textWindow.ReadAndAdvance(at);

            MemorizeString(ref textWindow, namespaceKey);

            textWindow.AdvanceChar(); //Skip over the @

            return true;
        }

        private static unsafe bool TryParseLocallyScopedNamePiece(ref TextWindow textWindow, out IdentifierNode identifier)
        {
            identifier = default;

            //StartsWithLocalScopePattern should not be returning true if we didn't start with a ?
            if (!textWindow.TryAdvance('?'))
                return false;

            if (!TryParseNumber(ref textWindow, out var isNegative, out var number))
                return false;

            if (isNegative)
                return false;

            if (!textWindow.TryAdvance('?'))
                return false;

            //There is now a nested symbol inside of us. So we need to parse from the very top

            if (!TryParseInternal(ref textWindow, out var scope))
                return false;

            identifier = textWindow.AllocScopedIdentifier(scope, number);
            return true;
        }

        private static bool TryParseStringLiteral(ref TextWindow textWindow, out SymbolNode symbolNode)
        {
            symbolNode = default;

            if (!textWindow.AdvanceIfMatches("@_"))
                return false;

            if (!textWindow.TryNextChar(out var c))
                return false;

            var isWideChar = false;

            switch (c)
            {
                case '1':
                    isWideChar = true;
                    break;

                case '0':
                    break;

                default:
                    return false;
            }

            if (!TryParseNumber(ref textWindow, out var isNegative, out var stringByteSize))
                return false;

            if (isNegative)
                return false;

            if (isWideChar ? (stringByteSize < 2) : (stringByteSize < 1))
                return false;

            //Next should be a CRC32 consisting of 8 characters and a terminator
            var at = textWindow.FindChar('@');

            if (at == -1)
                return false;

            var encodedStringLiteral = textWindow.AllocEncodedStringLiteral();

            encodedStringLiteral.Crc32 = textWindow.ReadAndAdvance(at);

            textWindow.AdvanceChar(); //Advance over the @

            if (isWideChar)
            {
                encodedStringLiteral.Char = CharKind.Wchar;

                if (stringByteSize > 64)
                    encodedStringLiteral.IsTruncated = true;

                var builder = new Utf8StringBuilder();

                try
                {
                    while (!textWindow.TryAdvance('@'))
                    {
                        //Wide strings should have an even length
                        if (stringByteSize % 2 != 0)
                            return false;

                        if (!TryParseWCharLiteral(ref textWindow, out var @char))
                            return false;

                        if (stringByteSize != 2 || encodedStringLiteral.IsTruncated)
                            OutputEscapedChar(ref builder, @char);

                        stringByteSize -= 2;
                    }

                    var str = builder.ToPointer();
                    textWindow.AddPointer(str);

                    encodedStringLiteral.DecodedString = str;
                }
                finally
                {
                    builder.Dispose();
                }
            }
            else
            {
                var builder = new Utf8StringBuilder();

                try
                {
                    while (!textWindow.TryAdvance('@'))
                    {
                        if (!TryParseCharLiteral(ref textWindow, out var @char))
                            return false;

                        OutputEscapedChar(ref builder, @char);
                    }

                    if ((int) stringByteSize > builder.Length)
                        encodedStringLiteral.IsTruncated = true;

                    var str = builder.ToPointer();
                    textWindow.AddPointer(str);

                    encodedStringLiteral.DecodedString = str;
                }
                finally
                {
                    builder.Dispose();
                }
            }

            symbolNode = encodedStringLiteral;
            return true;
        }

        private static bool TryParseVCallThunkNode(ref TextWindow textWindow, out SymbolNode symbolNode)
        {
            symbolNode = default;

            var functionSymbol = textWindow.AllocFunctionSymbol();
            var vcallThunkIdentifier = textWindow.AllocVCallThunkIdentifier();

            var signature = textWindow.AllocThunkSignature();
            signature.FunctionClass = FunctionClass.NoParameterList;

            functionSymbol.Signature = signature;

            if (!TryParseNameScopeChain(ref textWindow, vcallThunkIdentifier, out var qualifiedName))
                return false;

            functionSymbol.Name = qualifiedName;

            if (!textWindow.AdvanceIfMatches("$B"))
                return false;

            if (!TryParseUnsigned(ref textWindow, out var offsetInVTable))
                return false;

            vcallThunkIdentifier.OffsetInVTable = offsetInVTable;

            if (!textWindow.TryAdvance('A'))
                return false;

            if (!TryParseCallingConvention(ref textWindow, out var callingConvention))
                return false;

            signature.CallingConvention = callingConvention;

            symbolNode = functionSymbol;
            return true;
        }

        private static bool TryParseSimpleString(ref TextWindow textWindow, bool memorize, out FixedUtf8String str)
        {
            var at = textWindow.FindChar('@');

            if (at <= 0)
            {
                str = default;
                return false;
            }

            str = textWindow.ReadAndAdvance(at);

            textWindow.AdvanceChar();

            if (memorize)
                MemorizeString(ref textWindow, str);

            return true;
        }

        private static bool TryParseFunctionClass(ref TextWindow textWindow, out FunctionClass functionClass)
        {
            functionClass = default;

            if (!textWindow.TryNextChar(out var c))
                return false;

            //After subtracting a base value from these characters, they're actually bit patterns that specify each of these
            //combinations in a more succinct manor

            //All values A-X represent members
            switch (c)
            {
                case '9':
                    functionClass = FunctionClass.ExternC | FunctionClass.NoParameterList;
                    break;
                case 'A':
                    functionClass = FunctionClass.Member | FunctionClass.Private;
                    break;
                case 'B':
                    functionClass = FunctionClass.Member | FunctionClass.Private | FunctionClass.Far;
                    break;
                case 'C':
                    functionClass = FunctionClass.Member | FunctionClass.Private | FunctionClass.Static;
                    break;
                case 'D':
                    functionClass = FunctionClass.Member | FunctionClass.Private | FunctionClass.Static | FunctionClass.Far;
                    break;
                case 'E':
                    functionClass = FunctionClass.Member | FunctionClass.Private | FunctionClass.Virtual;
                    break;
                case 'F':
                    functionClass = FunctionClass.Member | FunctionClass.Private | FunctionClass.Virtual | FunctionClass.Far;
                    break;
                case 'G':
                    functionClass = FunctionClass.Member | FunctionClass.Private | FunctionClass.Adjustor;
                    break;
                case 'H':
                    functionClass = FunctionClass.Member | FunctionClass.Private | FunctionClass.Adjustor | FunctionClass.Far;
                    break;

                case 'I':
                    functionClass = FunctionClass.Member | FunctionClass.Protected;
                    break;
                case 'J':
                    functionClass = FunctionClass.Member | FunctionClass.Protected | FunctionClass.Far;
                    break;
                case 'K':
                    functionClass = FunctionClass.Member | FunctionClass.Protected | FunctionClass.Static;
                    break;
                case 'L':
                    functionClass = FunctionClass.Member | FunctionClass.Protected | FunctionClass.Static | FunctionClass.Far;
                    break;
                case 'M':
                    functionClass = FunctionClass.Member | FunctionClass.Protected | FunctionClass.Virtual;
                    break;
                case 'N':
                    functionClass = FunctionClass.Member | FunctionClass.Protected | FunctionClass.Virtual | FunctionClass.Far;
                    break;
                case 'O':
                    functionClass = FunctionClass.Member | FunctionClass.Protected | FunctionClass.Virtual | FunctionClass.Adjustor;
                    break;
                case 'P':
                    functionClass = FunctionClass.Member | FunctionClass.Protected | FunctionClass.Virtual | FunctionClass.Adjustor | FunctionClass.Far;
                    break;

                case 'Q':
                    functionClass = FunctionClass.Member | FunctionClass.Public;
                    break;
                case 'R':
                    functionClass = FunctionClass.Member | FunctionClass.Public | FunctionClass.Far;
                    break;
                case 'S':
                    functionClass = FunctionClass.Member | FunctionClass.Public | FunctionClass.Static;
                    break;
                case 'T':
                    functionClass = FunctionClass.Member | FunctionClass.Public | FunctionClass.Static | FunctionClass.Far;
                    break;
                case 'U':
                    functionClass = FunctionClass.Member | FunctionClass.Public | FunctionClass.Virtual;
                    break;
                case 'V':
                    functionClass = FunctionClass.Member | FunctionClass.Public | FunctionClass.Virtual | FunctionClass.Far;
                    break;
                case 'W':
                    functionClass = FunctionClass.Member | FunctionClass.Public | FunctionClass.Virtual | FunctionClass.Adjustor;
                    break;
                case 'X':
                    functionClass = FunctionClass.Member | FunctionClass.Public | FunctionClass.Adjustor | FunctionClass.Far;
                    break;

                case 'Y':
                    functionClass = FunctionClass.Global;
                    break;
                case 'Z':
                    functionClass = FunctionClass.Global | FunctionClass.Far;
                    break;

                case '$':
                    {
                        //e.g. ??_E?$basic_stringstream@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@$4PPPPPPPM@A@EAAPEAXI@Z

                        var virtualFlags = FunctionClass.VirtualThisAdjust;

                        if (textWindow.TryAdvance('R'))
                        {
                            //e.g. ?ExecQuery@CWin32PNPEntity@@$R4BA@7PPPPPPPM@DI@EAAJPEAVMethodContext@@AEAVCFrameworkQuery@@J@Z
                            virtualFlags |= FunctionClass.VirtualThisAdjustEx;
                        }

                        if (!textWindow.TryNextChar(out var c2))
                            return false;

                        switch (c2)
                        {
                            case '0':
                                functionClass = FunctionClass.Private | FunctionClass.Virtual | virtualFlags;
                                break;
                            case '1':
                                functionClass = FunctionClass.Private | FunctionClass.Virtual | FunctionClass.Far | virtualFlags;
                                break;

                            case '2':
                                functionClass = FunctionClass.Protected | FunctionClass.Virtual | virtualFlags;
                                break;
                            case '3':
                                functionClass = FunctionClass.Protected | FunctionClass.Virtual | FunctionClass.Far | virtualFlags;
                                break;

                            case '4':
                                functionClass = FunctionClass.Public | FunctionClass.Virtual | virtualFlags;
                                break;
                            case '5':
                                functionClass = FunctionClass.Public | FunctionClass.Virtual | FunctionClass.Far | virtualFlags;
                                break;
                            default:
                                return false;
                        }
                    }
                    break;
                default:
                    return false;
            }

            return true;
        }

        private static bool TryParseCallingConvention(ref TextWindow textWindow, out CallingConv callingConvention)
        {
            if (!textWindow.TryNextChar(out var c))
            {
                callingConvention = default;
                return false;
            }

            switch (c)
            {
                case 'A':
                case 'B':
                    callingConvention = CallingConv.Cdecl;
                    return true;

                case 'C':
                case 'D':
                    callingConvention = CallingConv.Pascal;
                    return true;

                case 'E':
                case 'F':
                    callingConvention = CallingConv.Thiscall;
                    return true;

                case 'G':
                case 'H':
                    callingConvention = CallingConv.Stdcall;
                    return true;

                case 'I':
                case 'J':
                    callingConvention = CallingConv.Fastcall;
                    return true;

                case 'M':
                case 'N':
                    callingConvention = CallingConv.Clrcall;
                    return true;

                case 'O':
                case 'P':
                    callingConvention = CallingConv.Eabi;
                    return true;

                case 'Q':
                    callingConvention = CallingConv.Vectorcall;
                    break;

                //S and W are Clang specific

                default:
                    callingConvention = CallingConv.None;
                    break;
            }

            return true;
        }

        private static StorageClass ParseVariableStorageClass(ref TextWindow textWindow)
        {
            //Caller should have checked that we have a valid character
            var c = textWindow.NextChar();

            return c switch
            {
                '0' => StorageClass.PrivateStatic,
                '1' => StorageClass.ProtectedStatic,
                '2' => StorageClass.PublicStatic,
                '3' => StorageClass.Global,
                '4' => StorageClass.FunctionLocalStatic
            };
        }

        private static bool TryParseThrowSpecification(ref TextWindow textWindow, out bool isNoExcept)
        {
            if (textWindow.AdvanceIfMatches("_E"))
            {
                isNoExcept = true;
                return true;
            }

            if (textWindow.TryAdvance('Z'))
            {
                isNoExcept = false;
                return true;
            }

            isNoExcept = default;
            return false;
        }

        private static bool TryParseWCharLiteral(ref TextWindow textWindow, out char result)
        {
            result = default;

            if (!TryParseCharLiteral(ref textWindow, out var c1))
                return false;

            if (!TryParseCharLiteral(ref textWindow, out var c2))
                return false;

            result = (char) (((byte) c1 << 8) | (byte) c2);
            return true;
        }

        private static bool TryParseCharLiteral(ref TextWindow textWindow, out char result)
        {
            result = default;

            if (!textWindow.TryNextChar(out var c))
                return false;

            if (c != '?')
            {
                result = c;
                return true;
            }

            if (textWindow.TryAdvance('$'))
            {
                //Two hex digits

                if (!textWindow.TryNextChar(out var c1) || !textWindow.TryNextChar(out var c2))
                    return false;

                if (!IsRebasedHexDigit(c1) || !IsRebasedHexDigit(c2))
                    return false;

                var v1 = RebasedHexDigitToNumber(c1);
                var v2 = RebasedHexDigitToNumber(c2);

                result = (char) ((v1 << 4) | v2);
                return true;
            }

            c = textWindow.PeekChar();

            if (char.IsDigit(c))
            {
                textWindow.AdvanceChar();

                var @char = c switch
                {
                    '0' => ',',
                    '1' => '/',
                    '2' => '\\',
                    '3' => ':',
                    '4' => '.',
                    '5' => ' ',
                    '6' => '\n',
                    '7' => '\t',
                    '8' => '\'',
                    '9' => '-',
                };

                result = @char;
                return true;
            }

            if (c >= 'a' && c <= 'z')
            {
                //llvm reads this as hex characters, but when I inspect these it's all garbage, so for now we don't support this
                //Debug.Assert(false); //Implement support for this
                return false;
            }

            if (c >= 'A' && c <= 'Z')
            {
                //llvm reads this as hex characters, but when I inspect these it's all garbage, so for now we don't support this
                //Debug.Assert(false); //Implement support for this
                return false;
            }

            return false;
        }

        private static bool IsRebasedHexDigit(char c) => c >= 'A' && c <= 'P';

        private static byte RebasedHexDigitToNumber(char c) =>
            (byte) ((c <= 'J') ? (c - 'A') : (10 + c - 'K'));

        private static bool TryParseQualifiers(ref TextWindow textWindow, out Qualifiers qualifiers, out bool isMember)
        {
            if (!textWindow.TryNextChar(out var c))
            {
                qualifiers = default;
                isMember = default;
                return false;
            }

            switch (c)
            {
                //Member qualifiers
                case 'Q':
                    qualifiers = Qualifiers.None;
                    isMember = true;
                    break;

                case 'R':
                    qualifiers = Qualifiers.Const;
                    isMember = true;
                    break;

                case 'S':
                    qualifiers = Qualifiers.Volatile;
                    isMember = true;
                    break;

                case 'T':
                    qualifiers = Qualifiers.Const | Qualifiers.Volatile;
                    isMember = true;
                    break;

                //Non-member qualifiers
                case 'A':
                    qualifiers = Qualifiers.None;
                    isMember = false;
                    break;

                case 'B':
                    qualifiers = Qualifiers.Const;
                    isMember = false;
                    break;

                case 'C':
                    qualifiers = Qualifiers.Volatile;
                    isMember = false;
                    break;

                case 'D':
                    qualifiers = Qualifiers.Const | Qualifiers.Volatile;
                    isMember = false;
                    break;

                default:
                    qualifiers = default;
                    isMember = default;
                    return false;
            }

            return true;
        }

        private static void ParsePointerCVQualifiers(ref TextWindow textWindow, out Qualifiers qualifiers, out PointerAffinity affinity)
        {
            if (textWindow.AdvanceIfMatches("$$Q"))
            {
                qualifiers = Qualifiers.None;
                affinity = PointerAffinity.RValueReference;
                return;
            }

            var c = textWindow.NextChar();

            switch (c)
            {
                case 'A':
                    qualifiers = Qualifiers.None;
                    affinity = PointerAffinity.Reference;
                    return;

                case 'P':
                    qualifiers = Qualifiers.None;
                    affinity = PointerAffinity.Pointer;
                    break;

                case 'Q':
                    qualifiers = Qualifiers.Const;
                    affinity = PointerAffinity.Pointer;
                    break;

                case 'R':
                    qualifiers = Qualifiers.Volatile;
                    affinity = PointerAffinity.Pointer;
                    break;

                case 'S':
                    qualifiers = Qualifiers.Const | Qualifiers.Volatile;
                    affinity = PointerAffinity.Pointer;
                    break;

                default:
                    //This method should only be called in the first place for the letters listed above
                    Debug.Assert(false);
                    qualifiers = default;
                    affinity = default;
                    break;
            }
        }

        private static FunctionRefQualifier ParseFunctionRefQualifier(ref TextWindow textWindow)
        {
            if (textWindow.TryAdvance('G'))
                return FunctionRefQualifier.Reference;
            else if (textWindow.TryAdvance('H'))
                return FunctionRefQualifier.RValueReference;

            return FunctionRefQualifier.None;
        }

        private static void OutputEscapedChar(ref Utf8StringBuilder builder, char c)
        {
            switch (c)
            {
                case '\0':
                    builder.Append("\\0");
                    break;

                case '\'':
                    builder.Append("\\\'");
                    break;

                case '\"':
                    builder.Append("\\\"");
                    break;

                case '\\':
                    builder.Append("\\\\");
                    break;

                case '\a':
                    builder.Append("\\a");
                    break;

                case '\b':
                    builder.Append("\\b");
                    break;

                case '\f':
                    builder.Append("\\f");
                    break;

                case '\n':
                    builder.Append("\\n");
                    break;

                case '\r':
                    builder.Append("\\r");
                    break;

                case '\t':
                    builder.Append("\\t");
                    break;

                case '\v':
                    builder.Append("\\v");
                    break;

                default:
                    if (c >= 0x1f && c < 0x7f)
                        builder.Append(c);
                    break;
            }
        }

        private static int GuessCharByteSize()
        {
            throw new NotImplementedException();
        }

        private static int ParseMultiByteChar()
        {
            throw new NotImplementedException();
        }

        private static bool StartsWithLocalScopePattern(TextWindow textWindow, out bool isWinRTBase) //Don't pass by ref; we don't want to mutate the caller
        {
            isWinRTBase = false;

            if (!textWindow.TryAdvance('?'))
                return false;

            if (textWindow.PeekChar() == 'Q')
            {
                isWinRTBase = true;
                return false;
            }

            var nextQuestionMark = textWindow.FindChar('?');

            if (nextQuestionMark == -1)
                return false;

            char c;

            if (nextQuestionMark == 1)
            {
                //There's an expression like ?1? or ?@?
                c = textWindow.PeekChar();

                return c == '@' || char.IsDigit(c);
            }

            //An encoded number terminated with '@'?
            if (textWindow.PeekChar(nextQuestionMark - 1) != '@')
                return false;

            c = textWindow.PeekChar();

            if (c < 'B' || c > 'P')
                return false;

            for (var i = 1; i < nextQuestionMark - 1; i++) //Last char is the @
            {
                c = textWindow.PeekChar(i);

                if (c < 'A' || c > 'P')
                    return false;
            }

            return true;
        }
    }
}
