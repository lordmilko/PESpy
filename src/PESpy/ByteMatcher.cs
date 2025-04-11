using System.Collections.Generic;
using System.IO;
using System.Linq;
#if !DEBUG_POSITION
using RVA = System.Int32;
#endif

namespace PESpy
{
    enum ByteMatchKind
    {
        __GSHandlerCheck = 1,
        __GSHandlerCheck_SEH,
        __GSHandlerCheck_EH4,
        __C_specific_handler,
        __C_specific_handler_noexcept,
        __CxxFrameHandler,
        __CxxFrameHandler3,
        __CxxFrameHandler4,

        Jmp
    }

    internal class ByteMatcher
    {
        private static Dictionary<ByteSequence, ByteMatchKind> sequences;
        private static ByteSequenceTreeNode tree;

        static ByteMatcher()
        {
            //These patterns are not guaranteed to match; I have seen variations on some of these patterns which are not covered here

            sequences = new Dictionary<ByteSequence, ByteMatchKind>
            {
                { new ByteSequence("4883EC284D8B4138488BCA498BD1E8........B8010000004883C428C3......", true), ByteMatchKind.__GSHandlerCheck },
                { new ByteSequence("488BC44889580848896810488970184889782041564883EC204D8B5138488BF2", true), ByteMatchKind.__GSHandlerCheck_SEH },
                { new ByteSequence("40534883EC20488BD9FF15........F64304667514813B63736DE0750C83F8017507FF15........CC4883C4205BC3", true), ByteMatchKind.__C_specific_handler_noexcept }, //Sometime's its embedded, sometimes it's an import
                { new ByteSequence("488BC44889580848896810488970184889782041564883EC20498B5938488BF24D8BF0488BE9498BD1488BCE498BF94C8D4304E8........8B45042466F6D8B801000000451BC041F7D84403C04485430474114C8BCF4D8BC6488BD6488BCDE8........488B5C2430488B6C2438488B742440488B7C24484883C420415EC3", true), ByteMatchKind.__GSHandlerCheck_EH4 },
                { new ByteSequence("48895C240848896C241048897424185741544155415641574883EC40488BE94D8BF9498BC8498BF04C8BEAE8........4D8B67084D8B37498B5F384D2BF4F6450466418B7F480F85DC00000048896C24304889742438", true), ByteMatchKind.__C_specific_handler },
                { new ByteSequence("488BC4488958104889681848897020574883EC40498B5908498BF9498BF048895008488BE9E8........48895860488B5D38E8........48895868E8........488B4F384C8BCF4C8BC68B11488BCD4803506033C0884424384889442430894424284889542420488D542450E8........488B5C2458488B6C2460488B7424684883C4405FC3", true), ByteMatchKind.__CxxFrameHandler3 },
                { new ByteSequence("488BC4488958104889681848897020574883EC608360DC00498BF98360E000498BF08360E400488BE98360E8008360EC00498B5908C640D80048895008E8........48895860488B5D38E8........48895868E8........488B4F38488D5424404C8B4708C6442420008B0948034860488B4710448B08E8........C644243800488D442440488364243000488D54247083642428004C8BCF4C8BC64889442420488BCDE8........4C8D5C2460498B5B18498B6B20498B7328498BE35FC3", true), ByteMatchKind.__CxxFrameHandler4 },
                { new ByteSequence("FF25........", true), ByteMatchKind.Jmp }
            };

            tree = ByteSequenceTreeNode.BuildTree(sequences.Keys.ToArray());
        }

        internal static bool TryMatch(IFileReader reader, PEFile peFile, RVA virtualOffset, ExceptionHandlerContext context, out ByteMatchKind kind)
        {
            if (context.TryGetKind(virtualOffset, out var rawKind))
            {
                if (rawKind == null)
                {
                    kind = default;
                    return false;
                }

                kind = rawKind.Value;
                return true;
            }

            if (peFile.TryGetOffset(virtualOffset, out var offset))
            {
                var stream = reader.GetStreamStartUnsafe();
                stream.Seek((int) offset, System.IO.SeekOrigin.Current);

                var match = tree.GetMatches(stream, true).FirstOrDefault();

                if (match.Sequence != null)
                {
                    kind = sequences[match.Sequence];

                    switch (kind)
                    {
                        case ByteMatchKind.Jmp:
                            //__C_specific_handler contains a jmp to an import that contains the actual implementation
                            reader.Seek(offset + 2); //Skip over FF 25 (jump indirect)
                            var relativeAddress = reader.ReadInt32();
                            var ripRelativeAddress = virtualOffset + relativeAddress + 6; //The instruction is 6 bytes long

                            //Cache discovered addresses for fast lookup
                            if (context.TryGetImport(ripRelativeAddress, out kind))
                                return true;

                            break;

                        default:
                            context.AddMatch(virtualOffset, kind);

                            return true;
                    }
                }
            }

            if (context.TryGetSymbol(virtualOffset, out kind))
                return true;

            context.AddMatch(virtualOffset, null);

            kind = default;
            return false;
        }
    }
}
