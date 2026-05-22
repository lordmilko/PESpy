using System;
using System.Reflection.Emit;
using ClrDebug;

namespace PESpy.IL
{
    //I think ideally we should have a custom debugger display here that shows an internal IL Instruction
    //type that only shows the 1 appropriate operand member based on the opcode type

    public interface ITokenFormatter
    {
        void Format(ref ValueStringBuilder builder, mdToken token);
    }

    public readonly struct ILInstruction
    {
        public int Offset { get; }

        public ILOpcode Opcode { get; }

        private readonly NativeSpan<byte> _operand;

        public unsafe mdToken Token
        {
            get
            {
                var operandType = Opcode.GetOperandType();

                switch (operandType)
                {
                    case OperandType.InlineField:
                    case OperandType.InlineMethod:
                    case OperandType.InlineString:
                    case OperandType.InlineTok:
                    case OperandType.InlineType:
                        return *(mdToken*) (byte*) _operand;

                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        public unsafe int I4
        {
            get
            {
                var operandType = Opcode.GetOperandType();

                switch (operandType)
                {
                    case OperandType.InlineI:
                        return *(int*) (byte*) _operand;

                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        public unsafe byte U1
        {
            get
            {
                var operandType = Opcode.GetOperandType();

                switch (operandType)
                {
                    case OperandType.ShortInlineVar:
                        return *(byte*) _operand;

                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        public unsafe sbyte I1
        {
            get
            {
                var operandType = Opcode.GetOperandType();

                switch (operandType)
                {
                    case OperandType.ShortInlineI: //This could be signed
                        return *(sbyte*) (byte*) _operand;

                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        internal ILInstruction(int offset, ILOpcode opcode, NativeSpan<byte> operand)
        {
            Offset = offset;
            Opcode = opcode;
            _operand = operand;
        }

        public unsafe string ToString(ref ValueStringBuilder builder, ITokenFormatter? tokenFormatter = null)
        {
            builder.Append("IL_");
            builder.AppendHex((uint) Offset, 4);
            builder.Append("   ");

            //Special case the opcodes that end in an underscore due to conflicts with C# keywords
            builder.Append(
                Opcode switch
                {
                    ILOpcode.break_ => "break",
                    ILOpcode.switch_ => "switch",
                    ILOpcode.sizeof_ => "sizeof",
                    ILOpcode.throw_ => "throw",
                    ILOpcode.readonly_ => "readonly",
                    _ => Opcode.ToString()
                }
            );

            var operandType = Opcode.GetOperandType();

            switch (operandType)
            {
                case OperandType.InlineNone:
                    break;

                case OperandType.InlineField:
                case OperandType.InlineMethod:
                case OperandType.InlineString:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                case OperandType.InlineSig:
                    var token = *(mdToken*) (byte*) _operand;

                    builder.Append(' ');

                    if (tokenFormatter != null)
                        tokenFormatter.Format(ref builder, token);
                    else
                    {
                        builder.Append("0x");
                        builder.AppendHex(token.Value);
                    }

                    break;

                case OperandType.InlineI:
                    builder.Append(' ');
                    builder.Append(*(int*) (byte*) _operand);
                    break;

                case OperandType.InlineBrTarget:
                    var b4 = *(int*) (byte*) _operand;
                    var off4 = Offset + b4 + ((int) Opcode < 256 ? 5 : 6); //Skip over the size of the operand (4) + the size of the opcode (1 or 2)
                    builder.Append(" IL_");
                    builder.AppendHex((uint) off4, 4);
                    break;

                case OperandType.InlineI8:
                    builder.Append(' ');
                    builder.Append(*(long*) (byte*) _operand);
                    break;

                case OperandType.ShortInlineR:
                    builder.Append(' ');
                    builder.Append(*(float*) (byte*) _operand);
                    break;

                case OperandType.InlineR:
                    builder.Append(' ');
                    builder.Append(*(double*) (byte*) _operand);
                    break;

                case OperandType.InlineSwitch:
                    var numTargets = *(int*) (byte*) _operand;
                    var targets = new NativeSpan<int>(((byte*) _operand) + 4, numTargets);
                    var endOffset = Offset + (_operand.Length + ((int) Opcode < 256 ? 1 : 2));

                    builder.Append(' ');

                    for (var i = 0; i < targets.Length; i++)
                    {
                        builder.Append("IL_");
                        builder.AppendHex((uint) (targets[i] + endOffset), 4);

                        if (i < targets.Length - 1)
                            builder.Append(", ");
                    }

                    break;

                case OperandType.ShortInlineBrTarget:
                    var b1 = *(byte*) _operand;
                    var off1 = Offset + b1 + ((int) Opcode < 256 ? 2 : 3); //Skip over the size of the operand (1) + the size of the opcode (1 or 2)
                    builder.Append(" IL_");
                    builder.AppendHex((uint) off1, 4);
                    break;

                case OperandType.ShortInlineI:
                    builder.Append(' ');
                    builder.Append(*(sbyte*) (byte*) _operand);
                    break;

                //Whether a variable refers to a parameter or a local depends on its OpCode (ldloc vs ldarg, etc)
                case OperandType.InlineVar:
                    builder.Append(' ');
                    builder.Append(*(short*) (byte*) _operand);
                    break;

                case OperandType.ShortInlineVar:
                    builder.Append(' ');
                    builder.Append(*(byte*) _operand);
                    break;

                default:
                    throw new NotImplementedException();
            }

            return builder.ToString();
        }

        public unsafe override string ToString()
        {
            var builder = new ValueStringBuilder();

            try
            {
                ToString(ref builder);

                return builder.ToString();
            }
            finally
            {
                builder.Dispose();
            }
        }
    }
}
