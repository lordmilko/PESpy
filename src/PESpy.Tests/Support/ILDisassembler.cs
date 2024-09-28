using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

#nullable enable

namespace PESpy.Tests
{
    /// <summary>
    /// Represents an engine capable of disassembling Common Intermediate Language instructions.
    /// </summary>
    public class ILDisassembler
    {
        private ILDecoder Decoder { get; }

        private readonly Stream stream;

        public static ILInstruction[] Disassemble(MethodBase method) =>
            Disassemble(method.GetMethodBody().GetILAsByteArray());

        public static ILInstruction[] Disassemble(byte[] ilBytes)
        {
            var disassembler = Create(ilBytes);

            return disassembler.EnumerateInstructions().ToArray();
        }

        public static ILDisassembler Create(byte[] ilBytes)
        {
            if (ilBytes == null)
                throw new ArgumentNullException(nameof(ilBytes));

            var stream = new MemoryStream(ilBytes);

            return new ILDisassembler(stream);
        }

        private ILDisassembler(Stream stream)
        {
            this.stream = stream;

            Decoder = new ILDecoder(stream);
        }

        public IEnumerable<ILInstruction> EnumerateInstructions()
        {
            //Always begin from the beginning of the stream
            stream.Position = 0;

            var results = new List<ILInstruction>();

            while (true)
            {
                if (stream.Position >= stream.Length)
                    break;

                var instr = Decoder.Decode();
                results.Add(instr);
            }

            var totalOffset = 0;

            var offsetMap = new Dictionary<int, ILInstruction>();

            foreach (var instr in results)
            {
                offsetMap.Add(totalOffset, instr);

                totalOffset += instr.Length;
            }

            ILInstruction GetBranchTarget(int offset)
            {
                if (offsetMap.TryGetValue(offset, out var instr))
                    return instr;

                throw new NotImplementedException($"Could not find the instruction pointed to by offset {offset}");
            }

            foreach (var instr in results)
            {
                switch (instr.OpCode.OperandType)
                {
                    case OperandType.InlineBrTarget:
                    case OperandType.ShortInlineBrTarget:
                        instr.Operand = GetBranchTarget((int) instr.Operand);
                        break;

                    case OperandType.InlineSwitch:
                    {
                        var existing = (int[]) instr.Operand;

                        var targets = new ILInstruction[existing.Length];

                        for (var i = 0; i < existing.Length; i++)
                            targets[i] = GetBranchTarget(existing[i]);

                        instr.Operand = targets;
                        break;
                    }
                }
            }

            return results;
        }
    }
}
