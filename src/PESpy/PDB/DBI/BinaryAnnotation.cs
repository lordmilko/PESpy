using System.Diagnostics;
using ClrDebug.PDB;
using static ClrDebug.PDB.PdbExtensions;

namespace PESpy.PDB
{
    //Type is made up
    [DebuggerDisplay("{DebuggerDisplay()}")]
    public readonly struct BinaryAnnotation
    {
        private string DebuggerDisplay()
        {
            if (OperandCount == 1)
                return $"{Instruction} (Operand: {Operand1})";

            return $"{Instruction} (Operand1: {Operand1}, Operand2: {Operand2})";
        }

        internal static unsafe bool TryDecode(ref byte* bytes, out BinaryAnnotation binaryAnnotation)
        {
            var instr = (BinaryAnnotationOpcode) CVUncompressData(ref bytes);

            if (instr == BinaryAnnotationOpcode.BA_OP_Invalid)
            {
                binaryAnnotation = default;
                return false;
            }

            var operandCount = BinaryAnnotationInstructionOperandCount(instr);

            if (operandCount == 1)
            {
                var operand1 = CVUncompressData(ref bytes);

                if (instr == BinaryAnnotationOpcode.BA_OP_ChangeLineOffset || instr == BinaryAnnotationOpcode.BA_OP_ChangeColumnEndDelta)
                {
                    operand1 = DecodeSignedInt32(operand1);
                }

                binaryAnnotation = new BinaryAnnotation(instr, operand1);
                return true;
            }
            else
            {
                //The only operand type that uses 2 is BA_OP_ChangeCodeLengthAndCodeOffset

                Debug.Assert(operandCount == 2);

                var operand1 = CVUncompressData(ref bytes);
                var operand2 = CVUncompressData(ref bytes);

                binaryAnnotation = new BinaryAnnotation(instr, operand1, operand2);
                return true;
            }
        }

        public BinaryAnnotationOpcode Instruction { get; }

        public int OperandCount => BinaryAnnotationInstructionOperandCount(Instruction);

        public int Operand1 { get; }

        public int Operand2 { get; }

        internal BinaryAnnotation(BinaryAnnotationOpcode opcode, int operand1, int operand2 = default)
        {
            Instruction = opcode;
            Operand1 = operand1;
            Operand2 = operand2;
        }
    }
}
