using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace PESpy
{
    static partial class Demangler
    {
        unsafe ref struct TextWindow
        {
            public const char InvalidCharacter = char.MaxValue;

            internal static DemanglerNodeArena? cachedArena;

            static TextWindow()
            {
                cachedArena = new DemanglerNodeArena();
            }

            private byte* buffer;
            private int length;

            internal int Position;

            public PooledList<TypeNode> BackRefFunctionParams;
            public PooledList<(FixedUtf8String key, IdentifierNode node)> BackRefNames;

            private PooledList<FixedUtf8String> strings; //We don't need to initialize this; if we add something to it, it will initialize itself

            private DemanglerNodeArena? arena;

            public bool IsEmpty => Position >= length;

            public TextWindow(byte* buffer, int length)
            {
                this.buffer = buffer;
                this.length = length;
                Position = 0;
                this.arena = Interlocked.Exchange(ref cachedArena, null);
            }

            /// <summary>
            /// Advance the current position by one. No guarantee that this
            /// position is valid.
            /// </summary>
            public void AdvanceChar() => Position++;

            /// <summary>
            /// Advance the current position by n. No guarantee that this position
            /// is valid.
            /// </summary>
            public void AdvanceChar(int n) => Position += n;

            /// <summary>
            /// Advances the text window if it currently pointing at the <paramref name="c"/> character.  Returns <see
            /// langword="true"/> if it did advance, <see langword="false"/> otherwise.
            /// </summary>
            public bool TryAdvance(char c)
            {
                if (PeekChar() != c)
                    return false;

                AdvanceChar();
                return true;
            }

            /// <summary>
            /// Grab the next character and advance the position.
            /// </summary>
            /// <returns>
            /// The next character, <see cref="InvalidCharacter" /> if there were no characters 
            /// remaining.
            /// </returns>
            public char NextChar()
            {
                char c = PeekChar();
                if (c != InvalidCharacter)
                {
                    this.AdvanceChar();
                }
                return c;
            }

            public bool TryNextChar(out char c)
            {
                c = PeekChar();

                if (c == InvalidCharacter)
                    return false;

                AdvanceChar();
                return true;
            }

            /// <summary>
            /// Gets the next character if there are any characters in the 
            /// SourceText.
            /// </summary>
            /// <returns>
            /// The next character if any are available. InvalidCharacter otherwise.
            /// </returns>
            public char PeekChar()
            {
                if (Position >= length)
                {
                    return InvalidCharacter;
                }

                return (char) buffer[Position];
            }

            public bool TryPeekChar(out char c)
            {
                if (Position >= length)
                {
                    c = default;
                    return false;
                }

                c = (char) buffer[Position];
                return true;
            }

            /// <summary>
            /// Gets the character at the given offset to the current position if
            /// the position is valid within the buffer.
            /// </summary>
            /// <returns>
            /// The next character if any are available. InvalidCharacter otherwise.
            /// </returns>
            public char PeekChar(int offset)
            {
                var off = Position + offset;

                if (off >= length)
                    return InvalidCharacter;

                return (char) buffer[off];
            }

            /// <summary>
            /// If the next characters in the window match the given string,
            /// then advance past those characters.  Otherwise, do nothing.
            /// </summary>
            internal bool AdvanceIfMatches(string desired)
            {
                int length = desired.Length;

                for (int i = 0; i < length; i++)
                {
                    if (PeekChar(i) != desired[i])
                    {
                        return false;
                    }
                }

                AdvanceChar(length);
                return true;
            }

            internal FixedUtf8String ReadAndAdvance(int length)
            {
                var str = new FixedUtf8String(buffer + Position, length);
                Position += length;
                return str;
            }

            public int FindChar(char c)
            {
                if (Position < length)
                {
                    return new Span<byte>(buffer + Position, length - Position).IndexOf((byte) c);
                }

                return -1;
            }

            public bool StartsWith(string desired)
            {
                int length = desired.Length;

                for (int i = 0; i < length; i++)
                {
                    if (PeekChar(i) != desired[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            public void AddPointer(FixedUtf8String value)
            {
                strings.Add(value);
            }

            public Span<FixedUtf8String> ExtractStrings()
            {
                var strings = this.strings;

                if (strings.Count == 0)
                    return default;

                var span = strings.Span;

                this.strings = default;

                return span;
            }

            public DemanglerNodeArena? ExtractArena()
            {
                var arena = this.arena;
                this.arena = null;
                return arena;
            }

            public void Dispose()
            {
                var strings = this.strings;

                if (strings.Count > 0)
                {
                    for (var i = 0; i < strings.Count; i++)
                        Marshal.FreeHGlobal((IntPtr) strings[i].Value);

                    strings.Dispose();
                    this.strings = default;
                }

                BackRefFunctionParams.Dispose();
                BackRefNames.Dispose();

                var a = this.arena;

                a?.Reset();
                Interlocked.CompareExchange(ref cachedArena, a, null);
            }

            #region Arena

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ArrayTypeNode AllocArrayType(NodeArrayNode dimensions)
            {
                var arrayTypeNode = arena?.ArrayType.Allocate() ?? new ArrayTypeNode();
                arrayTypeNode.Dimensions = dimensions;
                return arrayTypeNode;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ConversionOperatorIdentifierNode AllocConversionOperatorIdentifier() =>
                arena?.ConversionOperatorIdentifier.Allocate() ?? new ConversionOperatorIdentifierNode();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public CustomTypeNode AllocCustomType(IdentifierNode identifierNode)
            {
                var customTypeNode = arena?.CustomType.Allocate() ?? new CustomTypeNode();
                customTypeNode.Identifier = identifierNode;
                return customTypeNode;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public DynamicStructorIdentifierNode AllocDynamicStructorIdentifier(bool isDestructor)
            {
                var dynamicStructorIdentifierNode = arena?.DynamicStructorIdentifier.Allocate() ?? new DynamicStructorIdentifierNode();
                dynamicStructorIdentifierNode.IsDestructor = isDestructor;
                return dynamicStructorIdentifierNode;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public EncodedStringLiteralNode AllocEncodedStringLiteral() =>
                arena?.EncodedStringLiteral.Allocate() ?? new EncodedStringLiteralNode();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public FunctionSignatureNode AllocFunctionSignature() =>
                arena?.FunctionSignature.Allocate() ?? new FunctionSignatureNode();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public FunctionSignatureNode AllocFunctionSignature(
                Qualifiers qualifiers,
                Qualifiers extQualifiers,
                FunctionRefQualifier refQualifier,
                CallingConv callingConvention,
                TypeNode? returnType,
                NodeArrayNode parameterList,
                bool isVariadic,
                bool IsNoExcept)
            {
                var functionSignatureNode = arena?.FunctionSignature.Allocate() ?? new FunctionSignatureNode();
                functionSignatureNode.Qualifiers = qualifiers;
                functionSignatureNode.ExtQualifiers = extQualifiers;
                functionSignatureNode.RefQualifier = refQualifier;
                functionSignatureNode.CallingConvention = callingConvention;
                functionSignatureNode.ReturnType = returnType;
                functionSignatureNode.Parameters = parameterList;
                functionSignatureNode.IsVariadic = isVariadic;
                functionSignatureNode.IsNoExcept = IsNoExcept;
                return functionSignatureNode;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public FunctionSymbolNode AllocFunctionSymbol() =>
                arena?.FunctionSymbol.Allocate() ?? new FunctionSymbolNode();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public IntegerLiteralNode AllocIntegerLiteral(ulong value, bool isNegative)
            {
                var integerLiteralNode = arena?.IntegerLiteral.Allocate() ?? new IntegerLiteralNode();
                integerLiteralNode.Value = value;
                integerLiteralNode.IsNegative = isNegative;
                return integerLiteralNode;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public IntrinsicFunctionIdentifierNode AllocIntrinsicFunctionIdentifier(IntrinsicFunctionKind @operator)
            {
                var intrinsicFunctionIdentifier = arena?.IntrinsicFunctionIdentifier.Allocate() ?? new IntrinsicFunctionIdentifierNode();
                intrinsicFunctionIdentifier.Operator = @operator;
                return intrinsicFunctionIdentifier;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public LiteralOperatorIdentifierNode AllocLiteralOperatorIdentifier(FixedUtf8String name)
            {
                var literalOperatorIdentifierNode = arena?.LiteralOperatorIdentifier.Allocate() ?? new LiteralOperatorIdentifierNode();
                literalOperatorIdentifierNode.Name = name;
                return literalOperatorIdentifierNode;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public LocalStaticGuardIdentifierNode AllocLocalStaticGuardIdentifier(bool isThread)
            {
                var localStaticGuardIdentifierNode = arena?.LocalStaticGuardIdentifier.Allocate() ?? new LocalStaticGuardIdentifierNode();
                localStaticGuardIdentifierNode.IsThread = isThread;
                return localStaticGuardIdentifierNode;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public LocalStaticGuardVariableNode AllocLocalStaticGuardVariable(QualifiedNameNode qualifiedName)
            {
                var localStaticGuardVariable = arena?.LocalStaticGuardVariable.Allocate() ?? new LocalStaticGuardVariableNode();
                localStaticGuardVariable.Name = qualifiedName;
                return localStaticGuardVariable;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public NamedIdentifierNode AllocNamedIdentifier(ref Utf8StringBuilder builder)
            {
                var str = builder.ToPointer();
                AddPointer(str);

                return AllocNamedIdentifier(str);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public NamedIdentifierNode AllocNamedIdentifier(FixedUtf8String name)
            {
                var namedIdentifierNode = arena?.NamedIdentifier.Allocate() ?? new NamedIdentifierNode();
                namedIdentifierNode.Name = name;
                return namedIdentifierNode;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public PointerTypeNode AllocPointerType(
                Qualifiers qualifiers,
                PointerAffinity affinity,
                TypeNode pointee,
                //PointerAuthQualifiedNode? pointerAuthQualifier = null,
                QualifiedNameNode? classParent = null)
            {
                var pointerTypeNode = arena?.PointerType.Allocate() ?? new PointerTypeNode();
                pointerTypeNode.Qualifiers = qualifiers;
                pointerTypeNode.Affinity = affinity;
                pointerTypeNode.Pointee = pointee;
                //pointerTypeNode.PointerAuthQualifier = pointerAuthQualifier;
                pointerTypeNode.ClassParent = classParent;
                return pointerTypeNode;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public PrimitiveTypeNode AllocPrimitiveType(PrimitiveKind kind)
            {
                var primitiveTypeNode = arena?.PrimitiveType.Allocate() ?? new PrimitiveTypeNode();
                primitiveTypeNode.PrimitiveKind = kind;
                return primitiveTypeNode;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public QualifiedNameNode AllocQualifiedName(NodeArrayNode components)
            {
                var qualifiedNameNode = arena?.QualifiedName.Allocate() ?? new QualifiedNameNode();
                qualifiedNameNode.Components = components;
                return qualifiedNameNode;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public RttiBaseClassDescriptorNode AllocRttiBaseClassDescriptor() =>
                arena?.RttiBaseClassDescriptor.Allocate() ?? new RttiBaseClassDescriptorNode();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public SpecialTableSymbolNode AllocSpecialTableSymbol(QualifiedNameNode qualifiedName)
            {
                var specialTableSymbolNode = arena?.SpecialTableSymbol.Allocate() ?? new SpecialTableSymbolNode();
                specialTableSymbolNode.Name = qualifiedName;
                return specialTableSymbolNode;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public StructorIdentifierNode AllocStructorIdentifier(bool isDestructor)
            {
                var structorIdentifierNode = arena?.StructorIdentifier.Allocate() ?? new StructorIdentifierNode();
                structorIdentifierNode.IsDestructor = isDestructor;
                return structorIdentifierNode;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TagTypeNode AllocTagType(TagKind tag, QualifiedNameNode qualifiedName)
            {
                var tagTypeNode = arena?.TagType.Allocate() ?? new TagTypeNode();
                tagTypeNode.Tag = tag;
                tagTypeNode.QualifiedName = qualifiedName;
                return tagTypeNode;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TemplateParameterReferenceNode AllocTemplateParameterReference() =>
                arena?.TemplateParameterReference.Allocate() ?? new TemplateParameterReferenceNode();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ThunkSignatureNode AllocThunkSignature() =>
                arena?.ThunkSignature.Allocate() ?? new ThunkSignatureNode();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public VariableSymbolNode AllocVariableSymbol() =>
                arena?.VariableSymbol.Allocate() ?? new VariableSymbolNode();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public VariableSymbolNode AllocVariableSymbol(TypeNode type, StorageClass storageClass)
            {
                var variableSymbolNode = arena?.VariableSymbol.Allocate() ?? new VariableSymbolNode();
                variableSymbolNode.Type = type;
                variableSymbolNode.StorageClass = storageClass;
                return variableSymbolNode;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public VariableSymbolNode AllocVariableSymbol(QualifiedNameNode name, TypeNode type)
            {
                var variableSymbolNode = arena?.VariableSymbol.Allocate() ?? new VariableSymbolNode();
                variableSymbolNode.Name = name;
                variableSymbolNode.Type = type;
                return variableSymbolNode;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public VCallThunkIdentifierNode AllocVCallThunkIdentifier() =>
                arena?.VCallThunkIdentifier.Allocate() ?? new VCallThunkIdentifierNode();

            //Not in llvm-undname

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public AllocWinRTQualifiedBaseIdentifierNode AllocWinRTQualifiedBaseName(NodeArrayNode components)
            {
                var winRTQualifiedBaseNameNode = arena?.WinRTQualifiedBaseIdentifier.Allocate() ?? new AllocWinRTQualifiedBaseIdentifierNode();
                winRTQualifiedBaseNameNode.Components = components;
                return winRTQualifiedBaseNameNode;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public NodeArrayNode AllocNodeArray(ref PooledList<Node> list)
            {
                var nodeArrayNode = arena?.NodeArray.Allocate() ?? new NodeArrayNode();
                nodeArrayNode.count = list.Count;
                nodeArrayNode.rentedNodes = list.ExtractAndClear() ?? Array.Empty<Node>();
                return nodeArrayNode;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public NodeArrayNode AllocNodeArray(Node node)
            {
                var singletonArray = ArrayPool<Node>.Shared.Rent(1);
                singletonArray[0] = node;

                var nodeArrayNode = arena?.NodeArray.Allocate() ?? new NodeArrayNode();
                nodeArrayNode.count = 1;
                nodeArrayNode.rentedNodes = singletonArray;
                return nodeArrayNode;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ScopedIdentifierNode AllocScopedIdentifier(SymbolNode scope, ulong number)
            {
                var scopedIdentifierNode = arena?.ScopedIdentifier.Allocate() ?? new ScopedIdentifierNode();
                scopedIdentifierNode.Scope = scope;
                scopedIdentifierNode.Number = number;
                return scopedIdentifierNode;
            }

            #endregion

            public override string ToString()
            {
                return new FixedUtf8String(buffer + Position, length - Position).ToString();
            }
        }
    }

    
}
