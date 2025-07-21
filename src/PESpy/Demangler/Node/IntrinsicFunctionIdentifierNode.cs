using ClrDebug.DIA;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        public class IntrinsicFunctionIdentifierNode : IdentifierNode
        {
            public IntrinsicFunctionKind Operator { get; internal set; }

            public IntrinsicFunctionIdentifierNode() : base(NodeKind.IntrinsicFunctionIdentifier)
            {
            }

            public override void Output(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                var value = Operator switch
                {
                    IntrinsicFunctionKind.None                       => null,
                    IntrinsicFunctionKind.New                        => "operator new",
                    IntrinsicFunctionKind.Delete                     => "operator delete",
                    IntrinsicFunctionKind.Assign                     => "operator=",
                    IntrinsicFunctionKind.RightShift                 => "operator>>",
                    IntrinsicFunctionKind.LeftShift                  => "operator<<",
                    IntrinsicFunctionKind.LogicalNot                 => "operator!",
                    IntrinsicFunctionKind.Equals                     => "operator==",
                    IntrinsicFunctionKind.NotEquals                  => "operator!=",
                    IntrinsicFunctionKind.ArraySubscript             => "operator[]",
                    IntrinsicFunctionKind.Pointer                    => "operator->",
                    IntrinsicFunctionKind.Increment                  => "operator++",
                    IntrinsicFunctionKind.Decrement                  => "operator--",
                    IntrinsicFunctionKind.Minus                      => "operator-",
                    IntrinsicFunctionKind.Plus                       => "operator+",
                    IntrinsicFunctionKind.Dereference                => "operator*",
                    IntrinsicFunctionKind.BitwiseAnd                 => "operator&",
                    IntrinsicFunctionKind.MemberPointer              => "operator->*",
                    IntrinsicFunctionKind.Divide                     => "operator/",
                    IntrinsicFunctionKind.Modulus                    => "operator%",
                    IntrinsicFunctionKind.LessThan                   => "operator<",
                    IntrinsicFunctionKind.LessThanEqual              => "operator<=",
                    IntrinsicFunctionKind.GreaterThan                => "operator>",
                    IntrinsicFunctionKind.GreaterThanEqual           => "operator>=",
                    IntrinsicFunctionKind.Comma                      => "operator,",
                    IntrinsicFunctionKind.Parens                     => "operator()",
                    IntrinsicFunctionKind.BitwiseNot                 => "operator~",
                    IntrinsicFunctionKind.BitwiseXor                 => "operator^",
                    IntrinsicFunctionKind.BitwiseOr                  => "operator|",
                    IntrinsicFunctionKind.LogicalAnd                 => "operator&&",
                    IntrinsicFunctionKind.LogicalOr                  => "operator||",
                    IntrinsicFunctionKind.TimesEqual                 => "operator*=",
                    IntrinsicFunctionKind.PlusEqual                  => "operator+=",
                    IntrinsicFunctionKind.MinusEqual                 => "operator-=",
                    IntrinsicFunctionKind.DivEqual                   => "operator/=",
                    IntrinsicFunctionKind.ModEqual                   => "operator%=",
                    IntrinsicFunctionKind.RshEqual                   => "operator>>=",
                    IntrinsicFunctionKind.LshEqual                   => "operator<<=",
                    IntrinsicFunctionKind.BitwiseAndEqual            => "operator&=",
                    IntrinsicFunctionKind.BitwiseOrEqual             => "operator|=",
                    IntrinsicFunctionKind.BitwiseXorEqual            => "operator^=",
                    IntrinsicFunctionKind.VbaseDtor                  => "`vbase destructor'",
                    IntrinsicFunctionKind.VecDelDtor                 => "`vector deleting destructor'",
                    IntrinsicFunctionKind.DefaultCtorClosure         => "`default constructor closure'",
                    IntrinsicFunctionKind.ScalarDelDtor              => "`scalar deleting destructor'",
                    IntrinsicFunctionKind.VecCtorIter                => "`vector constructor iterator'",
                    IntrinsicFunctionKind.VecDtorIter                => "`vector destructor iterator'",
                    IntrinsicFunctionKind.VecVbaseCtorIter           => "`vector vbase constructor iterator'",
                    IntrinsicFunctionKind.VdispMap                   => "`virtual displacement map'",
                    IntrinsicFunctionKind.EHVecCtorIter              => "`eh vector constructor iterator'",
                    IntrinsicFunctionKind.EHVecDtorIter              => "`eh vector destructor iterator'",
                    IntrinsicFunctionKind.EHVecVbaseCtorIter         => "`eh vector vbase constructor iterator'",
                    IntrinsicFunctionKind.CopyCtorClosure            => "`copy constructor closure'",
                    IntrinsicFunctionKind.LocalVftableCtorClosure    => "`local vftable constructor closure'",
                    IntrinsicFunctionKind.ArrayNew                   => "operator new[]",
                    IntrinsicFunctionKind.ArrayDelete                => "operator delete[]",
                    IntrinsicFunctionKind.ManVectorCtorIter          => "`managed vector constructor iterator'",
                    IntrinsicFunctionKind.ManVectorDtorIter          => "`managed vector destructor iterator'",
                    IntrinsicFunctionKind.EHVectorCopyCtorIter       => "`eh vector copy constructor iterator'",
                    IntrinsicFunctionKind.EHVectorVbaseCopyCtorIter  => "`eh vector vbase copy constructor iterator'",
                    IntrinsicFunctionKind.VectorCopyCtorIter         => "`vector copy constructor iterator'",
                    IntrinsicFunctionKind.VectorVbaseCopyCtorIter    => "`vector vbase copy constructor iterator'",
                    IntrinsicFunctionKind.ManVectorVbaseCopyCtorIter => "`managed vector vbase copy constructor iterator'",
                    IntrinsicFunctionKind.CoAwait                    => "operator co_await",
                    IntrinsicFunctionKind.Spaceship                  => "operator<=>",
                };

                if (value != null)
                    builder.Append(value);

                OutputTemplateParameters(ref builder, flags);
            }

            public override void Reset()
            {
                base.Reset();

                Operator = default;
            }
        }
    }
}
