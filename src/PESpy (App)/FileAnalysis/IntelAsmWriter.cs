using Iced.Intel;
using StringBuilder = System.Text.StringBuilder;

namespace PESpy
{
    internal unsafe class ByteCodeReader : CodeReader
    {
        internal byte* _pBytes;

        public override int ReadByte() => *(_pBytes++);
    }

    public enum ViewByteFormatKind
    {
        Address,
        Code,
        Symbol,
        Number,
        String,
        Byte,
        Line
    }

    internal class IntelAsmWriter : FormatterOutput
    {
        private readonly Decoder _decoder;
        private readonly ByteCodeReader _reader;
        private readonly StringBuilder _builder;
        private readonly Formatter _formatter;
        private int _currentCharOffset;
        private ViewByteFormatRangeList _formatRanges;

        internal IntelAsmWriter(int bitness, ISymbolResolver? symbolResolver, ViewByteFormatRangeList formatRanges)
        {
            _reader = new ByteCodeReader();
            _decoder = Decoder.Create(bitness, _reader);
            _builder = new StringBuilder();
            _formatter = new MasmFormatter(symbolResolver);
            _formatter.Options.SpaceAfterOperandSeparator = true;
            _formatRanges = formatRanges;
        }

        internal unsafe int Write(byte* pBytes, int rva, ref ValueStringBuilder.NonRef builder)
        {
            _reader._pBytes = pBytes;
            _decoder.IP = (ulong) rva;

            var instr = _decoder.Decode();

            _currentCharOffset = builder.Length;
            _formatter.Format(instr, this);
            builder.Append(_builder);
            _builder.Clear();

            return instr.Length;
        }

        public override void Write(string text, FormatterTextKind kind)
        {
            var startPos = _currentCharOffset + _builder.Length;

            switch (kind)
            {
                case FormatterTextKind.Mnemonic:
                    //Options.TabSize does not do what we want

                    //We want to allocate 7 chars for displaying the name (and then have a 1 space gap between the mnemonic and the first operand).
                    //Iced will automatically add the 1 space gap, so we need to pad to 7 as required;
                    _builder.Append(text);

                    for (var i = text.Length; i < 7; i++)
                        _builder.Append(' ');

                    _formatRanges.Add(ViewByteFormatKind.Code, startPos, _currentCharOffset + _builder.Length);
                    break;

                case FormatterTextKind.Register:
                case FormatterTextKind.Operator:
                case FormatterTextKind.Punctuation:
                case FormatterTextKind.Keyword:
                case FormatterTextKind.LabelAddress:
                case FormatterTextKind.FunctionAddress:
                case FormatterTextKind.Prefix:
                case FormatterTextKind.SelectorValue:
                    _builder.Append(text);
                    _formatRanges.Add(ViewByteFormatKind.Code, startPos, _currentCharOffset + _builder.Length);
                    break;

                case FormatterTextKind.Number:
                    _builder.Append(text);
                    _formatRanges.Add(ViewByteFormatKind.Number, startPos, _currentCharOffset + _builder.Length);
                    break;

                case FormatterTextKind.Label:
                    _builder.Append(text);
                    _formatRanges.Add(ViewByteFormatKind.Symbol, startPos, _currentCharOffset + _builder.Length);
                    break;

                case FormatterTextKind.Text:
                    _builder.Append(text);
                    _formatRanges.AddToPrevious(_currentCharOffset + _builder.Length);
                    break;

                default:
                    throw new System.NotImplementedException();
            }
        }
    }
}
