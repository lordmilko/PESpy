using System;
using System.Reflection.Emit;
using ClrDebug;
using static PESpy.IL.ILOpcode;

namespace PESpy.IL
{
    /// <summary>
    /// Provides facilities for decoding IL without allocating.
    /// </summary>
    public struct ILDecoder
    {
        public static unsafe bool TryCreate(PEFile peFile, mdMethodDef methodDef, out ILDecoder decoder)
        {
            if (peFile.TryGetILMethod(methodDef, out var ilMethod))
            {
                var ilBytes = ilMethod.ILBytes;

                decoder = new ILDecoder(new ByteReader((byte*) ilBytes, ilBytes.Length));
                return true;
            }

            decoder = default;
            return false;
        }

        internal ByteReader _byteReader;

        public int RemainingBytes => _byteReader.RemainingBytes;

        public ILDecoder(ByteReader byteReader)
        {
            _byteReader = byteReader;
        }

        public void Seek(int offset)
        {
            _byteReader.Offset = offset;
        }

        public unsafe bool TryDecode(out ILInstruction instr)
        {
            if (_byteReader.RemainingBytes == 0)
            {
                instr = default;
                return false;
            }

            var offset = _byteReader.Offset;

            var opcode = ReadILOpcode();

            var operandType = opcode.GetOperandType();

            NativeSpan<byte> operand;

            switch (operandType)
            {
                case OperandType.InlineNone:
                    operand = default;
                    break;

                //Operand is a mdToken
                case OperandType.InlineField:
                case OperandType.InlineMethod:
                case OperandType.InlineString:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                case OperandType.InlineSig:

                //Operand is just an int
                case OperandType.InlineI:

                //Operand is a float
                case OperandType.ShortInlineR:

                //Position + 4 should be added to it
                case OperandType.InlineBrTarget:
                    operand = Read(4);
                    break;

                    //return reader.ReadInt32() + (int) reader.BaseStream.Position;
                    //throw new NotImplementedException();

                //Operand is a long
                case OperandType.InlineI8:

                //Operand is a double
                case OperandType.InlineR:
                    operand = Read(8);
                    break;

                case OperandType.InlineSwitch:
                    var numTargets = _byteReader.ReadInt32();

                    operand = new NativeSpan<byte>(_byteReader.CurrentPointer - sizeof(int), sizeof(int) + (numTargets * sizeof(int)));
                    _byteReader.Offset += (numTargets * sizeof(int));
                    break;

                //Byte
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:

                //Position + 1 should be added to it
                case OperandType.ShortInlineBrTarget:
                    operand = Read(1);
                    break;

                //Whether a variable refers to a parameter or a local depends on its OpCode (ldloc vs ldarg, etc)
                case OperandType.InlineVar:
                    operand = Read(2);
                    break;

                default:
                    throw new NotImplementedException();
            }

            instr = new ILInstruction(offset, opcode, operand);
            return true;
        }

        public ILInstruction Decode()
        {
            if (!TryDecode(out var instr))
                throw new NotImplementedException();

            return instr;
        }

        private unsafe NativeSpan<byte> Read(int count)
        {
            var ptr = _byteReader.CurrentPointer;
            _byteReader.Offset += count;

            return new NativeSpan<byte>(ptr, count);
        }

        private ILOpcode ReadILOpcode()
        {
            var opcode = (ILOpcode) _byteReader.ReadByte();

            //Currently, all multi-byte opcodes have Prefix1 in their high byte
            if (opcode == ILOpcode.prefix1)
                opcode = (ILOpcode) (0x100 + _byteReader.ReadByte());

            return opcode;
        }

        public mdToken ReadToken()
        {
            var instr = Decode();

            return instr.Token;
        }

        public int ReadI4()
        {
            var instr = Decode();

            return instr.Opcode switch
            {
                ldc_i4 => instr.I4,
                >= ldc_i4_0 and <= ldc_i4_8 => instr.Opcode - ldc_i4_0,
                ldc_i4_m1 => -1,
                ldc_i4_s => instr.I1,
                _ => throw new InvalidOperationException()
            };
        }
    }
}
