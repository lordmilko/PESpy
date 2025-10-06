using System;
using System.Diagnostics;
using static PESpy.Demangler;

namespace PESpy
{
    class DemanglerNodeArena
    {
        //These fields cannot be readonly, it seems this prevents us from mutating them
        internal NodeArena<ArrayTypeNode> ArrayType;
        internal NodeArena<ConversionOperatorIdentifierNode> ConversionOperatorIdentifier;
        internal NodeArena<CustomTypeNode> CustomType;
        internal NodeArena<DynamicStructorIdentifierNode> DynamicStructorIdentifier;
        internal NodeArena<EncodedStringLiteralNode> EncodedStringLiteral;
        internal NodeArena<FunctionSignatureNode> FunctionSignature;
        internal NodeArena<FunctionSymbolNode> FunctionSymbol;
        internal NodeArena<IntegerLiteralNode> IntegerLiteral;
        internal NodeArena<IntrinsicFunctionIdentifierNode> IntrinsicFunctionIdentifier;
        internal NodeArena<LiteralOperatorIdentifierNode> LiteralOperatorIdentifier;
        internal NodeArena<LocalStaticGuardIdentifierNode> LocalStaticGuardIdentifier;
        internal NodeArena<LocalStaticGuardVariableNode> LocalStaticGuardVariable;
        internal NodeArena<NamedIdentifierNode> NamedIdentifier;
        internal NodeArena<PointerTypeNode> PointerType;
        internal NodeArena<PrimitiveTypeNode> PrimitiveType;
        internal NodeArena<QualifiedNameNode> QualifiedName;
        internal NodeArena<RttiBaseClassDescriptorNode> RttiBaseClassDescriptor;
        internal NodeArena<SpecialTableSymbolNode> SpecialTableSymbol;
        internal NodeArena<StructorIdentifierNode> StructorIdentifier;
        internal NodeArena<TagTypeNode> TagType;
        internal NodeArena<TemplateParameterReferenceNode> TemplateParameterReference;
        internal NodeArena<ThunkSignatureNode> ThunkSignature;
        internal NodeArena<VariableSymbolNode> VariableSymbol;
        internal NodeArena<VCallThunkIdentifierNode> VCallThunkIdentifier;
        internal NodeArena<AllocWinRTQualifiedBaseIdentifierNode> WinRTQualifiedBaseIdentifier;
        internal NodeArena<NodeArrayNode> NodeArray;
        internal NodeArena<ScopedIdentifierNode> ScopedIdentifier;

        public DemanglerNodeArena()
        {
            ArrayType                    = new NodeArena<ArrayTypeNode>                        (6, () => new ArrayTypeNode());
            ConversionOperatorIdentifier = new NodeArena<ConversionOperatorIdentifierNode>     (1, () => new ConversionOperatorIdentifierNode());
            CustomType                   = new NodeArena<CustomTypeNode>                       (1, () => new CustomTypeNode());
            DynamicStructorIdentifier    = new NodeArena<DynamicStructorIdentifierNode>        (1, () => new DynamicStructorIdentifierNode());
            EncodedStringLiteral         = new NodeArena<EncodedStringLiteralNode>             (1, () => new EncodedStringLiteralNode());
            FunctionSignature            = new NodeArena<FunctionSignatureNode>                (6, () => new FunctionSignatureNode());
            FunctionSymbol               = new NodeArena<FunctionSymbolNode>                   (4, () => new FunctionSymbolNode());
            IntegerLiteral               = new NodeArena<IntegerLiteralNode>                   (27, () => new IntegerLiteralNode());
            IntrinsicFunctionIdentifier  = new NodeArena<IntrinsicFunctionIdentifierNode>      (1, () => new IntrinsicFunctionIdentifierNode());
            LiteralOperatorIdentifier    = new NodeArena<LiteralOperatorIdentifierNode>        (1, () => new LiteralOperatorIdentifierNode());
            LocalStaticGuardIdentifier   = new NodeArena<LocalStaticGuardIdentifierNode>       (1, () => new LocalStaticGuardIdentifierNode());
            LocalStaticGuardVariable     = new NodeArena<LocalStaticGuardVariableNode>         (1, () => new LocalStaticGuardVariableNode());
            NamedIdentifier              = new NodeArena<NamedIdentifierNode>                  (417, () => new NamedIdentifierNode());
            PointerType                  = new NodeArena<PointerTypeNode>                      (38, () => new PointerTypeNode());
            PrimitiveType                = new NodeArena<PrimitiveTypeNode>                    (38, () => new PrimitiveTypeNode());
            QualifiedName                = new NodeArena<QualifiedNameNode>                    (64, () => new QualifiedNameNode());
            RttiBaseClassDescriptor      = new NodeArena<RttiBaseClassDescriptorNode>          (1, () => new RttiBaseClassDescriptorNode());
            SpecialTableSymbol           = new NodeArena<SpecialTableSymbolNode>               (1, () => new SpecialTableSymbolNode());
            StructorIdentifier           = new NodeArena<StructorIdentifierNode>               (2, () => new StructorIdentifierNode());
            TagType                      = new NodeArena<TagTypeNode>                          (61, () => new TagTypeNode());
            TemplateParameterReference   = new NodeArena<TemplateParameterReferenceNode>       (6, () => new TemplateParameterReferenceNode());
            ThunkSignature               = new NodeArena<ThunkSignatureNode>                   (1, () => new ThunkSignatureNode());
            VariableSymbol               = new NodeArena<VariableSymbolNode>                   (6, () => new VariableSymbolNode());
            VCallThunkIdentifier         = new NodeArena<VCallThunkIdentifierNode>             (1, () => new VCallThunkIdentifierNode());
            WinRTQualifiedBaseIdentifier = new NodeArena<AllocWinRTQualifiedBaseIdentifierNode>(1, () => new AllocWinRTQualifiedBaseIdentifierNode());
            NodeArray                    = new NodeArena<NodeArrayNode>                        (110, () => new NodeArrayNode());
            ScopedIdentifier             = new NodeArena<ScopedIdentifierNode>                 (1, () => new ScopedIdentifierNode());
        }

        public void Reset()
        {
            ArrayType.Reset();
            ConversionOperatorIdentifier.Reset();
            CustomType.Reset();
            DynamicStructorIdentifier.Reset();
            EncodedStringLiteral.Reset();
            FunctionSignature.Reset();
            FunctionSymbol.Reset();
            IntegerLiteral.Reset();
            IntrinsicFunctionIdentifier.Reset();
            LiteralOperatorIdentifier.Reset();
            LocalStaticGuardIdentifier.Reset();
            LocalStaticGuardVariable.Reset();
            NamedIdentifier.Reset();
            PointerType.Reset();
            PrimitiveType.Reset();
            QualifiedName.Reset();
            RttiBaseClassDescriptor.Reset();
            SpecialTableSymbol.Reset();
            StructorIdentifier.Reset();
            TagType.Reset();
            TemplateParameterReference.Reset();
            ThunkSignature.Reset();
            VariableSymbol.Reset();
            VCallThunkIdentifier.Reset();
            WinRTQualifiedBaseIdentifier.Reset();
            NodeArray.Reset();
            ScopedIdentifier.Reset();
        }

        [DebuggerDisplay("{DebuggerDisplay(),nq}")]
        internal struct NodeArena<T> where T : Node
        {
            private string DebuggerDisplay()
            {
#if MONITOR_NODE_CACHE
                var max = (int) System.Linq.Enumerable.Max(allMisses);

                var average = (int) System.Linq.Enumerable.Average(allMisses);

                return $"{_position} (Max Miss: {max}, Avg Miss: {average}, Samples: {allMisses.Count})";
#else
                return _position.ToString();
#endif
            }

            private T[]? _items;
            private readonly int _maxCache;
            private int _position;
            private Func<T> alloc;

#if MONITOR_NODE_CACHE
            private int misses;

            private static System.Collections.Generic.List<int> allMisses;
#endif

            public NodeArena(int maxCache, Func<T> alloc)
            {
                _maxCache = maxCache;
                _items = default;
                _position = default;
                this.alloc = alloc;
#if MONITOR_NODE_CACHE
                misses = default;
#endif
            }

            public T Allocate()
            {
                var items = _items;

                if (items == null)
                {
                    Debug.Assert(_maxCache != 0);
                    items = new T[_maxCache];
                    _items = items;
                }

                if (_position < items.Length)
                {
                    ref var item = ref items[_position++];

                    //This is a bit of a gotcha, but the new() constraint actually results in a call being made to Activator.CreateInstance.
                    //Using Reflection Emit to generate concrete types is also unacceptable. So it seems like we're stuck with passing in a delegate
                    //instead
                    if (item == null)
                        item = alloc();

                    return item;
                }
                else
                {
#if MONITOR_NODE_CACHE
                    misses++;
#endif
                    //Miss: allocate a new T
                    return alloc();
                }
            }

            public void Reset()
            {
                var items = _items;

                if (items == null)
                    return;

                var position = _position;

                for (var i = 0; i < position; i++)
                    items[i].Reset();

                _position = 0;

#if MONITOR_NODE_CACHE
                if (allMisses == null)
                    allMisses = new System.Collections.Generic.List<int>();

                if (misses > 0)
                    allMisses.Add(misses);

                if (allMisses.Count > 10)
                {
                    var x = 0;
                }

                misses = 0;
#endif
            }
        }
    }
}
