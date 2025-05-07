using System;
using System.Collections.Generic;
using Iced.Intel;
using PESpy.View;

namespace PESpy.Tests
{
    class IntelViewDisassembler : IViewDisassembler
    {
        public bool TryParseBytes(ref int offset, int rva, ref Span<byte> bytes, List<IView> results)
        {
            throw new NotImplementedException();
        }

        public bool TryParseDosStub(ref int offset, ref Span<byte> bytes, List<IView> results)
        {
            var decoder = Decoder.Create(16, bytes.ToArray());

            ushort ip = 0;

            var valid = false;

            var instrs = new List<Instruction>();

            int length = 0;
            var interruptsFound = 0;

            while (true)
            {
                var instr = decoder.Decode();

                if (instr.Code == Code.INVALID || (instr.IP16 == ip && ip != 0))
                {
                    break;
                }

                length += instr.Length;
                instrs.Add(instr);

                //The first int 21 prints the string, the second exits back to DOS
                if (instr.Code == Code.Int_imm8 && instr.Immediate8 == 0x21)
                {
                    interruptsFound++;

                    if (interruptsFound == 2)
                    {
                        //This should be the last instruction
                        valid = true;
                        break;
                    }
                }

                ip = instr.IP16;
            }

            if (valid)
            {
                results.Add(new AsmView<Instruction>(offset, "DosStub", length, 16, instrs.ToArray(), ViewKind.DosStub));

                var newArr = new byte[bytes.Length - length];
                bytes.Slice(length, bytes.Length).CopyTo(newArr);

                offset += length;
                bytes = newArr;

                return true;
            }

            return false;
        }
    }
}
